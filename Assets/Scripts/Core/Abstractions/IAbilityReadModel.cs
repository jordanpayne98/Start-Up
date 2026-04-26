public interface IAbilityReadModel
{
    int GetEmployeeAbility(EmployeeId id);
    int GetEmployeePotential(EmployeeId id);
    int GetEmployeePotentialStars(EmployeeId id);
    // HR Skill average of the sourcing team determines Ability/Potential estimate accuracy.
    HiddenAttributes GetEmployeeHiddenAttributes(EmployeeId id);
    CandidatePotentialEstimate GetCandidatePotentialEstimate(int candidateId);
    // Returns the role-weighted ability score for any role given a raw skill array.
    int ComputeAbilityForRole(int[] skills, EmployeeRole role);
}

public struct CandidatePotentialEstimate
{
    public int AbilityMin;
    public int AbilityMax;
    public int PotentialStarsMin; // 1-5
    public int PotentialStarsMax; // 1-5
    public bool ShowAsUnknown; // true when no HR employees exist in Manual mode
}
