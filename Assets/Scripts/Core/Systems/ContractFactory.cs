// ContractFactory Version: Clean v1
using System;

public class ContractFactory
{
    private readonly ContractCategoryDefinition[] _categories;
    private readonly IRng _rng;
    private readonly ILogger _logger;
    private TuningConfig _tuning;

    // Scratch array for weighted category selection — avoids per-call allocation
    private int[] _weightScratch;
    // Pre-allocated skill weights buffer — avoids per-call allocation in BuildContract
    private float[] _skillWeightsScratch;
    // Pre-allocated variant weight buffer for skill-aware PickVariant — avoids per-call allocation
    private int[] _variantWeightScratch;

    public ContractFactory(ContractCategoryDefinition[] categories, IRng rng, ILogger logger)
    {
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _logger = logger ?? new NullLogger();
        _weightScratch = new int[categories.Length];
        _skillWeightsScratch = new float[SkillIdHelper.SkillCount];
        _variantWeightScratch = new int[16];
    }

    public void SetTuningConfig(TuningConfig tuning) { _tuning = tuning; }

    // Returns null if no categories are defined.
    public Contract GenerateContract(
        int currentTick,
        int difficultyCap,
        int[] existingPoolSkillCounts = null,
        string preferredCategoryId = null)
    {
        int availableCount = _categories.Length;
        for (int i = 0; i < _categories.Length; i++)
            _weightScratch[i] = 1;

        if (availableCount == 0)
        {
            _logger.LogWarning("ContractFactory: No categories defined.");
            return null;
        }

        // 1. Apply preferred bias (default 70/30 split)
        if (!string.IsNullOrEmpty(preferredCategoryId))
        {
            int preferredIdx = -1;
            for (int i = 0; i < _categories.Length; i++)
            {
                if (_weightScratch[i] > 0 && _categories[i].CategoryId == preferredCategoryId)
                {
                    preferredIdx = i;
                    break;
                }
            }

            if (preferredIdx >= 0 && availableCount > 1)
            {
                int preferredWeight = _tuning != null ? _tuning.ContractPreferredCategoryWeight    : 70;
                int otherTotal      = _tuning != null ? _tuning.ContractOtherCategoryTotalWeight   : 30;
                int otherCount = availableCount - 1;
                int otherWeight = otherCount > 0 ? otherTotal / otherCount : 0;
                for (int i = 0; i < _categories.Length; i++)
                {
                    if (_weightScratch[i] == 0) continue;
                    _weightScratch[i] = (i == preferredIdx) ? preferredWeight : otherWeight;
                }
            }
        }

        // 2. Weighted random category pick — no LINQ
        int totalCatWeight = 0;
        for (int i = 0; i < _categories.Length; i++)
            totalCatWeight += _weightScratch[i];

        int catRoll = _rng.Range(0, totalCatWeight);
        ContractCategoryDefinition selectedCategory = _categories[0];
        int cumulative = 0;
        for (int i = 0; i < _categories.Length; i++)
        {
            cumulative += _weightScratch[i];
            if (catRoll < cumulative)
            {
                selectedCategory = _categories[i];
                break;
            }
        }

        // 3. Roll difficulty via category's weight table, capped to difficultyCap
        int difficulty = RollDifficulty(selectedCategory, difficultyCap);

        return BuildContract(selectedCategory, difficulty, currentTick, existingPoolSkillCounts);
    }

    public Contract GenerateContractOfCategory(string categoryId, int difficulty, int currentTick)
    {
        ContractCategoryDefinition cat = null;
        for (int i = 0; i < _categories.Length; i++)
        {
            if (_categories[i].CategoryId == categoryId)
            {
                cat = _categories[i];
                break;
            }
        }

        if (cat == null)
        {
            _logger.LogWarning($"ContractFactory: Category '{categoryId}' not found.");
            return null;
        }

        int clampedDiff = Math.Max(cat.MinDifficulty, Math.Min(cat.MaxDifficulty, difficulty));
        return BuildContract(cat, clampedDiff, currentTick, null);
    }

    public string GenerateName(ContractCategoryDefinition category)
    {
        if (category.Subjects == null || category.Subjects.Length == 0)
            return $"{category.DisplayName} Contract";

        return category.Subjects[_rng.Range(0, category.Subjects.Length)];
    }

    public string GenerateDescription(ContractCategoryDefinition category, int difficulty)
    {
        if (category.DifficultyContextHints != null && category.DifficultyContextHints.Length > 0)
        {
            int idx = Math.Max(0, Math.Min(difficulty - 1, category.DifficultyContextHints.Length - 1));
            return category.DifficultyContextHints[idx];
        }
        return category.DisplayName;
    }

    private Contract BuildContract(ContractCategoryDefinition category, int difficulty, int currentTick, int[] existingPoolSkillCounts)
    {
        ContractDifficultyTier tier = FindTier(category, difficulty);

        float totalWork     = Lerp(tier.WorkMin, tier.WorkMax, _rng.NextFloat01());
        int   rawReward     = tier.RewardMin + _rng.Range(0, Math.Max(1, tier.RewardMax - tier.RewardMin + 1));
        float rewardMult    = _tuning != null ? _tuning.ContractRewardMultiplier : 1f;
        int   reward        = (int)Math.Round(rawReward * rewardMult);
        int   dlDays        = tier.DeadlineDaysMin + _rng.Range(0, Math.Max(1, tier.DeadlineDaysMax - tier.DeadlineDaysMin + 1));
        int   qThresh       = tier.QualityThresholdMin + _rng.Range(0, Math.Max(1, tier.QualityThresholdMax - tier.QualityThresholdMin + 1));
        int   deadlineTicks = dlDays * TimeState.TicksPerDay;

        // Primary skill and SkillRequirements from the chosen phase variant
        PhaseProfileSet variant = PickVariant(category, existingPoolSkillCounts);
        PhaseSkillProfile dominant = PickDominantFromVariant(variant);

        // SkillRequirements: blend across all phases in the variant, weighted by WorkFraction
        for (int i = 0; i < SkillIdHelper.SkillCount; i++) _skillWeightsScratch[i] = 0f;
        if (variant.Phases != null)
        {
            for (int i = 0; i < variant.Phases.Length; i++)
            {
                var profile = variant.Phases[i];
                _skillWeightsScratch[(int)profile.PrimarySkill] += profile.WorkFraction;
            }
        }
        float total = 0f;
        for (int i = 0; i < SkillIdHelper.SkillCount; i++) total += _skillWeightsScratch[i];
        if (total < 0.001f) { _skillWeightsScratch[(int)SkillId.Programming] = 1f; total = 1f; }
        for (int i = 0; i < SkillIdHelper.SkillCount; i++) _skillWeightsScratch[i] /= total;
        var requirements = new SkillRequirements(_skillWeightsScratch);

        string name        = GenerateName(category);
        string description = GenerateDescription(category, difficulty);
        int    repReward   = difficulty * 1;

        var contract = new Contract(
            id:                  new ContractId(0),
            name:                name,
            description:         description,
            difficulty:          difficulty,
            categoryId:          category.CategoryId,
            requirements:        requirements,
            totalWorkRequired:   totalWork,
            rewardMoney:         reward,
            reputationReward:    repReward,
            deadlineDurationTicks: deadlineTicks,
            requiredSkill:       dominant.PrimarySkill,
            minSkillRequired:    tier.MinSkill,
            targetSkill:         tier.TargetSkill,
            excellenceSkill:     tier.ExcellenceSkill,
            minContributors:     tier.MinContributors,
            optimalContributors: tier.OptimalContributors,
            maxContributors:     tier.MaxContributors,
            hasStretchGoal:      category.HasStretchGoal,
            qualityThreshold:    qThresh
        );
        contract.QualityExpectation = category.DefaultQualityExpectation;
        return contract;
    }

    private ContractDifficultyTier FindTier(ContractCategoryDefinition cat, int difficulty)
    {
        if (cat.DifficultyTiers != null && cat.DifficultyTiers.Length > 0)
        {
            for (int i = 0; i < cat.DifficultyTiers.Length; i++)
            {
                if (cat.DifficultyTiers[i].Difficulty == difficulty)
                    return cat.DifficultyTiers[i];
            }
            // Closest match fallback
            ContractDifficultyTier best = cat.DifficultyTiers[0];
            int bestDelta = Math.Abs(best.Difficulty - difficulty);
            for (int i = 1; i < cat.DifficultyTiers.Length; i++)
            {
                int delta = Math.Abs(cat.DifficultyTiers[i].Difficulty - difficulty);
                if (delta < bestDelta) { bestDelta = delta; best = cat.DifficultyTiers[i]; }
            }
            return best;
        }
        // Hard fallback — assets not yet configured
        return new ContractDifficultyTier
        {
            Difficulty = difficulty, MinSkill = 4, TargetSkill = 8, ExcellenceSkill = 12,
            MinContributors = 1, OptimalContributors = 2, MaxContributors = 6,
            WorkMin = 3000f, WorkMax = 5000f, RewardMin = 2000, RewardMax = 4000,
            DeadlineDaysMin = 7, DeadlineDaysMax = 14, QualityThresholdMin = 30, QualityThresholdMax = 50
        };
    }

    private PhaseProfileSet PickVariant(ContractCategoryDefinition category, int[] existingPoolSkillCounts)
    {
        if (category.PhaseProfileVariants == null || category.PhaseProfileVariants.Length == 0)
            return new PhaseProfileSet { Phases = category.PhaseProfiles };

        int variantCount = category.PhaseProfileVariants.Length;

        if (existingPoolSkillCounts == null)
        {
            return category.PhaseProfileVariants[_rng.Range(0, variantCount)];
        }

        // Check whether any pool skill counts are non-zero — if all zero, uniform pick
        bool hasAnyCounts = false;
        for (int i = 0; i < existingPoolSkillCounts.Length; i++)
        {
            if (existingPoolSkillCounts[i] > 0) { hasAnyCounts = true; break; }
        }
        if (!hasAnyCounts)
            return category.PhaseProfileVariants[_rng.Range(0, variantCount)];

        // Grow scratch buffer if needed — one-time reallocation
        if (_variantWeightScratch.Length < variantCount)
            _variantWeightScratch = new int[variantCount];

        int totalVariantWeight = 0;
        for (int i = 0; i < variantCount; i++)
        {
            PhaseProfileSet v = category.PhaseProfileVariants[i];
            // Find dominant skill (highest WorkFraction) in this variant
            SkillId dominant = SkillId.Programming;
            float bestFraction = -1f;
            if (v.Phases != null)
            {
                for (int p = 0; p < v.Phases.Length; p++)
                {
                    if (v.Phases[p].WorkFraction > bestFraction)
                    {
                        bestFraction = v.Phases[p].WorkFraction;
                        dominant = v.Phases[p].PrimarySkill;
                    }
                }
            }
            int count = existingPoolSkillCounts[(int)dominant];
            int w = Math.Max(1, 4 - count);
            _variantWeightScratch[i] = w;
            totalVariantWeight += w;
        }

        int roll = _rng.Range(0, totalVariantWeight);
        int cumulative = 0;
        for (int i = 0; i < variantCount; i++)
        {
            cumulative += _variantWeightScratch[i];
            if (roll < cumulative)
                return category.PhaseProfileVariants[i];
        }
        return category.PhaseProfileVariants[variantCount - 1];
    }

    private PhaseSkillProfile PickDominantFromVariant(PhaseProfileSet variant)
    {
        if (variant.Phases == null || variant.Phases.Length == 0)
            return new PhaseSkillProfile { PrimarySkill = SkillId.Programming, WorkFraction = 1f, QualityWeight = 1f };
        int best = 0;
        for (int i = 1; i < variant.Phases.Length; i++)
            if (variant.Phases[i].WorkFraction > variant.Phases[best].WorkFraction)
                best = i;
        return variant.Phases[best];
    }

    private PhaseSkillProfile PickDominantProfile(ContractCategoryDefinition category)
    {
        return PickDominantFromVariant(new PhaseProfileSet { Phases = category.PhaseProfiles });
    }

    private int RollDifficulty(ContractCategoryDefinition category, int difficultyCap)
    {
        int min = category.MinDifficulty > 0 ? category.MinDifficulty : 1;
        int max = category.MaxDifficulty > 0 ? category.MaxDifficulty : 5;

        if (category.DifficultyWeights != null && category.DifficultyWeights.Length > 0)
        {
            int totalWeight = 0;
            int weightLen = category.DifficultyWeights.Length;
            for (int i = 0; i < weightLen; i++)
                totalWeight += category.DifficultyWeights[i];

            int roll = _rng.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < weightLen; i++)
            {
                cumulative += category.DifficultyWeights[i];
                if (roll < cumulative)
                {
                    int d = min + i;
                    return Math.Min(d, difficultyCap);
                }
            }
            return Math.Min(min, difficultyCap);
        }

        int raw = _rng.Range(min, max + 1);
        return Math.Min(raw, difficultyCap);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>
    /// Generates starting contracts biased toward the given categories and matching founder roles.
    /// Contracts are low–medium difficulty to suit early game.
    /// </summary>
    public System.Collections.Generic.List<Contract> GenerateStartingContracts(
        string[] biasCategories,
        RoleId[] founderRoles,
        int count,
        IRng rng,
        int currentTick)
    {
        var results = new System.Collections.Generic.List<Contract>(count);

        bool hasCategories = biasCategories != null && biasCategories.Length > 0;

        for (int i = 0; i < count; i++)
        {
            // Slot 0: easy, matched to first bias category
            // Slot count-1: slightly harder stretch goal
            // Others: random low-medium difficulty
            int difficulty;
            if (i == 0)
                difficulty = 1;
            else if (i == count - 1)
                difficulty = 2;
            else
                difficulty = rng.Range(1, 3);

            string preferredCategory = null;
            if (hasCategories)
            {
                // 70% chance to honour bias, 30% open market
                if (rng.Range(0, 100) < 70)
                    preferredCategory = biasCategories[rng.Range(0, biasCategories.Length)];
            }

            // Difficulty cap: starting contracts never exceed 2
            var contract = GenerateContract(currentTick, Math.Min(difficulty, 2), null, preferredCategory);
            if (contract != null)
                results.Add(contract);
        }

        return results;
    }
}
