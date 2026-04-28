// SalaryModifierCalculator Version: v3

/// <summary>
/// Pure static utility. Computes salary modifiers from employment type, contract length,
/// role mismatch, and candidate preference mismatches. No Unity refs, no state, no allocations.
/// </summary>
public static class SalaryModifierCalculator
{
    private const float PTRatio         = 0.60f;
    private const float FTWorkCapacity  = 1.00f;
    private const float PTWorkCapacity  = 0.60f;
    private const float FTEfficiency    = 1.00f;
    private const float PTEfficiency    = 0.85f;
    private const float FTOutput        = 1.00f;  // FTWorkCapacity * FTEfficiency
    private const float PTOutput        = 0.51f;  // PTWorkCapacity * PTEfficiency

    // FT/PT mismatch premiums (Section 10.1)
    private const float PrefFTOfferedFT  = 0.00f;
    private const float PrefFTOfferedPT  = 0.20f;
    private const float FlexOfferedFT    = 0.00f;
    private const float FlexOfferedPT    = 0.15f;
    private const float PrefPTOfferedFT  = 0.15f;
    private const float PrefPTOfferedPT  = 0.00f;

    // Length mismatch premiums (Section 10.2)
    private const float SecPrefShort     = 0.10f;
    private const float SecPrefStandard  = 0.00f;
    private const float SecPrefLong      = -0.05f;
    private const float NoPrefAny        = 0.00f;
    private const float FlexPrefShort    = -0.05f;
    private const float FlexPrefStandard = 0.00f;
    private const float FlexPrefLong     = 0.10f;

    // Returns the salary premium for offering a role different from the candidate's preferred role.
    // Same role → 0.0. Off-role premium scales with how poorly they fit the offered role.
    public static float GetRoleMismatchPremium(RoleId preferred, RoleId offered, RoleSuitability suitability)
    {
        if (preferred == offered) return 0f;
        switch (suitability)
        {
            case RoleSuitability.Natural:      return 0.10f;
            case RoleSuitability.Accomplished: return 0.20f;
            case RoleSuitability.Competent:    return 0.35f;
            case RoleSuitability.Awkward:      return 0.50f;
            case RoleSuitability.Unsuitable:   return 0.75f;
            default:                           return 0f;
        }
    }

    public static float GetFtPtMismatchPremium(FtPtPreference pref, EmploymentType offered)
    {
        switch (pref)
        {
            case FtPtPreference.PrefersFullTime:
                return offered == EmploymentType.FullTime ? PrefFTOfferedFT : PrefFTOfferedPT;
            case FtPtPreference.Flexible:
                return offered == EmploymentType.FullTime ? FlexOfferedFT : FlexOfferedPT;
            case FtPtPreference.PrefersPartTime:
                return offered == EmploymentType.PartTime ? PrefPTOfferedPT : PrefPTOfferedFT;
            default:
                return 0f;
        }
    }

    public static float GetLengthMismatchPremium(LengthPreference pref, ContractLengthOption offered)
    {
        switch (pref)
        {
            case LengthPreference.PrefersSecurity:
                switch (offered)
                {
                    case ContractLengthOption.Short:    return SecPrefShort;
                    case ContractLengthOption.Standard: return SecPrefStandard;
                    case ContractLengthOption.Long:     return SecPrefLong;
                    default:                            return 0f;
                }
            case LengthPreference.NoPreference:
                return NoPrefAny;
            case LengthPreference.PrefersFlexibility:
                switch (offered)
                {
                    case ContractLengthOption.Short:    return FlexPrefShort;
                    case ContractLengthOption.Standard: return FlexPrefStandard;
                    case ContractLengthOption.Long:     return FlexPrefLong;
                    default:                            return 0f;
                }
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Computes the final salary demand for an offer. Premium stacking is multiplicative:
    /// base → PT ratio → length modifier → FT/PT mismatch → length mismatch → role mismatch.
    /// </summary>
    public static int ComputeOfferSalary(int baseSalary, EmploymentType type, ContractLengthOption length,
        CandidatePreferences prefs, RoleId preferredRole, RoleId offeredRole, RoleSuitability suitability)
    {
        float running = baseSalary;

        if (type == EmploymentType.PartTime)
            running *= PTRatio;

        running *= ContractLengthHelper.GetSalaryModifier(length);

        float ftPtPremium = GetFtPtMismatchPremium(prefs.FtPtPref, type);
        running *= 1f + ftPtPremium;

        float lengthPremium = GetLengthMismatchPremium(prefs.LengthPref, length);
        running *= 1f + lengthPremium;

        float roleMismatchPremium = GetRoleMismatchPremium(preferredRole, offeredRole, suitability);
        running *= 1f + roleMismatchPremium;

        return SalaryDemandCalculator.Round50(running);
    }

    /// <summary>
    /// Overload without role mismatch — for callers that offer the same role as preferred.
    /// </summary>
    public static int ComputeOfferSalary(int baseSalary, EmploymentType type, ContractLengthOption length, CandidatePreferences prefs)
    {
        return ComputeOfferSalary(baseSalary, type, length, prefs,
            RoleId.SoftwareEngineer, RoleId.SoftwareEngineer, RoleSuitability.Natural);
    }

    public static float GetWorkCapacity(EmploymentType type)
    {
        return type == EmploymentType.FullTime ? FTWorkCapacity : PTWorkCapacity;
    }

    public static float GetEfficiency(EmploymentType type)
    {
        return type == EmploymentType.FullTime ? FTEfficiency : PTEfficiency;
    }

    public static float GetEffectiveOutput(EmploymentType type)
    {
        return type == EmploymentType.FullTime ? FTOutput : PTOutput;
    }

    public static float ComputeValueEfficiency(int salary, EmploymentType type, int marketRate)
    {
        if (salary <= 0) return 0f;
        float output = GetEffectiveOutput(type);
        float salaryPerOutput = salary / output;
        return marketRate / salaryPerOutput * 100f;
    }

    /// <summary>
    /// Computes the renewal salary demand for an existing employee.
    /// Starts from the market rate, applies PT ratio, length modifier, preference premiums,
    /// role mismatch premium, and a per-strike 5% uplift for unresolved change requests.
    /// </summary>
    public static int ComputeRenewalDemand(Employee emp, int marketRate, EmploymentType type,
        ContractLengthOption length, int strikeCount,
        RoleId preferredRole = RoleId.SoftwareEngineer, RoleId offeredRole = RoleId.SoftwareEngineer,
        RoleSuitability suitability = RoleSuitability.Natural)
    {
        if (emp == null || marketRate <= 0) return 0;
        float running = marketRate;

        if (type == EmploymentType.PartTime)
            running *= PTRatio;

        running *= ContractLengthHelper.GetSalaryModifier(length);

        float ftPtPremium = GetFtPtMismatchPremium(emp.OriginalPreferences.FtPtPref, type);
        running *= 1f + ftPtPremium;

        float lengthPremium = GetLengthMismatchPremium(emp.OriginalPreferences.LengthPref, length);
        running *= 1f + lengthPremium;

        float roleMismatchPremium = GetRoleMismatchPremium(preferredRole, offeredRole, suitability);
        running *= 1f + roleMismatchPremium;

        if (strikeCount > 0)
            running *= 1f + strikeCount * 0.05f;

        return SalaryDemandCalculator.Round50(running);
    }

    public static PreferenceMatchState ComputePreferenceMatch(
        CandidatePreferences prefs,
        EmploymentType currentType,
        ContractLengthOption currentLength)
    {
        int ftPtResult = EvaluateFtPtMatch(prefs.FtPtPref, currentType);
        int lengthResult = EvaluateLengthMatch(prefs.LengthPref, currentLength);

        int matched = 0;
        int neutral = 0;
        int mismatched = 0;

        if (ftPtResult > 0) matched++;
        else if (ftPtResult == 0) neutral++;
        else mismatched++;

        if (lengthResult > 0) matched++;
        else if (lengthResult == 0) neutral++;
        else mismatched++;

        if (mismatched == 2) return PreferenceMatchState.BothMismatched;
        if (mismatched == 1) return PreferenceMatchState.OneMismatched;
        if (matched == 2) return PreferenceMatchState.BothMatched;
        if (matched == 1) return PreferenceMatchState.OneMatchedOneNeutral;
        return PreferenceMatchState.BothNeutral;
    }

    private static int EvaluateFtPtMatch(FtPtPreference pref, EmploymentType currentType) {
        if (pref == FtPtPreference.Flexible) return 0;
        if (pref == FtPtPreference.PrefersFullTime)
            return currentType == EmploymentType.FullTime ? 1 : -1;
        return currentType == EmploymentType.PartTime ? 1 : -1;
    }

    private static int EvaluateLengthMatch(LengthPreference pref, ContractLengthOption currentLength) {
        if (pref == LengthPreference.NoPreference) return 0;
        if (currentLength == ContractLengthOption.Standard) return 0;
        if (pref == LengthPreference.PrefersSecurity)
            return currentLength == ContractLengthOption.Long ? 1 : -1;
        return currentLength == ContractLengthOption.Short ? 1 : -1;
    }
}
