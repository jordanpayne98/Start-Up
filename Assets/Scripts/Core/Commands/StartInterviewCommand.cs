public struct StartInterviewCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public HiringMode Mode;
}
