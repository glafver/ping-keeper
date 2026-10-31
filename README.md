# Ping Keeper

Self-hosted uptime monitor. It keeps your sleeping free-tier sites awake and shows their status on a dashboard.

## Why

Free tiers fall asleep when idle: Supabase pauses a database after ~7 days without traffic, MongoDB Atlas M0 after ~30 days, and similar hosts do the same. Pinging the frontend alone is **not enough** — the database sleeps separately.

Ping Keeper solves the "who pings the pinger" problem: an always-on **Cloud Scheduler** job calls `GET /ping`, which probes each site at up to three levels and records the result.

## Features

- **Three-level checks** per site: **frontend**, **backend** (API), and **database** (a health URL that runs a real DB query, e.g. `SELECT 1`).
- **Per-site interval** (default once a day) and **per-level toggles** — turn off the frontend if you only care about the DB.
- **Dashboard**: up/down status, response time, uptime, and per-site history.
- **Response-time chart** + stats (avg / min / max / p95).
- **Light/dark theme** (follows the OS, with a manual toggle) and a responsive layout.
- **Public read-only** dashboard & history; adding/editing sites behind a single-user login.
- **Security**: CSRF protection, login rate limiting, liveness probe.
- Deploy-ready: Docker, GitHub Actions CI/CD.

## Architecture

| Component | Role |
| --- | --- |
| **Cloud Scheduler** | Always-on cron that calls `GET /ping` on a schedule. |
| **Cloud Run** | ASP.NET Core 8 app: `/ping` (probe + record) and `/` (dashboard). |
| **Firestore** | NoSQL storage: `sites` and `pings` collections. |

## Tech stack

- C# / .NET 8 — ASP.NET Core MVC + Razor
- Google Cloud Firestore (`Google.Cloud.Firestore`)
- Docker, Cloud Run, Cloud Scheduler
- No EF Core (Firestore is NoSQL)

## Data model

**`sites`** — one document per monitored site:

| Field | Type | Description |
| --- | --- | --- |
| `Name` | string | Display name |
| `Url` | string | Frontend URL |
| `ApiUrl` | string | Backend API URL (optional) |
| `HealthUrl` | string | DB-touching health URL |
| `Enabled` | bool | Skip in `/ping` when `false` |
| `IntervalMinutes` | int | How often to check (default `1440` = daily) |
| `CheckFrontend` / `CheckBackend` / `CheckDatabase` | bool | Per-level toggles |
| `CreatedAt` | timestamp | — |

**`pings`** — one document per probe result:

| Field | Type | Description |
| --- | --- | --- |
| `SiteId` | string | Reference to `sites` |
| `Type` | string | `frontend` / `backend` / `database` |
| `StatusCode` | int | HTTP code (`0` on failure) |
| `Success` | bool | `2xx` |
| `ResponseMs` | long | Round-trip time in ms (`-1` on failure) |
| `Error` | string | Error message |
| `CheckedAt` | timestamp | — |

## Run locally

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

Demo mode (in-memory, no Google Cloud) is enabled in Development:

```bash
dotnet run
# dashboard:   http://localhost:8080/   (login: admin / demo)
# one-off run: http://localhost:8080/ping
```

To monitor your real sites locally, list them under `Demo:Sites` in `appsettings.Development.json`.

## Deploy to Google Cloud

```bash
export PROJECT_ID="my-project-id"
export REGION="europe-west1"

# 1. APIs + Firestore database
gcloud services enable run.googleapis.com firestore.googleapis.com cloudscheduler.googleapis.com cloudbuild.googleapis.com
gcloud firestore databases create --location="$REGION"

# 2. Composite index (required for dashboard queries)
gcloud firestore indexes composite create \
  --collection-group=pings \
  --field-config=field-path=SiteId,order=ascending \
  --field-config=field-path=Type,order=ascending \
  --field-config=field-path=CheckedAt,order=descending

# 3. (Optional) TTL — auto-delete pings older than 3 months
#    Firebase Console → Firestore → TTL Policies → field `CheckedAt`, retention 3 months

# 4. Build + deploy
gcloud builds submit --tag="gcr.io/$PROJECT_ID/ping-keeper"
gcloud run deploy ping-keeper \
  --image="gcr.io/$PROJECT_ID/ping-keeper" \
  --region="$REGION" \
  --allow-unauthenticated \
  --set-env-vars="Firestore__ProjectId=$PROJECT_ID,Auth__Password=CHANGE_ME" \
  --min-instances=0 --max-instances=1

# 5. Cloud Scheduler
SERVICE_URL="$(gcloud run services describe ping-keeper --region="$REGION" --format='value(status.url)')"
gcloud scheduler jobs create http ping-keeper-ping \
  --location="$REGION" \
  --schedule="0 * * * *" \
  --uri="$SERVICE_URL/ping" \
  --http-method=get \
  --attempt-deadline=300s
```

### Or use GitHub Actions

`.github/workflows/deploy.yml` builds and deploys on push to `main`. Set these in the repo:

| Secret / variable | Purpose |
| --- | --- |
| `GCP_SA_KEY` (secret) | Service account key (roles: `run.admin`, `artifactregistry.admin`, `cloudbuild.builds.editor`, `datastore.user`) |
| `GCP_PROJECT_ID` (variable) | GCP project ID |
| `AUTH_PASSWORD` (secret) | Admin password (do **not** use `demo`) |
| `PING_SECRET` (secret) | Optional — protects `/ping` via the `X-Ping-Secret` header |

## Configuration

| Key | Default | Purpose |
| --- | --- | --- |
| `Auth:Username` | `admin` | Login username |
| `Auth:Password` | `demo` | Login password (change in production) |
| `Firestore:ProjectId` | — | GCP project ID (falls back to `GOOGLE_CLOUD_PROJECT`) |
| `Ping:Secret` | — | Optional `/ping` protection |
| `Demo:Enabled` | `false` | In-memory demo mode (local dev) |

## Project structure

```
PingKeeper/
├── Controllers/        # Dashboard, Ping, Auth, Sites, Health
├── Models/             # Site, SiteInput, PingRecord, view models, enums
├── Services/           # IMonitorStore, FirestoreService, DemoStore, PingService, DashboardService
├── Views/              # Razor views (dashboard, auth, sites, shared layout)
├── wwwroot/css/        # site.css (design tokens + themes)
├── Program.cs          # DI, auth, rate limiting, static files
├── Dockerfile
├── firestore.indexes.json
└── .github/workflows/  # ci.yml, deploy.yml
```

## Roadmap

- [x] MVP, login, site CRUD, history
- [x] Per-site interval + per-level toggles
- [x] Response-time chart + stats
- [x] Light/dark theme + responsive layout
- [x] Security (CSRF, rate limit, healthz)
- [x] CI/CD
- [ ] Alerts on downtime (email / Telegram)
- [ ] History pagination
