namespace Firebot.TalentEngine;

/// <summary>
///     One talent tree node. Nothing game-specific, so this project needs no Unity reference and can
///     be unit tested. Tier indexes the tree's TierThresholds. PrerequisiteIndex, when set, also
///     requires an earlier node to hold PrerequisiteMinRank; Firestone's own prerequisites aren't
///     mapped, so the real tree leaves it null and only the tests use it.
/// </summary>
public sealed record TalentNode(
    int Index,
    string Name,
    int MaxRank,
    int Tier,
    int? PrerequisiteIndex = null,
    int PrerequisiteMinRank = 1);
