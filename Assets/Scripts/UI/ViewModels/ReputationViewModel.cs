using System.Collections.Generic;

public struct ReputationMilestoneDisplay
{
    public string TierName;
    public int Threshold;
    public bool IsUnlocked;
}

public struct CategoryReputationDisplay
{
    public string CategoryName;
    public int Score;
    public string TierName;
}

public struct IndustryRankingDisplay
{
    public int Rank;
    public string EntityName;
    public string ReputationDisplay;
    public string MarketShareDisplay;
    public bool IsPlayer;
    public bool IsCompetitor;
    public CompetitorId? CompetitorId;
    public bool IsBankrupt;
    public bool IsAbsorbed;
}

public class ReputationViewModel : IViewModel
{
    public string TierName { get; private set; }
    public int Score { get; private set; }
    public int NextTierThreshold { get; private set; }
    public float ProgressPercent { get; private set; }
    public int CompletedContracts { get; private set; }
    public int FailedContracts { get; private set; }
    public int CompanyFans { get; private set; }
    public float FanSentiment { get; private set; }
    public string FanLaunchBonusDisplay { get; private set; }
    public string FanWomBonusDisplay { get; private set; }
    public string LaunchBonusText { get; private set; }
    public string WomBonusText { get; private set; }
    public int ContractsCompleted { get; private set; }
    public int ProductsShipped { get; private set; }

    private readonly List<ReputationMilestoneDisplay> _milestones = new List<ReputationMilestoneDisplay>();
    public List<ReputationMilestoneDisplay> Milestones => _milestones;

    private readonly List<CategoryReputationDisplay> _topCategories = new List<CategoryReputationDisplay>();
    public List<CategoryReputationDisplay> TopCategories => _topCategories;

    private readonly List<IndustryRankingDisplay> _industryRankings = new List<IndustryRankingDisplay>();
    public List<IndustryRankingDisplay> IndustryRankings => _industryRankings;

    public ReputationViewModel() {
        TierName = "Unknown";
    }

    public void Refresh(IReadOnlyGameState state) {
        Refresh(state, null);
    }

    public void Refresh(IReadOnlyGameState state, CompetitorState compState) {
        if (state == null) return;

        Score = state.Reputation;
        TierName = UIFormatting.FormatReputationTier(state.CurrentReputationTier);
        NextTierThreshold = GetNextTierThreshold(state.CurrentReputationTier);
        ProgressPercent = NextTierThreshold > 0 ? (float)Score / NextTierThreshold : 1f;

        // Count completed/failed from all contracts
        CompletedContracts = 0;
        FailedContracts = 0;

        // Build milestones
        _milestones.Clear();
        var tiers = new[] {
            (ReputationTier.Startup, "Startup", 200),
            (ReputationTier.Established, "Established", 1500),
            (ReputationTier.Respected, "Respected", 5000),
            (ReputationTier.IndustryLeader, "Industry Leader", 15000)
        };

        int currentTierIndex = (int)state.CurrentReputationTier;
        for (int i = 0; i < tiers.Length; i++) {
            _milestones.Add(new ReputationMilestoneDisplay {
                TierName = tiers[i].Item2,
                Threshold = tiers[i].Item3,
                IsUnlocked = (int)tiers[i].Item1 <= currentTierIndex
            });
        }

        CompanyFans = state.CompanyFans;
        FanSentiment = state.FanSentiment;
        float fanMult = state.FanLaunchMultiplier;
        FanLaunchBonusDisplay = UIFormatting.FormatPercent(fanMult - 1f);
        FanWomBonusDisplay = "";
        LaunchBonusText = "Launch Bonus: " + FanLaunchBonusDisplay;
        WomBonusText = "";
        ContractsCompleted = state.ContractsCompleted;
        ProductsShipped = state.ProductsShipped;

        _topCategories.Clear();
        var rawCategories = state.TopReputationCategories;
        if (rawCategories != null) {
            int catCount = rawCategories.Count;
            for (int i = 0; i < catCount; i++) {
                var tier = ReputationSystem.CalculateTier(rawCategories[i].score);
                _topCategories.Add(new CategoryReputationDisplay {
                    CategoryName = System.Enum.TryParse<ProductCategory>(rawCategories[i].category, out var cat)
                        ? UIFormatting.FormatCategory(cat)
                        : rawCategories[i].category,
                    Score = rawCategories[i].score,
                    TierName = UIFormatting.FormatReputationTier(tier)
                });
            }
        }

        // Industry rankings
        _industryRankings.Clear();

        // Player entry
        _industryRankings.Add(new IndustryRankingDisplay {
            Rank = 0,
            EntityName = state.CompanyName,
            ReputationDisplay = Score.ToString(),
            MarketShareDisplay = "--",
            IsPlayer = true,
            IsCompetitor = false
        });

        // Competitors
        if (compState?.competitors != null) {
            foreach (var kvp in compState.competitors) {
                var comp = kvp.Value;
                float totalShare = 0f;
                if (comp.NicheMarketShare != null) {
                    foreach (var shKvp in comp.NicheMarketShare)
                        totalShare += shKvp.Value;
                }
                _industryRankings.Add(new IndustryRankingDisplay {
                    Rank = 0,
                    EntityName = comp.CompanyName,
                    ReputationDisplay = comp.ReputationPoints.ToString(),
                    MarketShareDisplay = totalShare > 0f ? UIFormatting.FormatPercent(totalShare) : "--",
                    IsPlayer = false,
                    IsCompetitor = true,
                    CompetitorId = kvp.Key,
                    IsBankrupt = comp.IsBankrupt,
                    IsAbsorbed = comp.IsAbsorbed
                });
            }
        }

        // Sort descending by reputation score, assign ranks
        _industryRankings.Sort((a, b) => {
            int aRep = int.TryParse(a.ReputationDisplay, out int ar) ? ar : 0;
            int bRep = int.TryParse(b.ReputationDisplay, out int br) ? br : 0;
            return bRep.CompareTo(aRep);
        });

        for (int i = 0; i < _industryRankings.Count; i++) {
            var entry = _industryRankings[i];
            entry.Rank = i + 1;
            _industryRankings[i] = entry;
        }
    }

    private static int GetNextTierThreshold(ReputationTier current) {
        int nextIndex = (int)current + 1;
        var thresholds = new int[] { 0, 200, 1500, 5000, 15000 };
        if (nextIndex < thresholds.Length) return thresholds[nextIndex];
        return thresholds[thresholds.Length - 1];
    }
}
