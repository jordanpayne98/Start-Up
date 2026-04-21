using System;
using System.Collections.Generic;

[Serializable]
public struct CompanyFinance
{
    public long Cash;
    public long MonthlyRevenue;
    public long MonthlyExpenses;
    public long MonthlyProfit;
    public int ConsecutiveNegativeCashMonths;
}

[Serializable]
public class Competitor
{
    public CompetitorId Id;
    public string CompanyName;
    public string FounderName;
    public CompetitorArchetype Archetype;
    public CompetitorPersonality Personality;
    public ProductCategory[] Specializations;
    public bool IsFounderNamed;
    public CompanyFinance Finance;
    public bool IsBankrupt;
    public bool IsAbsorbed;
    public CompetitorId? AbsorbedById;
    public int FoundedTick;
    public Dictionary<ProductNiche, float> NicheMarketShare;
    public List<ProductId> ActiveProductIds;
    public List<ProductId> InDevelopmentProductIds;
    public List<EmployeeId> EmployeeIds;
    public Dictionary<TeamId, ProductId> TeamAssignments;
    public int LastProductEvalTick;
    public int LastFinanceEvalTick;
    public int ReputationPoints;
    public int LastHireTick;
    public int LastProductStartedTick;
    public int LastPricingReviewTick;
    public int CompanyFans;
    public float FanSentiment;
    public CompetitorTaxRecord TaxRecord;
    public CompetitorMemory Memory;
    public CompetitorMomentum Momentum;
    public int LastStrategicReviewTick;
    public int CompetitorEra;
}

[Serializable]
public class CompetitorState
{
    public Dictionary<CompetitorId, Competitor> competitors;
    public int nextCompetitorId;
    public int maxCompetitorCap;
    public int lastSpawnCheckTick;

    public static CompetitorState CreateNew()
    {
        return new CompetitorState
        {
            competitors = new Dictionary<CompetitorId, Competitor>(),
            nextCompetitorId = 1,
            maxCompetitorCap = 10,
            lastSpawnCheckTick = 0
        };
    }
}
