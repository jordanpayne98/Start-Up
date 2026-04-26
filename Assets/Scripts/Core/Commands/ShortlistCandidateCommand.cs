public class ShortlistCandidateCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public int DurationDays;
}
