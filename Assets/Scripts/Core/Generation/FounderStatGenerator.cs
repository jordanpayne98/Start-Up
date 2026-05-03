// FounderStatGenerator — deterministic founder stat generation from archetype + personality + weakness + age.
// All randomness goes through IRng. Same seed + same wizard choices = identical founders.
// Hard limits enforced per Page 04 section 8.3.
public static class FounderStatGenerator
{
    // Hard limits (Page 04 section 8.3)
    private const int CACap = 140;
    private const int PACap = 180;
    private const int MaxSkillValue = 18;
    private const int MaxAttributeValue = 18;

    // ─────────────────────────────────────────────────────────────────────────
    // Primary entry point
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a fully populated EmployeeStatBlock for a founder from wizard inputs.
    /// Uses the role profile bands from the archetype definition, applying archetype
    /// skill bias, personality modifiers, and weakness penalties.
    /// </summary>
    public static EmployeeStatBlock Generate(FounderGenerationParams p, IRng rng, RoleProfileTable roleProfileTable)
    {
        var stats = EmployeeStatBlock.Create();
        var archetype = p.Archetype;
        var personality = p.PersonalityStyle;
        var weakness = p.Weakness;
        bool solo = p.IsSoloFounder;

        // ── Phase 1: Determine CA / PA targets ────────────────────────────────
        int caMin = solo && archetype != null ? archetype.SoloCAMin : (archetype?.CAMin ?? 75);
        int caMax = solo && archetype != null ? archetype.SoloCAMax : (archetype?.CAMax ?? 100);
        int paMin = solo && archetype != null ? archetype.SoloPAMin : (archetype?.PAMin ?? 115);
        int paMax = solo && archetype != null ? archetype.SoloPAMax : (archetype?.PAMax ?? 150);

        // Fallback ranges if SO fields not populated yet
        if (caMin <= 0) caMin = solo ? 90 : 75;
        if (caMax <= 0 || caMax <= caMin) caMax = solo ? 115 : 100;
        if (paMin <= 0) paMin = solo ? 125 : 115;
        if (paMax <= 0 || paMax <= paMin) paMax = solo ? 160 : 150;

        // Age influence: older → closer to CA ceiling; younger → closer to CA floor but higher PA margin
        float ageNorm = System.Math.Min(System.Math.Max((p.Age - 22) / 18f, 0f), 1f); // 0 at 22, 1 at 40+
        int caTarget = caMin + (int)((caMax - caMin) * (0.35f + ageNorm * 0.50f));
        caTarget += rng.Range(-5, 6);
        caTarget = Clamp(caTarget, caMin, caMax);
        caTarget = System.Math.Min(caTarget, CACap);

        // PA uplift: younger founders get bigger PA distance from CA
        int maxUplift;
        if      (p.Age <= 22) maxUplift = 55;
        else if (p.Age <= 26) maxUplift = 45;
        else if (p.Age <= 30) maxUplift = 35;
        else if (p.Age <= 36) maxUplift = 22;
        else                  maxUplift = 12;

        int paTarget = caTarget + rng.Range(8, maxUplift + 1);
        paTarget = Clamp(paTarget, paMin, paMax);
        paTarget = System.Math.Min(paTarget, PACap);

        stats.PotentialAbility = paTarget;

        // ── Phase 2: Determine skill role band indices ────────────────────────
        int totalSkills = SkillIdHelper.SkillCount;
        var profile = archetype != null && roleProfileTable != null
            ? roleProfileTable.Get(archetype.Role)
            : (p.RoleProfile ?? (roleProfileTable?.Get(RoleId.SoftwareEngineer)));

        int[] primaryIndices;
        int[] secondaryIndices;
        int[] tertiaryIndices;

        BuildBandIndices(profile, archetype, totalSkills,
            out primaryIndices, out secondaryIndices, out tertiaryIndices);

        // ── Phase 3: Allocate skills via budget model (mirroring CandidateData pattern) ──
        // skill budget = caTarget in weighted units
        // Primary:   10 pts / level (weight 1.0)
        // Secondary:  6 pts / level (weight 0.6)
        // Tertiary:   3 pts / level (weight 0.3)

        // Skill cap ranges per Page 04 section 8.4
        int mainPrimaryMax = solo ? 18 : 16;
        int otherPrimaryMax = solo ? 16 : 14;
        int secondaryMax   = solo ? 14 : 12;
        int tertiaryMax    = solo ? 10 : 9;
        int ignoredMax     = solo ? 6  : 5;

        // Primary skills
        for (int p2 = 0; p2 < primaryIndices.Length; p2++)
        {
            int idx = primaryIndices[p2];
            int cap = (p2 == 0) ? mainPrimaryMax : otherPrimaryMax;
            int share = primaryIndices.Length > 0
                ? caTarget * 3 / (10 * primaryIndices.Length)
                : 0;
            int val = Clamp(share + rng.Range(-1, 2), 0, cap);
            stats.Skills[idx] = val;
        }

        // Secondary skills
        for (int s = 0; s < secondaryIndices.Length; s++)
        {
            int idx = secondaryIndices[s];
            int share = secondaryIndices.Length > 0
                ? caTarget * 2 / (10 * secondaryIndices.Length)
                : 0;
            int val = Clamp(share + rng.Range(-1, 2), 0, secondaryMax);
            stats.Skills[idx] = val;
        }

        // Tertiary skills
        for (int t = 0; t < tertiaryIndices.Length; t++)
        {
            int idx = tertiaryIndices[t];
            bool isIgnored = profile != null
                && idx < profile.SkillBands.Length
                && profile.SkillBands[idx] != RoleWeightBand.Primary
                && profile.SkillBands[idx] != RoleWeightBand.Secondary
                && profile.SkillBands[idx] != RoleWeightBand.Tertiary;
            int cap = isIgnored ? ignoredMax : tertiaryMax;
            int share = tertiaryIndices.Length > 0
                ? caTarget / (10 * tertiaryIndices.Length)
                : 0;
            int val = Clamp(share + rng.Range(-1, 1), 0, cap);
            stats.Skills[idx] = val;
        }

        // Apply archetype skill biases (SkillBiasProfile extra boost)
        if (archetype?.SkillBiasProfile != null)
        {
            for (int b = 0; b < archetype.SkillBiasProfile.Length; b++)
            {
                int si = (int)archetype.SkillBiasProfile[b];
                if (si >= 0 && si < totalSkills)
                    stats.Skills[si] = Clamp(stats.Skills[si] + rng.Range(1, 3), 0, MaxSkillValue);
            }
        }

        // Enforce: max one skill at 18
        EnforceSkillCap(stats.Skills, totalSkills);

        // ── Phase 4: Nudge primary skills to match CA target ─────────────────
        if (profile != null)
        {
            RoleWeightBand[] bands = profile.SkillBands;
            int actualCA = AbilityCalculator.ComputeRoleCA(stats.Skills, bands);
            int delta = actualCA - caTarget;

            if (delta > 5 && primaryIndices.Length > 0)
            {
                for (int i = primaryIndices.Length - 1; i >= 0 && delta > 5; i--)
                {
                    int idx = primaryIndices[i];
                    while (stats.Skills[idx] > 0 && delta > 5)
                    {
                        stats.Skills[idx]--;
                        delta--;
                    }
                }
            }
            else if (delta < -5 && primaryIndices.Length > 0)
            {
                int pIdx = primaryIndices[0];
                while (stats.Skills[pIdx] < MaxSkillValue && delta < -5)
                {
                    stats.Skills[pIdx]++;
                    delta++;
                }
            }
        }

        // ── Phase 5: Generate visible attributes ─────────────────────────────
        int attrCount = VisibleAttributeHelper.AttributeCount;

        // Base ranges per Page 04 section 9.1
        int attrCritMin = solo ? 11 : 10;
        int attrCritMax = solo ? 18 : 16;
        int attrUsefulMin = solo ? 8 : 7;
        int attrUsefulMax = solo ? 15 : 14;
        int attrNeutralMin = solo ? 5 : 5;
        int attrNeutralMax = solo ? 12 : 11;

        // Build bias set from archetype
        bool[] isBiasedAttr = new bool[attrCount];
        if (archetype?.AttributeBias != null)
        {
            for (int b = 0; b < archetype.AttributeBias.Length; b++)
            {
                int ai = (int)archetype.AttributeBias[b];
                if (ai >= 0 && ai < attrCount) isBiasedAttr[ai] = true;
            }
        }

        for (int i = 0; i < attrCount; i++)
        {
            int val;
            if (isBiasedAttr[i])
                val = rng.Range(attrCritMin, attrCritMax + 1);
            else
                val = rng.Range(attrNeutralMin, attrUsefulMax + 1);
            stats.SetVisibleAttribute((VisibleAttributeId)i, Clamp(val, 0, MaxAttributeValue));
        }

        // Apply personality style boosts and penalties
        if (personality != null)
        {
            int boostAmt = personality.BoostAmount > 0 ? personality.BoostAmount : 2;
            int penaltyAmt = personality.PenaltyAmount > 0 ? personality.PenaltyAmount : 2;

            VisibleAttributeId[] boostTargets = personality.BoostAttributes ?? personality.StrengthAttributes;
            if (boostTargets != null)
            {
                for (int b = 0; b < boostTargets.Length; b++)
                {
                    int ai = (int)boostTargets[b];
                    if (ai >= 0 && ai < attrCount)
                    {
                        int cur = stats.GetVisibleAttribute(boostTargets[b]);
                        stats.SetVisibleAttribute(boostTargets[b], Clamp(cur + boostAmt, 0, MaxAttributeValue));
                    }
                }
            }

            if (personality.WeakAttributes != null)
            {
                for (int w = 0; w < personality.WeakAttributes.Length; w++)
                {
                    int ai = (int)personality.WeakAttributes[w];
                    if (ai >= 0 && ai < attrCount)
                    {
                        int cur = stats.GetVisibleAttribute(personality.WeakAttributes[w]);
                        stats.SetVisibleAttribute(personality.WeakAttributes[w], Clamp(cur - penaltyAmt, 0, MaxAttributeValue));
                    }
                }
            }
        }

        // Apply weakness AffectedAttributes penalties
        if (weakness != null && weakness.AffectedAttributes != null && weakness.AttributeModifiers != null)
        {
            int modCount = System.Math.Min(weakness.AffectedAttributes.Length, weakness.AttributeModifiers.Length);
            for (int w = 0; w < modCount; w++)
            {
                int ai = (int)weakness.AffectedAttributes[w];
                if (ai >= 0 && ai < attrCount)
                {
                    int cur = stats.GetVisibleAttribute(weakness.AffectedAttributes[w]);
                    stats.SetVisibleAttribute(weakness.AffectedAttributes[w], Clamp(cur + weakness.AttributeModifiers[w], 0, MaxAttributeValue));
                }
            }
        }

        // Mandatory weakness: at least one visible attribute must be in the low range (3-9)
        EnforceMandatoryWeakness(stats, rng, archetype, attrCount, solo);

        // Enforce at most one attribute at 18
        EnforceAttributeCap(stats, attrCount);

        // ── Phase 6: Generate hidden attributes ───────────────────────────────
        int[] hidden = GenerateHiddenAttributes(p, rng);
        for (int h = 0; h < hidden.Length && h < HiddenAttributeHelper.AttributeCount; h++)
            stats.SetHiddenAttribute((HiddenAttributeId)h, hidden[h]);

        // Final: clamp all skills
        for (int i = 0; i < totalSkills; i++)
            stats.Skills[i] = Clamp(stats.Skills[i], 0, MaxSkillValue);

        return stats;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hidden attribute generation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates the 7 hidden attributes for a founder with archetype/personality bias.
    /// Ranges from Page 04 section 10.2.
    /// </summary>
    public static int[] GenerateHiddenAttributes(FounderGenerationParams p, IRng rng)
    {
        var archetype = p.Archetype;
        var personality = p.PersonalityStyle;
        int count = HiddenAttributeHelper.AttributeCount; // 7

        // Ranges per Page 04 section 10.2
        //  LearningRate:      6–18
        //  Ambition:          9–20
        //  Loyalty:          13–20
        //  PressureTolerance: 5–18
        //  Ego:               3–17
        //  Consistency:       5–18
        //  Mentoring:         3–18
        int[] minValues = { 6,  9,  13, 5,  3,  5,  3  };
        int[] maxValues = { 18, 20, 20, 18, 17, 18, 18 };

        // Build bias set: biased attributes get +2 to min
        bool[] biased = new bool[count];
        if (archetype?.HiddenBias != null)
        {
            for (int b = 0; b < archetype.HiddenBias.Length; b++)
            {
                int hi = (int)archetype.HiddenBias[b];
                if (hi >= 0 && hi < count) biased[hi] = true;
            }
        }
        if (personality?.HiddenTendencies != null)
        {
            for (int b = 0; b < personality.HiddenTendencies.Length; b++)
            {
                int hi = (int)personality.HiddenTendencies[b];
                if (hi >= 0 && hi < count) biased[hi] = true;
            }
        }

        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            int lo = biased[i] ? minValues[i] + 2 : minValues[i];
            int hi = maxValues[i];
            result[i] = rng.Range(lo, hi + 1);
        }

        // Apply personality HiddenAttributeModifiers if populated
        if (personality?.HiddenAttributeModifiers != null)
        {
            int modCount = System.Math.Min(personality.HiddenAttributeModifiers.Length, count);
            for (int i = 0; i < modCount; i++)
            {
                result[i] = Clamp(result[i] + personality.HiddenAttributeModifiers[i], minValues[i], maxValues[i]);
            }
        }

        // Loyalty floor: always 13+ for founders
        int loyaltyIdx = (int)HiddenAttributeId.Loyalty;
        if (result[loyaltyIdx] < 13) result[loyaltyIdx] = 13;

        // Clamp all
        for (int i = 0; i < count; i++)
            result[i] = Clamp(result[i], 1, 20);

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildBandIndices(
        RoleProfileDefinition profile,
        FounderArchetypeDefinition archetype,
        int totalSkills,
        out int[] primaryIndices,
        out int[] secondaryIndices,
        out int[] tertiaryIndices)
    {
        if (profile != null)
        {
            int pCount = 0, sCount = 0, tCount = 0;
            for (int i = 0; i < totalSkills; i++)
            {
                switch (profile.SkillBands[i])
                {
                    case RoleWeightBand.Primary:   pCount++; break;
                    case RoleWeightBand.Secondary: sCount++; break;
                    default:                       tCount++; break;
                }
            }
            primaryIndices   = new int[pCount];
            secondaryIndices = new int[sCount];
            tertiaryIndices  = new int[tCount];
            int pi = 0, si = 0, ti = 0;
            for (int i = 0; i < totalSkills; i++)
            {
                switch (profile.SkillBands[i])
                {
                    case RoleWeightBand.Primary:   primaryIndices[pi++]   = i; break;
                    case RoleWeightBand.Secondary: secondaryIndices[si++] = i; break;
                    default:                       tertiaryIndices[ti++]  = i; break;
                }
            }
        }
        else if (archetype != null)
        {
            // Fall back to archetype PrimarySkillBiases / SecondarySkillBiases
            int pCount = archetype.PrimarySkillBiases?.Length ?? 0;
            int sCount = archetype.SecondarySkillBiases?.Length ?? 0;
            primaryIndices   = new int[pCount];
            secondaryIndices = new int[sCount];

            for (int i = 0; i < pCount; i++)
                primaryIndices[i] = (int)archetype.PrimarySkillBiases[i];
            for (int i = 0; i < sCount; i++)
                secondaryIndices[i] = (int)archetype.SecondarySkillBiases[i];

            // Tertiary = everything else
            bool[] covered = new bool[totalSkills];
            for (int i = 0; i < pCount; i++) covered[primaryIndices[i]]   = true;
            for (int i = 0; i < sCount; i++) covered[secondaryIndices[i]] = true;
            int tCount = 0;
            for (int i = 0; i < totalSkills; i++) if (!covered[i]) tCount++;
            tertiaryIndices = new int[tCount];
            int ti = 0;
            for (int i = 0; i < totalSkills; i++) if (!covered[i]) tertiaryIndices[ti++] = i;
        }
        else
        {
            primaryIndices   = new int[0];
            secondaryIndices = new int[totalSkills];
            tertiaryIndices  = new int[0];
            for (int i = 0; i < totalSkills; i++) secondaryIndices[i] = i;
        }
    }

    private static void EnforceSkillCap(int[] skills, int count)
    {
        // Max one skill at 18; others capped at 17
        bool foundMax = false;
        for (int i = 0; i < count; i++)
        {
            if (skills[i] >= MaxSkillValue)
            {
                if (!foundMax)
                {
                    skills[i] = MaxSkillValue;
                    foundMax = true;
                }
                else
                {
                    skills[i] = MaxSkillValue - 1;
                }
            }
        }
    }

    private static void EnforceAttributeCap(EmployeeStatBlock stats, int count)
    {
        // At most one visible attribute at 18
        bool foundMax = false;
        for (int i = 0; i < count; i++)
        {
            int v = stats.GetVisibleAttribute((VisibleAttributeId)i);
            if (v >= MaxAttributeValue)
            {
                if (!foundMax)
                {
                    stats.SetVisibleAttribute((VisibleAttributeId)i, MaxAttributeValue);
                    foundMax = true;
                }
                else
                {
                    stats.SetVisibleAttribute((VisibleAttributeId)i, MaxAttributeValue - 1);
                }
            }
        }
    }

    private static void EnforceMandatoryWeakness(
        EmployeeStatBlock stats,
        IRng rng,
        FounderArchetypeDefinition archetype,
        int attrCount,
        bool solo)
    {
        // If no attribute is already in the 3-9 range, force one low
        bool hasWeakness = false;
        int weaknessMax = solo ? 9 : 8;
        for (int i = 0; i < attrCount; i++)
        {
            if (stats.GetVisibleAttribute((VisibleAttributeId)i) <= weaknessMax)
            {
                hasWeakness = true;
                break;
            }
        }

        if (!hasWeakness)
        {
            // Pick a weakness risk attribute from archetype, or fallback to a neutral one
            int targetAttr = -1;
            if (archetype?.WeaknessRiskAttributes != null && archetype.WeaknessRiskAttributes.Length > 0)
            {
                int pick = rng.Range(0, archetype.WeaknessRiskAttributes.Length);
                targetAttr = (int)archetype.WeaknessRiskAttributes[pick];
            }
            else
            {
                // Pick the highest non-biased attribute and reduce it
                int highest = -1;
                int highestVal = -1;
                bool[] biased = new bool[attrCount];
                if (archetype?.AttributeBias != null)
                    for (int b = 0; b < archetype.AttributeBias.Length; b++)
                        biased[(int)archetype.AttributeBias[b]] = true;
                for (int i = 0; i < attrCount; i++)
                {
                    if (!biased[i] && stats.GetVisibleAttribute((VisibleAttributeId)i) > highestVal)
                    {
                        highestVal = stats.GetVisibleAttribute((VisibleAttributeId)i);
                        highest = i;
                    }
                }
                targetAttr = highest >= 0 ? highest : rng.Range(0, attrCount);
            }

            if (targetAttr >= 0 && targetAttr < attrCount)
            {
                int weakVal = rng.Range(3, weaknessMax + 1);
                stats.SetVisibleAttribute((VisibleAttributeId)targetAttr, weakVal);
            }
        }
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
