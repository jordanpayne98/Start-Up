using System;
using System.Collections.Generic;

[Serializable]
public class RecruitmentReputationState
{
    public int score;
    public int lastDecayDay;
    public Dictionary<EmployeeId, bool> loyaltyBonusAwarded;

    public static RecruitmentReputationState CreateNew()
    {
        return new RecruitmentReputationState
        {
            score = 50,
            lastDecayDay = 0,
            loyaltyBonusAwarded = new Dictionary<EmployeeId, bool>()
        };
    }
}
