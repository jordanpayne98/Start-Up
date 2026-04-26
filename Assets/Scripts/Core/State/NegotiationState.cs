using System;
using System.Collections.Generic;

public enum NegotiationStatus
{
    None,
    Pending,
    Accepted,
    Rejected,
    CounterOffered,
    PatienceExhausted
}

[Serializable]
public struct ActiveNegotiation
{
    public int candidateId;
    public int offeredSalary;
    public NegotiationStatus status;
    public HiringMode mode;
    public OfferPackage lastOffer;
    public CounterOffer counterOffer;
    public bool hasCounterOffer;
    public int maxPatience;
    public int currentPatience;
    public int roundCount;
}

[Serializable]
public struct EmployeeNegotiation
{
    public EmployeeId employeeId;
    public OfferPackage lastOffer;
    public CounterOffer counterOffer;
    public bool hasCounterOffer;
    public NegotiationStatus status;
    public int maxPatience;
    public int currentPatience;
    public int roundCount;
    public int cooldownExpiryTick;
}

[Serializable]
public class NegotiationState
{
    public Dictionary<int, ActiveNegotiation> activeNegotiations;
    public List<EmployeeNegotiation> employeeNegotiations;

    public static NegotiationState CreateNew()
    {
        return new NegotiationState
        {
            activeNegotiations = new Dictionary<int, ActiveNegotiation>(),
            employeeNegotiations = new List<EmployeeNegotiation>()
        };
    }
}
