using UnityEngine;

public enum QualityExpectation { Standard, High, Premium }

[System.Serializable]
public struct ContractDifficultyTier
{
    public int Difficulty;

    [Header("Skill Thresholds")]
    public int MinSkill;           // floor for viable contribution
    public int TargetSkill;        // standard quality outcome
    public int ExcellenceSkill;    // premium quality outcome

    [Header("Staffing")]
    public int MinContributors;
    public int OptimalContributors;
    public int MaxContributors;

    [Header("Work")]
    public float WorkMin;          // total work units (lower bound)
    public float WorkMax;          // total work units (upper bound)

    [Header("Reward")]
    public int RewardMin;
    public int RewardMax;

    [Header("Deadline")]
    public int DeadlineDaysMin;
    public int DeadlineDaysMax;

    [Header("Quality Bar")]
    public int QualityThresholdMin;   // minimum % quality to pass
    public int QualityThresholdMax;
}

[System.Serializable]
public struct PhaseProfileSet
{
    public PhaseSkillProfile[] Phases; // 1-3 entries; work fractions must sum to 1.0
}

[CreateAssetMenu(menuName = "StartUp/Contract Category")]
public class ContractCategoryDefinition : ScriptableObject
{
    [Header("Identity")]
    public string CategoryId;
    public string DisplayName;

    [Header("Naming")]
    public string[] Subjects;

    [Header("Descriptions")]
    public string[] DifficultyContextHints;

    [Header("Difficulty")]
    public int MinDifficulty;
    public int MaxDifficulty;
    public int[] DifficultyWeights;

    [Header("Difficulty Tiers")]
    public ContractDifficultyTier[] DifficultyTiers;

    [Header("Milestone")]
    public float MilestonePaymentFraction;

    [Header("Stretch Goal")]
    public bool HasStretchGoal;
    public float StretchGoalBonusMultiplier;
    public float StretchGoalWorkMultiplier;

    [Header("Quality")]
    public QualityExpectation DefaultQualityExpectation;

    [Header("Phase Variants")]
    public PhaseProfileSet[] PhaseProfileVariants;

    // Legacy — kept until all assets are migrated; PickVariant falls back to this.
    [Header("Phases (Legacy)")]
    public PhaseSkillProfile[] PhaseProfiles;

    // Legacy — kept for backward compat with existing save data and assets.
    // These fields are no longer serialized in new assets.
#pragma warning disable 0649
    [HideInInspector] public string RequiredUpgradeId;
    [HideInInspector] public string[] Adjectives;
    [HideInInspector] public string[] Verbs;
#pragma warning restore 0649
}

[System.Serializable]
public struct PhaseSkillProfile
{
    public SkillId PrimarySkill;
    public float WorkFraction;
    public float QualityWeight;
}
