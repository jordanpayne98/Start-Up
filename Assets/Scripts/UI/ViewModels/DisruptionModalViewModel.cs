using System.Collections.Generic;

public class DisruptionModalViewModel : IViewModel
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public List<string> NichesBoosted { get; private set; }
    public List<string> NichesPenalized { get; private set; }
    public string Duration { get; private set; }
    public string ImpactSummary { get; private set; }

    private readonly List<string> _nichesBoosted = new List<string>();
    private readonly List<string> _nichesPenalized = new List<string>();

    public DisruptionModalViewModel() {
        NichesBoosted = _nichesBoosted;
        NichesPenalized = _nichesPenalized;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

    public void Refresh(ActiveDisruption disruption, int currentTick) {
        _nichesBoosted.Clear();
        _nichesPenalized.Clear();

        if (disruption == null) {
            Title = "Disruption";
            Description = "--";
            Duration = "--";
            ImpactSummary = "--";
            return;
        }

        Title = FormatTitle(disruption.EventType);
        Description = disruption.Description ?? FormatDescription(disruption.EventType);

        int ticksLeft = (disruption.StartTick + disruption.DurationTicks) - currentTick;
        int daysLeft = ticksLeft / TimeState.TicksPerDay;
        Duration = daysLeft > 0 ? daysLeft + " days remaining" : "Ending soon";

        ImpactSummary = FormatImpactSummary(disruption);

        if (disruption.AffectedNiche.HasValue) {
            if (disruption.Magnitude >= 0f)
                _nichesBoosted.Add(disruption.AffectedNiche.Value.ToString());
            else
                _nichesPenalized.Add(disruption.AffectedNiche.Value.ToString());
        }
    }

    private static string FormatTitle(DisruptionEventType type) {
        switch (type) {
            case DisruptionEventType.NicheDemandShift:      return "Market Shift";
            case DisruptionEventType.SalarySpike:           return "Salary Spike";
            case DisruptionEventType.CandidateBurst:        return "Candidate Surplus";
            case DisruptionEventType.EconomicBoom:          return "Economic Boom";
            case DisruptionEventType.EconomicDip:           return "Economic Dip";
            case DisruptionEventType.CompetitorScandal:     return "Competitor Scandal";
            case DisruptionEventType.CompetitorPartnership: return "Competitor Partnership";
            case DisruptionEventType.TechParadigmShift:     return "Tech Paradigm Shift";
            case DisruptionEventType.Recession:             return "Recession";
            case DisruptionEventType.RegulatoryOverhaul:    return "Regulatory Overhaul";
            default:                                        return type.ToString();
        }
    }

    private static string FormatDescription(DisruptionEventType type) {
        switch (type) {
            case DisruptionEventType.TechParadigmShift:  return "A major technology shift is reshaping the industry. Existing product quality relevance is penalized.";
            case DisruptionEventType.Recession:          return "An economic recession has hit. All revenue is expected to drop significantly for several months.";
            case DisruptionEventType.RegulatoryOverhaul: return "New regulations require compliance from affected niches or they face revenue penalties.";
            case DisruptionEventType.EconomicBoom:       return "The economy is booming. All segments see short-term revenue increases.";
            case DisruptionEventType.EconomicDip:        return "Economic conditions have weakened. All segments see short-term revenue decreases.";
            default:                                     return type.ToString();
        }
    }

    private static string FormatImpactSummary(ActiveDisruption disruption) {
        float magnitude = disruption.Magnitude;
        string sign = magnitude >= 0f ? "+" : "";
        string pct = sign + ((int)(magnitude * 100f)).ToString() + "%";
        switch (disruption.EventType) {
            case DisruptionEventType.Recession:         return "All revenue: -20% to -30%";
            case DisruptionEventType.TechParadigmShift: return "Quality relevance penalty applied globally";
            case DisruptionEventType.EconomicBoom:      return "All revenue: +" + pct;
            case DisruptionEventType.EconomicDip:       return "All revenue: " + pct;
            case DisruptionEventType.NicheDemandShift:  return "Niche demand: " + pct;
            default:                                    return "Impact: " + pct;
        }
    }
}
