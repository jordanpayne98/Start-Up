// Per-role monthly salary base bands (in currency units).
// These represent the expected monthly salary of a mid-tier (Competent, CA ~80) candidate.
// CA and Ambition scale up from this base; see CandidateData.ComputeSalary.
public static class SalaryBand
{
    public static int GetBase(RoleId role) {
        switch (role) {
            case RoleId.SoftwareEngineer:           return 6000;
            case RoleId.SystemsEngineer:            return 6500;
            case RoleId.SecurityEngineer:           return 6200;
            case RoleId.PerformanceEngineer:        return 6200;
            case RoleId.HardwareEngineer:           return 6000;
            case RoleId.ManufacturingEngineer:      return 5800;
            case RoleId.ProductDesigner:            return 5500;
            case RoleId.GameDesigner:               return 5500;
            case RoleId.TechnicalArtist:            return 5500;
            case RoleId.AudioDesigner:              return 5500;
            case RoleId.QaEngineer:                 return 4500;
            case RoleId.TechnicalSupportSpecialist: return 4000;
            case RoleId.Marketer:                   return 5500;
            case RoleId.SalesExecutive:             return 5000;
            case RoleId.Accountant:                 return 5000;
            case RoleId.HrSpecialist:               return 5000;
            default:                                return 4500;
        }
    }
}
