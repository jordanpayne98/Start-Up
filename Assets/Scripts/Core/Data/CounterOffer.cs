using System;

[Serializable]
public struct CounterOffer
{
    public int CandidateId;
    public int CounterSalary;
    public RoleId CounterRole;
    public EmploymentType CounterType;
    public ContractLengthOption CounterLength;
    public int CreatedTick;
    public int ExpiryTick;
}
