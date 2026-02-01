using System.Diagnostics;

namespace TraefikForwardAuth.Helpers;

internal static class ActivitySources
{
    public const string AppName = "TraefikForwardAuth";
    public static ActivitySource AppActivitySource { get; } = new ActivitySource(AppName);
}