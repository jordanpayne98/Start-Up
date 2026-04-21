// NewsReportGenerator Version: Clean v1
using System;
using System.Collections.Generic;

[Serializable]
public class NicheTrendEntry
{
    public ProductNiche Niche;
    public MarketTrend Trend;
    public float Demand;
    public float DemandDelta;
}

[Serializable]
public class CompanyRankEntry
{
    public CompetitorId? Id;
    public string CompanyName;
    public float MarketShareDelta;
}

[Serializable]
public class ProductRankEntry
{
    public ProductId Id;
    public string ProductName;
    public string CompanyName;
    public int NewUsers;
}

[Serializable]
public class MonthlyNewsReport
{
    public int ReportMonth;
    public int ReportYear;
    public List<NicheTrendEntry> NicheTrends;
    public List<CompanyRankEntry> TopGainers;
    public List<CompanyRankEntry> TopLosers;
    public List<ProductRankEntry> TopProducts;
    public List<string> IndustryEvents;
    public List<ShowdownResult> Showdowns;
}

public static class NewsReportGenerator
{
    private const int TopN = 3;

    public static MonthlyNewsReport Generate(
        MarketState market,
        CompetitorState competitors,
        ProductState products,
        DisruptionSystem disruptions,
        int currentTick)
    {
        int totalDays = currentTick / TimeState.TicksPerDay;
        int reportMonth = TimeState.GetMonth(totalDays);
        int reportYear = TimeState.GetYear(totalDays);

        var report = new MonthlyNewsReport
        {
            ReportMonth = reportMonth,
            ReportYear = reportYear,
            NicheTrends = BuildNicheTrends(market),
            TopGainers = new List<CompanyRankEntry>(TopN),
            TopLosers = new List<CompanyRankEntry>(TopN),
            TopProducts = BuildTopProducts(products, competitors),
            IndustryEvents = BuildIndustryEvents(disruptions),
            Showdowns = new List<ShowdownResult>()
        };

        BuildCompanyRankings(competitors, report.TopGainers, report.TopLosers);

        return report;
    }

    private static List<NicheTrendEntry> BuildNicheTrends(MarketState market)
    {
        var entries = new List<NicheTrendEntry>(market.nicheDemand.Count);
        foreach (var kvp in market.nicheDemand)
        {
            var niche = kvp.Key;
            float demand = kvp.Value;
            market.nicheTrends.TryGetValue(niche, out MarketTrend trend);

            float prevDemand = demand;
            if (trend == MarketTrend.Rising)
                prevDemand = demand - (market.nicheMomentum.TryGetValue(niche, out float mom) ? mom : 0f);
            else if (trend == MarketTrend.Falling)
                prevDemand = demand - (market.nicheMomentum.TryGetValue(niche, out float mom2) ? mom2 : 0f);

            entries.Add(new NicheTrendEntry
            {
                Niche = niche,
                Trend = trend,
                Demand = demand,
                DemandDelta = demand - prevDemand
            });
        }
        return entries;
    }

    private static void BuildCompanyRankings(
        CompetitorState competitors,
        List<CompanyRankEntry> topGainers,
        List<CompanyRankEntry> topLosers)
    {
        var allEntries = new List<CompanyRankEntry>(competitors.competitors.Count);

        foreach (var kvp in competitors.competitors)
        {
            var comp = kvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            float shareDelta = 0f;
            if (comp.NicheMarketShare != null)
            {
                foreach (var shareKvp in comp.NicheMarketShare)
                    shareDelta += shareKvp.Value;
            }

            allEntries.Add(new CompanyRankEntry
            {
                Id = kvp.Key,
                CompanyName = comp.CompanyName,
                MarketShareDelta = shareDelta
            });
        }

        allEntries.Sort((a, b) => b.MarketShareDelta.CompareTo(a.MarketShareDelta));

        int gainCount = allEntries.Count < TopN ? allEntries.Count : TopN;
        for (int i = 0; i < gainCount; i++)
            topGainers.Add(allEntries[i]);

        int loserStart = allEntries.Count - 1;
        int loserCount = 0;
        for (int i = loserStart; i >= 0 && loserCount < TopN; i--, loserCount++)
            topLosers.Add(allEntries[i]);
    }

    private static List<ProductRankEntry> BuildTopProducts(ProductState products, CompetitorState competitors)
    {
        var all = new List<ProductRankEntry>(16);

        foreach (var kvp in products.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;

            string companyName = "Player";
            if (product.IsCompetitorProduct &&
                competitors.competitors.TryGetValue(product.OwnerCompanyId.ToCompetitorId(), out var owner))
            {
                companyName = owner.CompanyName;
            }

            int newUsers = product.ActiveUserCount - product.PreviousActiveUsers;
            if (newUsers <= 0) continue;

            all.Add(new ProductRankEntry
            {
                Id = product.Id,
                ProductName = product.ProductName,
                CompanyName = companyName,
                NewUsers = newUsers
            });
        }

        all.Sort((a, b) => b.NewUsers.CompareTo(a.NewUsers));

        var result = new List<ProductRankEntry>(TopN);
        int count = all.Count < TopN ? all.Count : TopN;
        for (int i = 0; i < count; i++)
            result.Add(all[i]);

        return result;
    }

    private static List<string> BuildIndustryEvents(DisruptionSystem disruptions)
    {
        var events = new List<string>();
        if (disruptions == null) return events;

        var active = disruptions.GetActiveDisruptions();
        int count = active.Count;
        for (int i = 0; i < count; i++)
        {
            if (!string.IsNullOrEmpty(active[i].Description))
                events.Add(active[i].Description);
        }
        return events;
    }
}
