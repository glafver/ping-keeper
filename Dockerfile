# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so dependency changes don't invalidate the whole layer.
COPY PingKeeper.csproj .
RUN dotnet restore PingKeeper.csproj

COPY . .
RUN dotnet publish PingKeeper.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cloud Run injects PORT, but we pin the default so it also works elsewhere.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PingKeeper.dll"]
