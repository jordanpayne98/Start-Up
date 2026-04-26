public struct RejectCounterOfferCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
}
