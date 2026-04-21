// Per-role monthly salary base bands (in currency units).
// These represent the expected monthly salary of a mid-tier (Competent, CA ~80) candidate.
// CA and Ambition scale up from this base; see CandidateData.ComputeSalary.
public static class SalaryBand
{
    public static int GetBase(EmployeeRole role) {
        switch (role) {
            case EmployeeRole.Developer:     return 6000;
            case EmployeeRole.Designer:      return 5500;
            case EmployeeRole.QAEngineer:    return 4500;
            case EmployeeRole.HR:            return 5000;
            case EmployeeRole.SoundEngineer: return 5500;
            case EmployeeRole.VFXArtist:     return 5500;
            case EmployeeRole.Accountant:    return 5000;
            case EmployeeRole.Marketer:      return 5500;
            default:                         return 4500;
        }
    }
}
