using System.Collections.Generic;

/// <summary>
/// Generates the starting candidate pool at new game start.
/// Pool is biased by company background, founder roles, and founder weakness gaps.
/// Same seed + same background + same founders = same pool (deterministic).
/// </summary>
public static class StartingPoolGenerator
{
    // How long starting-pool candidates persist (20 days) before expiring
    private const int StartingPoolExpiryDays = 20;

    // Skill weakness threshold — scores below this count as a founder weakness
    private const int WeaknessSkillThreshold = 8;

    // Maximum candidates of the same role allowed in the pool
    private const int MaxSameRole = 3;

    // Minimum distinct role families required in the pool
    private const int MinDistinctFamilies = 3;

    public static List<CandidateData> GenerateStartingCandidatePool(
        StartingPoolParams poolParams,
        IRng rng,
        RoleProfileTable roleProfileTable)
    {
        int ticksPerDay = TimeState.TicksPerDay;
        int expiryTicks = StartingPoolExpiryDays * ticksPerDay;

        int poolSize = poolParams.PoolSize > 0 ? poolParams.PoolSize : 10;
        // Clamp to 8–12
        if (poolSize < 8) poolSize = 8;
        if (poolSize > 12) poolSize = 12;

        float quality = poolParams.QualityMultiplier > 0f ? poolParams.QualityMultiplier : 1.0f;

        // Build role-family weight overrides from background bias + founder weakness gaps
        float[] familyWeights = BuildFamilyWeights(poolParams);

        var candidates = new List<CandidateData>(poolSize);

        // Role distribution tracker to enforce MaxSameRole guardrail
        int[] roleCountScratch = new int[RoleIdHelper.RoleCount];

        // --- Slot 1-2: Cover founder weakness areas (at least 2) ---
        int weaknessSlotsTarget = 2;
        int weaknessSlotsPlaced = 0;
        if (poolParams.FounderWeakFamilies != null && poolParams.FounderWeakFamilies.Length > 0)
        {
            for (int wi = 0; wi < poolParams.FounderWeakFamilies.Length && weaknessSlotsPlaced < weaknessSlotsTarget; wi++)
            {
                RoleFamily weakFamily = poolParams.FounderWeakFamilies[wi];
                var p = CandidateGenerationParams.StartingPool(quality, weakFamily);
                var candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);
                candidate.Source = CandidateSource.StartingPool;
                candidate.ExpiryTick = expiryTicks;
                candidates.Add(candidate);
                roleCountScratch[(int)candidate.Role]++;
                weaknessSlotsPlaced++;
            }
        }
        // If not enough weak families, fill remaining weakness slots with any biased family
        while (weaknessSlotsPlaced < weaknessSlotsTarget && candidates.Count < poolSize)
        {
            RoleFamily biasedFamily = PickWeightedFamily(rng, familyWeights);
            var p = CandidateGenerationParams.StartingPool(quality * 0.9f, biasedFamily);
            var candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);
            candidate.Source = CandidateSource.StartingPool;
            candidate.ExpiryTick = expiryTicks;
            candidates.Add(candidate);
            roleCountScratch[(int)candidate.Role]++;
            weaknessSlotsPlaced++;
        }

        // --- Slot 3-4: At least 2 affordable junior candidates ---
        for (int j = 0; j < 2 && candidates.Count < poolSize; j++)
        {
            RoleFamily biasedFamily = PickWeightedFamily(rng, familyWeights);
            var p = CandidateGenerationParams.StartingPool(quality * 0.85f, biasedFamily);
            p.ForceCareerStage = rng.Range(0, 2) == 0 ? CareerStage.Junior : CareerStage.EarlyCareer;
            var candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);
            candidate.Source = CandidateSource.StartingPool;
            candidate.ExpiryTick = expiryTicks;
            candidates.Add(candidate);
            roleCountScratch[(int)candidate.Role]++;
        }

        // --- Slot 5: At least 1 strong but expensive senior candidate ---
        if (candidates.Count < poolSize)
        {
            RoleFamily biasedFamily = PickWeightedFamily(rng, familyWeights);
            var p = CandidateGenerationParams.StartingPool(quality * 1.2f, biasedFamily);
            p.ForceCareerStage = CareerStage.Senior;
            var candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);
            candidate.Source = CandidateSource.StartingPool;
            candidate.ExpiryTick = expiryTicks;
            candidates.Add(candidate);
            roleCountScratch[(int)candidate.Role]++;
        }

        // --- Slot 6: At least 1 generalist ---
        if (candidates.Count < poolSize)
        {
            var p = CandidateGenerationParams.StartingPool(quality, null);
            p.ForceArchetype = CandidateArchetype.Generalist;
            var candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);
            candidate.Source = CandidateSource.StartingPool;
            candidate.ExpiryTick = expiryTicks;
            candidates.Add(candidate);
            roleCountScratch[(int)candidate.Role]++;
        }

        // --- Fill remaining slots with background-biased candidates ---
        while (candidates.Count < poolSize)
        {
            RoleFamily biasedFamily = PickWeightedFamily(rng, familyWeights);
            var p = CandidateGenerationParams.StartingPool(quality, biasedFamily);
            var candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);

            // Enforce MaxSameRole guardrail — reroll once if over-represented
            if (roleCountScratch[(int)candidate.Role] >= MaxSameRole)
            {
                // Try a different family
                RoleFamily altFamily = PickLeastRepresentedFamily(candidates, familyWeights, rng);
                p = CandidateGenerationParams.StartingPool(quality, altFamily);
                candidate = CandidateData.GenerateCandidate(rng, roleProfileTable, p);
            }

            candidate.Source = CandidateSource.StartingPool;
            candidate.ExpiryTick = expiryTicks;
            candidates.Add(candidate);
            roleCountScratch[(int)candidate.Role]++;
        }

        return candidates;
    }

    // --- Helpers ---

    private static float[] BuildFamilyWeights(StartingPoolParams poolParams)
    {
        int familyCount = 7; // RoleFamily has 7 values (0–6)
        float[] weights = new float[familyCount];

        // Default: equal weight
        for (int i = 0; i < familyCount; i++)
            weights[i] = 1.0f;

        // Apply explicit typed bias from background definition
        if (poolParams.Background != null
            && poolParams.Background.CandidatePoolBiasFamilies != null
            && poolParams.Background.CandidatePoolBiasWeights != null)
        {
            int len = poolParams.Background.CandidatePoolBiasFamilies.Length;
            int wLen = poolParams.Background.CandidatePoolBiasWeights.Length;
            for (int i = 0; i < len; i++)
            {
                int idx = (int)poolParams.Background.CandidatePoolBiasFamilies[i];
                if (idx >= 0 && idx < familyCount)
                {
                    float w = i < wLen ? poolParams.Background.CandidatePoolBiasWeights[i] : 2.0f;
                    weights[idx] = w;
                }
            }
        }

        // Boost weak families so the pool compensates for founder gaps
        if (poolParams.FounderWeakFamilies != null)
        {
            for (int i = 0; i < poolParams.FounderWeakFamilies.Length; i++)
            {
                int idx = (int)poolParams.FounderWeakFamilies[i];
                if (idx >= 0 && idx < familyCount)
                    weights[idx] += 1.5f;
            }
        }

        return weights;
    }

    private static RoleFamily PickWeightedFamily(IRng rng, float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        float roll = rng.NextFloat01() * total;
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return (RoleFamily)i;
        }
        return (RoleFamily)(weights.Length - 1);
    }

    private static RoleFamily PickLeastRepresentedFamily(
        List<CandidateData> existing,
        float[] familyWeights,
        IRng rng)
    {
        int familyCount = familyWeights.Length;
        int[] counts = new int[familyCount];
        for (int i = 0; i < existing.Count; i++)
        {
            int fidx = (int)existing[i].RoleFamily;
            if (fidx >= 0 && fidx < familyCount)
                counts[fidx]++;
        }

        // Find minimum count
        int minCount = int.MaxValue;
        for (int i = 0; i < familyCount; i++)
            if (counts[i] < minCount) minCount = counts[i];

        // Collect families at minimum count
        int candidateCount = 0;
        for (int i = 0; i < familyCount; i++)
            if (counts[i] == minCount) candidateCount++;

        if (candidateCount == 0) return (RoleFamily)rng.Range(0, familyCount);

        int pick = rng.Range(0, candidateCount);
        int seen = 0;
        for (int i = 0; i < familyCount; i++)
        {
            if (counts[i] == minCount)
            {
                if (seen == pick) return (RoleFamily)i;
                seen++;
            }
        }

        return (RoleFamily)rng.Range(0, familyCount);
    }
}
