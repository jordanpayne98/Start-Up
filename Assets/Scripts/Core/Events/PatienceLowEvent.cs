public class PatienceLowEvent : GameEvent
{
    public int CandidateId;
    public string Name;
    public int RemainingPatience;
    public bool IsEmployee;

    public PatienceLowEvent(int tick, int candidateId, string name, int remainingPatience, bool isEmployee = false)
        : base(tick)
    {
        CandidateId = candidateId;
        Name = name;
        RemainingPatience = remainingPatience;
        IsEmployee = isEmployee;
    }
}
