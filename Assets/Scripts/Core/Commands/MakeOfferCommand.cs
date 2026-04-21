public struct MakeOfferCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public int OfferedSalary;
    public HiringMode Mode;
}
