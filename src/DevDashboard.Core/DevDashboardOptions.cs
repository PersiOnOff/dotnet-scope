namespace DevDashboard.Core;

/// <summary>
/// Constants shared by the dashboard endpoints, the hub and the embedded UI.
/// </summary>
public static class DevDashboardDefaults
{
    /// <summary>Base path the dashboard is served from.</summary>
    public const string PathBase = "/_dashboard";

    /// <summary>Route the SignalR hub is mapped to.</summary>
    public const string HubPath = PathBase + "/hub";

    /// <summary>Route the JSON API is mapped to.</summary>
    public const string ApiPath = PathBase + "/api";

    /// <summary>
    /// Logger categories that only produce framework chatter. Requests, SQL and exceptions are
    /// already captured by dedicated hooks, so their logs would be duplicates of the real thing.
    /// </summary>
    public static readonly IReadOnlyList<string> NoiseCategories = new[]
    {
        "Microsoft.AspNetCore.SignalR",
        "Microsoft.AspNetCore.Http.Connections",
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "Microsoft.AspNetCore.Routing",
        "Microsoft.AspNetCore.Server.Kestrel",
        "Microsoft.AspNetCore.Diagnostics",
        "Microsoft.AspNetCore.Http.Result",
        "Microsoft.AspNetCore.StaticFiles",
        "Microsoft.EntityFrameworkCore",
        "DevDashboard",
    };
}

/// <summary>
/// Configuration for the embedded dashboard.
/// </summary>
public sealed class DevDashboardOptions
{
    /// <summary>
    /// When <see langword="false"/> every dashboard route answers 404. Useful to turn the
    /// dashboard off in production without removing the package.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When <see langword="false"/> (the default) the dashboard only answers requests that
    /// originate from the local machine. Telemetry contains SQL parameters and response
    /// bodies, so exposing it to the network by accident is a data leak.
    /// </summary>
    public bool AllowRemoteAccess { get; set; }

    /// <summary>Maximum number of telemetry events kept in memory. Oldest are dropped first.</summary>
    public int MaxEvents { get; set; } = 1000;

    /// <summary>Capture response bodies (text only, truncated to <see cref="MaxBodyChars"/>).</summary>
    public bool CaptureResponseBodies { get; set; } = true;

    /// <summary>Capture request bodies for POST/PUT/PATCH requests.</summary>
    public bool CaptureRequestBodies { get; set; } = true;

    /// <summary>Maximum number of characters kept per captured body.</summary>
    public int MaxBodyChars { get; set; } = 4000;

    /// <summary>
    /// Logger category prefixes that are ignored. Defaults to <see cref="DevDashboardDefaults.NoiseCategories"/>;
    /// add your own noisy framework categories here.
    /// </summary>
    public IList<string> ExcludedCategories { get; } = new List<string>(DevDashboardDefaults.NoiseCategories);
}
