using HarmonyLib;
using Il2Cpp;

namespace Firebot.Core;

/// <summary>
///     Every refusal the server sends, with its code, as the game itself shows it (GAMESTATE_PLAN.md,
///     lever G). Observe-only postfixes: they log and count, and change nothing in the game.
/// </summary>
internal static class ServerErrors
{
    public static int Count { get; private set; }

    internal static void Record(string error)
    {
        Count++;
        Logger.Info($"[Server] error #{Count}: '{error}'");
    }

    // __0 is the method's first argument (the game names it "Error").
    [HarmonyPatch(typeof(SocketAPI), nameof(SocketAPI.ServerErrorMessage))]
    private static class ServerErrorMessagePatch
    {
        private static void Postfix(string __0) => Record(__0);
    }

    [HarmonyPatch(typeof(SocketAPI), nameof(SocketAPI.ShowDefaultServerError))]
    private static class DefaultServerErrorPatch
    {
        private static void Postfix() => Record("(default server error)");
    }
}
