using System;
using System.Collections.Generic;

public class CandidateData
{
    public int CandidateId;
    public string Name;
    public Gender Gender;
    public int Age;
    public int Salary;
    public RoleId Role;

    // Hiring pipeline fields (set post-generation)
    public int InterviewStage;
    public int ExpiryTick;
    public bool IsTimerPaused;
    public bool IsHeadhunted;   // kept for backward compat; prefer IsTargeted
    public bool IsTargeted;     // set by HRSystem on delivery
    public TeamId SourcingTeamId;   // set by HRSystem on candidate delivery; default = 0 (not HR-sourced)
    public bool IsPendingReview; // true = HR-delivered, awaiting player accept/decline before entering pool
    public int SourceTier;      // 0 = Basic, 1 = Standard, 2 = Executive
    public int HRSkill;         // only populated for Role == HrSpecialist

    // Follow-up / patience state
    public bool HasSentFollowUp;        // true once the follow-up inbox message is sent
    public int FollowUpSentTick;        // tick when the follow-up was sent
    public int WithdrawalDeadlineTick;  // set when follow-up sends; candidate withdraws at this tick
    public int LastOfferTick;           // tick when the last offer was made; 0 = no offer sent

    // CA/PA fields
    public int CurrentAbility;             // set to ca at generation; 0 = lazy-compute fallback on old saves
    public Personality personality;

    // Employment preference fields — generated in GenerateCandidate, revealed via interview/HR
    public CandidatePreferences Preferences;
    public bool PreferencesRevealed;   // true = player has seen exact FT/PT + length preference
    public bool PreferencesHinted;     // true = player has seen a vague hint about preferences

    // Stat model
    public EmployeeStatBlock Stats;
    public ConfidenceLevel[] SkillConfidence;           // length 26, per-skill confidence (legacy; use Confidence for grouped)
    public ConfidenceLevel[] VisibleAttributeConfidence; // length 8
    public ConfidenceLevel[] HiddenAttributeConfidence;  // length 7

    // ── Wave 4B: new generation fields ───────────────────────────────────────
    public CandidateSource Source;
    public CandidateArchetype Archetype;
    public CareerStage CareerStage;
    public RoleId TargetRole;
    public RoleFamily RoleFamily;
    public CandidateConfidenceData Confidence;
    public CandidateReport Report;
    public int SalaryDemandActual;          // true salary (hidden from player until confirmed)
    public int SalaryEstimateMin;           // displayed estimate range min
    public int SalaryEstimateMax;           // displayed estimate range max
    public bool CompetitorInterest;
    public List<RoleId> ProjectedRoleFits;  // roles this candidate could also fit

    public int GetSkill(SkillId id) => Stats.GetSkill(id);

    // Compute salary that this candidate demands.
    public int ComputeSalary()
    {
        return SalaryDemandCalculator.ComputeDemand(this);
    }

    // 0–100, pure computed — no RNG, no alloc
    public int RoleFitScore => RoleRelevantAverage;

    // Returns the top-3 skill average using the role's primary+secondary skill bands.
    // Requires a RoleProfileTable to be provided; falls back to zero if unavailable.
    public int ComputeRoleRelevantAverage(RoleProfileTable roleProfileTable)
    {
        if (roleProfileTable == null) return 0;
        var profile = roleProfileTable.Get(Role);
        if (profile == null) return 0;

        int primary = 0;
        int secondary = 0;
        int third = 0;
        int skillCount = SkillIdHelper.SkillCount;
        for (int i = 0; i < skillCount; i++)
        {
            var band = profile.SkillBands[i];
            if (band != RoleWeightBand.Primary && band != RoleWeightBand.Secondary) continue;
            int val = Stats.GetSkill((SkillId)i);
            if (val > primary) { third = secondary; secondary = primary; primary = val; }
            else if (val > secondary) { third = secondary; secondary = val; }
            else if (val > third) { third = val; }
        }
        return (primary + secondary + third) / 3;
    }

    public int RoleRelevantAverage
    {
        get
        {
            // Fallback: use highest 3 skills when no profile table available
            int s0 = 0, s1 = 0, s2 = 0;
            int count = SkillIdHelper.SkillCount;
            for (int i = 0; i < count; i++)
            {
                int val = Stats.Skills[i];
                if (val > s0) { s2 = s1; s1 = s0; s0 = val; }
                else if (val > s1) { s2 = s1; s1 = val; }
                else if (val > s2) { s2 = val; }
            }
            return (s0 + s1 + s2) / 3;
        }
    }

    public int AverageSkill => RoleRelevantAverage;

    public SkillTier SuggestedSkillTier
    {
        get
        {
            int avg = RoleRelevantAverage;
            if (avg >= 14) return SkillTier.Master;
            if (avg >= 10) return SkillTier.Expert;
            if (avg >= 6)  return SkillTier.Competent;
            return SkillTier.Apprentice;
        }
    }

    public string SkillLevel
    {
        get
        {
            switch (SuggestedSkillTier)
            {
                case SkillTier.Master: return "Master";
                case SkillTier.Expert: return "Expert";
                case SkillTier.Competent: return "Competent";
                default: return "Apprentice";
            }
        }
    }

    // ── New primary generation entry point (16-step pipeline) ─────────────────
    public static CandidateData GenerateCandidate(
        IRng rng,
        RoleProfileTable roleProfileTable,
        CandidateGenerationParams genParams)
    {
        // Step 1: Pick source
        CandidateSource source = genParams.Source;

        // Step 2: Pick target role
        RoleId role;
        if (genParams.ForceRole.HasValue)
        {
            role = genParams.ForceRole.Value;
        }
        else
        {
            role = PickWeightedRole(rng, roleProfileTable, genParams);
        }

        // Step 3: Determine role family from role
        RoleFamily roleFamily = genParams.ForceFamily ?? RoleIdHelper.GetFamily(role);

        // Step 4: Pick archetype
        CandidateArchetype archetype = genParams.ForceArchetype ?? PickArchetypeForSource(rng, source);

        // Step 5: Pick career stage (may be overridden later after age is rolled)
        // We roll age first to allow stage derivation, then use stage for CA target
        int rawAge = rng.Range(18, 66);
        CareerStage careerStage = genParams.ForceCareerStage ?? CareerStageHelper.FromAge(rawAge, rng);
        CareerStageData stageData = CareerStageHelper.GetData(careerStage);

        // Clamp age to stage range
        int age = Clamp(rawAge, stageData.AgeMin, stageData.AgeMax);

        // Step 6: Generate CA target using career stage bands + quality multiplier
        float q = genParams.QualityMultiplier;
        int caRangeMin = stageData.CAMin;
        int caRangeMax = stageData.CAMax;

        // Quality multiplier shifts distribution within stage band
        float qBias = (q - 1.0f) * 0.5f; // -0.5 to +0.5 range bias
        int caTarget = rng.Range(caRangeMin, caRangeMax + 1);
        // Apply quality bias: pull toward high end if q > 1, low end if q < 1
        int caBiased = Clamp((int)(caTarget + (caRangeMax - caRangeMin) * qBias), caRangeMin, caRangeMax);
        int ca = caBiased;

        // Step 7: Generate PA target using career stage PA margin, clamp to [CA, 200]
        int paMargin = rng.Range(stageData.PAMarginMin, stageData.PAMarginMax + 1);
        if (paMargin < 0) paMargin = 0; // ensure PA >= CA floor
        int pa = Clamp(ca + paMargin, ca, 200);

        // RawTalent archetype: boost PA significantly
        if (archetype == CandidateArchetype.RawTalent)
        {
            int extraPa = rng.Range(20, 50);
            pa = Clamp(pa + extraPa, pa, 200);
        }

        // Step 8: Generate role-weighted skills with archetype modifiers
        var stats = EmployeeStatBlock.Create();
        stats.PotentialAbility = pa;

        RoleProfileDefinition profile = roleProfileTable?.Get(role);
        int totalSkills = SkillIdHelper.SkillCount;

        int[] primaryIndices;
        int[] secondaryIndices;
        int[] tertiaryIndices;

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
        else
        {
            primaryIndices   = new int[0];
            secondaryIndices = new int[totalSkills];
            tertiaryIndices  = new int[0];
            for (int i = 0; i < totalSkills; i++) secondaryIndices[i] = i;
        }

        // Archetype modifiers for skill distribution
        GetArchetypeSkillModifiers(archetype, out float primaryMod, out float secondaryMod, out float tertiaryMod);

        int skillBudget = ca;

        // Primary allocation
        int pLen = primaryIndices.Length;
        for (int p = 0; p < pLen; p++)
        {
            int idx = primaryIndices[p];
            int share = pLen > 0 ? (int)(skillBudget * 3 * primaryMod / (10f * pLen)) : 0;
            stats.Skills[idx] = Clamp(share + rng.Range(-1, 2), 0, 20);
        }

        // Secondary allocation
        int sLen = secondaryIndices.Length;
        for (int s = 0; s < sLen; s++)
        {
            int idx = secondaryIndices[s];
            int share = sLen > 0 ? (int)(skillBudget * 2 * secondaryMod / (10f * sLen)) : 0;
            stats.Skills[idx] = Clamp(share + rng.Range(-1, 2), 0, 20);
        }

        // Tertiary allocation
        int tLen = tertiaryIndices.Length;
        for (int t = 0; t < tLen; t++)
        {
            int idx = tertiaryIndices[t];
            int share = tLen > 0 ? (int)(skillBudget * tertiaryMod / (10f * tLen)) : 0;
            stats.Skills[idx] = Clamp(share + rng.Range(-1, 1), 0, 20);
        }

        // CommercialClimber: boost sales/marketing/negotiation skills
        if (archetype == CandidateArchetype.CommercialClimber)
        {
            ApplyCommercialClimberBoost(stats, rng);
        }

        // Compute actual CA and nudge primary skills to match target
        RoleWeightBand[] skillBandsForCA = profile != null ? profile.SkillBands : null;
        int actualCA = skillBandsForCA != null
            ? AbilityCalculator.ComputeRoleCA(stats.Skills, skillBandsForCA)
            : 0;
        int delta = actualCA - ca;

        if (delta > 5 && pLen > 0)
        {
            for (int p = pLen - 1; p >= 0 && delta > 5; p--)
            {
                int idx = primaryIndices[p];
                while (stats.Skills[idx] > 0 && delta > 5) { stats.Skills[idx]--; delta--; }
            }
        }
        else if (delta < -5 && pLen > 0)
        {
            int pIdx = primaryIndices[0];
            while (stats.Skills[pIdx] < 20 && delta < -5) { stats.Skills[pIdx]++; delta++; }
        }

        // 5% chance cross-skill spike on a non-role skill
        if (rng.Range(0, 100) < 5)
        {
            int crossIdx = rng.Range(0, totalSkills);
            bool isRoleSkill = false;
            for (int p = 0; p < pLen && !isRoleSkill; p++) if (primaryIndices[p] == crossIdx) isRoleSkill = true;
            for (int s = 0; s < sLen && !isRoleSkill; s++) if (secondaryIndices[s] == crossIdx) isRoleSkill = true;
            if (!isRoleSkill && stats.Skills[crossIdx] < 18)
                stats.Skills[crossIdx] = Clamp(stats.Skills[crossIdx] + rng.Range(2, 5), 0, 20);
        }

        int computedCA = skillBandsForCA != null
            ? AbilityCalculator.ComputeRoleCA(stats.Skills, skillBandsForCA)
            : actualCA;

        // Step 9: Generate all 8 visible attributes using role profile AttributeBands + archetype bias
        GenerateVisibleAttributes(stats, profile, archetype, pa, rng);

        // Step 10: Generate 7 hidden attributes with archetype-driven bias
        GenerateHiddenAttributes(stats, archetype, pa, rng);

        // Step 11: Compute projected role fits (secondary roles with decent fit)
        var projectedFits = ComputeProjectedRoleFits(stats, role, roleProfileTable);

        // Step 12: Calculate salary demand with confidence-based estimate
        CandidateConfidenceData confidence = CandidateConfidenceData.FromSource(source);
        int actualSalary    = SalaryDemandCalculator.ComputeDemand(role, stats, computedCA, age);
        var (estimateMin, estimateMax) = SalaryDemandCalculator.ComputeEstimateRange(actualSalary, confidence.SalaryConfidence, archetype);

        // Apply archetype salary modifier
        actualSalary = ApplyArchetypeSalaryModifier(actualSalary, archetype);
        estimateMin  = ApplyArchetypeSalaryModifier(estimateMin, archetype);
        estimateMax  = ApplyArchetypeSalaryModifier(estimateMax, archetype);

        // Step 13: Competitor interest — 10% chance for high CA candidates
        bool competitorInterest = computedCA >= 120 && rng.Range(0, 100) < 10;

        // Step 14: Generate gender and name
        int genderIndex = rng.Range(0, 2);
        Gender gender = (Gender)genderIndex;
        string name = NameGenerator.GenerateRandomName(rng, gender);

        // Step 15: Preference generation (carried over from previous implementation)
        bool isCoreRole = role == RoleId.SoftwareEngineer || role == RoleId.ProductDesigner || role == RoleId.QaEngineer;
        CandidatePreferences preferences = GeneratePreferences(rng, age, isCoreRole);

        // Step 16: Assemble candidate
        var candidateData = new CandidateData
        {
            Name             = name,
            Gender           = gender,
            Age              = age,
            Role             = role,
            TargetRole       = role,
            RoleFamily       = roleFamily,
            CurrentAbility   = computedCA,
            Stats            = stats,
            Source           = source,
            Archetype        = archetype,
            CareerStage      = careerStage,
            Confidence       = confidence,
            SalaryDemandActual = actualSalary,
            SalaryEstimateMin  = estimateMin,
            SalaryEstimateMax  = estimateMax,
            CompetitorInterest = competitorInterest,
            ProjectedRoleFits  = projectedFits,
            Preferences      = preferences,
        };

        candidateData.Salary      = actualSalary;
        candidateData.personality = PersonalitySystem.GeneratePersonality(rng);

        // Generate report (cached on candidate; updated after interviews)
        candidateData.Report = CandidateReportGenerator.Generate(candidateData, roleProfileTable);

        return candidateData;
    }

    // ── Backward-compatible wrapper (old callers during incremental migration) ──
    public static CandidateData GenerateCandidate(IRng rng, RoleProfileTable roleProfileTable, float qualityMultiplier = 1.0f, RoleId? forceRole = null)
    {
        var genParams = new CandidateGenerationParams
        {
            Source            = CandidateSource.OpenMarket,
            ForceRole         = forceRole,
            QualityMultiplier = qualityMultiplier
        };
        return GenerateCandidate(rng, roleProfileTable, genParams);
    }

    // ── Backward-compatible wrapper without table ─────────────────────────────
    public static CandidateData GenerateCandidate(IRng rng, float qualityMultiplier = 1.0f, RoleId? forceRole = null)
    {
        return GenerateCandidate(rng, null, qualityMultiplier, forceRole);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static RoleId PickWeightedRole(IRng rng, RoleProfileTable roleProfileTable, CandidateGenerationParams genParams)
    {
        int roleCount = RoleIdHelper.RoleCount;

        // Build weight array
        float[] weights = new float[roleCount];
        float totalWeight = 0f;
        for (int i = 0; i < roleCount; i++)
        {
            RoleId candidate = (RoleId)i;

            // Skip roles not in forced family
            if (genParams.ForceFamily.HasValue && RoleIdHelper.GetFamily(candidate) != genParams.ForceFamily.Value)
            {
                weights[i] = 0f;
                continue;
            }

            float w = 1.0f;
            // Use profile pool weight if available
            if (roleProfileTable != null && roleProfileTable.HasProfile(candidate))
                w = roleProfileTable.Get(candidate).CandidatePoolWeight;
            // Apply override if provided
            if (genParams.RoleWeightOverrides != null && i < genParams.RoleWeightOverrides.Length)
                w *= genParams.RoleWeightOverrides[i];

            weights[i] = w > 0f ? w : 0f;
            totalWeight += weights[i];
        }

        if (totalWeight <= 0f)
            return (RoleId)rng.Range(0, roleCount);

        // Pick via CDF
        float pick = rng.Range(0, (int)(totalWeight * 1000)) / 1000f;
        float cumulative = 0f;
        for (int i = 0; i < roleCount; i++)
        {
            cumulative += weights[i];
            if (pick < cumulative) return (RoleId)i;
        }
        return (RoleId)(roleCount - 1);
    }

    private static CandidateArchetype PickArchetypeForSource(IRng rng, CandidateSource source)
    {
        // Archetype weights vary by source (Page 05 section 7.1)
        switch (source)
        {
            case CandidateSource.HRSearch:
                // HR search tends to find more specialists and reliable workers
                return PickFromWeights(rng, new int[] { 25, 15, 5, 5, 20, 8, 8, 5, 5, 4 });

            case CandidateSource.StartingPool:
                // Starting pool is generalist-heavy
                return PickFromWeights(rng, new int[] { 10, 30, 15, 5, 20, 5, 5, 5, 3, 2 });

            case CandidateSource.Referral:
                // Referrals lean specialist or reliable
                return PickFromWeights(rng, new int[] { 20, 15, 8, 8, 20, 8, 10, 5, 4, 2 });

            case CandidateSource.FormerEmployee:
                // Former employees are reliable or mentors
                return PickFromWeights(rng, new int[] { 15, 15, 5, 5, 25, 5, 20, 5, 3, 2 });

            case CandidateSource.CompetitorLayoff:
                // Layoffs produce specialists and difficult stars
                return PickFromWeights(rng, new int[] { 25, 10, 5, 20, 10, 8, 5, 8, 5, 4 });

            default: // OpenMarket: uniform-ish
                return PickFromWeights(rng, new int[] { 15, 15, 12, 8, 15, 10, 8, 7, 5, 5 });
        }
    }

    // weights maps 1:1 to CandidateArchetype enum values
    private static CandidateArchetype PickFromWeights(IRng rng, int[] weights)
    {
        int total = 0;
        int count = weights.Length;
        for (int i = 0; i < count; i++) total += weights[i];
        int roll = rng.Range(0, total);
        int cumulative = 0;
        for (int i = 0; i < count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return (CandidateArchetype)i;
        }
        return CandidateArchetype.Generalist;
    }

    // Returns (primaryMod, secondaryMod, tertiaryMod) skill multipliers per archetype (Page 05 section 11.3)
    private static void GetArchetypeSkillModifiers(CandidateArchetype archetype,
        out float primaryMod, out float secondaryMod, out float tertiaryMod)
    {
        switch (archetype)
        {
            case CandidateArchetype.Specialist:
                primaryMod = 1.3f; secondaryMod = 0.9f; tertiaryMod = 0.6f;
                break;
            case CandidateArchetype.Generalist:
                primaryMod = 0.9f; secondaryMod = 1.1f; tertiaryMod = 1.1f;
                break;
            case CandidateArchetype.RawTalent:
                primaryMod = 0.9f; secondaryMod = 0.9f; tertiaryMod = 0.8f;
                break;
            case CandidateArchetype.DifficultStar:
                primaryMod = 1.4f; secondaryMod = 0.8f; tertiaryMod = 0.5f;
                break;
            case CandidateArchetype.ReliableWorker:
                primaryMod = 1.0f; secondaryMod = 1.0f; tertiaryMod = 0.9f;
                break;
            case CandidateArchetype.CreativeRisk:
                primaryMod = 1.1f; secondaryMod = 1.1f; tertiaryMod = 0.7f;
                break;
            case CandidateArchetype.Mentor:
                primaryMod = 1.1f; secondaryMod = 1.2f; tertiaryMod = 1.0f;
                break;
            case CandidateArchetype.PressurePlayer:
                primaryMod = 1.0f; secondaryMod = 1.0f; tertiaryMod = 0.9f;
                break;
            case CandidateArchetype.CommercialClimber:
                primaryMod = 0.9f; secondaryMod = 0.9f; tertiaryMod = 0.8f;
                break;
            case CandidateArchetype.StableOperator:
                primaryMod = 1.0f; secondaryMod = 1.0f; tertiaryMod = 1.0f;
                break;
            default:
                primaryMod = 1.0f; secondaryMod = 1.0f; tertiaryMod = 1.0f;
                break;
        }
    }

    private static void ApplyCommercialClimberBoost(EmployeeStatBlock stats, IRng rng)
    {
        // Boost Sales, Marketing, Negotiation skills
        int[] commercialIndices = new int[]
        {
            (int)SkillId.Marketing,
            (int)SkillId.Sales,
            (int)SkillId.Negotiation
        };
        for (int ci = 0; ci < commercialIndices.Length; ci++)
        {
            int idx = commercialIndices[ci];
            stats.Skills[idx] = Clamp(stats.Skills[idx] + rng.Range(2, 5), 0, 20);
        }
    }

    // Step 9: Generate all 8 visible attributes using role profile AttributeBands + archetype bias
    private static void GenerateVisibleAttributes(EmployeeStatBlock stats, RoleProfileDefinition profile, CandidateArchetype archetype, int pa, IRng rng)
    {
        int attrCount = VisibleAttributeHelper.AttributeCount;
        int paFloor = pa / 20;
        if (paFloor < 1) paFloor = 1;
        if (paFloor > 10) paFloor = 10;
        int spread = (20 - paFloor) / 2 + 1;

        for (int i = 0; i < attrCount; i++)
        {
            int baseVal;
            if (profile != null)
            {
                AttributeWeightBand band = profile.AttributeBands[i];
                switch (band)
                {
                    case AttributeWeightBand.Critical:
                        baseVal = Clamp(rng.Range(paFloor + 3, paFloor + spread + 4), 6, 20);
                        break;
                    case AttributeWeightBand.Useful:
                        baseVal = Clamp(rng.Range(paFloor + 1, paFloor + spread + 2), 4, 18);
                        break;
                    case AttributeWeightBand.Minor:
                        baseVal = Clamp(rng.Range(paFloor, paFloor + spread + 1), 2, 16);
                        break;
                    default: // Irrelevant
                        baseVal = Clamp(rng.Range(1, spread + 1), 1, 12);
                        break;
                }
            }
            else
            {
                baseVal = Clamp(rng.Range(paFloor, paFloor + spread + 1) + rng.Range(-1, 2), 1, 20);
            }

            // Archetype biases
            baseVal = ApplyArchetypeAttributeBias(baseVal, (VisibleAttributeId)i, archetype, rng);
            stats.VisibleAttributes[i] = Clamp(baseVal, 1, 20);
        }
    }

    private static int ApplyArchetypeAttributeBias(int val, VisibleAttributeId attrId, CandidateArchetype archetype, IRng rng)
    {
        switch (archetype)
        {
            case CandidateArchetype.DifficultStar:
                // High primary ability but lower communication and adaptability
                if (attrId == VisibleAttributeId.Communication || attrId == VisibleAttributeId.Adaptability)
                    return Clamp(val - rng.Range(2, 5), 1, 20);
                break;
            case CandidateArchetype.CreativeRisk:
                if (attrId == VisibleAttributeId.Creativity)
                    return Clamp(val + rng.Range(2, 5), 1, 20);
                break;
            case CandidateArchetype.Mentor:
                if (attrId == VisibleAttributeId.Leadership || attrId == VisibleAttributeId.Communication)
                    return Clamp(val + rng.Range(2, 4), 1, 20);
                break;
            case CandidateArchetype.PressurePlayer:
                if (attrId == VisibleAttributeId.Composure || attrId == VisibleAttributeId.Focus)
                    return Clamp(val + rng.Range(2, 4), 1, 20);
                break;
            case CandidateArchetype.ReliableWorker:
                if (attrId == VisibleAttributeId.WorkEthic)
                    return Clamp(val + rng.Range(1, 3), 1, 20);
                break;
            case CandidateArchetype.CommercialClimber:
                if (attrId == VisibleAttributeId.Initiative || attrId == VisibleAttributeId.Communication)
                    return Clamp(val + rng.Range(1, 3), 1, 20);
                break;
        }
        return val;
    }

    // Step 10: Generate 7 hidden attributes with archetype-driven bias (Page 05 section 13.2)
    private static void GenerateHiddenAttributes(EmployeeStatBlock stats, CandidateArchetype archetype, int pa, IRng rng)
    {
        int floor = pa / 20;
        if (floor < 1) floor = 1;
        if (floor > 10) floor = 10;
        int spread = (20 - floor) / 2 + 1;

        int BaseAttr() => Clamp(rng.Range(floor, floor + spread + 1) + rng.Range(-1, 2), 1, 20);

        int learning  = BaseAttr();
        int ambition  = BaseAttr();
        int loyalty   = BaseAttr();
        int pressure  = BaseAttr();
        int ego       = BaseAttr();
        int consist   = BaseAttr();
        int mentoring = BaseAttr();

        // Archetype biases on hidden attributes
        switch (archetype)
        {
            case CandidateArchetype.DifficultStar:
                ambition = Clamp(ambition + rng.Range(3, 7), 1, 20);
                ego      = Clamp(ego      + rng.Range(3, 7), 1, 20);
                loyalty  = Clamp(loyalty  - rng.Range(1, 4), 1, 20);
                break;
            case CandidateArchetype.RawTalent:
                learning = Clamp(learning + rng.Range(3, 7), 1, 20);
                ambition = Clamp(ambition + rng.Range(2, 5), 1, 20);
                break;
            case CandidateArchetype.Mentor:
                mentoring = Clamp(mentoring + rng.Range(3, 7), 1, 20);
                loyalty   = Clamp(loyalty   + rng.Range(2, 4), 1, 20);
                break;
            case CandidateArchetype.PressurePlayer:
                pressure = Clamp(pressure + rng.Range(3, 6), 1, 20);
                consist  = Clamp(consist  + rng.Range(2, 4), 1, 20);
                break;
            case CandidateArchetype.ReliableWorker:
                consist  = Clamp(consist  + rng.Range(2, 4), 1, 20);
                loyalty  = Clamp(loyalty  + rng.Range(1, 3), 1, 20);
                ego      = Clamp(ego      - rng.Range(1, 3), 1, 20);
                break;
            case CandidateArchetype.CommercialClimber:
                ambition = Clamp(ambition + rng.Range(2, 5), 1, 20);
                ego      = Clamp(ego      + rng.Range(1, 3), 1, 20);
                break;
            case CandidateArchetype.CreativeRisk:
                consist  = Clamp(consist  - rng.Range(1, 3), 1, 20);
                learning = Clamp(learning + rng.Range(1, 3), 1, 20);
                break;
            case CandidateArchetype.Generalist:
                // No strong biases — all moderate
                break;
        }

        stats.HiddenAttributes[(int)HiddenAttributeId.LearningRate]    = learning;
        stats.HiddenAttributes[(int)HiddenAttributeId.Ambition]        = ambition;
        stats.HiddenAttributes[(int)HiddenAttributeId.Loyalty]         = loyalty;
        stats.HiddenAttributes[(int)HiddenAttributeId.PressureTolerance] = pressure;
        stats.HiddenAttributes[(int)HiddenAttributeId.Ego]             = ego;
        stats.HiddenAttributes[(int)HiddenAttributeId.Consistency]     = consist;
        stats.HiddenAttributes[(int)HiddenAttributeId.Mentoring]       = mentoring;
    }

    // Step 11: Compute projected role fits — roles where computed CA would also be respectable
    private static List<RoleId> ComputeProjectedRoleFits(EmployeeStatBlock stats, RoleId primaryRole, RoleProfileTable roleProfileTable)
    {
        var fits = new List<RoleId>();
        if (roleProfileTable == null) return fits;

        int roleCount = RoleIdHelper.RoleCount;
        for (int i = 0; i < roleCount; i++)
        {
            RoleId candidateRole = (RoleId)i;
            if (candidateRole == primaryRole) continue;
            var altProfile = roleProfileTable.Get(candidateRole);
            if (altProfile == null) continue;
            int altCA = AbilityCalculator.ComputeRoleCA(stats.Skills, altProfile.SkillBands);
            if (altCA >= 60) // minimum threshold for a viable fit
                fits.Add(candidateRole);
        }
        return fits;
    }

    private static int ApplyArchetypeSalaryModifier(int salary, CandidateArchetype archetype)
    {
        float mod;
        switch (archetype)
        {
            case CandidateArchetype.DifficultStar:     mod = 1.20f; break;
            case CandidateArchetype.CommercialClimber: mod = 1.10f; break;
            case CandidateArchetype.Specialist:        mod = 1.05f; break;
            case CandidateArchetype.ReliableWorker:    mod = 0.95f; break;
            case CandidateArchetype.RawTalent:         mod = 0.90f; break;
            case CandidateArchetype.StableOperator:    mod = 0.95f; break;
            default:                                   mod = 1.00f; break;
        }
        return SalaryDemandCalculator.Round50(salary * mod);
    }

    private static CandidatePreferences GeneratePreferences(IRng rng, int age, bool isCoreRole)
    {
        int wFT   = isCoreRole ? 65 : 40;
        int wFlex = isCoreRole ? 20 : 35;
        int wPT   = isCoreRole ? 15 : 25;

        if (age > 40)
        {
            int shift = 8;
            wFT  += shift;
            wPT   = wPT > shift ? wPT - shift : 0;
        }
        else if (age < 28)
        {
            int shift = 7;
            wPT  += shift;
            wFT   = wFT > shift ? wFT - shift : 0;
        }

        int ftPtTotal = wFT + wFlex + wPT;
        int ftPtRoll = rng.Range(0, ftPtTotal);
        FtPtPreference ftPtPref;
        if (ftPtRoll < wFT)
            ftPtPref = FtPtPreference.PrefersFullTime;
        else if (ftPtRoll < wFT + wFlex)
            ftPtPref = FtPtPreference.Flexible;
        else
            ftPtPref = FtPtPreference.PrefersPartTime;

        int wSec   = 40;
        int wNone  = 35;
        int wFlex2 = 25;

        if (age > 40)
        {
            int shift = 5;
            wSec   += shift;
            wFlex2  = wFlex2 > shift ? wFlex2 - shift : 0;
        }
        else if (age < 28)
        {
            int shift = 5;
            wFlex2 += shift;
            wSec    = wSec > shift ? wSec - shift : 0;
        }

        int lenTotal = wSec + wNone + wFlex2;
        int lenRoll = rng.Range(0, lenTotal);
        LengthPreference lengthPref;
        if (lenRoll < wSec)
            lengthPref = LengthPreference.PrefersSecurity;
        else if (lenRoll < wSec + wNone)
            lengthPref = LengthPreference.NoPreference;
        else
            lengthPref = LengthPreference.PrefersFlexibility;

        return new CandidatePreferences { FtPtPref = ftPtPref, LengthPref = lengthPref };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static int RoundToInt(float value)
    {
        return (int)Math.Round(value);
    }

    private static int Max(int a, int b)
    {
        return a > b ? a : b;
    }
}
