using System;
using System.Collections.Generic;

public struct BuyerOfferVM
{
    public CompetitorId BuyerId;
    public string CompanyName;
    public string OfferPrice;
    public string OfferPercent;
    public string NichePresence;
    public string Cash;
}

public class SellProductViewModel : IViewModel
{
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; }
    public string Niche { get; private set; }
    public string FairMarketValue { get; private set; }
    public List<BuyerOfferVM> Offers { get; private set; }

    private readonly List<BuyerOfferVM> _offers = new List<BuyerOfferVM>();

    public SellProductViewModel() {
        Offers = _offers;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

    public void Refresh(ProductId id, ProductState productState, CompetitorState compState, StockState stockState) {
        _offers.Clear();
        ProductId = id;

        Product product = null;
        if (productState?.shippedProducts != null) productState.shippedProducts.TryGetValue(id, out product);

        if (product == null) {
            ProductName = "Unknown Product";
            Niche = "--";
            FairMarketValue = "--";
            return;
        }

        ProductName = product.ProductName;
        Niche = product.TemplateId ?? "--";

        long fairValue = product.SaleValue > 0 ? product.SaleValue
            : (long)(product.MonthlyRevenue * 12);
        FairMarketValue = UIFormatting.FormatMoney(fairValue);

        if (compState?.competitors == null) return;

        foreach (var kvp in compState.competitors) {
            var comp = kvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            if (comp.Finance.Cash <= 0) continue;

            float repNormalized = Math.Min(comp.ReputationPoints / 5000f, 1f);
            float interestMultiplier = 0.7f + (repNormalized * 0.4f);
            long offerAmount = (long)(fairValue * interestMultiplier);

            if (offerAmount > comp.Finance.Cash) continue;

            float offerPct = fairValue > 0 ? (float)offerAmount / fairValue : 0f;

            float nichePresence = 0f;
            if (comp.NicheMarketShare != null) {
                foreach (var ms in comp.NicheMarketShare) nichePresence += ms.Value;
            }

            _offers.Add(new BuyerOfferVM {
                BuyerId = comp.Id,
                CompanyName = comp.CompanyName,
                OfferPrice = UIFormatting.FormatMoney(offerAmount),
                OfferPercent = UIFormatting.FormatPercent(offerPct) + " of FMV",
                NichePresence = UIFormatting.FormatPercent(nichePresence) + " mkt share",
                Cash = UIFormatting.FormatMoney(comp.Finance.Cash)
            });
        }

        SortOffersByPrice();
    }

    private void SortOffersByPrice() {
        int count = _offers.Count;
        for (int i = 1; i < count; i++) {
            var key = _offers[i];
            int j = i - 1;
            while (j >= 0 && string.Compare(_offers[j].OfferPrice, key.OfferPrice, System.StringComparison.Ordinal) < 0) {
                _offers[j + 1] = _offers[j];
                j--;
            }
            _offers[j + 1] = key;
        }
    }
}
