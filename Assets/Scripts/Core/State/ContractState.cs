using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

public class ContractIdConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string s && int.TryParse(s, out int id))
            return new ContractId(id);
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string) && value is ContractId cid)
            return cid.Value.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(ContractIdConverter))]
[Serializable]
public struct ContractId
{
    public int Value;

    public ContractId(int value)
    {
        Value = value;
    }

    public override bool Equals(object obj)
    {
        if (obj is ContractId other)
            return Value == other.Value;
        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ContractId a, ContractId b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(ContractId a, ContractId b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public enum ContractStatus
{
    Available,
    Accepted,
    InProgress,
    Completed,
    Failed
}

public enum SkillType
{
    Programming = 0,
    Design = 1,
    QA = 2,
    VFX = 3,
    SFX = 4,
    HR = 5,
    Negotiation = 6,
    Accountancy = 7,
    Marketing = 8
}

public static class SkillTypeHelper
{
    public const int SkillTypeCount = 9;

    private static readonly string[] _names = { "Programming", "Design", "QA", "VFX", "SFX", "HR", "Negotiation", "Accountancy", "Marketing" };

    public static string GetName(SkillType type)
    {
        int idx = (int)type;
        if (idx >= 0 && idx < _names.Length) return _names[idx];
        return "Unknown";
    }
}

[Serializable]
public class SkillRequirements
{
    public float[] Weights;

    // Backward-compat properties for existing code and serialization
    public float ProgrammingWeight { get => Weights[(int)SkillType.Programming]; set => Weights[(int)SkillType.Programming] = value; }
    public float DesignWeight { get => Weights[(int)SkillType.Design]; set => Weights[(int)SkillType.Design] = value; }
    public float QAWeight { get => Weights[(int)SkillType.QA]; set => Weights[(int)SkillType.QA] = value; }

    private SkillRequirements() { Weights = new float[SkillTypeHelper.SkillTypeCount]; }

    public SkillRequirements(float programmingWeight, float designWeight, float qaWeight)
    {
        Weights = new float[SkillTypeHelper.SkillTypeCount];
        Weights[(int)SkillType.Programming] = programmingWeight;
        Weights[(int)SkillType.Design] = designWeight;
        Weights[(int)SkillType.QA] = qaWeight;
    }

    public SkillRequirements(float[] weights)
    {
        Weights = new float[SkillTypeHelper.SkillTypeCount];
        if (weights != null)
        {
            int len = Math.Min(weights.Length, SkillTypeHelper.SkillTypeCount);
            for (int i = 0; i < len; i++) Weights[i] = weights[i];
        }
    }

    public float GetWeight(SkillType type) => Weights[(int)type];

    public bool Validate()
    {
        float sum = 0f;
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
            sum += Weights[i];
        return Math.Abs(sum - 1.0f) < 0.01f;
    }
}

[Serializable]
public class Contract
{
    public ContractId Id;
    public string Name;
    public string Description;
    public int Difficulty;
    public string CategoryId;
    public SkillRequirements Requirements;
    public float TotalWorkRequired;
    public float WorkCompleted;
    public float QualityScore;
    public int RewardMoney;
    public int ReputationReward;
    public int DeadlineTick;              // set on accept: AcceptedTick + DeadlineDurationTicks
    public int DeadlineDurationTicks;     // stored at generation; clock starts on accept
    public int AcceptedTick;
    public TeamId? AssignedTeamId;
    public ContractStatus Status;
    public int QualityThreshold;
    public bool HasStretchGoal;
    public bool StretchGoalActivated;
    public float StretchGoalProgress;
    public QualityExpectation QualityExpectation;

    // Skill requirements resolved at generation
    public SkillType RequiredSkill;
    public int MinSkillRequired;
    public int TargetSkill;
    public int ExcellenceSkill;
    public int MinContributors;
    public int OptimalContributors;
    public int MaxContributors;

    // Competitor source — null if player-generated, set if sourced from a competitor's product pipeline
    public CompetitorId? SourceCompetitorId;
    public ProductId? SourceProductId;

    public float ProgressPercent => TotalWorkRequired > 0f ? WorkCompleted / TotalWorkRequired : 0f;

    private Contract() { }

    public Contract(
        ContractId id,
        string name,
        string description,
        int difficulty,
        string categoryId,
        SkillRequirements requirements,
        float totalWorkRequired,
        int rewardMoney,
        int reputationReward,
        int deadlineDurationTicks,
        SkillType requiredSkill,
        int minSkillRequired,
        int targetSkill,
        int excellenceSkill,
        int minContributors,
        int optimalContributors,
        int maxContributors,
        bool hasStretchGoal = false,
        int qualityThreshold = 50)
    {
        Id = id;
        Name = name;
        Description = description;
        Difficulty = difficulty;
        CategoryId = categoryId;
        Requirements = requirements;
        TotalWorkRequired = totalWorkRequired;
        WorkCompleted = 0f;
        QualityScore = 0f;
        RewardMoney = rewardMoney;
        ReputationReward = reputationReward;
        DeadlineDurationTicks = deadlineDurationTicks;
        DeadlineTick = -1;  // set when accepted
        AcceptedTick = -1;
        AssignedTeamId = null;
        Status = ContractStatus.Available;
        QualityThreshold = qualityThreshold;
        HasStretchGoal = hasStretchGoal;
        StretchGoalActivated = false;
        StretchGoalProgress = 0f;
        RequiredSkill = requiredSkill;
        MinSkillRequired = minSkillRequired;
        TargetSkill = targetSkill;
        ExcellenceSkill = excellenceSkill;
        MinContributors = minContributors;
        OptimalContributors = optimalContributors;
        MaxContributors = maxContributors;
    }
}

[Serializable]
public class ContractState
{
    public Dictionary<ContractId, Contract> availableContracts;
    public Dictionary<ContractId, Contract> activeContracts;
    public Dictionary<TeamId, ContractId> teamAssignments;
    public int nextContractId;
    public int maxAvailableContracts;

    public int lastPoolRefreshTick;
    public int poolRefreshIntervalTicks;
    public int rerollsUsedThisCycle;

    public static ContractState CreateNew()
    {
        return new ContractState
        {
            availableContracts = new Dictionary<ContractId, Contract>(),
            activeContracts = new Dictionary<ContractId, Contract>(),
            teamAssignments = new Dictionary<TeamId, ContractId>(),
            nextContractId = 1,
            maxAvailableContracts = 5,
            lastPoolRefreshTick = 0,
            poolRefreshIntervalTicks = 7 * TimeState.TicksPerDay,
            rerollsUsedThisCycle = 0
        };
    }
}
