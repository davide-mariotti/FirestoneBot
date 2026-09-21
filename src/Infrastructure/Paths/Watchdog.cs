namespace Firebot.Infrastructure;

/// <summary>Generic paths used by Core.Watchdog to sweep and close any leftover popup/event/menu -
/// not feature-specific, reused across whatever happens to be open at the time.</summary>
public static partial class Paths
{
    public static class WatchdogLoc
    {
        public const string EventsRoot = MenusLoc.Root + "/events";

        public const string PopupsRoot = MenusLoc.Root + "/popups";

        public const string MenusRoot = MenusLoc.Root + "/menus";

        public const string CloseSuffix = "/bg/closeButton";

        public const string CollectSuffix = "/bg/collectButton";

        public const string MenuCloseSuffix = "/closeButton";
    }
}
