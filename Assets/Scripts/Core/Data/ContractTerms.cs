public struct ContractTerms
{
    public EmploymentType Type;
    public ContractLengthOption Length;
    public int ContractMonths;
    public int MonthlySalary;
    public int HiredTick;
    public float WorkCapacity;
    public float Efficiency;
    public float EffectiveOutput;

    public static ContractTerms FromOffer(EmploymentOffer offer, int currentTick)
    {
        int months = ContractLengthHelper.GetDurationMonths(offer.Length, offer.Type);
        float capacity = SalaryModifierCalculator.GetWorkCapacity(offer.Type);
        float efficiency = SalaryModifierCalculator.GetEfficiency(offer.Type);
        float output = SalaryModifierCalculator.GetEffectiveOutput(offer.Type);
        return new ContractTerms
        {
            Type           = offer.Type,
            Length         = offer.Length,
            ContractMonths = months,
            MonthlySalary  = offer.MonthlySalary,
            HiredTick      = currentTick,
            WorkCapacity   = capacity,
            Efficiency     = efficiency,
            EffectiveOutput = output
        };
    }
}
