namespace Firebot.TalentEngine;

/// <summary>
///     One node in a talent tree - deliberately game-agnostic (no Firestone-specific concepts), so
///     this whole project stays free of any IL2Cpp/Unity dependency and is directly unit-testable.
///     <see cref="Tier" /> indexes into the owning <see cref="TalentTreeDefinition" />'s
///     <c>TierThresholds</c> - a node only becomes eligible for investment once the tree's cumulative
///     spent points reach that tier's threshold.
///     <see cref="PrerequisiteIndex" />, when set, additionally requires another node (by index, which
///     must be earlier in the tree) to already hold at least <see cref="PrerequisiteMinRank" /> - a
///     second, independent gate on top of the tier threshold. Firestone's real tree doesn't have this
///     mapped (see TalentTreeData's own doc comment), so production data always leaves this null, but
///     the allocator supports it as first-class data so it can be exercised directly in tests.
/// </summary>
public sealed record TalentNode(
    int Index,
    string Name,
    int MaxRank,
    int Tier,
    int? PrerequisiteIndex = null,
    int PrerequisiteMinRank = 1);
