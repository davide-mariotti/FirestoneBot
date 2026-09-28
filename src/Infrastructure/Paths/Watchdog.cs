namespace Firebot.Infrastructure;

/// <summary>The three screen roots Watchdog sweeps, and where their close/collect buttons sit.</summary>
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
