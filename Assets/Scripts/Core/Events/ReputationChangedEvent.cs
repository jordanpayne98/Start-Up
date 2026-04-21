public class ReputationChangedEvent : GameEvent
{
    public int CurrentReputation;
    public ReputationTier CurrentTier;
    public ReputationTier PreviousTier;
    public int Delta;
    public int CompanyFans;
    public float FanSentiment;

    public ReputationChangedEvent(int tick, int currentReputation, ReputationTier currentTier, ReputationTier previousTier, int delta, int companyFans, float fanSentiment) : base(tick)
    {
        CurrentReputation = currentReputation;
        CurrentTier = currentTier;
        PreviousTier = previousTier;
        Delta = delta;
        CompanyFans = companyFans;
        FanSentiment = fanSentiment;
    }
}
