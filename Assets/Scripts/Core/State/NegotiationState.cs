using System;
using System.Collections.Generic;

public enum NegotiationStatus
{
    None,
    Pending,
    Accepted,
    Rejected
}

[Serializable]
public struct ActiveNegotiation
{
    public int candidateId;
    public int offeredSalary;
    public NegotiationStatus status;
    public HiringMode mode;
}

[Serializable]
public class NegotiationState
{
    public Dictionary<int, ActiveNegotiation> activeNegotiations;

    public static NegotiationState CreateNew()
    {
        return new NegotiationState
        {
            activeNegotiations = new Dictionary<int, ActiveNegotiation>()
        };
    }
}
