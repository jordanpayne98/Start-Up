using System;
using System.Collections.Generic;

// StockSystem Version: Clean v1
public class StockSystem : ISystem
{
    // ─── Pending event struct ────────────────────────────────────────────────────

    private enum StockEventKind : byte
    {
        StockPurchased,
        StockSold,
        CompanyAcquired,
        DividendPaid,
        PlayerAcquired
    }

    private struct PendingStockEvent
    {
        public StockEventKind Kind;
        public CompetitorId   A;
        public CompetitorId   B;
        public float          Pct;
        public long           Amount;
    }

    // ─── Constants ───────────────────────────────────────────────────────────────

    private const float RevenueWeight      = 0.40f;
    private const float ProductWeight      = 0.15f;
    private const float MarketShareWeight  = 0.30f;
    private const float ReputationWeight   = 0.15f;
    private const float RevenueMultiplier  = 12f;
    private const float MarketShareScale   = 100_000f;
    private const float ReputationScale    = 10_000f;
    private const float ProductValueBase   = 50_000f;

    private const float DividendRateDefault        = 0.07f;
    private const float InvestorPremiumMin         = 0.15f;
    private const float InvestorPremiumMax         = 0.25f;
    private const float AcquirerBuyChancePerMonth  = 0.35f;
    private const float OtherBuyChancePerMonth     = 0.03f;
    private const float AcquirerStakeSize          = 0.10f;
    private const float OtherStakeSize             = 0.03f;
    private const float PlayerConsentChance        = 0.02f;
    private const float PlayerBankruptConsentChance = 0.90f;
    private const float AcquirerCashReserveRatio   = 0.40f;
    private const float OtherCashReserveRatio      = 0.70f;
    private const float AcquisitionThreshold       = 1.0f;

    // ─── Events ──────────────────────────────────────────────────────────────────

    public event Action<CompetitorId, CompetitorId, float> OnStockPurchased;
    public event Action<CompetitorId, CompetitorId, float> OnStockSold;
    public event Action<CompetitorId, CompetitorId>        OnCompanyAcquired;
    public event Action<CompetitorId, long>                OnDividendPaid;
    public event Action<CompetitorId>                      OnPlayerAcquired;

    // ─── Private state ───────────────────────────────────────────────────────────

    private readonly StockState       _state;
    private readonly CompetitorState  _compState;
    private readonly FinanceSystem    _financeSystem;
    private readonly IRng             _rng;
    private readonly ILogger          _logger;

    private TimeSystem       _timeSystem;
    private CompetitorSystem _competitorSystem;

    private readonly List<PendingStockEvent> _pendingEvents;
    private readonly List<CompetitorId>      _scratchIds;
    private readonly List<CompetitorId>      _acquisitionPending;
    private readonly List<StockHolding>      _scratchHoldings;
    private readonly List<StockHoldingId>    _scratchHoldingIds;

    // Player is represented by a sentinel CompetitorId(0) meaning "the player company".
    // IsPlayerOwned on holdings is the authoritative flag.

    // ─── Constructor ─────────────────────────────────────────────────────────────

    public StockSystem(
        StockState state,
        CompetitorState compState,
        FinanceSystem finance,
        IRng rng,
        ILogger logger)
    {
        _state         = state         ?? throw new ArgumentNullException(nameof(state));
        _compState     = compState     ?? throw new ArgumentNullException(nameof(compState));
        _financeSystem = finance       ?? throw new ArgumentNullException(nameof(finance));
        _rng           = rng           ?? throw new ArgumentNullException(nameof(rng));
        _logger        = logger        ?? new NullLogger();
        _pendingEvents      = new List<PendingStockEvent>(16);
        _scratchIds         = new List<CompetitorId>(16);
        _acquisitionPending = new List<CompetitorId>(4);
        _scratchHoldings    = new List<StockHolding>(8);
        _scratchHoldingIds  = new List<StockHoldingId>(8);
    }

    public void SetTimeSystem(TimeSystem ts)       { _timeSystem       = ts; }
    public void SetCompetitorSystem(CompetitorSystem cs) { _competitorSystem = cs; }

    // ─── Read Model ──────────────────────────────────────────────────────────────

    public StockListing GetListing(CompetitorId companyId)
    {
        _state.listings.TryGetValue(companyId, out var listing);
        return listing;
    }

    public List<StockHolding> GetPlayerHoldings()
    {
        var result = new List<StockHolding>();
        foreach (var kvp in _state.holdings)
        {
            if (kvp.Value.IsPlayerOwned)
                result.Add(kvp.Value);
        }
        return result;
    }

    public List<StockHolding> GetHoldingsFor(CompetitorId ownerId)
    {
        var result = new List<StockHolding>();
        foreach (var kvp in _state.holdings)
        {
            if (!kvp.Value.IsPlayerOwned && kvp.Value.OwnerCompanyId == ownerId)
                result.Add(kvp.Value);
        }
        return result;
    }

    public float GetPlayerOwnershipOf(CompetitorId companyId)
    {
        float total = 0f;
        foreach (var kvp in _state.holdings)
        {
            var h = kvp.Value;
            if (h.IsPlayerOwned && h.TargetCompanyId == companyId)
                total += h.PercentageOwned;
        }
        return total;
    }

    public long GetTotalOwnedBy(CompetitorId ownerId)
    {
        long total = 0L;
        foreach (var kvp in _state.holdings)
        {
            var h = kvp.Value;
            if (h.IsPlayerOwned || h.OwnerCompanyId != ownerId) continue;
            if (_state.listings.TryGetValue(h.TargetCompanyId, out var listing))
                total += (long)(listing.StockPrice * h.PercentageOwned);
        }
        return total;
    }

    public long CalculateStockPrice(CompetitorId companyId)
    {
        if (!_compState.competitors.TryGetValue(companyId, out var comp))
            return 1L;
        if (comp.IsBankrupt || comp.IsAbsorbed)
            return 0L;

        float revenue    = (float)comp.Finance.MonthlyRevenue * RevenueMultiplier;
        int   prodCount  = (comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0)
                         + (comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0);
        float marketShare = 0f;
        if (comp.NicheMarketShare != null)
        {
            foreach (var kvp in comp.NicheMarketShare)
                marketShare += kvp.Value;
        }
        float reputation = (float)comp.ReputationPoints;

        long price = (long)(
            revenue   * RevenueWeight
          + prodCount * ProductValueBase * ProductWeight
          + marketShare * MarketShareScale * MarketShareWeight
          + reputation  * ReputationScale  * ReputationWeight
        );

        return price < 1L ? 1L : price;
    }

    public long CalculateBuyFromInvestorPrice(CompetitorId companyId, CompetitorId investorId, float percentage)
    {
        long marketPrice = CalculateStockPrice(companyId);
        float premium = InvestorPremiumMin + _rng.NextFloat01() * (InvestorPremiumMax - InvestorPremiumMin);
        long total = (long)(marketPrice * percentage * (1f + premium));
        return total < 1L ? 1L : total;
    }

    public bool CanBuyStock(CompetitorId buyerId, CompetitorId targetId, float percentage)
    {
        if (!_compState.competitors.TryGetValue(targetId, out var target))
            return false;
        if (target.IsBankrupt || target.IsAbsorbed)
            return false;

        if (!_state.listings.TryGetValue(targetId, out var listing))
            return false;
        if (listing.UnownedPercentage < percentage)
            return false;

        long price = (long)(listing.StockPrice * percentage);
        long buyerCash = GetCashFor(buyerId);
        return buyerCash >= price;
    }

    public bool CanSellStock(CompetitorId sellerId, CompetitorId targetId, float percentage)
    {
        float owned = GetOwnershipFor(sellerId, targetId);
        return owned >= percentage - 0.0001f;
    }

    public long GetAnnualDividendIncome(CompetitorId ownerId)
    {
        long total = 0L;
        foreach (var kvp in _state.holdings)
        {
            var h = kvp.Value;
            if (h.IsPlayerOwned || h.OwnerCompanyId != ownerId) continue;
            if (!_compState.competitors.TryGetValue(h.TargetCompanyId, out var comp)) continue;
            long annualProfit = comp.Finance.MonthlyProfit * 12L;
            if (annualProfit <= 0) continue;
            total += (long)(annualProfit * DividendRateDefault * h.PercentageOwned);
        }
        return total;
    }

    public Dictionary<CompetitorId, float> GetOwnershipBreakdown(CompetitorId companyId)
    {
        if (_state.listings.TryGetValue(companyId, out var listing))
            return listing.OwnershipBreakdown;
        return new Dictionary<CompetitorId, float>();
    }

    // ─── Transactions ────────────────────────────────────────────────────────────

    public bool ProcessBuyStock(CompetitorId buyerId, CompetitorId targetId, float percentage, CompetitorId? fromInvestorId)
    {
        if (!_compState.competitors.TryGetValue(targetId, out var target))
        {
            _logger.LogWarning($"[StockSystem] Buy failed: target {targetId.Value} not found.");
            return false;
        }
        if (target.IsBankrupt || target.IsAbsorbed)
        {
            _logger.LogWarning($"[StockSystem] Buy failed: target {targetId.Value} is bankrupt or absorbed.");
            return false;
        }

        EnsureListing(targetId);
        var listing = _state.listings[targetId];

        long price;
        if (fromInvestorId.HasValue)
        {
            float investorOwned = GetOwnershipFor(fromInvestorId.Value, targetId);
            if (investorOwned < percentage - 0.0001f)
            {
                _logger.LogWarning($"[StockSystem] Buy-from-investor failed: investor {fromInvestorId.Value.Value} owns only {investorOwned:P0}.");
                return false;
            }
            price = CalculateBuyFromInvestorPrice(targetId, fromInvestorId.Value, percentage);

            long buyerCash = GetCashFor(buyerId);
            if (buyerCash < price)
            {
                _logger.LogWarning($"[StockSystem] Buy-from-investor failed: insufficient cash.");
                return false;
            }

            DeductCashFrom(buyerId, price);
            AddCashTo(fromInvestorId.Value, price);
            RemoveOwnership(fromInvestorId.Value, targetId, percentage);
        }
        else
        {
            if (listing.UnownedPercentage < percentage - 0.0001f)
            {
                _logger.LogWarning($"[StockSystem] Buy failed: not enough unowned shares ({listing.UnownedPercentage:P0} available).");
                return false;
            }

            price = (long)(listing.StockPrice * percentage);
            long buyerCash = GetCashFor(buyerId);
            if (buyerCash < price)
            {
                _logger.LogWarning($"[StockSystem] Buy failed: insufficient cash.");
                return false;
            }

            DeductCashFrom(buyerId, price);
            listing.UnownedPercentage -= percentage;
            if (listing.UnownedPercentage < 0f) listing.UnownedPercentage = 0f;
        }

        AddOwnership(buyerId, targetId, percentage, price, 0);

        if (!listing.OwnershipBreakdown.ContainsKey(buyerId))
            listing.OwnershipBreakdown[buyerId] = 0f;
        listing.OwnershipBreakdown[buyerId] += percentage;

        listing.StockPrice = CalculateStockPrice(targetId);
        _state.listings[targetId] = listing;

        _logger.Log($"[StockSystem] {FormatOwner(buyerId)} bought {percentage:P0} of {targetId.Value} for ${price:N0}.");

        bool acquisitionReached = GetOwnershipFor(buyerId, targetId) >= AcquisitionThreshold - 0.0001f;

        _pendingEvents.Add(new PendingStockEvent
        {
            Kind = StockEventKind.StockPurchased,
            A    = buyerId,
            B    = targetId,
            Pct  = percentage
        });

        if (acquisitionReached && !_acquisitionPending.Contains(targetId))
            _acquisitionPending.Add(targetId);

        return true;
    }

    public bool ProcessSellStock(CompetitorId sellerId, CompetitorId targetId, float percentage)
    {
        float owned = GetOwnershipFor(sellerId, targetId);
        if (owned < percentage - 0.0001f)
        {
            _logger.LogWarning($"[StockSystem] Sell failed: {FormatOwner(sellerId)} owns only {owned:P0} of {targetId.Value}.");
            return false;
        }

        EnsureListing(targetId);
        var listing = _state.listings[targetId];
        long proceeds = (long)(listing.StockPrice * percentage);

        RemoveOwnership(sellerId, targetId, percentage);
        AddCashTo(sellerId, proceeds);
        listing.UnownedPercentage += percentage;
        if (listing.UnownedPercentage > 1f) listing.UnownedPercentage = 1f;

        if (listing.OwnershipBreakdown.TryGetValue(sellerId, out float cur))
        {
            float newVal = cur - percentage;
            if (newVal <= 0.0001f)
                listing.OwnershipBreakdown.Remove(sellerId);
            else
                listing.OwnershipBreakdown[sellerId] = newVal;
        }

        listing.StockPrice = CalculateStockPrice(targetId);
        _state.listings[targetId] = listing;

        _logger.Log($"[StockSystem] {FormatOwner(sellerId)} sold {percentage:P0} of {targetId.Value} for ${proceeds:N0}.");

        _pendingEvents.Add(new PendingStockEvent
        {
            Kind = StockEventKind.StockSold,
            A    = sellerId,
            B    = targetId,
            Pct  = percentage
        });

        return true;
    }

    public void ProcessAcquisition(CompetitorId acquirerId, CompetitorId targetId, int tick)
    {
        if (!_compState.competitors.TryGetValue(targetId, out var target)) return;
        if (!_compState.competitors.TryGetValue(acquirerId, out var acquirer)) return;

        _logger.Log($"[StockSystem] ACQUISITION: {acquirerId.Value} acquires {targetId.Value}.");

        // Transfer employees
        if (target.EmployeeIds != null)
        {
            if (acquirer.EmployeeIds == null) acquirer.EmployeeIds = new List<EmployeeId>();
            int empCount = target.EmployeeIds.Count;
            for (int i = 0; i < empCount; i++)
                acquirer.EmployeeIds.Add(target.EmployeeIds[i]);
            target.EmployeeIds.Clear();
        }

        // Transfer active products
        if (target.ActiveProductIds != null)
        {
            if (acquirer.ActiveProductIds == null) acquirer.ActiveProductIds = new List<ProductId>();
            int actCount = target.ActiveProductIds.Count;
            for (int i = 0; i < actCount; i++)
                acquirer.ActiveProductIds.Add(target.ActiveProductIds[i]);
            target.ActiveProductIds.Clear();
        }

        // Transfer in-development products
        if (target.InDevelopmentProductIds != null)
        {
            if (acquirer.InDevelopmentProductIds == null) acquirer.InDevelopmentProductIds = new List<ProductId>();
            int devCount = target.InDevelopmentProductIds.Count;
            for (int i = 0; i < devCount; i++)
                acquirer.InDevelopmentProductIds.Add(target.InDevelopmentProductIds[i]);
            target.InDevelopmentProductIds.Clear();
        }

        // Cascade: transfer target's stock holdings in other companies to acquirer
        _scratchHoldingIds.Clear();
        foreach (var kvp in _state.holdings)
        {
            if (!kvp.Value.IsPlayerOwned && kvp.Value.OwnerCompanyId == targetId)
                _scratchHoldingIds.Add(kvp.Key);
        }
        int transferCount = _scratchHoldingIds.Count;
        for (int i = 0; i < transferCount; i++)
        {
            StockHoldingId hid = _scratchHoldingIds[i];
            if (!_state.holdings.TryGetValue(hid, out var holding)) continue;

            if (holding.TargetCompanyId == acquirerId)
            {
                // Target held acquirer's stock — those become unowned
                EnsureListing(acquirerId);
                var acqListing = _state.listings[acquirerId];
                acqListing.UnownedPercentage += holding.PercentageOwned;
                if (acqListing.UnownedPercentage > 1f) acqListing.UnownedPercentage = 1f;
                acqListing.OwnershipBreakdown.Remove(targetId);
                _state.listings[acquirerId] = acqListing;
                _state.holdings.Remove(hid);
            }
            else
            {
                // Transfer ownership to acquirer
                var newHolding = holding;
                newHolding.OwnerCompanyId = acquirerId;
                _state.holdings[hid] = newHolding;

                EnsureListing(holding.TargetCompanyId);
                var otherListing = _state.listings[holding.TargetCompanyId];
                if (otherListing.OwnershipBreakdown.ContainsKey(targetId))
                {
                    float pct = otherListing.OwnershipBreakdown[targetId];
                    otherListing.OwnershipBreakdown.Remove(targetId);
                    if (!otherListing.OwnershipBreakdown.ContainsKey(acquirerId))
                        otherListing.OwnershipBreakdown[acquirerId] = 0f;
                    otherListing.OwnershipBreakdown[acquirerId] += pct;
                    _state.listings[holding.TargetCompanyId] = otherListing;
                }

                // Cascading: check if acquirer now owns 100% of another company
                if (!_acquisitionPending.Contains(holding.TargetCompanyId))
                {
                    if (GetOwnershipFor(acquirerId, holding.TargetCompanyId) >= AcquisitionThreshold - 0.0001f)
                        _acquisitionPending.Add(holding.TargetCompanyId);
                }
            }
        }

        // Mark target as absorbed
        target.IsAbsorbed   = true;
        target.AbsorbedById = acquirerId;

        // Remove target's listing
        _state.listings.Remove(targetId);

        bool isPlayerTarget = IsPlayer(targetId);

        _pendingEvents.Add(new PendingStockEvent
        {
            Kind = StockEventKind.CompanyAcquired,
            A    = acquirerId,
            B    = targetId
        });

        if (isPlayerTarget)
        {
            _pendingEvents.Add(new PendingStockEvent
            {
                Kind = StockEventKind.PlayerAcquired,
                A    = acquirerId
            });
        }
    }

    public void ProcessBankruptStockLoss(CompetitorId bankruptId)
    {
        _scratchHoldingIds.Clear();
        foreach (var kvp in _state.holdings)
        {
            if (kvp.Value.TargetCompanyId == bankruptId)
                _scratchHoldingIds.Add(kvp.Key);
        }
        int count = _scratchHoldingIds.Count;
        for (int i = 0; i < count; i++)
            _state.holdings.Remove(_scratchHoldingIds[i]);

        _state.listings.Remove(bankruptId);
        _logger.Log($"[StockSystem] All holdings in bankrupt company {bankruptId.Value} wiped out.");
    }

    // ─── ISystem ─────────────────────────────────────────────────────────────────

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        if (_timeSystem == null) return;

        bool isMonthly = IsMonthlyBoundary(tick);
        bool isYearly  = IsYearlyBoundary(tick);

        if (isMonthly)
        {
            RecalculateAllStockPrices();
            EvaluateAIStockPurchases(tick);
            CheckPlayerAcquisitionThreshold();
        }

        if (isYearly)
        {
            ProcessDividends(tick);
        }

        int acqCount = _acquisitionPending.Count;
        for (int i = 0; i < acqCount; i++)
            ProcessAcquisitionForBuyer(_acquisitionPending[i], tick);
        _acquisitionPending.Clear();
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var e = _pendingEvents[i];
            switch (e.Kind)
            {
                case StockEventKind.StockPurchased:
                    OnStockPurchased?.Invoke(e.A, e.B, e.Pct);
                    break;
                case StockEventKind.StockSold:
                    OnStockSold?.Invoke(e.A, e.B, e.Pct);
                    break;
                case StockEventKind.CompanyAcquired:
                    OnCompanyAcquired?.Invoke(e.A, e.B);
                    break;
                case StockEventKind.DividendPaid:
                    OnDividendPaid?.Invoke(e.A, e.Amount);
                    break;
                case StockEventKind.PlayerAcquired:
                    OnPlayerAcquired?.Invoke(e.A);
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is BuyStockCommand buyCmd)
        {
            ProcessBuyStock(PlayerSentinel(), buyCmd.TargetCompanyId, buyCmd.Percentage, buyCmd.FromInvestorId);
        }
        else if (command is SellStockCommand sellCmd)
        {
            ProcessSellStock(PlayerSentinel(), sellCmd.TargetCompanyId, sellCmd.Percentage);
        }
        else if (command is SellProductToCompetitorCommand saleCmd)
        {
            ProcessSellProductToCompetitor(saleCmd.ProductId, saleCmd.BuyerCompetitorId, command.Tick);
        }
    }

    public void Dispose()
    {
        _pendingEvents.Clear();
        OnStockPurchased  = null;
        OnStockSold       = null;
        OnCompanyAcquired = null;
        OnDividendPaid    = null;
        OnPlayerAcquired  = null;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────

    private void RecalculateAllStockPrices()
    {
        _scratchIds.Clear();
        foreach (var kvp in _state.listings)
            _scratchIds.Add(kvp.Key);

        int count = _scratchIds.Count;
        for (int i = 0; i < count; i++)
        {
            CompetitorId id = _scratchIds[i];
            if (!_state.listings.TryGetValue(id, out var listing)) continue;
            listing.StockPrice = CalculateStockPrice(id);
            _state.listings[id] = listing;
        }
    }

    private void EvaluateAIStockPurchases(int tick)
    {
        _scratchIds.Clear();
        foreach (var kvp in _compState.competitors)
        {
            if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                _scratchIds.Add(kvp.Key);
        }

        int buyerCount = _scratchIds.Count;
        for (int i = 0; i < buyerCount; i++)
        {
            CompetitorId buyerId = _scratchIds[i];
            if (!_compState.competitors.TryGetValue(buyerId, out var buyer)) continue;

            bool isAcquirer = buyer.Archetype == CompetitorArchetype.FullStack
                           || buyer.Archetype == CompetitorArchetype.PlatformGiant;
            float buyChance = isAcquirer ? AcquirerBuyChancePerMonth : OtherBuyChancePerMonth;

            if (!_rng.Chance(buyChance)) continue;

            float reserveRatio = isAcquirer ? AcquirerCashReserveRatio : OtherCashReserveRatio;
            long spendable = (long)(buyer.Finance.Cash * (1f - reserveRatio));
            if (spendable <= 0) continue;

            // Pick a random target (not self, not bankrupt, not absorbed)
            int attempts = 0;
            while (attempts < 5)
            {
                attempts++;
                int idx = _rng.Range(0, buyerCount);
                CompetitorId targetId = _scratchIds[idx];
                if (targetId == buyerId) continue;
                if (!_compState.competitors.TryGetValue(targetId, out var targetComp)) continue;
                if (targetComp.IsBankrupt || targetComp.IsAbsorbed) continue;

                bool targetIsPlayer = IsPlayer(targetId);
                if (targetIsPlayer)
                {
                    bool nearBankrupt = _financeSystem.FinancialHealth >= FinancialHealthState.Distressed;
                    float consentChance = nearBankrupt ? PlayerBankruptConsentChance : PlayerConsentChance;
                    if (!_rng.Chance(consentChance)) break;
                }

                EnsureListing(targetId);
                float stake = isAcquirer ? AcquirerStakeSize : OtherStakeSize;
                var listing = _state.listings[targetId];

                if (listing.UnownedPercentage < stake - 0.0001f) break;

                long price = (long)(listing.StockPrice * stake);
                if (spendable < price) break;

                buyer.Finance.Cash -= price;
                AddOwnership(buyerId, targetId, stake, price, tick);
                listing.UnownedPercentage -= stake;
                if (listing.UnownedPercentage < 0f) listing.UnownedPercentage = 0f;
                if (!listing.OwnershipBreakdown.ContainsKey(buyerId))
                    listing.OwnershipBreakdown[buyerId] = 0f;
                listing.OwnershipBreakdown[buyerId] += stake;
                listing.StockPrice = CalculateStockPrice(targetId);
                _state.listings[targetId] = listing;

                _logger.Log($"[StockSystem] AI {buyerId.Value} ({buyer.Archetype}) bought {stake:P0} of {targetId.Value}.");

                _pendingEvents.Add(new PendingStockEvent
                {
                    Kind = StockEventKind.StockPurchased,
                    A    = buyerId,
                    B    = targetId,
                    Pct  = stake
                });

                if (GetOwnershipFor(buyerId, targetId) >= AcquisitionThreshold - 0.0001f
                    && !_acquisitionPending.Contains(targetId))
                    _acquisitionPending.Add(targetId);

                break;
            }
        }
    }

    private void CheckPlayerAcquisitionThreshold()
    {
        foreach (var kvp in _compState.competitors)
        {
            if (kvp.Value.IsBankrupt || kvp.Value.IsAbsorbed) continue;
            CompetitorId buyerId = kvp.Key;
            float playerOwned = GetPlayerOwnershipOf(buyerId);
            if (playerOwned >= AcquisitionThreshold - 0.0001f
                && !_acquisitionPending.Contains(buyerId))
            {
                // Competitor owns 100% of player's stock — player is acquired
                if (!_acquisitionPending.Contains(new CompetitorId(0)))
                    _acquisitionPending.Add(new CompetitorId(0));
            }
        }
    }

    private void ProcessDividends(int tick)
    {
        // Player dividends
        float playerDividendTotal = 0L;
        _scratchHoldings.Clear();
        foreach (var kvp in _state.holdings)
        {
            if (kvp.Value.IsPlayerOwned)
                _scratchHoldings.Add(kvp.Value);
        }
        int playerCount = _scratchHoldings.Count;
        for (int i = 0; i < playerCount; i++)
        {
            var h = _scratchHoldings[i];
            if (!_compState.competitors.TryGetValue(h.TargetCompanyId, out var comp)) continue;
            long annualProfit = comp.Finance.MonthlyProfit * 12L;
            if (annualProfit <= 0) continue;
            long dividend = (long)(annualProfit * DividendRateDefault * h.PercentageOwned);
            if (dividend <= 0) continue;
            playerDividendTotal += dividend;
        }

        if (playerDividendTotal > 0)
        {
            long capturedAmount = (long)playerDividendTotal;
            _financeSystem.AddDividendIncome(capturedAmount, tick);
            _pendingEvents.Add(new PendingStockEvent
            {
                Kind   = StockEventKind.DividendPaid,
                A      = PlayerSentinel(),
                Amount = capturedAmount
            });
            _logger.Log($"[StockSystem] Player received ${capturedAmount:N0} in annual dividends.");
        }

        // AI dividends
        _scratchIds.Clear();
        foreach (var kvp in _compState.competitors)
        {
            if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                _scratchIds.Add(kvp.Key);
        }

        int aiCount = _scratchIds.Count;
        for (int i = 0; i < aiCount; i++)
        {
            CompetitorId ownerId = _scratchIds[i];
            if (!_compState.competitors.TryGetValue(ownerId, out var owner)) continue;

            long totalDividend = 0L;
            foreach (var kvp in _state.holdings)
            {
                var h = kvp.Value;
                if (h.IsPlayerOwned || h.OwnerCompanyId != ownerId) continue;
                if (!_compState.competitors.TryGetValue(h.TargetCompanyId, out var target)) continue;
                long annualProfit = target.Finance.MonthlyProfit * 12L;
                if (annualProfit <= 0) continue;
                totalDividend += (long)(annualProfit * DividendRateDefault * h.PercentageOwned);
            }

            if (totalDividend <= 0) continue;
            owner.Finance.Cash += totalDividend;

            _pendingEvents.Add(new PendingStockEvent
            {
                Kind   = StockEventKind.DividendPaid,
                A      = ownerId,
                Amount = totalDividend
            });
        }
    }

    private void ProcessAcquisitionForBuyer(CompetitorId targetId, int tick)
    {
        // Find who holds 100% of targetId
        if (!_state.listings.TryGetValue(targetId, out var listing))
        {
            // No listing — check player ownership of some competitor
            // (player is represented by holdings only)
            return;
        }

        CompetitorId acquirerId = default;
        bool found = false;
        foreach (var kvp in listing.OwnershipBreakdown)
        {
            if (kvp.Value >= AcquisitionThreshold - 0.0001f)
            {
                acquirerId = kvp.Key;
                found = true;
                break;
            }
        }

        if (!found) return;
        ProcessAcquisition(acquirerId, targetId, tick);
    }

    private void ProcessSellProductToCompetitor(ProductId productId, CompetitorId buyerId, int tick)
    {
        if (!_compState.competitors.TryGetValue(buyerId, out var buyer)) return;

        // Use a simple fair market value placeholder — product sale pricing
        long fairValue = 50_000L + _rng.Range(0, 50000);
        float interestMult = buyer.Archetype == CompetitorArchetype.PlatformGiant
                          || buyer.Archetype == CompetitorArchetype.FullStack ? 1.3f : 1.0f;
        float pct = 0.4f + _rng.NextFloat01() * 0.6f;
        long offerPrice = (long)(fairValue * pct * interestMult);

        _financeSystem.AddProductSaleProceeds(offerPrice, tick);

        _pendingEvents.Add(new PendingStockEvent
        {
            Kind = StockEventKind.StockPurchased,
            A    = buyerId,
            B    = buyerId,
            Pct  = 0f
        });

        _logger.Log($"[StockSystem] Product {productId.Value} sold to competitor {buyerId.Value} for ${offerPrice:N0}.");
    }

    // ─── Ownership helpers ───────────────────────────────────────────────────────

    public void EnsureListingsForAll() {
        foreach (var kvp in _compState.competitors) {
            if (kvp.Value != null && !kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                EnsureListing(kvp.Key);
        }
    }

    private void EnsureListing(CompetitorId companyId)
    {
        if (_state.listings.ContainsKey(companyId)) return;
        _state.listings[companyId] = new StockListing
        {
            CompanyId         = companyId,
            StockPrice        = CalculateStockPrice(companyId),
            UnownedPercentage = 1f,
            OwnershipBreakdown = new Dictionary<CompetitorId, float>(),
            LastDividendPayout = 0L,
            LastDividendTick   = 0
        };
    }

    private void AddOwnership(CompetitorId ownerId, CompetitorId targetId, float percentage, long price, int tick)
    {
        // Find existing holding for this pair
        StockHoldingId? existingId = null;
        foreach (var kvp in _state.holdings)
        {
            var h = kvp.Value;
            bool ownerMatch = IsPlayer(ownerId) ? h.IsPlayerOwned : (!h.IsPlayerOwned && h.OwnerCompanyId == ownerId);
            if (ownerMatch && h.TargetCompanyId == targetId)
            {
                existingId = kvp.Key;
                break;
            }
        }

        if (existingId.HasValue)
        {
            var holding = _state.holdings[existingId.Value];
            holding.PercentageOwned += percentage;
            _state.holdings[existingId.Value] = holding;
        }
        else
        {
            var id = new StockHoldingId(_state.nextHoldingId++);
            _state.holdings[id] = new StockHolding
            {
                Id              = id,
                TargetCompanyId = targetId,
                OwnerCompanyId  = IsPlayer(ownerId) ? default : ownerId,
                IsPlayerOwned   = IsPlayer(ownerId),
                PercentageOwned = percentage,
                PurchasePrice   = price,
                PurchaseTick    = tick
            };
        }
    }

    private void RemoveOwnership(CompetitorId ownerId, CompetitorId targetId, float percentage)
    {
        StockHoldingId? foundId = null;
        foreach (var kvp in _state.holdings)
        {
            var h = kvp.Value;
            bool ownerMatch = IsPlayer(ownerId) ? h.IsPlayerOwned : (!h.IsPlayerOwned && h.OwnerCompanyId == ownerId);
            if (ownerMatch && h.TargetCompanyId == targetId)
            {
                foundId = kvp.Key;
                break;
            }
        }
        if (!foundId.HasValue) return;

        var holding = _state.holdings[foundId.Value];
        holding.PercentageOwned -= percentage;
        if (holding.PercentageOwned <= 0.0001f)
            _state.holdings.Remove(foundId.Value);
        else
            _state.holdings[foundId.Value] = holding;
    }

    private float GetOwnershipFor(CompetitorId ownerId, CompetitorId targetId)
    {
        float total = 0f;
        foreach (var kvp in _state.holdings)
        {
            var h = kvp.Value;
            bool ownerMatch = IsPlayer(ownerId) ? h.IsPlayerOwned : (!h.IsPlayerOwned && h.OwnerCompanyId == ownerId);
            if (ownerMatch && h.TargetCompanyId == targetId)
                total += h.PercentageOwned;
        }
        return total;
    }

    private long GetCashFor(CompetitorId ownerId)
    {
        if (IsPlayer(ownerId))
            return _financeSystem.Money;
        if (_compState.competitors.TryGetValue(ownerId, out var comp))
            return comp.Finance.Cash;
        return 0L;
    }

    private void DeductCashFrom(CompetitorId ownerId, long amount)
    {
        if (IsPlayer(ownerId))
        {
            _financeSystem.RecordTransaction(-(int)amount, FinanceCategory.MiscExpense, 0, "stock-buy");
        }
        else if (_compState.competitors.TryGetValue(ownerId, out var comp))
        {
            comp.Finance.Cash -= amount;
            if (comp.Finance.Cash < 0) comp.Finance.Cash = 0;
        }
    }

    private void AddCashTo(CompetitorId ownerId, long amount)
    {
        if (IsPlayer(ownerId))
        {
            _financeSystem.RecordTransaction((int)amount, FinanceCategory.MiscIncome, 0, "stock-sell");
        }
        else if (_compState.competitors.TryGetValue(ownerId, out var comp))
        {
            comp.Finance.Cash += amount;
        }
    }

    private static bool IsPlayer(CompetitorId id) => id.Value == 0;
    private static CompetitorId PlayerSentinel() => new CompetitorId(0);
    private static string FormatOwner(CompetitorId id) => IsPlayer(id) ? "Player" : $"AI-{id.Value}";

    private bool IsMonthlyBoundary(int tick)
    {
        if (_timeSystem == null) return false;
        int dayOfTick  = tick / TimeState.TicksPerDay;
        if (dayOfTick <= 0) return false;
        int prevDay    = (tick - 1) / TimeState.TicksPerDay;
        int curMonth   = TimeState.GetMonth(dayOfTick);
        int prevMonth  = TimeState.GetMonth(prevDay);
        return curMonth != prevMonth;
    }

    private bool IsYearlyBoundary(int tick)
    {
        if (_timeSystem == null) return false;
        int dayOfTick = tick / TimeState.TicksPerDay;
        if (dayOfTick <= 0) return false;
        int prevDay   = (tick - 1) / TimeState.TicksPerDay;
        int curYear   = TimeState.GetYear(dayOfTick);
        int prevYear  = TimeState.GetYear(prevDay);
        return curYear != prevYear;
    }
}
