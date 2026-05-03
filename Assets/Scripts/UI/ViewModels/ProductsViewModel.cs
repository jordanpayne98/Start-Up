using System.Collections.Generic;

// ─── Display structs ──────────────────────────────────────────────────────────

public struct DevProductDisplay
{
    public ProductId Id;
    public string Name;
    public string TemplateName;
    public string ProductTypeLabel;      // e.g., "Video Game"
    public string PricingLabel;          // e.g., "$59.99 (One-Time)" or "$9.99/mo"
    public int FeatureCount;
    public float OverallProgressPercent;     // avg across all unlocked phases (0-1)
    public int PhaseCount;
    public int CompletedPhaseCount;
    public bool AllPhasesComplete;
    public bool AnyPhaseIterating;
    public string StatusLabel;               // "3/5 phases complete"
    public string UpfrontCostDisplay;        // "$5,000"
    public string ProductionCostDisplay;     // "$12,000"
    public string CreatedDateLabel;          // "Started Day X"
    public string DevDurationLabel;          // "X days in dev"
    public bool HasReleaseDate;
    public string ReleaseDateLabel;          // "Ships: 15/March/2026"
    public int DaysUntilRelease;
    public string DaysUntilReleaseLabel;     // "42 days left" or "3 days overdue"
    public bool IsOverdue;
    public bool CanShip;
    public int DateShiftCount;
}

public struct PhaseDisplay
{
    public ProductPhaseType PhaseType;
    public string PhaseLabel;
    public float WorkProgressPercent;        // 0-1
    public float IterationProgressPercent;   // 0-1 (bonusWorkCompleted / bonusWorkTarget)
    public float Quality;                    // 0-100
    public bool IsLocked;
    public bool IsComplete;
    public bool IsIterating;
    public bool CanIterate;
    public int IterationCount;
    public string PrimaryRoleLabel;
    public string AssignedTeamName;
    public TeamId AssignedTeamId;
    public ProductTeamRole PrimaryRole;
    public string StatusBadgeText;
    public string StatusBadgeClass;
    public string FillClass;
    public float BugAccumulation;
    public bool IsCrunching;
}

public struct ProductTemplateDisplay
{
    public string TemplateId;
    public string DisplayName;
    public string Description;          // one-line description for Step 1 card
    public string CategoryLabel;
    public string CategoryGroupLabel;   // "Games", "Apps", "Web", "Services" for group headers
    public int BaseUpfrontCost;
    public int PhaseCount;
    public string PhaseSummary;
    public string[] PhasePills;         // individual phase names for pill rendering
}

public struct FeatureToggleDisplay
{
    public string FeatureId;
    public string DisplayName;
    public string Description;
    public FeatureCategory FeatureCategory;
    public bool IsSelected;
    public bool IsPreSelected;

    // Generation / lock state
    public bool IsLocked;
    public string LockReason;
    public int AvailableFromGeneration;
    public bool IsNativeOnly;
    public string AffinityStars; // e.g. "★★☆"
    public string CapLabel; // Non-null if feature is quality-capped by upstream

    // Market demand data
    public float CurrentDemand;
    public MarketTrend DemandTrend;
    public float Volatility;
    public float ProjectedShipDemand;

    // Synergy / conflict
    public bool HasSynergyWithSelected;
    public bool HasConflictWithSelected;
    public string SynergyLabel;
    public string ConflictLabel;

    // Cost & dev time
    public int BaseCost;
    public float DevCostMultiplier;
    public int PriceContribution;

    // Demand lifecycle
    public FeatureDemandStage DemandStage;
    public string DemandStageLabel;
}

public struct PlatformTargetDisplay
{
    public ProductId PlatformId;
    public string DisplayName;
    public string OwnerLabel;
    public int ActiveUsers;
    public float MarketSharePercent;
    public string LicensingCostLabel;
    public bool IsPlayerOwned;
    public string PlatformTypeLabel;
}

public struct ToolSelectionDisplay
{
    public ProductId ToolId;
    public ProductCategory Category;
    public string DisplayName;
    public string OwnerLabel;
    public float QualityScore;
    public string QualitativeLabel;
    public string LicensingCostLabel;
    public bool IsPlayerOwned;
    public float RoyaltyRate;
}

public struct NicheOptionDisplay
{
    public ProductNiche Niche;
    public string DisplayName;
    public string RetentionLabel;
    public string VolatilityLabel;
}

public struct ShippedProductDisplay
{
    public ProductId Id;
    public string Name;
    public string TemplateName;
    public string ProductTypeLabel;
    public string PricingLabel;
    public int FeatureCount;
    public string LifecycleStageLabel;       // "Launch", "Growth", "Plateau", "Decline"
    public string LifecycleBadgeClass;       // "lifecycle--launch", "lifecycle--growth", etc.
    public int ActiveUsers;
    public string ActiveUsersDisplay;        // "12,400"
    public float PopularityPercent;          // 0-100
    public string MonthlyRevenueDisplay;     // "$4,200"
    public string TotalRevenueDisplay;       // "$45,000"
    public string ProductionCostDisplay;     // "$12,000"
    public float BugsRemainingPercent;       // clamped 0-100
    public bool IsOnMarket;
    public bool IsMaintained;
    public bool IsUpdating;
    public float UpdateProgressPercent;      // 0-1
    public string UpdateTypeLabel;           // "Bug Fix", "Adding Features"
    public float OverallQuality;
    // Current month direction (vs last month)
    public int CurrentUsersTrendDirection;   // -1, 0, +1
    public int CurrentRevenueTrendDirection; // -1, 0, +1
    // Next month projections
    public int ProjectedUsersTrendDirection;   // -1, 0, +1
    public string ProjectedUsersTrendLabel;    // "▲ 1.2K" or "▼ 200" or "—"
    public int ProjectedRevenueTrendDirection; // -1, 0, +1
    public string ProjectedRevenueTrendLabel;  // "▲ $500" or "▼ $1.2K" or "—"
    // Marketing / Hype
    public float HypeScore;                    // 0-100 raw
    public float HypeScoreNormalized;          // 0-1 for bar fill
    public string HypeScoreDisplay;            // "Hype: 45/100" or "No Marketing"
    public bool IsMarketingActive;
    public bool IsRunningAds;
    public string AdStatusDisplay;             // "Ads Running (12d left)" or "Ads Available"
    public bool CanRunAds;
    public bool CanAnnounceUpdate;
    public bool HasAnnouncedUpdate;
    public float UpdateHypeNormalized;
    public string UpdateHypeDisplay;           // "Update Hype: 23/100" or ""
    public bool HasMarketingTeam;
    public bool HasCrisis;
    public CrisisEventType? CrisisType;
    public string CrisisDescription;
    public bool CanSell;
    // Distribution model (Layer 1 / Tool products only)
    public bool IsTool;
    public bool IsPlatform;
    public ToolDistributionModel DistributionModel;
    public string DistributionModelLabel;
    public float PlayerLicensingRate;
    public int ActiveLicenseeCount;
    public string MonthlyLicensingRevenueDisplay;
    public string TotalLicensingRevenueDisplay;
    // Subscription (Licensed tools only)
    public int ActiveSubscriberCount;
    public float MonthlySubscriptionPrice;
    public string MonthlySubscriptionRevenueDisplay;
    public string TotalSubscriptionRevenueDisplay;
}

// ─── ViewModel ────────────────────────────────────────────────────────────────

public class ProductsViewModel : IViewModel
{
    // Product lists
    private readonly List<DevProductDisplay> _devProducts = new List<DevProductDisplay>();
    public List<DevProductDisplay> DevProducts => _devProducts;

    private readonly List<ShippedProductDisplay> _shippedProducts = new List<ShippedProductDisplay>();
    public List<ShippedProductDisplay> ShippedProducts => _shippedProducts;

    // Selected dev product detail
    public ProductId SelectedProductId;
    public bool HasSelection;
    public string SelectedProductName;
    public string SelectedTemplateName;
    public float SelectedOverallQuality;
    public string SelectedProductTypeLabel;
    public string SelectedPricingLabel;
    public int SelectedFeatureCount;
    public string SelectedCreatedDateLabel;
    public string SelectedDevDurationLabel;

    private readonly PhaseDisplay[] _phases = new PhaseDisplay[8];
    public PhaseDisplay[] Phases => _phases;
    public int PhaseCount;
    public bool CanShip;
    public float SelectedTotalBugs;
    public string SelectedBugLabel;
    public string CodeHealthLabel;
    public string CodeHealthClass;
    public bool SelectedHasReleaseDate;
    public string SelectedReleaseDateLabel;
    public string SelectedDaysUntilReleaseLabel;
    public int SelectedDaysUntilRelease;
    public bool SelectedIsOverdue;
    public int SelectedDateShiftCount;
    public int SelectedTargetReleaseTick;

    // Selected shipped product
    public bool HasShippedSelection;
    public ShippedProductDisplay SelectedShippedProduct;

    // Crunch state aggregates for dev product
    public bool AnyActivePhaseCrunching;
    public bool HasActivePhasesWithTeams;

    // Dev marketing budget display
    public string DevMarketingBudgetDisplay;
    public long DevMarketingBudgetRaw;

    // Available teams for assignment dropdown
    private readonly List<TeamSummaryDisplay> _availableTeams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> AvailableTeams => _availableTeams;

    // Template / feature data (for create modal)
    private readonly List<ProductTemplateDisplay> _templates = new List<ProductTemplateDisplay>();
    public List<ProductTemplateDisplay> Templates => _templates;

    private readonly List<FeatureToggleDisplay> _features = new List<FeatureToggleDisplay>();
    public List<FeatureToggleDisplay> Features => _features;

    public int CalculatedCost;
    public bool CanAfford;

    // Selected dev product marketing state
    public string DevHypeScoreDisplay;        // "Hype: 45/100" or "No Marketing"
    public float DevHypeScoreNormalized;      // 0-1 for bar fill
    public float DevHypeScore;
    public bool DevIsMarketingActive;
    public bool DevCanStartMarketing;         // has team assigned, not already active
    public bool DevHasMarketingTeam;
    public string DevMarketingTeamDisplay;    // "Marketing Team: Alpha" or "No Marketing Team"

    // Selected shipped product marketing state
    public string ShippedHypeDisplay;
    public float ShippedHypeNormalized;
    public bool ShippedIsMarketingActive;
    public bool ShippedCanStartMarketing;
    public bool ShippedCanStopMarketing;
    public bool ShippedCanRunAds;
    public bool ShippedIsRunningAds;
    public string ShippedAdStatusDisplay;
    public bool ShippedCanAnnounceUpdate;
    public bool ShippedHasAnnouncedUpdate;
    public float ShippedUpdateHypeNormalized;
    public string ShippedUpdateHypeDisplay;
    public bool ShippedHasMarketingTeam;

    private IReadOnlyGameState _lastState;

    private readonly ProductDetailViewModel _detailVM = new ProductDetailViewModel();
    private ProductId _cachedDetailId;

    public ProductDetailViewModel GetDetailVM(ProductId id)
    {
        if (_lastState == null) return null;
        var snapshot = _lastState as GameStateSnapshot;
        if (snapshot == null) return null;
        _cachedDetailId = id;
        _detailVM.SetTemplates(null);

        long estimatedMaintDrain = 0L;
        long estimatedMktDrain = 0L;
        Product detailProduct = null;
        if (snapshot.ProductStateRef.shippedProducts != null)
            snapshot.ProductStateRef.shippedProducts.TryGetValue(id, out detailProduct);
        if (detailProduct != null && !detailProduct.IsCompetitorProduct) {
            var activeTeams = snapshot.ActiveTeams;
            var activeEmployees = snapshot.ActiveEmployees;
            int teamCount = activeTeams.Count;

            if (detailProduct.TeamAssignments != null && detailProduct.TeamAssignments.TryGetValue(ProductTeamRole.QA, out var qaTeamId)) {
                long qaSalary = 0L;
                for (int t = 0; t < teamCount; t++) {
                    if (activeTeams[t].id == qaTeamId) {
                        var members = activeTeams[t].members;
                        int mc = members.Count;
                        int ec = activeEmployees.Count;
                        for (int m = 0; m < mc; m++) {
                            for (int e = 0; e < ec; e++) {
                                if (activeEmployees[e].id == members[m]) { qaSalary += activeEmployees[e].salary; break; }
                            }
                        }
                        break;
                    }
                }
                long infraCost = System.Math.Max(1L, detailProduct.ActiveUserCount / 1000);
                estimatedMaintDrain = qaSalary + infraCost;
            }

            if (detailProduct.TeamAssignments != null && detailProduct.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var mktTeamId)) {
                long mktSalary = 0L;
                for (int t = 0; t < teamCount; t++) {
                    if (activeTeams[t].id == mktTeamId) {
                        var members = activeTeams[t].members;
                        int mc = members.Count;
                        int ec = activeEmployees.Count;
                        for (int m = 0; m < mc; m++) {
                            for (int e = 0; e < ec; e++) {
                                if (activeEmployees[e].id == members[m]) { mktSalary += activeEmployees[e].salary; break; }
                            }
                        }
                        break;
                    }
                }
                long campaignCost = System.Math.Max(1L, detailProduct.ActiveUserCount / 2000);
                estimatedMktDrain = mktSalary + campaignCost;
            }
        }

        _detailVM.SetEstimatedDrains(estimatedMaintDrain, estimatedMktDrain);
        _detailVM.Refresh(id, snapshot.ProductStateRef, snapshot.CompetitorState, snapshot.MarketStateRef);
        return _detailVM;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot)
    {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;
        _lastState = state;

        // ── Dev product list ──────────────────────────────────────────────────
        _devProducts.Clear();
        var devProducts = state.DevelopmentProducts;
        foreach (var kvp in devProducts)
        {
            var product = kvp.Value;
            if (product.IsCompetitorProduct) continue;
            int phaseCount = product.Phases != null ? product.Phases.Length : 0;
            int completedCount = 0;
            bool anyIterating = false;
            float totalWork = 0f;
            float completedWork = 0f;

            for (int p = 0; p < phaseCount; p++)
            {
                var ph = product.Phases[p];
                totalWork += ph.totalWorkRequired;
                completedWork += ph.workCompleted;
                if (ph.isComplete) completedCount++;
                if (ph.isIterating) anyIterating = true;
            }

            float overallProgress = totalWork > 0f ? completedWork / totalWork : 0f;

            bool hasReleaseDate = product.HasAnnouncedReleaseDate;
            int targetReleaseTick = product.TargetReleaseTick;
            int currentTick = state.CurrentTick;
            string releaseDateLabel = "";
            int daysUntilRelease = 0;
            string daysUntilReleaseLabel = "";
            bool isOverdue = false;
            if (hasReleaseDate && targetReleaseTick > 0) {
                int targetDay = targetReleaseTick / TimeState.TicksPerDay;
                int dom = TimeState.GetDayOfMonth(targetDay);
                int mon = TimeState.GetMonth(targetDay);
                int yr = TimeState.GetYear(targetDay);
                releaseDateLabel = "Ships: " + UIFormatting.FormatDate(dom, mon, yr);
                daysUntilRelease = (targetReleaseTick - currentTick) / TimeState.TicksPerDay;
                isOverdue = daysUntilRelease < 0;
                if (isOverdue)
                    daysUntilReleaseLabel = (-daysUntilRelease) + " days overdue";
                else
                    daysUntilReleaseLabel = daysUntilRelease + " days left";
            }
            bool allPhasesComplete = completedCount == phaseCount && phaseCount > 0;
            bool canShip = hasReleaseDate && allPhasesComplete && !anyIterating;

            _devProducts.Add(new DevProductDisplay {
                Id = product.Id,
                Name = product.ProductName,
                TemplateName = product.TemplateId,
                ProductTypeLabel = product.Category.ToString(),
                PricingLabel = FormatPricing(product.PriceOverride, product.IsSubscriptionBased),
                FeatureCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0,
                OverallProgressPercent = overallProgress,
                PhaseCount = phaseCount,
                CompletedPhaseCount = completedCount,
                AllPhasesComplete = allPhasesComplete,
                AnyPhaseIterating = anyIterating,
                StatusLabel = completedCount + "/" + phaseCount + " phases complete",
                UpfrontCostDisplay = UIFormatting.FormatMoney(product.UpfrontCostPaid),
                ProductionCostDisplay = UIFormatting.FormatMoney(state.GetProductTotalProductionCost(product.Id)),
                CreatedDateLabel = "Started Day " + (product.CreationTick / TimeState.TicksPerDay),
                DevDurationLabel = ((state.CurrentTick - product.CreationTick) / TimeState.TicksPerDay) + " days in dev",
                HasReleaseDate = hasReleaseDate,
                ReleaseDateLabel = releaseDateLabel,
                DaysUntilRelease = daysUntilRelease,
                DaysUntilReleaseLabel = daysUntilReleaseLabel,
                IsOverdue = isOverdue,
                CanShip = canShip,
                DateShiftCount = product.DateShiftCount
            });
        }

        // ── Shipped product list ──────────────────────────────────────────────
        _shippedProducts.Clear();
        var shippedProducts = state.ShippedProducts;
        foreach (var kvp in shippedProducts)
        {
            var product = kvp.Value;
            if (product.IsCompetitorProduct) continue;
            var stage = state.GetProductLifecycleStage(product.Id);
            string stageLabel = FormatLifecycleStageLabel(stage);
            string badgeClass = FormatLifecycleBadgeClass(stage);
            bool isUpdating = state.IsProductUpdating(product.Id);
            float updateProgress = 0f;
            string updateTypeLabel = "";
            if (isUpdating && product.CurrentUpdate != null && product.CurrentUpdate.updateWorkRequired > 0f)
            {
                updateProgress = product.CurrentUpdate.updateWorkCompleted / product.CurrentUpdate.updateWorkRequired;
                updateTypeLabel = FormatUpdateTypeLabel(product.CurrentUpdate.updateType);
            }

            bool isGame = product.Category == ProductCategory.VideoGame;
            int activeUsers = state.GetProductActiveUsers(product.Id);
            int prevUsers = state.GetProductPreviousActiveUsers(product.Id);
            int projUsers = state.GetProductProjectedActiveUsers(product.Id);
            int monthlyRevenue = state.GetProductMonthlyRevenue(product.Id);
            int prevRevenue = state.GetProductPreviousMonthlyRevenue(product.Id);
            int projRevenue = state.GetProductProjectedMonthlyRevenue(product.Id);

            bool hasMarketingTeam = state.HasMarketingTeamAssigned(product.Id);
            float hypeScore = state.GetProductHype(product.Id);
            float hypeNorm = hypeScore / 100f;
            if (hypeNorm < 0f) hypeNorm = 0f;
            if (hypeNorm > 1f) hypeNorm = 1f;
            bool marketingActive = state.IsProductMarketingActive(product.Id);
            bool runningAds = state.IsProductRunningAds(product.Id);
            bool hasAnnouncedUpdate = state.HasProductAnnouncedUpdate(product.Id);
            float updateHype = state.GetProductUpdateHype(product.Id);
            float updateHypeNorm = updateHype / 100f;
            if (updateHypeNorm < 0f) updateHypeNorm = 0f;
            if (updateHypeNorm > 1f) updateHypeNorm = 1f;

            string hypeDisplay = hasMarketingTeam
                ? "Hype: " + hypeScore.ToString("F0") + "/100"
                : "No Marketing";
            string adStatus = runningAds ? "Ads Running" : (hasMarketingTeam ? "Ads Available" : "No Marketing Team");
            string updateHypeDisplay = hasAnnouncedUpdate
                ? "Update Hype: " + updateHype.ToString("F0") + "/100"
                : "";

            _shippedProducts.Add(new ShippedProductDisplay {
                Id = product.Id,
                Name = product.ProductName,
                TemplateName = product.TemplateId,
                ProductTypeLabel = isGame ? "Video Game" : product.Category.ToString(),
                PricingLabel = FormatPricing(product.PriceOverride, product.IsSubscriptionBased),
                FeatureCount = product.SelectedFeatureIds != null ? product.SelectedFeatureIds.Length : 0,
                LifecycleStageLabel = stageLabel,
                LifecycleBadgeClass = badgeClass,
                ActiveUsers = activeUsers,
                ActiveUsersDisplay = FormatNumber(activeUsers),
                PopularityPercent = state.GetProductPopularity(product.Id),
                MonthlyRevenueDisplay = UIFormatting.FormatMoney(monthlyRevenue),
                TotalRevenueDisplay = UIFormatting.FormatMoney(state.GetProductTotalLifetimeRevenue(product.Id)),
                ProductionCostDisplay = UIFormatting.FormatMoney(state.GetProductTotalProductionCost(product.Id)),
                BugsRemainingPercent = product.BugsRemaining > 100f ? 100f : product.BugsRemaining,
                IsOnMarket = product.IsOnMarket,
                IsMaintained = product.IsMaintained,
                IsUpdating = isUpdating,
                UpdateProgressPercent = updateProgress,
                UpdateTypeLabel = updateTypeLabel,
                OverallQuality = product.OverallQuality,
                CurrentUsersTrendDirection = ComputeTrendDirection(prevUsers, activeUsers),
                CurrentRevenueTrendDirection = ComputeTrendDirection(prevRevenue, monthlyRevenue),
                ProjectedUsersTrendDirection = ComputeTrendDirection(activeUsers, projUsers),
                ProjectedUsersTrendLabel = FormatTrendLabel(activeUsers, projUsers),
                ProjectedRevenueTrendDirection = ComputeTrendDirection(monthlyRevenue, projRevenue),
                ProjectedRevenueTrendLabel = FormatRevenueTrendLabel(monthlyRevenue, projRevenue),
                HypeScore = hypeScore,
                HypeScoreNormalized = hypeNorm,
                HypeScoreDisplay = hypeDisplay,
                IsMarketingActive = marketingActive,
                IsRunningAds = runningAds,
                AdStatusDisplay = adStatus,
                CanRunAds = hasMarketingTeam && product.IsOnMarket && !runningAds,
                CanAnnounceUpdate = hasMarketingTeam && isUpdating && !hasAnnouncedUpdate,
                HasAnnouncedUpdate = hasAnnouncedUpdate,
                UpdateHypeNormalized = updateHypeNorm,
                UpdateHypeDisplay = updateHypeDisplay,
                HasMarketingTeam = hasMarketingTeam,
                HasCrisis = product.CrisisLevel > 0,
                CrisisType = product.CrisisLevel >= 3 ? CrisisEventType.Catastrophic
                           : product.CrisisLevel >= 2 ? CrisisEventType.ModerateBreach
                           : product.CrisisLevel >= 1 ? (CrisisEventType?)CrisisEventType.MinorBug
                           : null,
                CrisisDescription = product.CrisisLevel > 0 ? "Product crisis severity: " + product.CrisisLevel : "",
                CanSell = product.SaleValue > 0,
                IsTool = product.Category.IsTool(),
                IsPlatform = product.Category.IsPlatform(),
                DistributionModel = product.DistributionModel,
                DistributionModelLabel = FormatDistributionModelLabel(product.DistributionModel),
                PlayerLicensingRate = product.PlayerLicensingRate,
                ActiveLicenseeCount = product.ActiveLicenseeCount,
                MonthlyLicensingRevenueDisplay = "—",
                TotalLicensingRevenueDisplay = UIFormatting.FormatMoney((int)product.TotalLicensingRevenue),
                ActiveSubscriberCount = product.ActiveSubscriberCount,
                MonthlySubscriptionPrice = product.MonthlySubscriptionPrice,
                MonthlySubscriptionRevenueDisplay = product.Category.IsTool() && product.DistributionModel == ToolDistributionModel.Licensed
                    ? UIFormatting.FormatMoney(monthlyRevenue)
                    : "—",
                TotalSubscriptionRevenueDisplay = UIFormatting.FormatMoney((int)product.TotalSubscriptionRevenue)
            });
        }

        // Refresh shipped selection
        if (HasShippedSelection)
        {
            bool found = false;
            int sc = _shippedProducts.Count;
            for (int i = 0; i < sc; i++)
            {
                if (_shippedProducts[i].Id == SelectedShippedProduct.Id)
                {
                    SelectedShippedProduct = _shippedProducts[i];
                    found = true;
                    break;
                }
            }
            if (!found) HasShippedSelection = false;
        }

        // Populate selected shipped marketing state
        if (HasShippedSelection)
        {
            var sp = SelectedShippedProduct;
            ShippedHypeDisplay = sp.HypeScoreDisplay;
            ShippedHypeNormalized = sp.HypeScoreNormalized;
            ShippedIsMarketingActive = sp.IsMarketingActive;
            ShippedHasMarketingTeam = sp.HasMarketingTeam;
            ShippedCanStartMarketing = sp.HasMarketingTeam && !sp.IsMarketingActive;
            ShippedCanStopMarketing = sp.IsMarketingActive;
            ShippedCanRunAds = sp.CanRunAds;
            ShippedIsRunningAds = sp.IsRunningAds;
            ShippedAdStatusDisplay = sp.AdStatusDisplay;
            ShippedCanAnnounceUpdate = sp.CanAnnounceUpdate;
            ShippedHasAnnouncedUpdate = sp.HasAnnouncedUpdate;
            ShippedUpdateHypeNormalized = sp.UpdateHypeNormalized;
            ShippedUpdateHypeDisplay = sp.UpdateHypeDisplay;
        }

        // ── Selected dev product detail ───────────────────────────────────────
        RefreshSelectedDetail();

        // ── Available teams (not on a contract, not on a product) ─────────────
        _availableTeams.Clear();
        var activeTeams = state.ActiveTeams;
        int teamCount = activeTeams.Count;
        for (int t = 0; t < teamCount; t++)
        {
            var team = activeTeams[t];
            var contractForTeam = state.GetContractForTeam(team.id);
            if (contractForTeam != null) continue;
            if (state.IsTeamAssignedToProduct(team.id)) continue;
            var teamType = state.GetTeamType(team.id);
            if (teamType == TeamType.HR) continue;

            _availableTeams.Add(new TeamSummaryDisplay {
                Id = team.id,
                Name = team.name,
                MemberCount = team.members?.Count ?? 0,
                ContractName = "",
                TeamType = UIFormatting.FormatTeamType(state.GetTeamType(team.id)),
                AvgMorale = 0
            });
        }
    }

    public void RefreshSelectedDetail()
    {
        if (_lastState == null) return;
        var state = _lastState;

        PhaseCount = 0;
        CanShip = false;
        AnyActivePhaseCrunching = false;
        HasActivePhasesWithTeams = false;

        var devProducts = state.DevelopmentProducts;
        if (HasSelection && devProducts.TryGetValue(SelectedProductId, out var sel))
        {
            bool selIsGame = sel.Category == ProductCategory.VideoGame;
            SelectedProductName = sel.ProductName;
            SelectedTemplateName = sel.TemplateId;
            SelectedOverallQuality = sel.OverallQuality;
            SelectedProductTypeLabel = selIsGame ? "Video Game" : sel.Category.ToString();
            SelectedPricingLabel = FormatPricing(sel.PriceOverride, sel.IsSubscriptionBased);
            SelectedFeatureCount = sel.SelectedFeatureIds != null ? sel.SelectedFeatureIds.Length : 0;
            SelectedCreatedDateLabel = "Started Day " + (sel.CreationTick / TimeState.TicksPerDay);
            SelectedDevDurationLabel = ((state.CurrentTick - sel.CreationTick) / TimeState.TicksPerDay) + " days in dev";

            int phaseCount = sel.Phases != null ? sel.Phases.Length : 0;
            PhaseCount = phaseCount > 8 ? 8 : phaseCount;

            bool allComplete = phaseCount > 0;
            bool anyIterating = false;

            for (int p = 0; p < PhaseCount; p++)
            {
                var ph = sel.Phases[p];

                string badgeText;
                string badgeClass;
                if (ph.isIterating)      { badgeText = "Iterating";   badgeClass = "badge--warning"; }
                else if (ph.isComplete)  { badgeText = "Complete";    badgeClass = "badge--success"; }
                else if (!ph.isUnlocked) { badgeText = "Locked";      badgeClass = "badge--neutral"; }
                else                     { badgeText = "In Progress"; badgeClass = "badge--accent"; }

                string fillClass;
                if (ph.isComplete && ph.phaseQuality >= 75f) fillClass = "progress-bar__fill--success";
                else if (ph.isComplete && ph.phaseQuality < 75f) fillClass = "progress-bar__fill--warning";
                else fillClass = "progress-bar__fill";

                string teamName = "Unassigned";
                TeamId assignedTeamId = default;
                bool isCrunching = false;
                if (sel.TeamAssignments != null && sel.TeamAssignments.TryGetValue(ph.primaryRole, out var tId))
                {
                    assignedTeamId = tId;
                    var teams = state.ActiveTeams;
                    int tc = teams.Count;
                    for (int t = 0; t < tc; t++)
                    {
                        if (teams[t].id == tId) { teamName = teams[t].name; isCrunching = teams[t].isCrunching; break; }
                    }
                }

                if (!ph.isComplete) allComplete = false;
                if (ph.isIterating) anyIterating = true;

                bool isActivePhase = ph.isUnlocked && !ph.isComplete;
                if (isActivePhase) {
                    HasActivePhasesWithTeams = HasActivePhasesWithTeams || assignedTeamId.Value != 0;
                    AnyActivePhaseCrunching = AnyActivePhaseCrunching || (assignedTeamId.Value != 0 && isCrunching);
                }

                _phases[p] = new PhaseDisplay {
                    PhaseType = ph.phaseType,
                    PhaseLabel = FormatPhaseLabel(ph.phaseType),
                    WorkProgressPercent = ph.totalWorkRequired > 0f ? ph.workCompleted / ph.totalWorkRequired : 0f,
                    IterationProgressPercent = (ph.isIterating && ph.bonusWorkTarget > 0f)
                        ? ph.bonusWorkCompleted / ph.bonusWorkTarget : 0f,
                    Quality = ph.phaseQuality,
                    IsLocked = !ph.isUnlocked,
                    IsComplete = ph.isComplete,
                    IsIterating = ph.isIterating,
                    CanIterate = ph.isComplete && !ph.isIterating,
                    IterationCount = ph.iterationCount,
                    PrimaryRoleLabel = ph.primaryRole.ToString() + " slot",
                    AssignedTeamName = teamName,
                    AssignedTeamId = assignedTeamId,
                    PrimaryRole = ph.primaryRole,
                    StatusBadgeText = badgeText,
                    StatusBadgeClass = badgeClass,
                    FillClass = fillClass,
                    BugAccumulation = ph.bugAccumulation,
                    IsCrunching = isCrunching
                };
            }

            CanShip = allComplete && !anyIterating && phaseCount > 0 && sel.HasAnnouncedReleaseDate;

            SelectedHasReleaseDate = sel.HasAnnouncedReleaseDate;
            SelectedTargetReleaseTick = sel.TargetReleaseTick;
            SelectedDateShiftCount = sel.DateShiftCount;
            if (sel.HasAnnouncedReleaseDate && sel.TargetReleaseTick > 0) {
                int targetDay = sel.TargetReleaseTick / TimeState.TicksPerDay;
                int dom = TimeState.GetDayOfMonth(targetDay);
                int mon = TimeState.GetMonth(targetDay);
                int yr = TimeState.GetYear(targetDay);
                SelectedReleaseDateLabel = "Ships: " + UIFormatting.FormatDate(dom, mon, yr);
                int daysLeft = (sel.TargetReleaseTick - state.CurrentTick) / TimeState.TicksPerDay;
                SelectedDaysUntilRelease = daysLeft;
                SelectedIsOverdue = daysLeft < 0;
                SelectedDaysUntilReleaseLabel = SelectedIsOverdue
                    ? (-daysLeft) + " days overdue"
                    : daysLeft + " days left";
            } else {
                SelectedReleaseDateLabel = "No release date set";
                SelectedDaysUntilReleaseLabel = "";
                SelectedDaysUntilRelease = 0;
                SelectedIsOverdue = false;
            }

            float totalBugs = 0f;
            for (int p = 0; p < PhaseCount; p++)
            {
                if (_phases[p].PhaseType != ProductPhaseType.QA)
                    totalBugs += _phases[p].BugAccumulation;
            }
            SelectedTotalBugs = totalBugs;
            SelectedBugLabel = totalBugs > 0f ? totalBugs.ToString("F0") + " bugs" : "No bugs";
            if (totalBugs < 10f)
            {
                CodeHealthLabel = "Code Health: Good";
                CodeHealthClass = "code-health--good";
            }
            else if (totalBugs <= 30f)
            {
                CodeHealthLabel = "Code Health: Fair";
                CodeHealthClass = "code-health--fair";
            }
            else
            {
                CodeHealthLabel = "Code Health: Poor";
                CodeHealthClass = "code-health--poor";
            }

            DevHasMarketingTeam = sel.TeamAssignments != null && sel.TeamAssignments.ContainsKey(ProductTeamRole.Marketing);
            DevHypeScore = state.GetProductHype(SelectedProductId);
            DevHypeScoreNormalized = DevHypeScore / 100f;
            if (DevHypeScoreNormalized < 0f) DevHypeScoreNormalized = 0f;
            if (DevHypeScoreNormalized > 1f) DevHypeScoreNormalized = 1f;
            DevIsMarketingActive = state.IsProductMarketingActive(SelectedProductId);
            DevCanStartMarketing = DevHasMarketingTeam && !DevIsMarketingActive;
            DevHypeScoreDisplay = DevHasMarketingTeam
                ? "Hype: " + DevHypeScore.ToString("F0") + "/100"
                : "No Marketing";
            if (DevHasMarketingTeam && sel.TeamAssignments.TryGetValue(ProductTeamRole.Marketing, out var mktTeamId))
            {
                var activeTeams = state.ActiveTeams;
                int tc = activeTeams.Count;
                string mktTeamName = "Unknown";
                for (int t = 0; t < tc; t++)
                {
                    if (activeTeams[t].id == mktTeamId) { mktTeamName = activeTeams[t].name; break; }
                }
                DevMarketingTeamDisplay = "Marketing: " + mktTeamName;
            }
            else
            {
                DevMarketingTeamDisplay = "No Marketing Team";
            }

            DevMarketingBudgetRaw = sel.MarketingBudgetMonthly;
            DevMarketingBudgetDisplay = UIFormatting.FormatMoney(sel.MarketingBudgetMonthly) + "/mo";
        }
        else if (HasSelection)
        {
            HasSelection = false;
        }
    }

    private static string FormatPricing(float price, bool isSubscription)
    {
        if (price <= 0f) return "—";
        string priceStr = "$" + price.ToString("F2");
        return isSubscription ? priceStr + "/mo (Subscription)" : priceStr + " (One-Time)";
    }

    private static string FormatDistributionModelLabel(ToolDistributionModel model)
    {
        switch (model)
        {
            case ToolDistributionModel.Licensed:    return "Licensed";
            case ToolDistributionModel.OpenSource:  return "Open Source";
            default:                               return "Proprietary";
        }
    }

    private static string FormatPhaseLabel(ProductPhaseType phaseType)
    {
        switch (phaseType)
        {
            case ProductPhaseType.Design:      return "Design";
            case ProductPhaseType.Programming: return "Programming";
            case ProductPhaseType.SFX:         return "SFX";
            case ProductPhaseType.VFX:         return "VFX";
            case ProductPhaseType.QA:          return "QA";
            default:                           return phaseType.ToString();
        }
    }

    private static string FormatLifecycleStageLabel(ProductLifecycleStage stage)
    {
        switch (stage)
        {
            case ProductLifecycleStage.PreLaunch: return "Pre-Launch";
            case ProductLifecycleStage.Launch:    return "Launch";
            case ProductLifecycleStage.Growth:    return "Growth";
            case ProductLifecycleStage.Plateau:   return "Plateau";
            case ProductLifecycleStage.Decline:   return "Decline";
            default:                              return stage.ToString();
        }
    }

    private static string FormatLifecycleBadgeClass(ProductLifecycleStage stage)
    {
        switch (stage)
        {
            case ProductLifecycleStage.Launch:  return "lifecycle--launch";
            case ProductLifecycleStage.Growth:  return "lifecycle--growth";
            case ProductLifecycleStage.Plateau: return "lifecycle--plateau";
            case ProductLifecycleStage.Decline: return "lifecycle--decline";
            default:                            return "lifecycle--prelaunch";
        }
    }

    private static string FormatUpdateTypeLabel(ProductUpdateType type)
    {
        switch (type)
        {
            case ProductUpdateType.BugFix:       return "Bug Fix";
            case ProductUpdateType.AddFeatures:  return "Adding Features";
            case ProductUpdateType.RemoveFeature: return "Removing Feature";
            default:                             return type.ToString();
        }
    }

    private static string FormatNumber(int value)
    {
        if (value >= 1000000) return (value / 1000000f).ToString("F1") + "M";
        if (value >= 1000)    return (value / 1000f).ToString("F1") + "K";
        return value.ToString();
    }

    private static int ComputeTrendDirection(int current, int projected)
    {
        if (projected > current) return 1;
        if (projected < current) return -1;
        return 0;
    }

    private static string FormatTrendLabel(int current, int projected)
    {
        int delta = projected - current;
        if (delta == 0) return "\u2014";
        string sign = delta > 0 ? "\u25B2 " : "\u25BC ";
        int abs = delta < 0 ? -delta : delta;
        return sign + FormatNumber(abs);
    }

    private static string FormatRevenueTrendLabel(int current, int projected)
    {
        int delta = projected - current;
        if (delta == 0) return "\u2014";
        string sign = delta > 0 ? "\u25B2 " : "\u25BC ";
        int abs = delta < 0 ? -delta : delta;
        return sign + UIFormatting.FormatMoney(abs);
    }

    // ── Shipped product capability tooltip ────────────────────────────────────

    // Template / niche cache for create product flow
    public ProductTemplateDefinition[] CachedTemplates { get; private set; }
    public MarketNicheData[] CachedNicheData { get; private set; }

    private CrossProductGateConfig _gateConfig;
    private ProductTemplateDefinition[] _allTemplates;

    public void SetGateConfig(CrossProductGateConfig config, ProductTemplateDefinition[] allTemplates) {
        _gateConfig = config;
        _allTemplates = allTemplates;
        CachedTemplates = allTemplates;
    }

    public void SetNiches(MarketNicheData[] niches) {
        CachedNicheData = niches;
    }

    public TooltipData BuildShippedCapabilityTooltip(ProductId productId) {
        if (_lastState?.ShippedProducts == null) return new TooltipData { Title = "No Data", Body = "" };
        if (!_lastState.ShippedProducts.TryGetValue(productId, out var product))
            return new TooltipData { Title = "Unknown Product", Body = "" };

        bool isTool = product.Category.IsTool();
        string title = product.ProductName + (isTool ? " (Tool)" : " (Platform)");
        string body = "Quality: " + product.OverallQuality.ToString("F0") + "/100";

        var stats = new List<TooltipStatRow>();

        if (product.Features != null && product.Features.Length > 0) {
            int fc = product.Features.Length;
            for (int i = 0; i < fc; i++) {
                var fs = product.Features[i];
                string label = GetShippedFeatureDisplayName(fs.FeatureId);
                string qualLabel = fs.Quality > 0f ? fs.Quality.ToString("F0") : "Not Built";
                stats.Add(new TooltipStatRow { Label = label, Value = qualLabel });
            }
        }

        AppendShippedUnlockRows(stats, isTool, product);

        return new TooltipData { Title = title, Body = body, Stats = stats.ToArray() };
    }

    private void AppendShippedUnlockRows(List<TooltipStatRow> stats, bool isTool, Product product) {
        if (_allTemplates == null) return;
        var unlocked = new List<TooltipStatRow>();
        int tplCount = _allTemplates.Length;
        for (int t = 0; t < tplCount; t++) {
            var tpl = _allTemplates[t];
            if (tpl == null || tpl.availableFeatures == null) continue;
            int fc = tpl.availableFeatures.Length;
            for (int f = 0; f < fc; f++) {
                var feat = tpl.availableFeatures[f];
                if (feat == null) continue;
                string req = isTool ? feat.requiresToolFeature : feat.requiresPlatformFeature;
                if (string.IsNullOrEmpty(req)) continue;
                float upstreamQuality = GetShippedUpstreamFeatureQuality(product, req);
                if (upstreamQuality <= 0f) continue;
                float ceiling = _gateConfig != null ? _gateConfig.GetTierCeiling(upstreamQuality) : float.MaxValue;
                string capLabel = ceiling >= 100f ? "cap: Unlimited" : "cap: " + ceiling.ToString("F0") + "+";
                unlocked.Add(new TooltipStatRow {
                    Label = feat.displayName ?? feat.featureId,
                    Value = capLabel,
                    Style = TooltipRowStyle.Unlocked
                });
            }
        }
        if (unlocked.Count > 0) {
            stats.Add(new TooltipStatRow { Label = "Unlocked", Value = "", Style = TooltipRowStyle.Header });
            int uc = unlocked.Count;
            for (int i = 0; i < uc; i++) stats.Add(unlocked[i]);
        }
    }

    private static float GetShippedUpstreamFeatureQuality(Product product, string featureId) {
        if (product?.Features == null) return 0f;
        int fc = product.Features.Length;
        for (int i = 0; i < fc; i++) {
            if (product.Features[i].FeatureId == featureId) return product.Features[i].Quality;
        }
        return 0f;
    }

    private string GetShippedFeatureDisplayName(string featureId) {
        if (_allTemplates == null || string.IsNullOrEmpty(featureId)) return featureId ?? "";
        int tplCount = _allTemplates.Length;
        for (int t = 0; t < tplCount; t++) {
            var tpl = _allTemplates[t];
            if (tpl?.availableFeatures == null) continue;
            int fc = tpl.availableFeatures.Length;
            for (int f = 0; f < fc; f++) {
                var feat = tpl.availableFeatures[f];
                if (feat != null && feat.featureId == featureId) return feat.displayName ?? featureId;
            }
        }
        return featureId;
    }
}
