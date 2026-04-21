public struct NegotiationProbabilities
{
    public int AcceptChance;        // 0-100
    public int CounterChance;       // 0-100
    public int DeclineChance;       // 0-100 (AcceptChance + CounterChance + DeclineChance = 100)
    public int SalaryModifier;      // e.g. -30 (shown as "Salary Gap   -30%")
    public int RecruitRepModifier;  // e.g. +15
    public int AmbitionRepModifier; // e.g. +8 — candidate's self-worth vs offered salary
    public int SeniorityModifier;   // e.g. -10
    public int PriorityModifier;    // e.g. +12
    public bool IsValid;            // false if candidate not found or not hireable
}
