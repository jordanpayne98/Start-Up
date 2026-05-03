using System.Collections.Generic;

public struct StockListingVM
{
    public CompetitorId CompanyId;
    public string CompanyName;
    public string StockPrice;
    public string UnownedPercent;
    public string PlayerOwned;
    public string TotalInvestorOwned;
    public bool CanBuy;
}

public class StockInvestmentsViewModel : IViewModel
{
    private readonly List<StockListingVM> _allListings = new List<StockListingVM>();
    public List<StockListingVM> AllListings => _allListings;

    public string TotalPortfolioValue { get; private set; }
    public string TotalDividendIncome { get; private set; }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        if (snapshot == null) return;
        Refresh(snapshot.StockState, snapshot.CompetitorState);
        IsDirty = true;
    }

    public void Refresh(StockState stockState, CompetitorState compState) {
        if (stockState == null || compState == null) return;
        _allListings.Clear();

        long portfolioValue = 0L;
        long dividendTotal = 0L;

        foreach (var listingKvp in stockState.listings) {
            var listing = listingKvp.Value;
            if (!compState.competitors.TryGetValue(listing.CompanyId, out var comp)) continue;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            float playerOwned = 0f;
            float totalInvestorOwned = 0f;

            if (listing.OwnershipBreakdown != null) {
                foreach (var ownerKvp in listing.OwnershipBreakdown) {
                    if (ownerKvp.Key.Value == 0)
                        playerOwned = ownerKvp.Value;
                    else
                        totalInvestorOwned += ownerKvp.Value;
                }
            }

            if (playerOwned > 0f) {
                long holdingValue = (long)(listing.StockPrice * playerOwned);
                portfolioValue += holdingValue;
                dividendTotal += (long)(listing.LastDividendPayout * playerOwned);
            }

            _allListings.Add(new StockListingVM {
                CompanyId = listing.CompanyId,
                CompanyName = comp.CompanyName,
                StockPrice = UIFormatting.FormatMoney(listing.StockPrice),
                UnownedPercent = UIFormatting.FormatPercent(listing.UnownedPercentage),
                PlayerOwned = playerOwned > 0f ? UIFormatting.FormatPercent(playerOwned) : "--",
                TotalInvestorOwned = totalInvestorOwned > 0f ? UIFormatting.FormatPercent(totalInvestorOwned) : "--",
                CanBuy = listing.UnownedPercentage > 0f
            });
        }

        TotalPortfolioValue = UIFormatting.FormatMoney(portfolioValue);
        TotalDividendIncome = UIFormatting.FormatMoney(dividendTotal);
    }
}
