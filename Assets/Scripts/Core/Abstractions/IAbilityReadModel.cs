public interface IAbilityReadModel
{
    int GetEmployeeAbility(EmployeeId id);
    int GetEmployeePotential(EmployeeId id);
    int GetEmployeePotentialStars(EmployeeId id);
    CandidatePotentialEstimate GetCandidatePotentialEstimate(int candidateId);
    int ComputeAbilityForRole(int[] skills, RoleId role, RoleProfileTable roleProfileTable);
    int ComputeAbilityForRole(int[] skills, RoleId role);
}

public struct CandidatePotentialEstimate
{
    public int AbilityMin;
    public int AbilityMax;
    public int PotentialStarsMin; // 1-5
    public int PotentialStarsMax; // 1-5
    public bool ShowAsUnknown; // true when no HR employees exist in Manual mode
}
