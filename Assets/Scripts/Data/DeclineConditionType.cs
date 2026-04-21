public enum DeclineConditionType
{
    None,
    SalaryFloor,      // MoneyDriven / AmbitionDriven: offeredSalary >= DeclineConditionThreshold
    ReputationFloor   // StabilityDriven / BalanceDriven: globalRep or recRep >= DeclineConditionThreshold
}
