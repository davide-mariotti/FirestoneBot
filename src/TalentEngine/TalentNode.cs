namespace Firebot.TalentEngine;

/// <summary>
///     One talent tree node. Nothing game-specific, so this project needs no Unity reference and can
///     be unit tested. Tier indexes the tree's TierThresholds.
/// </summary>
public sealed record TalentNode(int Index, string Name, int MaxRank, int Tier);
