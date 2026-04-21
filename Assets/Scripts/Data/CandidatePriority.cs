public enum CandidatePriority
{
    MoneyDriven,      // weights salary gap heavily; amplified by Ambition hidden attr
    AmbitionDriven,   // cares about salary AND company growth/reputation (replaces GrowthDriven)
    StabilityDriven,  // wants established company; lower bar if Rec rep is high (replaces PrestigeDriven)
    BalanceDriven     // evenly spread weights, hardest to read
}
