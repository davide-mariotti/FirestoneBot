namespace Firebot.TalentEngine;

/// <summary>One decision from <see cref="TalentAllocator" />: invest PointsToAdd more points into the
///     node at NodeIndex, on top of whatever it currently holds.</summary>
public sealed record TalentAllocationStep(int NodeIndex, int PointsToAdd);
