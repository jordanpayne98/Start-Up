using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Competitor Archetype Config")]
public class CompetitorArchetypeConfig : ScriptableObject
{
    [Header("Identity")]
    public CompetitorArchetype archetype;

    [Header("Release Cadence")]
    public float releaseIntervalMonthsMin;
    public float releaseIntervalMonthsMax;

    [Header("Personality Ranges — rolled per competitor at spawn")]
    public CompetitorPersonality personalityMin;
    public CompetitorPersonality personalityMax;

    [Header("Category Specialization")]
    public ProductCategory[] primaryCategories;
    public ProductCategory[] secondaryCategories;

    [Header("Budget Ratios")]
    [Range(0f, 1f)]
    // Part 2: real budget allocation ratio for AI marketing spend decisions — NOT a salary multiplier
    public float marketingBudgetRatio;
    [Range(0f, 1f)]
    public float salaryBudgetRatio;
    [Range(0f, 1f)]
    // Part 2: real budget allocation ratio for AI maintenance/upkeep spend decisions — NOT a salary multiplier
    public float maintenanceBudgetRatio;
    [Range(0f, 1f)]
    public float reserveBudgetRatio;

    [Header("Expansion")]
    public float expansionCashThreshold;
    [Range(0f, 1f)]
    public float expansionRiskTolerance;

    [Header("Product Lifecycle")]
    public float sunsetMonths;

    [Header("Capacity")]
    public int maxSimultaneousProducts;

    [Header("Employment Mix")]
    [Range(0f, 1f)]
    public float fullTimeRatio = 0.65f;
    [Range(0.8f, 1.2f)]
    public float salaryTierModifier = 1.0f;

    [Header("Release Date Reactions")]
    [Range(0f, 1f)]
    public float dateShiftReactivity;
    public bool prefersRush;
    [Range(0f, 0.5f)]
    public float maxDateShiftFraction;

    [Header("Synthetic Team Stats (Ranges — rolled once at competitor spawn)")]
    public Vector2 baseSkillRange = new Vector2(7f, 9f);
    public Vector2 syntheticCARange = new Vector2(90f, 110f);
    public Vector2 syntheticMoraleRange = new Vector2(50f, 70f);
    public Vector2 syntheticWorkEthicRange = new Vector2(9f, 11f);
    public Vector2 syntheticCreativeRange = new Vector2(9f, 11f);
    public Vector2 syntheticAdaptabilityRange = new Vector2(8f, 12f);
    [Range(0.15f, 0.60f)]
    public float roleFitRatio = 0.35f;
    public Vector2Int syntheticTeamSizeRange = new Vector2Int(3, 5);
}
