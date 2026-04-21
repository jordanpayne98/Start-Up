using System;
using System.Collections.Generic;

public class CandidateData
{
    public int CandidateId;
    public string Name;
    public Gender Gender;
    public int Age;
    public int[] Skills;
    public int Salary;
    public EmployeeRole Role;

    // Hiring pipeline fields (set post-generation)
    public int InterviewStage;
    public int ExpiryTick;
    public bool IsTimerPaused;
    public bool IsHeadhunted;   // kept for backward compat; prefer IsTargeted
    public bool IsTargeted;     // set by HRSystem on delivery
    public TeamId SourcingTeamId;   // set by HRSystem on candidate delivery; default = 0 (not HR-sourced)
    public bool IsPendingReview; // true = HR-delivered, awaiting player accept/decline before entering pool
    public int SourceTier;      // 0 = Basic, 1 = Standard, 2 = Executive
    public int HRSkill;         // only populated for Role == HR

    // Follow-up / patience state
    public bool HasSentFollowUp;        // true once the follow-up inbox message is sent
    public int FollowUpSentTick;        // tick when the follow-up was sent
    public int WithdrawalDeadlineTick;  // set when follow-up sends; candidate withdraws at this tick

    // Backward-compat properties
    public int ProgrammingSkill { get => Skills[(int)SkillType.Programming]; set => Skills[(int)SkillType.Programming] = value; }
    public int DesignSkill { get => Skills[(int)SkillType.Design]; set => Skills[(int)SkillType.Design] = value; }
    public int QASkill { get => Skills[(int)SkillType.QA]; set => Skills[(int)SkillType.QA] = value; }

    // CA/PA fields — CurrentAbility set during GenerateCandidate; PotentialAbility also set during GenerateCandidate
    public int CurrentAbility;             // set to ca at generation; 0 = lazy-compute fallback on old saves
    public int PotentialAbility;           // 0-200; 0 = not yet generated
    public HiddenAttributes HiddenAttributes;

    public int GetSkill(SkillType type) => Skills[(int)type];

    // Compute salary that this candidate demands.
    // Base = role band, scaled by CA and Ambition.
    public int ComputeSalary()
    {
        GetRoleTiers(Role, out int[] tiers, out _, out _);
        int ability = AbilityCalculator.ComputeAbility(Skills, tiers);
        int ambition = HiddenAttributes.Ambition; // 1-20

        // Base bands per role – same order as EmployeeRole enum
        int baseSalary = SalaryBand.GetBase(Role);

        // Ability contribution: +1% per Ability point above 40, capped at +80%
        float caFactor = 1f + (float)System.Math.Max(0, ability - 40) / 100f;
        if (caFactor > 1.8f) caFactor = 1.8f;

        // Ambition contribution: ambition 1 → +0%, ambition 20 → +30%
        float ambFactor = 1f + (ambition - 1) * (0.30f / 19f);

        int salary = (int)(baseSalary * caFactor * ambFactor);

        // Round to nearest 100
        salary = ((salary + 50) / 100) * 100;
        if (salary < 1000) salary = 1000;
        return salary;
    }

    // 0–100, pure computed — no RNG, no alloc
    public int RoleFitScore => RoleRelevantAverage;

    public int RoleRelevantAverage
    {
        get
        {
            // Use top 2-3 role-relevant skills for seniority/salary instead of all-skill average
            int primary = 0;
            int secondary = 0;
            int third = 0;
            GetRoleTiers(Role, out _, out int[] primaryIndices, out int[] secondaryIndices);
            int pLen = primaryIndices.Length;
            for (int i = 0; i < pLen; i++)
            {
                int val = Skills[primaryIndices[i]];
                if (val > primary) { third = secondary; secondary = primary; primary = val; }
                else if (val > secondary) { third = secondary; secondary = val; }
                else if (val > third) { third = val; }
            }
            int sLen = secondaryIndices.Length;
            for (int i = 0; i < sLen; i++)
            {
                int val = Skills[secondaryIndices[i]];
                if (val > primary) { third = secondary; secondary = primary; primary = val; }
                else if (val > secondary) { third = secondary; secondary = val; }
                else if (val > third) { third = val; }
            }
            return (primary + secondary + third) / 3;
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

    // Role-skill tier mapping — hardcoded to match RoleTierProfile ScriptableObject assets.
    // Tier values: 2 = Primary (1.0x cost), 3 = Secondary (1.5x), 4 = Tertiary (2.0x).
    // SkillType indices: 0=Prog, 1=Design, 2=QA, 3=VFX, 4=SFX, 5=HR, 6=Neg, 7=Acct, 8=Mktg
    private static void GetRoleTiers(EmployeeRole role, out int[] tiers, out int[] primaryIndices, out int[] secondaryIndices)
    {
        switch (role)
        {
            case EmployeeRole.Developer:
                tiers = new[] { 2, 3, 3, 3, 4, 4, 4, 4, 4 };
                primaryIndices = new[] { 0 };
                secondaryIndices = new[] { 1, 2, 3 };
                break;
            case EmployeeRole.Designer:
                tiers = new[] { 3, 2, 4, 3, 3, 4, 4, 4, 4 };
                primaryIndices = new[] { 1 };
                secondaryIndices = new[] { 0, 3, 4 };
                break;
            case EmployeeRole.QAEngineer:
                tiers = new[] { 3, 3, 2, 4, 4, 4, 3, 4, 4 };
                primaryIndices = new[] { 2 };
                secondaryIndices = new[] { 0, 1, 6 };
                break;
            case EmployeeRole.HR:
                tiers = new[] { 4, 4, 4, 4, 4, 2, 3, 3, 3 };
                primaryIndices = new[] { 5 };
                secondaryIndices = new[] { 6, 7, 8 };
                break;
            case EmployeeRole.SoundEngineer:
                tiers = new[] { 3, 3, 4, 3, 2, 4, 4, 4, 4 };
                primaryIndices = new[] { 4 };
                secondaryIndices = new[] { 0, 1, 3 };
                break;
            case EmployeeRole.VFXArtist:
                tiers = new[] { 3, 3, 4, 2, 3, 4, 4, 4, 4 };
                primaryIndices = new[] { 3 };
                secondaryIndices = new[] { 0, 1, 4 };
                break;
            case EmployeeRole.Accountant:
                tiers = new[] { 3, 4, 4, 4, 4, 4, 3, 2, 3 };
                primaryIndices = new[] { 7 };
                secondaryIndices = new[] { 0, 6, 8 };
                break;
            case EmployeeRole.Marketer:
                tiers = new[] { 4, 3, 4, 4, 4, 3, 3, 4, 2 };
                primaryIndices = new[] { 8 };
                secondaryIndices = new[] { 1, 5, 6 };
                break;
            default:
                tiers = new[] { 2, 3, 3, 3, 4, 4, 4, 4, 4 };
                primaryIndices = new[] { 0 };
                secondaryIndices = new[] { 1, 2, 3 };
                break;
        }
    }
    
    public static CandidateData GenerateCandidate(IRng rng, float qualityMultiplier = 1.0f, EmployeeRole? forceRole = null)
    {
        int genderIndex = rng.Range(0, 2);
        Gender gender = (Gender)genderIndex;
        string name = NameGenerator.GenerateRandomName(rng, gender);

        EmployeeRole role;
        if (forceRole.HasValue)
        {
            role = forceRole.Value;
        }
        else
        {
            // Free candidate pool: Developer, Designer, QAEngineer, HR, SoundEngineer, VFXArtist, Marketer.
            // Pool index → role mapping (enum gap at 3 since Researcher was removed):
            //   0→Dev(0), 1→Des(1), 2→QA(2), 3→HR(4), 4→SoundEngineer(5), 5→VFXArtist(6), 6→Marketer(8)
            int poolIndex = rng.Range(0, 7);
            if (poolIndex < 3)
                role = (EmployeeRole)poolIndex;           // 0→Dev, 1→Des, 2→QA
            else if (poolIndex < 6)
                role = (EmployeeRole)(poolIndex + 1);     // 3→HR(4), 4→Sound(5), 5→VFX(6)
            else
                role = EmployeeRole.Marketer;             // 6→Marketer(8)
        }

        // ── Phase 1: Roll CA from weighted distribution ─────────────────────────
        // Four buckets matching star thresholds. qualityMultiplier (HR searches)
        // shifts probability mass from lower buckets toward higher ones.
        // Default weights (q=0): Low 45% | Average 40% | High 10% | Exceptional 2%
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
        // Age has no mechanical effect on CA. High CA nudges displayed age older,
        // low CA nudges younger. Bias is 40% blend — never a hard clamp.
        int rawAge = rng.Range(18, 46);
        int ageBias;
        if      (ca >= 170) ageBias = 38;
        else if (ca >= 120) ageBias = 30;
        else if (ca >= 60)  ageBias = 24;
        else                ageBias = 20;
        int age = Clamp((int)(rawAge * 0.6f + ageBias * 0.4f), 20, 55);

        // ── Phase 3: Derive PA — soft age bias preserved for growth ceiling ──────
        // PA >= CA always by construction. Younger age = more uplift headroom.
        int maxUplift;
        if      (age <= 22) maxUplift = 110;
        else if (age <= 26) maxUplift = 85;
        else if (age <= 30) maxUplift = 60;
        else if (age <= 36) maxUplift = 35;
        else                maxUplift = 18;

        int pa = ca + rng.Range(8, maxUplift + 1);
        if (pa > 200) pa = 200;

        // ── Phase 3: Distribute CA as skill budget using reverse-solve ──────────
        // Greedy allocation: primary first (cheapest per CA), then secondary, then
        // tertiary. Final CA is computed and validated against the rolled target.
        int[] skills = new int[SkillTypeHelper.SkillTypeCount];

        GetRoleTiers(role, out int[] tiers, out int[] primaryIndices, out int[] secondaryIndices);

        // Build tertiary index list (all skills not in primary or secondary)
        int totalSkills = SkillTypeHelper.SkillTypeCount;
        int[] tertiaryIndices = new int[totalSkills - primaryIndices.Length - secondaryIndices.Length];
        int tertiaryCount = 0;
        for (int i = 0; i < totalSkills; i++)
        {
            bool isPrimary = false;
            for (int p = 0; p < primaryIndices.Length; p++) if (primaryIndices[p] == i) { isPrimary = true; break; }
            if (isPrimary) continue;
            bool isSecondary = false;
            for (int s = 0; s < secondaryIndices.Length; s++) if (secondaryIndices[s] == i) { isSecondary = true; break; }
            if (!isSecondary) tertiaryIndices[tertiaryCount++] = i;
        }

        int remaining = ca;

        // Primary allocation: greedily add points while marginal cost fits in budget
        // Target ~30% of total CA for primaries combined
        int primaryTarget = (ca * 30) / 100;
        int primarySpent = 0;
        bool primaryActive = true;
        while (primaryActive && primarySpent < primaryTarget)
        {
            primaryActive = false;
            for (int p = 0; p < primaryIndices.Length; p++)
            {
                int idx = primaryIndices[p];
                if (skills[idx] >= 20) continue;
                int marginal = AbilityCalculator.GetMarginalCost(skills[idx], tiers[idx]);
                if (marginal > 0 && marginal <= remaining && primarySpent + marginal <= primaryTarget + 5)
                {
                    skills[idx]++;
                    remaining -= marginal;
                    primarySpent += marginal;
                    primaryActive = true;
                }
            }
        }

        // Secondary allocation: greedily add points, target ~32% of total CA
        int secondaryTarget = (ca * 32) / 100;
        int secondarySpent = 0;
        bool secondaryActive = true;
        while (secondaryActive && secondarySpent < secondaryTarget)
        {
            secondaryActive = false;
            for (int s = 0; s < secondaryIndices.Length; s++)
            {
                int idx = secondaryIndices[s];
                if (skills[idx] >= 20) continue;
                int marginal = AbilityCalculator.GetMarginalCost(skills[idx], tiers[idx]);
                if (marginal > 0 && marginal <= remaining && secondarySpent + marginal <= secondaryTarget + 5)
                {
                    skills[idx]++;
                    remaining -= marginal;
                    secondarySpent += marginal;
                    secondaryActive = true;
                }
            }
        }

        // Tertiary allocation: distribute remaining budget across tertiary skills
        bool tertiaryActive = true;
        while (tertiaryActive && remaining > 0)
        {
            tertiaryActive = false;
            for (int t = 0; t < tertiaryCount; t++)
            {
                int idx = tertiaryIndices[t];
                if (skills[idx] >= 20) continue;
                int marginal = AbilityCalculator.GetMarginalCost(skills[idx], tiers[idx]);
                if (marginal > 0 && marginal <= remaining)
                {
                    skills[idx]++;
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
            skills[idx] = Clamp(skills[idx] + variance, 0, 20);
        }
        for (int s = 0; s < secondaryIndices.Length; s++)
        {
            int idx = secondaryIndices[s];
            int variance = rng.Range(-1, 2);
            skills[idx] = Clamp(skills[idx] + variance, 0, 20);
        }

        // Validation: compute actual Ability and trim/bump to stay within [ca-5, ca+5]
        int actualCA = AbilityCalculator.ComputeAbility(skills, tiers);
        int delta = actualCA - ca;

        // Overshoot: trim highest tertiary skills
        if (delta > 5)
        {
            for (int t = tertiaryCount - 1; t >= 0 && delta > 5; t--)
            {
                int idx = tertiaryIndices[t];
                while (skills[idx] > 0 && delta > 5)
                {
                    int marginal = AbilityCalculator.GetMarginalCost(skills[idx] - 1, tiers[idx]);
                    skills[idx]--;
                    delta -= marginal;
                }
            }
        }
        // Undershoot: bump a secondary skill
        else if (delta < -5)
        {
            int sIdx = secondaryIndices[0];
            while (skills[sIdx] < 20 && delta < -5)
            {
                int marginal = AbilityCalculator.GetMarginalCost(skills[sIdx], tiers[sIdx]);
                skills[sIdx]++;
                delta += marginal;
            }
        }

        // 5% chance cross-skill spike: one off-role skill gets a minor bump
        if (rng.Range(0, 100) < 5)
        {
            int crossIdx = rng.Range(0, totalSkills);
            bool isRoleSkill = false;
            for (int p = 0; p < primaryIndices.Length; p++) if (primaryIndices[p] == crossIdx) { isRoleSkill = true; break; }
            if (!isRoleSkill)
                for (int s = 0; s < secondaryIndices.Length; s++) if (secondaryIndices[s] == crossIdx) { isRoleSkill = true; break; }
            if (!isRoleSkill && skills[crossIdx] < 20)
            {
                // Only spike if it doesn't overshoot CA budget
                int spikeAmt = rng.Range(2, 4);
                int spikeCost = 0;
                for (int bump = 0; bump < spikeAmt && skills[crossIdx] + bump < 20; bump++)
                    spikeCost += AbilityCalculator.GetMarginalCost(skills[crossIdx] + bump, tiers[crossIdx]);
                if (AbilityCalculator.ComputeAbility(skills, tiers) + spikeCost <= ca + 10)
                    skills[crossIdx] = Clamp(skills[crossIdx] + spikeAmt, 0, 20);
            }
        }

        // Ability is the actual computed value from the generated skills
        int computedCA = AbilityCalculator.ComputeAbility(skills, tiers);

        // Salary based on role-relevant skill average (top 2-3 skills)
        var candidateData = new CandidateData
        {
            Name = name,
            Gender = gender,
            Age = age,
            Skills = skills,
            Role = role,
            CurrentAbility = computedCA,
            PotentialAbility = pa
        };

        // Generate HiddenAttributes from PA immediately (same formula as AbilitySystem)
        {
            int floor = pa / 20;
            if (floor < 1) floor = 1;
            if (floor > 10) floor = 10;
            int spread = (20 - floor) / 2 + 1;
            int Attr() => Clamp(rng.Range(floor, floor + spread + 1) + rng.Range(-1, 2), 1, 20);
            candidateData.HiddenAttributes = new HiddenAttributes
            {
                LearningRate = Attr(),
                Creative     = Attr(),
                WorkEthic    = Attr(),
                Adaptability = Attr(),
                Ambition     = Attr()
            };
        }

        // Salary derived from CA, role base band, and Ambition
        candidateData.Salary = candidateData.ComputeSalary();

        return candidateData;
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
