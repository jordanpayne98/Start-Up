public class ShowdownResultViewModel : IViewModel
{
    public string WinnerProductName { get; private set; }
    public string WinnerCompanyName { get; private set; }
    public string WinnerQuality { get; private set; }
    public string LoserProductName { get; private set; }
    public string LoserCompanyName { get; private set; }
    public string LoserQuality { get; private set; }
    public string Niche { get; private set; }
    public bool PlayerWon { get; private set; }
    public string ChurnPenaltyDescription { get; private set; }

    public ShowdownResultViewModel() {
        WinnerProductName = "--";
        WinnerCompanyName = "--";
        WinnerQuality = "--";
        LoserProductName = "--";
        LoserCompanyName = "--";
        LoserQuality = "--";
        Niche = "--";
        PlayerWon = false;
        ChurnPenaltyDescription = "--";
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) { IsDirty = true; }

    public void Refresh(ProductId winnerProductId, ProductId loserProductId,
        ProductState productState, CompetitorState compState, bool playerWon) {
        PlayerWon = playerWon;

        Product winner = null;
        Product loser = null;

        if (productState?.shippedProducts != null) {
            productState.shippedProducts.TryGetValue(winnerProductId, out winner);
            productState.shippedProducts.TryGetValue(loserProductId, out loser);
        }

        if (winner != null) {
            WinnerProductName = winner.ProductName;
            WinnerQuality = winner.ReviewResult != null
                ? ((int)winner.ReviewResult.AggregateScore).ToString() + "%"
                : ((int)winner.OverallQuality).ToString() + "%";
            WinnerCompanyName = ResolveOwnerName(winner, compState);
        } else {
            WinnerProductName = "Unknown";
            WinnerQuality = "--";
            WinnerCompanyName = "--";
        }

        if (loser != null) {
            LoserProductName = loser.ProductName;
            LoserQuality = loser.ReviewResult != null
                ? ((int)loser.ReviewResult.AggregateScore).ToString() + "%"
                : ((int)loser.OverallQuality).ToString() + "%";
            LoserCompanyName = ResolveOwnerName(loser, compState);
        } else {
            LoserProductName = "Unknown";
            LoserQuality = "--";
            LoserCompanyName = "--";
        }

        Niche = winner?.TemplateId ?? loser?.TemplateId ?? "--";

        int churnMonths = loser != null ? (int)(loser.ShowdownChurnMultiplier * 2f) : 3;
        ChurnPenaltyDescription = playerWon
            ? "Opponent suffers 2x user churn for " + churnMonths + " months"
            : "Your product suffers 2x user churn for " + churnMonths + " months";
    }

    private static string ResolveOwnerName(Product product, CompetitorState compState) {
        if (!product.IsCompetitorProduct) return "Player";
        if (compState?.competitors != null
            && compState.competitors.TryGetValue(product.OwnerCompanyId.ToCompetitorId(), out var comp)) {
            return comp.CompanyName;
        }
        return "AI Competitor";
    }
}
