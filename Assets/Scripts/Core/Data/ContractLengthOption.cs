public enum ContractLengthOption
{
    Short,
    Standard,
    Long
}

public static class ContractLengthHelper
{
    public static int GetDurationMonths(ContractLengthOption option, EmploymentType type)
    {
        if (type == EmploymentType.FullTime)
        {
            switch (option)
            {
                case ContractLengthOption.Short:    return 6;
                case ContractLengthOption.Standard: return 12;
                case ContractLengthOption.Long:     return 18;
                default:                            return 12;
            }
        }
        else
        {
            switch (option)
            {
                case ContractLengthOption.Short:    return 3;
                case ContractLengthOption.Standard: return 6;
                case ContractLengthOption.Long:     return 12;
                default:                            return 6;
            }
        }
    }

    public static float GetSalaryModifier(ContractLengthOption option)
    {
        switch (option)
        {
            case ContractLengthOption.Short:    return 1.05f;
            case ContractLengthOption.Standard: return 1.00f;
            case ContractLengthOption.Long:     return 0.95f;
            default:                            return 1.00f;
        }
    }
}
