namespace Firebot.TalentEngine;

/// <summary>Invest PointsToAdd more points in the node at NodeIndex.</summary>
public sealed record TalentAllocationStep(int NodeIndex, int PointsToAdd);
