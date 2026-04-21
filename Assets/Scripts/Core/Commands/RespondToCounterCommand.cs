public struct RespondToCounterCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public bool Accept;
}
