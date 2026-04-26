public class CounterOfferReceivedEvent : GameEvent
{
    public int CandidateId;
    public string CandidateName;
    public CounterOffer Counter;
    public int RemainingPatience;

    public CounterOfferReceivedEvent(int tick, int candidateId, string candidateName, CounterOffer counter, int remainingPatience = 0)
        : base(tick)
    {
        CandidateId = candidateId;
        CandidateName = candidateName;
        Counter = counter;
        RemainingPatience = remainingPatience;
    }
}
