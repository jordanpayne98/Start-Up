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
    public ConfidenceLevel[] SkillConfidence;           // length 26, per-skill confidence
    public ConfidenceLevel[] VisibleAttributeConfidence; // length 8
    public ConfidenceLevel[] HiddenAttributeConfidence;  // length 7

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

    // Candidate generation using new RoleId-based system.
    public static CandidateData GenerateCandidate(IRng rng, RoleProfileTable roleProfileTable, float qualityMultiplier = 1.0f, RoleId? forceRole = null)
    {
        int genderIndex = rng.Range(0, 2);
        Gender gender = (Gender)genderIndex;
        string name = NameGenerator.GenerateRandomName(rng, gender);

        RoleId role;
        if (forceRole.HasValue)
        {
            role = forceRole.Value;
        }
        else
        {
            // Free candidate pool: common roles weighted by CandidatePoolWeight from profile table
            // Fallback: equal distribution across all 16 roles
            int poolIndex = rng.Range(0, RoleIdHelper.RoleCount);
            role = (RoleId)poolIndex;
        }

        // ── Phase 1: Roll CA from weighted distribution ─────────────────────────
        float q = qualityMultiplier - 1.0f;
        int wLow  = Clamp((int)(45 - q * 50),  5, 45);
        int wAvg  = Clamp((int)(40 - q * 15), 15, 40);
        int wHigh = Clamp((int)(10 + q * 40), 10, 55);
        int wExc  = Clamp((int)( 2 + q * 30),  2, 40);
        int wTotal = wLow + wAvg + wHigh + wExc;

        int caMin, caMax;
        int caRoll = rng.Range(0, wTotal);
        if      (caRoll < wLow)                      { caMin = 15;  caMax = 59;  }
        else if (caRoll < wLow + wAvg)               { caMin = 60;  caMax = 119; }
        else if (caRoll < wLow + wAvg + wHigh)       { caMin = 120; caMax = 169; }
        else                                          { caMin = 170; caMax = 200; }

        int ca = rng.Range(caMin, caMax + 1);

        // ── Phase 2: Roll age independently, soft-nudge toward CA-plausible range
        int rawAge = rng.Range(18, 46);
        int ageBias;
        if      (ca >= 170) ageBias = 38;
        else if (ca >= 120) ageBias = 30;
        else if (ca >= 60)  ageBias = 24;
        else                ageBias = 20;
        int age = Clamp((int)(rawAge * 0.6f + ageBias * 0.4f), 20, 55);

        // ── Phase 3: Derive PA
        int maxUplift;
        if      (age <= 22) maxUplift = 110;
        else if (age <= 26) maxUplift = 85;
        else if (age <= 30) maxUplift = 60;
        else if (age <= 36) maxUplift = 35;
        else                maxUplift = 18;

        int pa = ca + rng.Range(8, maxUplift + 1);
        if (pa > 200) pa = 200;

        // ── Phase 4: Distribute CA as skill budget across new 26-skill model ────
        var stats = EmployeeStatBlock.Create();
        stats.PotentialAbility = pa;

        RoleProfileDefinition profile = roleProfileTable?.Get(role);
        int totalSkills = SkillIdHelper.SkillCount;

        // Build tier int array from profile bands (Primary=2, Secondary=3, Tertiary=4, None=4)
        int[] tiers = new int[totalSkills];
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
                    case RoleWeightBand.Primary:   tiers[i] = 2; pCount++; break;
                    case RoleWeightBand.Secondary: tiers[i] = 3; sCount++; break;
                    default:                       tiers[i] = 4; tCount++; break;
                }
            }
            primaryIndices = new int[pCount];
            secondaryIndices = new int[sCount];
            tertiaryIndices = new int[tCount];
            int pi = 0, si = 0, ti = 0;
            for (int i = 0; i < totalSkills; i++)
            {
                if (tiers[i] == 2) primaryIndices[pi++] = i;
                else if (tiers[i] == 3) secondaryIndices[si++] = i;
                else tertiaryIndices[ti++] = i;
            }
        }
        else
        {
            for (int i = 0; i < totalSkills; i++) tiers[i] = 3;
            primaryIndices = new int[0];
            secondaryIndices = new int[totalSkills];
            tertiaryIndices = new int[0];
            for (int i = 0; i < totalSkills; i++) secondaryIndices[i] = i;
        }

        int remaining = ca;

        // Primary allocation: ~30% of CA budget
        int primaryTarget = (ca * 30) / 100;
        int primarySpent = 0;
        bool primaryActive = true;
        while (primaryActive && primarySpent < primaryTarget)
        {
            primaryActive = false;
            for (int p = 0; p < primaryIndices.Length; p++)
            {
                int idx = primaryIndices[p];
                if (stats.Skills[idx] >= 20) continue;
                int marginal = AbilityCalculator.GetMarginalCost(stats.Skills[idx], tiers[idx]);
                if (marginal > 0 && marginal <= remaining && primarySpent + marginal <= primaryTarget + 5)
                {
                    stats.Skills[idx]++;
                    remaining -= marginal;
                    primarySpent += marginal;
                    primaryActive = true;
                }
            }
        }

        // Secondary allocation: ~32% of CA budget
        int secondaryTarget = (ca * 32) / 100;
        int secondarySpent = 0;
        bool secondaryActive = true;
        while (secondaryActive && secondarySpent < secondaryTarget)
        {
            secondaryActive = false;
            for (int s = 0; s < secondaryIndices.Length; s++)
            {
                int idx = secondaryIndices[s];
                if (stats.Skills[idx] >= 20) continue;
                int marginal = AbilityCalculator.GetMarginalCost(stats.Skills[idx], tiers[idx]);
                if (marginal > 0 && marginal <= remaining && secondarySpent + marginal <= secondaryTarget + 5)
                {
                    stats.Skills[idx]++;
                    remaining -= marginal;
                    secondarySpent += marginal;
                    secondaryActive = true;
                }
            }
        }

        // Tertiary allocation: distribute remaining budget
        bool tertiaryActive = true;
        while (tertiaryActive && remaining > 0)
        {
            tertiaryActive = false;
            for (int t = 0; t < tertiaryIndices.Length; t++)
            {
                int idx = tertiaryIndices[t];
                if (stats.Skills[idx] >= 20) continue;
                int marginal = AbilityCalculator.GetMarginalCost(stats.Skills[idx], tiers[idx]);
                if (marginal > 0 && marginal <= remaining)
                {
                    stats.Skills[idx]++;
                    remaining -= marginal;
                    tertiaryActive = true;
                }
            }
        }

        // Apply variance to primary and secondary skills
        for (int p = 0; p < primaryIndices.Length; p++)
        {
            int idx = primaryIndices[p];
            int variance = rng.Range(-2, 3);
            stats.Skills[idx] = Clamp(stats.Skills[idx] + variance, 0, 20);
        }
        for (int s = 0; s < secondaryIndices.Length; s++)
        {
            int idx = secondaryIndices[s];
            int variance = rng.Range(-1, 2);
            stats.Skills[idx] = Clamp(stats.Skills[idx] + variance, 0, 20);
        }

        // Compute actual CA and trim/bump to stay within [ca-5, ca+5]
        int actualCA = AbilityCalculator.ComputeAbility(stats.Skills, tiers);
        int delta = actualCA - ca;

        if (delta > 5)
        {
            for (int t = tertiaryIndices.Length - 1; t >= 0 && delta > 5; t--)
            {
                int idx = tertiaryIndices[t];
                while (stats.Skills[idx] > 0 && delta > 5)
                {
                    int marginal = AbilityCalculator.GetMarginalCost(stats.Skills[idx] - 1, tiers[idx]);
                    stats.Skills[idx]--;
                    delta -= marginal;
                }
            }
        }
        else if (delta < -5 && secondaryIndices.Length > 0)
        {
            int sIdx = secondaryIndices[0];
            while (stats.Skills[sIdx] < 20 && delta < -5)
            {
                int marginal = AbilityCalculator.GetMarginalCost(stats.Skills[sIdx], tiers[sIdx]);
                stats.Skills[sIdx]++;
                delta += marginal;
            }
        }

        // 5% chance cross-skill spike
        if (rng.Range(0, 100) < 5)
        {
            int crossIdx = rng.Range(0, totalSkills);
            bool isRoleSkill = false;
            for (int p = 0; p < primaryIndices.Length; p++) if (primaryIndices[p] == crossIdx) { isRoleSkill = true; break; }
            if (!isRoleSkill)
                for (int s = 0; s < secondaryIndices.Length; s++) if (secondaryIndices[s] == crossIdx) { isRoleSkill = true; break; }
            if (!isRoleSkill && stats.Skills[crossIdx] < 20)
            {
                int spikeAmt = rng.Range(2, 4);
                int spikeCost = 0;
                for (int bump = 0; bump < spikeAmt && stats.Skills[crossIdx] + bump < 20; bump++)
                    spikeCost += AbilityCalculator.GetMarginalCost(stats.Skills[crossIdx] + bump, tiers[crossIdx]);
                if (AbilityCalculator.ComputeAbility(stats.Skills, tiers) + spikeCost <= ca + 10)
                    stats.Skills[crossIdx] = Clamp(stats.Skills[crossIdx] + spikeAmt, 0, 20);
            }
        }

        int computedCA = AbilityCalculator.ComputeAbility(stats.Skills, tiers);

        // Generate hidden attributes from PA
        {
            int floor = pa / 20;
            if (floor < 1) floor = 1;
            if (floor > 10) floor = 10;
            int spread = (20 - floor) / 2 + 1;
            int Attr() => Clamp(rng.Range(floor, floor + spread + 1) + rng.Range(-1, 2), 1, 20);
            stats.HiddenAttributes[(int)HiddenAttributeId.LearningRate] = Attr();
            stats.HiddenAttributes[(int)HiddenAttributeId.Ambition]     = Attr();
            stats.HiddenAttributes[(int)HiddenAttributeId.Loyalty]      = Attr();
            stats.HiddenAttributes[(int)HiddenAttributeId.PressureTolerance] = Attr();
            stats.HiddenAttributes[(int)HiddenAttributeId.Ego]          = Attr();
            stats.HiddenAttributes[(int)HiddenAttributeId.Consistency]  = Attr();
            stats.HiddenAttributes[(int)HiddenAttributeId.Mentoring]    = Attr();
            stats.VisibleAttributes[(int)VisibleAttributeId.WorkEthic]  = Attr();
            stats.VisibleAttributes[(int)VisibleAttributeId.Creativity] = Attr();
            stats.VisibleAttributes[(int)VisibleAttributeId.Adaptability] = Attr();
            // remaining visible attributes: default 10 from Create()
        }

        var candidateData = new CandidateData
        {
            Name = name,
            Gender = gender,
            Age = age,
            Role = role,
            CurrentAbility = computedCA,
            Stats = stats,
        };

        // Salary derived from CA, role base band, and Ambition
        candidateData.Salary = candidateData.ComputeSalary();
        candidateData.personality = PersonalitySystem.GeneratePersonality(rng);

        // ── Preference generation ─────────────────────────────────────────────
        bool isCoreRole = role == RoleId.SoftwareEngineer || role == RoleId.ProductDesigner || role == RoleId.QaEngineer;

        int wFT  = isCoreRole ? 65 : 40;
        int wFlex = isCoreRole ? 20 : 35;
        int wPT  = isCoreRole ? 15 : 25;

        if (age > 40)
        {
            int shift = 8;
            wFT  += shift;
            wPT  -= shift < wPT ? shift : wPT;
        }
        else if (age < 28)
        {
            int shift = 7;
            wPT  += shift;
            wFT  -= shift < wFT ? shift : wFT;
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

        int wSec = 40;
        int wNone = 35;
        int wFlex2 = 25;

        if (age > 40)
        {
            int shift = 5;
            wSec   += shift;
            wFlex2 -= shift < wFlex2 ? shift : wFlex2;
        }
        else if (age < 28)
        {
            int shift = 5;
            wFlex2 += shift;
            wSec   -= shift < wSec ? shift : wSec;
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

        candidateData.Preferences = new CandidatePreferences
        {
            FtPtPref  = ftPtPref,
            LengthPref = lengthPref
        };

        return candidateData;
    }

    public static CandidateData GenerateCandidate(IRng rng, float qualityMultiplier = 1.0f, RoleId? forceRole = null)
    {
        return GenerateCandidate(rng, null, qualityMultiplier, forceRole);
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
