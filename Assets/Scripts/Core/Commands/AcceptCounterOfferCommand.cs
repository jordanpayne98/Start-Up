public struct AcceptCounterOfferCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
}
