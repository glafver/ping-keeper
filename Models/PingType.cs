namespace PingKeeper.Models;

/// <summary>The three levels a monitored site can be checked on.</summary>
public enum PingType
{
    Frontend,
    Backend,
    Database,
}

public static class PingTypeExtensions
{
    /// <summary>The string value stored in the "pings" collection (e.g. "frontend").</summary>
    public static string ToStore(this PingType type) => type.ToString().ToLowerInvariant();
}
