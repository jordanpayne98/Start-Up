// Pure static utility — no state, no Unity refs, no allocations.
// Computes satisfaction scores and determines negotiation outcomes.
public enum OfferOutcome
{
    Accept,
    Counter,
    Reject
}

public static class OfferEvaluator
{
    private const float AcceptThreshold  = 80f;
    private const float CounterThreshold = 40f;

    // Returns a 0-100 satisfaction score for the given offer.
    // Salary contributes 50 pts, role 20 pts, FT/PT 15 pts, length 15 pts.
    public static float ComputeSatisfaction(OfferPackage offer, int computedDemand,
        EmployeeRole preferredRole, CandidatePreferences prefs, RoleSuitability offeredRoleSuitability)
    {
        float salaryScore = SalaryCurve(computedDemand > 0 ? (float)offer.OfferedSalary / computedDemand : 0f) * 50f;
        float roleScore   = RoleFactor(preferredRole, offer.OfferedRole, offeredRoleSuitability) * 20f;
        float ftPtScore   = FtPtFactor(prefs.FtPtPref, offer.EmploymentType) * 15f;
        float lengthScore = LengthFactor(prefs.LengthPref, offer.Length) * 15f;
        return salaryScore + roleScore + ftPtScore + lengthScore;
    }

    // Maps satisfaction score to an outcome.
    public static OfferOutcome Evaluate(float satisfaction)
    {
        if (satisfaction >= AcceptThreshold)  return OfferOutcome.Accept;
        if (satisfaction >= CounterThreshold) return OfferOutcome.Counter;
        return OfferOutcome.Reject;
    }

    // Returns true if the gamble succeeds (candidate accepts same terms again).
    // adaptability is HiddenAttributes.Adaptability (1-20 scale → up to 20% chance).
    public static bool EvaluateSameTermsGamble(float adaptability, IRng rng)
    {
        return rng.NextFloat01() < adaptability / 100f;
    }

    // Computes max patience rounds from hidden attributes. Range 2-6.
    public static int ComputeMaxPatience(HiddenAttributes hidden)
    {
        float adaptBonus = hidden.Adaptability / 10f;
        float ambitionPenalty = hidden.Ambition / 20f;
        int patience = 3 + (int)adaptBonus - (int)ambitionPenalty;
        if (patience < 2) patience = 2;
        if (patience > 6) patience = 6;
        return patience;
    }

    // Generates a counter-offer from the candidate based on the player's offer and their preferences.
    public static CounterOffer GenerateCounter(OfferPackage original, CandidateData candidate, int computedDemand, int currentTick)
    {
        int mid = (original.OfferedSalary + computedDemand * 2) / 3;
        int counterSalary = SalaryDemandCalculator.Round50(mid);
        if (counterSalary < 500) counterSalary = 500;

        EmploymentType counterType = original.EmploymentType;
        if (candidate.Preferences.FtPtPref != FtPtPreference.Flexible)
        {
            counterType = candidate.Preferences.FtPtPref == FtPtPreference.PrefersFullTime
                ? EmploymentType.FullTime
                : EmploymentType.PartTime;
        }

        ContractLengthOption counterLength = original.Length;
        if (candidate.Preferences.LengthPref != LengthPreference.NoPreference)
        {
            counterLength = candidate.Preferences.LengthPref == LengthPreference.PrefersSecurity
                ? ContractLengthOption.Long
                : ContractLengthOption.Short;
        }

        return new CounterOffer
        {
            CandidateId  = candidate.CandidateId,
            CounterSalary = counterSalary,
            CounterRole  = candidate.Role,
            CounterType  = counterType,
            CounterLength = counterLength,
            CreatedTick  = currentTick,
            ExpiryTick   = currentTick + 5 * TimeState.TicksPerDay
        };
    }

    // ─── Private scoring helpers ──────────────────────────────────────────────

    // Piecewise linear salary curve with 5 control points.
    // (0.6→0.0), (0.8→0.3), (1.0→0.7), (1.2→0.95), (1.5+→1.0)
    private static float SalaryCurve(float ratio)
    {
        if (ratio <= 0.6f) return 0f;
        if (ratio >= 1.5f) return 1f;
        if (ratio < 0.8f)  return Lerp(0f,  0.30f, (ratio - 0.6f) / 0.2f);
        if (ratio < 1.0f)  return Lerp(0.30f, 0.70f, (ratio - 0.8f) / 0.2f);
        if (ratio < 1.2f)  return Lerp(0.70f, 0.95f, (ratio - 1.0f) / 0.2f);
        return Lerp(0.95f, 1.0f,  (ratio - 1.2f) / 0.3f);
    }

    // Role factor: preferred role = 1.0, off-role scales down by suitability.
    private static float RoleFactor(EmployeeRole preferred, EmployeeRole offered, RoleSuitability suitability)
    {
        if (preferred == offered) return 1.0f;
        switch (suitability)
        {
            case RoleSuitability.Natural:      return 0.6f;
            case RoleSuitability.Accomplished: return 0.4f;
            case RoleSuitability.Competent:    return 0.2f;
            case RoleSuitability.Awkward:
            case RoleSuitability.Unsuitable:   return 0.0f;
            default:                           return 0.0f;
        }
    }

    // FT/PT factor: matched=1.0, flexible=0.7, mismatched=0.2
    private static float FtPtFactor(FtPtPreference pref, EmploymentType offered)
    {
        if (pref == FtPtPreference.Flexible)
            return 0.7f;
        if (pref == FtPtPreference.PrefersFullTime)
            return offered == EmploymentType.FullTime ? 1.0f : 0.2f;
        return offered == EmploymentType.PartTime ? 1.0f : 0.2f;
    }

    // Length factor: matched=1.0, neutral=0.7, mismatched=0.2
    private static float LengthFactor(LengthPreference pref, ContractLengthOption offered)
    {
        if (pref == LengthPreference.NoPreference)
            return 0.7f;
        if (pref == LengthPreference.PrefersSecurity)
        {
            if (offered == ContractLengthOption.Long)     return 1.0f;
            if (offered == ContractLengthOption.Standard) return 0.7f;
            return 0.2f;
        }
        if (offered == ContractLengthOption.Short)    return 1.0f;
        if (offered == ContractLengthOption.Standard) return 0.7f;
        return 0.2f;
    }

    private static float Lerp(float a, float b, float t)
    {
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        return a + (b - a) * t;
    }
}
