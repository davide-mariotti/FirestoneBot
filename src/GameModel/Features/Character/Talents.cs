using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Live read/click primitives for the Talents tab (Paths.TalentsLoc/TalentPreviewLoc). The tree's
///     actual shape (89 nodes, 46 tiers, names, caps, thresholds) lives in TalentTreeData, and the
///     decision of what to invest in lives in TalentEngine.TalentAllocator - this class only ever talks
///     to the live game.
/// </summary>
public static class Talents
{
    /// <summary>
    ///     Live-confirmed, 2026-09-18: the text reads "available/total" (e.g. "1/96", total points
    ///     ever earned - not the tree's max) - GetParsedInt()'s strict full-string parse silently
    ///     failed on this and fell back to 0, making the task think there was never anything to
    ///     spend. Takes the leading number.
    /// </summary>
    public static int AvailablePoints => new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedLeadingInt();

    /// <summary>The "total" side of the same "available/total" counter - the cumulative number of
    ///     points ever awarded. Together with AvailablePoints this gives the tree's current total spent
    ///     (TotalPointsAwarded - AvailablePoints) in one read, instead of summing every node's live
    ///     rank.</summary>
    public static int TotalPointsAwarded => new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedTrailingInt();

    // Defensive - clicked once after any batch of upgrades in case investments are staged rather
    // than instant (see TalentsLoc.SaveBtn comment). Safe no-op if not needed/not clickable.
    public static IEnumerator Save => new GameButton(Paths.TalentsLoc.SaveBtn).Click();

    public static IEnumerator OpenNode(int catalogIndex) =>
        new GameButton($"{Paths.TalentsLoc.NodeRoot}/talentInteraction ({catalogIndex})").Click();

    public static IEnumerator ClosePreview => new GameButton(Paths.TalentPreviewLoc.CloseBtn).Click();

    /// <summary>True when the open preview shows "locked" instead of the upgrade button - the real
    ///     game's own per-node prerequisite gate (see TalentsTask's doc comment for why this is checked
    ///     reactively rather than pre-mapped as data).</summary>
    public static bool IsPreviewLocked => new GameElement(Paths.TalentPreviewLoc.LockedRoot).IsVisible();

    /// <summary>The open preview's displayed talent name - live-confirmed, 2026-09-24, Steam-0, used to
    ///     verify TalentTreeData's names against the real game.</summary>
    public static string PreviewName => new GameText(Paths.TalentPreviewLoc.NameTxt).GetParsedText();

    public static GameButton UpgradeButton => new(Paths.TalentPreviewLoc.UpgradeBtn);

    /// <summary>
    ///     Current rank shown on the open TalentPreview popup. Live-confirmed, 2026-09-24: the text
    ///     reads "Level N/M" (e.g. "Level 1/25") - GetParsedLeadingInt's plain '/'-split parse fails on
    ///     the "Level " prefix and silently fell back to 0 for every node, which then made the allocator
    ///     think every already-invested talent still had its full cap free (see TalentsTask/TalentAllocator
    ///     investigation, 2026-09-24 - this was the root cause of a wrong-priority investment live on
    ///     Steam-0/Steam-5).
    /// </summary>
    public static int PreviewCurrentRank => new GameText(Paths.TalentPreviewLoc.LevelTxt).GetParsedFirstInt();
}
