public struct DismissCandidateCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
}
