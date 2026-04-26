using System;
using System.Collections.Generic;

[Serializable]
public struct TeamChemistrySnapshot
{
    public int Score;
    public ChemistryBand Band;
}

[Serializable]
public struct ConflictPenalty
{
    public TeamId teamId;
    public int ticksRemaining;
    public float speedPenalty;
    public float qualityPenalty;
}

[Serializable]
public class ChemistryState
{
    public Dictionary<long, float> relationships = new Dictionary<long, float>();
    public Dictionary<TeamId, TeamChemistrySnapshot> teamChemistry = new Dictionary<TeamId, TeamChemistrySnapshot>();
    public List<ConflictPenalty> activeConflictPenalties = new List<ConflictPenalty>();

    public static long PackPairKey(EmployeeId a, EmployeeId b)
    {
        int lo = a.Value < b.Value ? a.Value : b.Value;
        int hi = a.Value < b.Value ? b.Value : a.Value;
        return ((long)lo << 32) | (uint)hi;
    }

    public static ChemistryState CreateNew()
    {
        return new ChemistryState
        {
            relationships = new Dictionary<long, float>(),
            teamChemistry = new Dictionary<TeamId, TeamChemistrySnapshot>(),
            activeConflictPenalties = new List<ConflictPenalty>()
        };
    }
}
