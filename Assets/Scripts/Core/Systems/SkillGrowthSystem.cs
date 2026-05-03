// SkillGrowthSystem Version: Wave 3B (Growth Redesign)
// Changes:
//   - All Award methods now return SkillGrowthResult containing skill and attribute increase records.
//   - MoraleSystem added to AwardSkillXP and AwardProductPhaseXP signatures.
//   - New multipliers applied: LearningRate, RoleFit, Morale, Mentoring, PADistance.
//   - Binary PA gate replaced with graduated PADistanceXPMultiplier + soft ceiling at PA+20.
//   - Founder multiplier changed from 1.5 to 1.05 (spec section 15.1).
//   - Age multiplier updated to 5-bracket spec (18-24/25-34/35-44/45-54/55+).
//   - Visible attribute growth added via AwardAttributeXP and ProcessAttributeGrowthTriggers.
using System.Collections.Generic;

public static class SkillGrowthSystem
{
    private const int MaxXPPerContract = 2;
    private const float DefaultVarianceMin          = 0.8f;
    private const float DefaultVarianceRange        = 0.4f;
    private const float DefaultSpilloverBase        = 0.35f;
    private const float DefaultSpilloverSpread      = 0.30f;
    private const float DefaultMisfitXPRate         = 0.25f;
    private const float DefaultNativeXPRate         = 0.15f;
    private const float DefaultProductPhaseXPPerDay = 0.15f;
    private const float DefaultFounderMultiplier    = 1.05f;
    private const float DefaultVisibleAttrThreshold = 3.0f;
    private const float DefaultVisibleAttrBaseXP    = 0.03f;

    // Hard ceiling: XP awarded only when bestRoleCA <= PA + this margin.
    private const int PASoftCeilingMargin = 20;

    private static readonly RoleWeightBand[] UniformSkillBands = BuildUniformSkillBands();

    // =========================================================================
    // AwardSkillXP — called once per contract completion
    // =========================================================================
    public static SkillGrowthResult AwardSkillXP(
        Contract contract,
        Team team,
        EmployeeSystem employeeSystem,
        AbilitySystem abilitySystem,
        MoraleSystem moraleSystem,
        IRng rng,
        RoleProfileTable roleProfileTable,
        TuningConfig tuning = null)
    {
        var result = new SkillGrowthResult
        {
            SkillIncreases     = new List<SkillIncreaseRecord>(4),
            AttributeIncreases = new List<AttributeIncreaseRecord>(4)
        };

        if (team == null) return result;
        int memberCount = team.members.Count;
        if (memberCount == 0) return result;

        int   maxXP           = tuning != null ? tuning.MaxXPPerContract         : MaxXPPerContract;
        float varianceMin     = tuning != null ? tuning.XPVarianceMin            : DefaultVarianceMin;
        float varianceRange   = tuning != null ? tuning.XPVarianceRange          : DefaultVarianceRange;
        float spilloverBase   = tuning != null ? tuning.SkillSpilloverRateBase   : DefaultSpilloverBase;
        float spilloverSpread = tuning != null ? tuning.SkillSpilloverRateSpread : DefaultSpilloverSpread;
        float misfitRate      = tuning != null ? tuning.ContractMisfitXPRate     : DefaultMisfitXPRate;
        float nativeRate      = tuning != null ? tuning.ContractNativeXPRate     : DefaultNativeXPRate;
        float founderMult     = tuning != null ? tuning.FounderXPMultiplier      : DefaultFounderMultiplier;

        float overallQuality = ComputeOverallQuality(contract);
        float rawBaseXP = (contract.Difficulty / 10f) * (overallQuality / 100f) * maxXP;

        float totalEffective = 0f;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = employeeSystem.GetEmployee(team.members[i]);
            if (emp == null || !emp.isActive) continue;
            totalEffective += emp.EffectiveOutput;
        }
        if (totalEffective <= 0f) totalEffective = memberCount;
        float perUnitXP = rawBaseXP / totalEffective;

        float[] weights = contract.Requirements.Weights;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float variance    = varianceMin + (rng.NextFloat01() * varianceRange);
            float ageDecay    = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP      = perUnitXP * employee.EffectiveOutput * variance * ageDecay;
            if (employee.isFounder) baseXP *= founderMult;
            if (baseXP < 0f) baseXP = 0f;

            // Growth multipliers
            float lrMult     = GetLearningRateMultiplier(employee.Stats.GetHiddenAttribute(HiddenAttributeId.LearningRate), tuning);
            float rfScore    = ComputeRoleFitScore(employee, abilitySystem, memberId);
            float rfMult     = GetRoleFitMultiplier(rfScore, tuning);
            float morale     = moraleSystem != null ? moraleSystem.GetMorale(memberId) : 50f;
            float moraleMult = GetMoraleMultiplier(morale, tuning);

            RoleWeightBand[] skillBands = GetSkillBandsForRole(employee.role, roleProfileTable);
            int bestRoleCA   = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
            float paMult     = AbilityCalculator.GetPADistanceXPMultiplier(bestRoleCA, employee.Stats.PotentialAbility);
            float mentorMult = GetMentoringMultiplier(employee, team, employeeSystem, tuning);

            float modifiedBase = baseXP * lrMult * rfMult * moraleMult * mentorMult * paMult;

            SkillId nativeSkill = GetNativeSkillForRole(employee.role);
            bool nativeSkillAwarded = false;
            bool anyFit = false;
            int currentCA = bestRoleCA;

            int skillCount = SkillIdHelper.SkillCount;
            for (int s = 0; s < skillCount; s++)
            {
                float w = (weights != null && s < weights.Length) ? weights[s] : 0f;
                if (w <= 0f) continue;

                float weightedXP = modifiedBase * w;
                bool fit = TeamWorkEngine.IsRoleFitForSkill(employee.role, (SkillId)s);
                if (fit) anyFit = true;
                float finalXP = fit ? weightedXP : weightedXP * misfitRate;

                if (currentCA <= employee.Stats.PotentialAbility + PASoftCeilingMargin)
                {
                    int oldSkill = employee.Stats.Skills[s];
                    AccumulateXP(employee, s, finalXP, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }

                if ((SkillId)s == nativeSkill) nativeSkillAwarded = true;
            }

            if (!nativeSkillAwarded && !anyFit)
            {
                if (currentCA <= employee.Stats.PotentialAbility + PASoftCeilingMargin)
                {
                    AccumulateXP(employee, (int)nativeSkill, modifiedBase * nativeRate, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }
            }

            float adaptability   = employee.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);
            float spilloverRate  = spilloverBase + (adaptability / 20f) * spilloverSpread;
            float spilloverAward = modifiedBase * spilloverRate;
            if (spilloverAward > 0f)
            {
                for (int s = 0; s < skillCount; s++)
                {
                    float w = (weights != null && s < weights.Length) ? weights[s] : 0f;
                    if (w > 0f) continue;
                    if (currentCA > employee.Stats.PotentialAbility + PASoftCeilingMargin) break;
                    AccumulateXP(employee, s, spilloverAward, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }
            }

            abilitySystem?.InvalidateCA(memberId);
        }

        return result;
    }

    // =========================================================================
    // AwardProductPhaseXP — called once per day during TickPhaseWork
    // =========================================================================
    public static SkillGrowthResult AwardProductPhaseXP(
        Team team,
        SkillId phaseSkill,
        EmployeeSystem employeeSystem,
        AbilitySystem abilitySystem,
        MoraleSystem moraleSystem,
        RoleProfileTable roleProfileTable,
        TuningConfig tuning = null)
    {
        var result = new SkillGrowthResult
        {
            SkillIncreases     = new List<SkillIncreaseRecord>(4),
            AttributeIncreases = new List<AttributeIncreaseRecord>(4)
        };

        if (team == null) return result;
        int memberCount = team.members.Count;
        if (memberCount == 0) return result;

        float xpPerDay        = tuning != null ? tuning.ProductPhaseXPPerDay     : DefaultProductPhaseXPPerDay;
        float misfitRate      = tuning != null ? tuning.ProductPhaseMisfitXPRate  : DefaultMisfitXPRate;
        float nativeRate      = tuning != null ? tuning.ProductPhaseNativeXPRate  : DefaultNativeXPRate;
        float spilloverBase   = tuning != null ? tuning.SkillSpilloverRateBase    : DefaultSpilloverBase;
        float spilloverSpread = tuning != null ? tuning.SkillSpilloverRateSpread  : DefaultSpilloverSpread;

        float totalEffective = 0f;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = employeeSystem.GetEmployee(team.members[i]);
            if (emp == null || !emp.isActive) continue;
            totalEffective += emp.EffectiveOutput;
        }
        if (totalEffective <= 0f) totalEffective = memberCount;
        float perUnitXP = xpPerDay / totalEffective;

        int skillCount = SkillIdHelper.SkillCount;
        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float ageDecay = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP   = perUnitXP * employee.EffectiveOutput * ageDecay;
            if (baseXP < 0f) baseXP = 0f;

            // Growth multipliers
            float lrMult     = GetLearningRateMultiplier(employee.Stats.GetHiddenAttribute(HiddenAttributeId.LearningRate), tuning);
            float rfScore    = ComputeRoleFitScore(employee, abilitySystem, memberId);
            float rfMult     = GetRoleFitMultiplier(rfScore, tuning);
            float morale     = moraleSystem != null ? moraleSystem.GetMorale(memberId) : 50f;
            float moraleMult = GetMoraleMultiplier(morale, tuning);

            RoleWeightBand[] skillBands = GetSkillBandsForRole(employee.role, roleProfileTable);
            int bestRoleCA   = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
            float paMult     = AbilityCalculator.GetPADistanceXPMultiplier(bestRoleCA, employee.Stats.PotentialAbility);
            float mentorMult = GetMentoringMultiplier(employee, team, employeeSystem, tuning);

            float modifiedBase = baseXP * lrMult * rfMult * moraleMult * mentorMult * paMult;
            int currentCA = bestRoleCA;

            bool fit = TeamWorkEngine.IsRoleFitForSkill(employee.role, phaseSkill);
            if (fit)
            {
                if (currentCA <= employee.Stats.PotentialAbility + PASoftCeilingMargin)
                {
                    AccumulateXP(employee, (int)phaseSkill, modifiedBase, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }
            }
            else
            {
                if (currentCA <= employee.Stats.PotentialAbility + PASoftCeilingMargin)
                {
                    AccumulateXP(employee, (int)phaseSkill, modifiedBase * misfitRate, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }
                SkillId nativeSkill = GetNativeSkillForRole(employee.role);
                if (nativeSkill != phaseSkill && currentCA <= employee.Stats.PotentialAbility + PASoftCeilingMargin)
                {
                    AccumulateXP(employee, (int)nativeSkill, modifiedBase * nativeRate, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }
            }

            float adaptability   = employee.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);
            float spilloverRate  = spilloverBase + (adaptability / 20f) * spilloverSpread;
            float spilloverAward = modifiedBase * spilloverRate;
            if (spilloverAward > 0f)
            {
                for (int s = 0; s < skillCount; s++)
                {
                    if ((SkillId)s == phaseSkill) continue;
                    if (currentCA > employee.Stats.PotentialAbility + PASoftCeilingMargin) break;
                    AccumulateXP(employee, s, spilloverAward, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);
                    currentCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
                }
            }

            abilitySystem?.InvalidateCA(memberId);
        }

        return result;
    }

    // =========================================================================
    // AwardMarketingXP — called for marketing team work events
    // =========================================================================
    public static SkillGrowthResult AwardMarketingXP(
        Team team,
        EmployeeSystem employeeSystem,
        float xpAmount,
        IRng rng,
        RoleProfileTable roleProfileTable,
        AbilitySystem abilitySystem,
        TuningConfig tuning = null)
    {
        var result = new SkillGrowthResult
        {
            SkillIncreases     = new List<SkillIncreaseRecord>(4),
            AttributeIncreases = new List<AttributeIncreaseRecord>(4)
        };

        if (team == null) return result;
        if (team.teamType != TeamType.Marketing) return result;
        int memberCount = team.members.Count;
        if (memberCount == 0) return result;

        float totalEffective = 0f;
        for (int i = 0; i < memberCount; i++)
        {
            var emp = employeeSystem.GetEmployee(team.members[i]);
            if (emp == null || !emp.isActive) continue;
            totalEffective += emp.EffectiveOutput;
        }
        if (totalEffective <= 0f) totalEffective = memberCount;
        float perUnitXP = xpAmount / totalEffective;

        for (int i = 0; i < memberCount; i++)
        {
            var memberId = team.members[i];
            var employee = employeeSystem.GetEmployee(memberId);
            if (employee == null || !employee.isActive) continue;

            float varianceMin   = tuning != null ? tuning.XPVarianceMin   : DefaultVarianceMin;
            float varianceRange = tuning != null ? tuning.XPVarianceRange  : DefaultVarianceRange;
            float variance      = varianceMin + rng.NextFloat01() * varianceRange;
            float ageDecay      = GetAgeDecayMultiplier(employee.age, tuning);
            float baseXP        = perUnitXP * employee.EffectiveOutput * variance * ageDecay;

            // Growth multipliers (no mentor or morale for marketing — simple form)
            float lrMult = GetLearningRateMultiplier(employee.Stats.GetHiddenAttribute(HiddenAttributeId.LearningRate), tuning);

            RoleWeightBand[] skillBands = GetSkillBandsForRole(employee.role, roleProfileTable);
            int bestRoleCA = abilitySystem != null ? abilitySystem.GetBestRoleCA(memberId) : AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
            float paMult   = AbilityCalculator.GetPADistanceXPMultiplier(bestRoleCA, employee.Stats.PotentialAbility);

            float finalXP = baseXP * lrMult * paMult;

            if (bestRoleCA <= employee.Stats.PotentialAbility + PASoftCeilingMargin)
                AccumulateXP(employee, (int)SkillId.Marketing, finalXP, skillBands, employee.Stats.PotentialAbility, result.SkillIncreases, memberId);

            abilitySystem?.InvalidateCA(memberId);
        }

        return result;
    }

    // =========================================================================
    // Visible Attribute Growth
    // =========================================================================

    /// <summary>
    /// Awards XP to a single visible attribute. Returns an increase record if a level-up occurred, null otherwise.
    /// Threshold: 3.0 XP per level by default. Clamps attribute to [1, 20].
    /// </summary>
    public static AttributeIncreaseRecord? AwardAttributeXP(
        Employee employee,
        EmployeeId employeeId,
        VisibleAttributeId attribute,
        float amount,
        TuningConfig tuning = null)
    {
        if (employee == null || amount <= 0f) return null;

        employee.Stats.EnsureAttributeXpArrays();

        int idx = (int)attribute;
        if (idx < 0 || idx >= VisibleAttributeHelper.AttributeCount) return null;

        int currentValue = employee.Stats.VisibleAttributes[idx];
        if (currentValue >= 20) return null;

        float threshold = tuning != null ? tuning.VisibleAttributeXPThreshold : DefaultVisibleAttrThreshold;
        employee.Stats.VisibleAttributeXp[idx] += amount;

        if (employee.Stats.VisibleAttributeXp[idx] >= threshold)
        {
            int oldValue = currentValue;
            int newValue = currentValue + 1;
            if (newValue > 20) newValue = 20;
            employee.Stats.VisibleAttributes[idx] = newValue;
            employee.Stats.VisibleAttributeDeltaDirection[idx] = 1;
            employee.Stats.VisibleAttributeXp[idx] -= threshold;
            if (employee.Stats.VisibleAttributeXp[idx] < 0f)
                employee.Stats.VisibleAttributeXp[idx] = 0f;

            return new AttributeIncreaseRecord
            {
                EmployeeId = employeeId,
                Attribute  = attribute,
                OldValue   = oldValue,
                NewValue   = newValue
            };
        }

        return null;
    }

    /// <summary>
    /// Processes a batch of attribute growth triggers for one employee after a qualifying work event.
    /// Each triggered attribute gets a tiny base XP amount. Results appended to the provided list.
    /// </summary>
    public static void ProcessAttributeGrowthTriggers(
        Employee employee,
        EmployeeId employeeId,
        AttributeGrowthContext context,
        List<AttributeIncreaseRecord> results,
        TuningConfig tuning = null)
    {
        if (employee == null || results == null) return;

        float baseXP = context.BaseXpAmount > 0f
            ? context.BaseXpAmount
            : (tuning != null ? tuning.VisibleAttributeBaseXPPerTrigger : DefaultVisibleAttrBaseXP);

        // Leadership — senior role + team delivery
        if (context.IsTeamSenior || context.HasTeamDelivery)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Leadership, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Creativity — creative phase work
        if (context.IsCreativePhase)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Creativity, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Focus — reliable completion
        if (context.HasReliableCompletion)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Focus, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Communication — team work contexts
        if (context.IsTeamWork)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Communication, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Adaptability — off-role work
        if (context.IsOffRoleWork)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Adaptability, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Work Ethic — team delivery
        if (context.HasTeamDelivery)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.WorkEthic, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Composure — pressure work
        if (context.IsPressureWork)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Composure, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }

        // Initiative — autonomous tasks
        if (context.IsAutonomousTask)
        {
            var rec = AwardAttributeXP(employee, employeeId, VisibleAttributeId.Initiative, baseXP, tuning);
            if (rec.HasValue) results.Add(rec.Value);
        }
    }

    // =========================================================================
    // Private helpers — growth modifier multipliers
    // =========================================================================

    /// <summary>Spec section 9 — LearningRate hidden attribute → XP multiplier.</summary>
    private static float GetLearningRateMultiplier(int learningRate, TuningConfig tuning)
    {
        if (learningRate >= 17) return tuning != null ? tuning.LearningRateHighMultiplier      : 1.30f;
        if (learningRate >= 13) return tuning != null ? tuning.LearningRateAboveAvgMultiplier  : 1.15f;
        if (learningRate >= 9)  return tuning != null ? tuning.LearningRateAvgMultiplier       : 1.00f;
        if (learningRate >= 5)  return tuning != null ? tuning.LearningRateBelowAvgMultiplier  : 0.85f;
        return tuning != null ? tuning.LearningRateLowMultiplier : 0.70f;
    }

    /// <summary>Spec section 10 — Role Fit score (0-100) → XP multiplier.</summary>
    private static float GetRoleFitMultiplier(float roleFitScore, TuningConfig tuning)
    {
        if (roleFitScore >= 80f) return tuning != null ? tuning.RoleFitExcellentMultiplier  : 1.20f;
        if (roleFitScore >= 65f) return tuning != null ? tuning.RoleFitGoodMultiplier        : 1.10f;
        if (roleFitScore >= 45f) return tuning != null ? tuning.RoleFitAvgMultiplier         : 1.00f;
        if (roleFitScore >= 25f) return tuning != null ? tuning.RoleFitBelowAvgMultiplier    : 0.75f;
        return tuning != null ? tuning.RoleFitPoorMultiplier : 0.50f;
    }

    /// <summary>Spec section 12 — Morale → XP multiplier.</summary>
    private static float GetMoraleMultiplier(float morale, TuningConfig tuning)
    {
        if (morale >= 81f) return tuning != null ? tuning.MoraleHighXPMultiplier      : 1.10f;
        if (morale >= 61f) return tuning != null ? tuning.MoraleAboveAvgXPMultiplier  : 1.05f;
        if (morale >= 41f) return tuning != null ? tuning.MoraleAvgXPMultiplier       : 1.00f;
        if (morale >= 21f) return tuning != null ? tuning.MoraleBelowAvgXPMultiplier  : 0.75f;
        return tuning != null ? tuning.MoraleLowXPMultiplier : 0.50f;
    }

    /// <summary>Spec section 14.3 — Best mentor on team boosts learner XP.</summary>
    private static float GetMentoringMultiplier(Employee learner, Team team, EmployeeSystem empSystem, TuningConfig tuning)
    {
        if (team == null || empSystem == null) return 1.0f;

        int requiredGap = tuning != null ? tuning.MentoringSkillGapRequired : 5;
        int bestMentoring = 0;
        int memberCount = team.members.Count;

        SkillId nativeSkill = GetNativeSkillForRole(learner.role);
        int learnerSkillLevel = learner.Stats.Skills[(int)nativeSkill];

        for (int i = 0; i < memberCount; i++)
        {
            var candidate = empSystem.GetEmployee(team.members[i]);
            if (candidate == null || !candidate.isActive) continue;
            if (candidate.id.Value == learner.id.Value) continue;

            int candidateSkillLevel = candidate.Stats.Skills[(int)nativeSkill];
            if (candidateSkillLevel - learnerSkillLevel < requiredGap) continue;

            int candidateMentoring = candidate.Stats.GetHiddenAttribute(HiddenAttributeId.Mentoring);
            if (candidateMentoring > bestMentoring) bestMentoring = candidateMentoring;
        }

        if (bestMentoring == 0) return 1.0f;

        float bonus;
        if (bestMentoring >= 13)
            bonus = tuning != null ? tuning.MentoringExceptionalBonus : 0.12f;
        else if (bestMentoring >= 9)
            bonus = tuning != null ? tuning.MentoringGoodBonus : 0.07f;
        else
            bonus = tuning != null ? tuning.MentoringAvgBonus : 0.03f;

        float maxBonus = tuning != null ? tuning.MentoringMaxBonus : 0.15f;
        if (bonus > maxBonus) bonus = maxBonus;

        return 1.0f + bonus;
    }

    /// <summary>
    /// Role Fit proxy: ratio of employee's current-role CA to their Best Role CA, scaled to 0-100.
    /// Used until a dedicated RoleFitCalculator is available.
    /// </summary>
    private static float ComputeRoleFitScore(Employee employee, AbilitySystem abilitySystem, EmployeeId memberId)
    {
        if (abilitySystem == null) return 50f;
        int bestCA    = abilitySystem.GetBestRoleCA(memberId);
        int currentCA = abilitySystem.GetCurrentRoleCA(memberId);
        if (bestCA <= 0) return 50f;
        float ratio = (float)currentCA / bestCA;
        return ratio * 100f;
    }

    // =========================================================================
    // AccumulateXP — XP accumulation with level-up detection and soft PA ceiling
    // =========================================================================
    private static void AccumulateXP(
        Employee employee,
        int skillIndex,
        float amount,
        RoleWeightBand[] skillBands,
        int potentialAbility,
        List<SkillIncreaseRecord> increases,
        EmployeeId employeeId)
    {
        if (amount <= 0f) return;
        if (skillIndex < 0 || skillIndex >= SkillIdHelper.SkillCount) return;
        if (employee.Stats.Skills[skillIndex] >= 20) return;

        int oldLevel = employee.Stats.Skills[skillIndex];
        employee.Stats.SkillXp[skillIndex] += amount;

        while (employee.Stats.SkillXp[skillIndex] >= 1.0f && employee.Stats.Skills[skillIndex] < 20)
        {
            employee.Stats.Skills[skillIndex]++;
            int newCA = AbilityCalculator.ComputeRoleCA(employee.Stats.Skills, skillBands);
            if (newCA > potentialAbility + PASoftCeilingMargin)
            {
                employee.Stats.Skills[skillIndex]--;
                employee.Stats.SkillXp[skillIndex] = 0f;
                return;
            }
            employee.Stats.SkillXp[skillIndex] -= 1.0f;
        }

        if (employee.Stats.Skills[skillIndex] >= 20)
            employee.Stats.SkillXp[skillIndex] = 0f;

        int finalLevel = employee.Stats.Skills[skillIndex];
        employee.Stats.SkillDeltaDirection[skillIndex] = (sbyte)(finalLevel > oldLevel ? 1 : 0);

        if (finalLevel > oldLevel && increases != null)
        {
            increases.Add(new SkillIncreaseRecord
            {
                EmployeeId = employeeId,
                Skill      = (SkillId)skillIndex,
                OldValue   = oldLevel,
                NewValue   = finalLevel
            });
        }
    }

    // =========================================================================
    // Age decay — spec section 13 (5 brackets, TuningConfig-overrideable)
    // =========================================================================
    private static float GetAgeDecayMultiplier(int age, TuningConfig tuning = null)
    {
        if (tuning != null)
        {
            var brackets    = tuning.SkillAgeDecayBrackets;
            var multipliers = tuning.SkillAgeDecayMultipliers;
            if (brackets != null && multipliers != null)
            {
                for (int i = 0; i < brackets.Length; i++)
                {
                    if (age <= brackets[i])
                        return i < multipliers.Length ? multipliers[i] : multipliers[multipliers.Length - 1];
                }
                return multipliers[multipliers.Length - 1];
            }
        }
        // Default spec section 13 brackets
        if (age <= 24) return 1.15f;
        if (age <= 34) return 1.05f;
        if (age <= 44) return 1.00f;
        if (age <= 54) return 0.90f;
        return 0.75f;
    }

    // =========================================================================
    // Utility lookups
    // =========================================================================
    private static SkillId GetNativeSkillForRole(RoleId role)
    {
        switch (role)
        {
            case RoleId.SoftwareEngineer:          return SkillId.Programming;
            case RoleId.SystemsEngineer:           return SkillId.SystemsArchitecture;
            case RoleId.SecurityEngineer:          return SkillId.Security;
            case RoleId.PerformanceEngineer:       return SkillId.PerformanceOptimisation;
            case RoleId.HardwareEngineer:          return SkillId.HardwareIntegration;
            case RoleId.ManufacturingEngineer:     return SkillId.Manufacturing;
            case RoleId.ProductDesigner:           return SkillId.ProductDesign;
            case RoleId.GameDesigner:              return SkillId.GameDesign;
            case RoleId.TechnicalArtist:           return SkillId.Vfx;
            case RoleId.AudioDesigner:             return SkillId.AudioDesign;
            case RoleId.QaEngineer:                return SkillId.QaTesting;
            case RoleId.TechnicalSupportSpecialist:return SkillId.TechnicalSupport;
            case RoleId.Marketer:                  return SkillId.Marketing;
            case RoleId.SalesExecutive:            return SkillId.Sales;
            case RoleId.Accountant:                return SkillId.Accountancy;
            case RoleId.HrSpecialist:              return SkillId.HrRecruitment;
            default:                               return SkillId.Programming;
        }
    }

    private static RoleWeightBand[] GetSkillBandsForRole(RoleId role, RoleProfileTable roleProfileTable)
    {
        if (roleProfileTable != null)
        {
            var profile = roleProfileTable.Get(role);
            if (profile != null && profile.SkillBands != null) return profile.SkillBands;
        }
        return UniformSkillBands;
    }

    private static RoleWeightBand[] BuildUniformSkillBands()
    {
        int count = SkillIdHelper.SkillCount;
        var bands = new RoleWeightBand[count];
        for (int i = 0; i < count; i++) bands[i] = RoleWeightBand.Secondary;
        return bands;
    }

    public static SkillId? TeamTypeToSkillId(TeamType type)
    {
        switch (type)
        {
            case TeamType.Development: return null;
            case TeamType.Design:      return SkillId.ProductDesign;
            case TeamType.QA:          return SkillId.QaTesting;
            case TeamType.Marketing:   return SkillId.Marketing;
            case TeamType.HR:          return SkillId.HrRecruitment;
            default: return null;
        }
    }

    private static float ComputeOverallQuality(Contract contract)
    {
        return contract.QualityScore > 0f ? contract.QualityScore : 50f;
    }
}

// =========================================================================
// Result and context structs
// =========================================================================

public struct SkillGrowthResult
{
    public List<SkillIncreaseRecord> SkillIncreases;
    public List<AttributeIncreaseRecord> AttributeIncreases;
}

public struct SkillIncreaseRecord
{
    public EmployeeId EmployeeId;
    public SkillId    Skill;
    public int        OldValue;
    public int        NewValue;
}

public struct AttributeIncreaseRecord
{
    public EmployeeId        EmployeeId;
    public VisibleAttributeId Attribute;
    public int               OldValue;
    public int               NewValue;
}

/// <summary>
/// Context passed to ProcessAttributeGrowthTriggers describing what kind of work the employee performed.
/// Flags drive which visible attributes receive tiny XP amounts.
/// </summary>
public struct AttributeGrowthContext
{
    /// <summary>Employee is the team's senior member (triggers Leadership).</summary>
    public bool IsTeamSenior;
    /// <summary>Team successfully delivered work this period (triggers Leadership, Work Ethic).</summary>
    public bool HasTeamDelivery;
    /// <summary>Work was in a creative phase (triggers Creativity).</summary>
    public bool IsCreativePhase;
    /// <summary>Employee completed work reliably within deadline (triggers Focus).</summary>
    public bool HasReliableCompletion;
    /// <summary>Work involved collaboration with teammates (triggers Communication).</summary>
    public bool IsTeamWork;
    /// <summary>Employee worked outside their primary role (triggers Adaptability).</summary>
    public bool IsOffRoleWork;
    /// <summary>Work was performed under high-pressure conditions (triggers Composure).</summary>
    public bool IsPressureWork;
    /// <summary>Employee operated autonomously with minimal direction (triggers Initiative).</summary>
    public bool IsAutonomousTask;
    /// <summary>Override base XP per trigger. If 0, TuningConfig or default is used.</summary>
    public float BaseXpAmount;
}
