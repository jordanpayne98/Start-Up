using System.Collections.Generic;

public struct HoldingRowVM
{
    public CompetitorId CompanyId;
    public string CompanyName;
    public string PercentOwned;
    public string CurrentValue;
    public string LastDividend;
    public string ProfitLoss;
    public bool CanBuyMore;
    public bool CanBuyout;
}

public struct WatchlistRowVM
{
    public CompetitorId CompanyId;
    public string CompanyName;
    public string StockPrice;
    public string Revenue;
    public string MarketShare;
}

public class MyInvestmentsViewModel : IViewModel
{
    private readonly List<HoldingRowVM> _holdings = new List<HoldingRowVM>();
    private readonly List<WatchlistRowVM> _watchlist = new List<WatchlistRowVM>();

    public List<HoldingRowVM> Holdings => _holdings;
    public List<WatchlistRowVM> Watchlist => _watchlist;
    public string TotalPortfolioValue { get; private set; }
    public string TotalAnnualDividends { get; private set; }
    public int CompaniesInvested { get; private set; }
    public string ClosestToBuyout { get; private set; }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        var snapshot = state as GameStateSnapshot;
        if (snapshot == null) return;
        Refresh(snapshot.StockState, snapshot.CompetitorState);
    }

    public void Refresh(StockState stockState, CompetitorState compState) {
        if (stockState == null || compState == null) return;

        _holdings.Clear();
        _watchlist.Clear();

        long portfolioValue = 0L;
        long annualDividends = 0L;
        float closestPercent = 0f;
        string closestName = "";

        foreach (var holdingKvp in stockState.holdings) {
            var holding = holdingKvp.Value;
            if (!holding.IsPlayerOwned) continue;

            if (!compState.competitors.TryGetValue(holding.TargetCompanyId, out var comp)) continue;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            long currentValue = 0L;
            long lastDividend = 0L;
            float unowned = 1f;

            if (stockState.listings.TryGetValue(holding.TargetCompanyId, out var listing)) {
                currentValue = (long)(listing.StockPrice * holding.PercentageOwned);
                lastDividend = (long)(listing.LastDividendPayout * holding.PercentageOwned);
                unowned = listing.UnownedPercentage;
            }

            portfolioValue += currentValue;
            annualDividends += lastDividend * 12;

            long profitLossVal = currentValue - (long)(holding.PurchasePrice * holding.PercentageOwned);

            bool canBuyout = holding.PercentageOwned >= 0.5f && unowned > 0f;
            bool canBuyMore = !canBuyout && unowned > 0f;

            if (holding.PercentageOwned > closestPercent) {
                closestPercent = holding.PercentageOwned;
                closestName = comp.CompanyName;
            }

            _holdings.Add(new HoldingRowVM {
                CompanyId = holding.TargetCompanyId,
                CompanyName = comp.CompanyName,
                PercentOwned = UIFormatting.FormatPercent(holding.PercentageOwned),
                CurrentValue = UIFormatting.FormatMoney(currentValue),
                LastDividend = UIFormatting.FormatMoney(lastDividend),
                ProfitLoss = UIFormatting.FormatMoney(profitLossVal),
                CanBuyMore = canBuyMore,
                CanBuyout = canBuyout
            });
        }

        CompaniesInvested = _holdings.Count;
        TotalPortfolioValue = UIFormatting.FormatMoney(portfolioValue);
        TotalAnnualDividends = UIFormatting.FormatMoney(annualDividends);
        ClosestToBuyout = string.IsNullOrEmpty(closestName) ? "--"
            : closestName + " (" + UIFormatting.FormatPercent(closestPercent) + ")";

        foreach (var compKvp in compState.competitors) {
            var comp = compKvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            bool alreadyHeld = false;
            int holdCount = _holdings.Count;
            for (int i = 0; i < holdCount; i++) {
                if (_holdings[i].CompanyId == comp.Id) { alreadyHeld = true; break; }
            }
            if (alreadyHeld) continue;

            string price = "--";
            string revenue = UIFormatting.FormatMoney(comp.Finance.MonthlyRevenue);
            float totalShare = 0f;
            if (comp.NicheMarketShare != null) {
                foreach (var ms in comp.NicheMarketShare) totalShare += ms.Value;
            }

            if (stockState.listings.TryGetValue(comp.Id, out var l))
                price = UIFormatting.FormatMoney(l.StockPrice);

            _watchlist.Add(new WatchlistRowVM {
                CompanyId = comp.Id,
                CompanyName = comp.CompanyName,
                StockPrice = price,
                Revenue = revenue,
                MarketShare = UIFormatting.FormatPercent(totalShare)
            });
        }
    }
}
