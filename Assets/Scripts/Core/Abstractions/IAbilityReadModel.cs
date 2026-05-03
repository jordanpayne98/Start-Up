public interface IAbilityReadModel
{
    int GetEmployeeAbility(EmployeeId id);
    int GetEmployeePotential(EmployeeId id);
    int GetEmployeePotentialStars(EmployeeId id);
    CandidatePotentialEstimate GetCandidatePotentialEstimate(int candidateId);
    int ComputeAbilityForRole(int[] skills, RoleId role, RoleProfileTable roleProfileTable);
    int ComputeAbilityForRole(int[] skills, RoleId role);
}

// Confidence level for candidate CA/PA estimates (spec section 15.2)
public enum CandidateConfidenceLevel
{
    Low,
    Medium,
    High,
    Confirmed
}

public struct CandidatePotentialEstimate
{
    public int AbilityMin;
    public int AbilityMax;
    public int PotentialStarsMin; // 1-5
    public int PotentialStarsMax; // 1-5
    public bool ShowAsUnknown;    // true when no information is available

    // Role CA range (spec section 15.2)
    public int RoleCAMin;
    public int RoleCAMax;

    // PA range (spec section 15.2)
    public int PAMin;
    public int PAMax;

    // Confidence label
    public CandidateConfidenceLevel ConfidenceLabel;
}

// Inputs for confidence-based candidate estimate computation (spec section 15.2)
public struct CandidateConfidenceInputs
{
    public HiringMode Source;        // Free = Manual, HR-sourced = HR
    public bool InterviewComplete;
    public float InterviewKnowledge; // 0–1 knowledge level from InterviewSystem
    public int HrSkillAverage;       // average HR skill of sourcing team; -1 = unavailable
}
