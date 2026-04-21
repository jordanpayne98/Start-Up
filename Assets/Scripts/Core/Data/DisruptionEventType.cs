public enum DisruptionEventType
{
    NicheDemandShift,      // Minor: specific niche demand up/down
    SalarySpike,           // Minor: specific role salary spike
    CandidateBurst,        // Minor: burst of candidates in market
    EconomicBoom,          // Minor: short-term revenue boost all segments
    EconomicDip,           // Minor: short-term revenue drop all segments
    CompetitorScandal,     // Minor: competitor loses market share
    CompetitorPartnership, // Minor: competitor gains temporary boost
    TechParadigmShift,     // Major: niche demand restructure + quality relevance penalty
    Recession,             // Major: all revenue drops 20-30% for 6 months
    RegulatoryOverhaul     // Major: specific niches require compliance or face penalties
}
