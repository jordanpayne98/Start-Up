using System;
using System.Collections.Generic;

public class AIDecisionSystem : ISystem
{
    private enum CashHealthTier : byte
    {
        Critical,
        Tight,
        Stable,
        Flush
    }

    private enum PendingEventType : byte
    {
        HiredCandidate,
        ProductSunset
    }

    private struct PendingAIEvent
    {
        public PendingEventType Type;
        public CompetitorId CompId;
        public ProductId ProductId;
        public string CandidateName;
        public EmployeeRole Role;
        public string CompanyName;
    }

    private const int TwoWeeksTicks = TimeState.TicksPerDay * 14;

    private readonly CompetitorState _competitorState;
    private readonly EmployeeSystem _employeeSystem;
    private readonly TeamSystem _teamSystem;
    private readonly ProductState _productState;
    private readonly MarketState _marketState;
    private readonly GameEventBus _eventBus;
    private readonly IRng _rng;
    private readonly TimeSystem _timeSystem;
    private readonly EmployeeState _employeeState;
    private readonly ILogger _logger;

    private CompetitorArchetypeConfig[] _archetypeConfigs;
    private MarketSystem _marketSystem;
    private GenerationSystem _generationSystem;
    private ReviewSystem _reviewSystem;
    private PlatformState _platformState;
    private MoraleSystem _moraleSystem;
    private TaxSystem _taxSystem;
    private Dictionary<string, ProductTemplateDefinition> _templateLookup;
    private ProductSystem _productSystem;
    private CompetitorSystem _competitorSystem;
    private CompetitorNameData _nameData;
    private CrossProductGateConfig _crossProductGateConfig;

    private Action<ProductId, int> _onReleaseDateAnnouncedHandler;
    private Action<ReleaseDateChangedEvent> _onReleaseDateChangedHandler;

    private readonly List<PendingAIEvent> _pendingEvents;
    private readonly List<CompetitorId> _scratchIds;
    private readonly List<ProductId> _scratchProductIds;

    private int _lastKnownTick;

    public AIDecisionSystem(
        CompetitorState competitorState,
        EmployeeSystem employeeSystem,
        TeamSystem teamSystem,
        ProductState productState,
        MarketState marketState,
        GameEventBus eventBus,
        IRng rng,
        TimeSystem timeSystem,
        EmployeeState employeeState,
        ILogger logger)
    {
        _competitorState = competitorState ?? throw new ArgumentNullException(nameof(competitorState));
        _employeeSystem = employeeSystem ?? throw new ArgumentNullException(nameof(employeeSystem));
        _teamSystem = teamSystem ?? throw new ArgumentNullException(nameof(teamSystem));
        _productState = productState ?? throw new ArgumentNullException(nameof(productState));
        _marketState = marketState ?? throw new ArgumentNullException(nameof(marketState));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _timeSystem = timeSystem ?? throw new ArgumentNullException(nameof(timeSystem));
        _employeeState = employeeState ?? throw new ArgumentNullException(nameof(employeeState));
        _logger = logger ?? new NullLogger();
        _templateLookup = new Dictionary<string, ProductTemplateDefinition>();
        _pendingEvents = new List<PendingAIEvent>();
        _scratchIds = new List<CompetitorId>(16);
        _scratchProductIds = new List<ProductId>(16);
    }

    public void SetArchetypeConfigs(CompetitorArchetypeConfig[] configs) { _archetypeConfigs = configs; }
    public void SetMarketSystem(MarketSystem ms) { _marketSystem = ms; }
    public void SetGenerationSystem(GenerationSystem gs) { _generationSystem = gs; }
    public void SetReviewSystem(ReviewSystem rs) { _reviewSystem = rs; }
    public void SetPlatformState(PlatformState ps) { _platformState = ps; }
    public void SetMoraleSystem(MoraleSystem ms) { _moraleSystem = ms; }
    public void SetTaxSystem(TaxSystem ts) { _taxSystem = ts; }
    public void SetCrossProductGateConfig(CrossProductGateConfig cfg) { _crossProductGateConfig = cfg; }
    public void SetNameData(CompetitorNameData nameData) { _nameData = nameData; }
    public void SetCompetitorSystem(CompetitorSystem cs) { _competitorSystem = cs; }

    public void RegisterTemplates(ProductTemplateDefinition[] templates)
    {
        _templateLookup.Clear();
        if (templates == null) return;
        for (int i = 0; i < templates.Length; i++)
        {
            var t = templates[i];
            if (t != null && !string.IsNullOrEmpty(t.templateId))
                _templateLookup[t.templateId] = t;
        }
    }

    public void SetProductSystem(ProductSystem ps)
    {
        if (_productSystem != null)
        {
            if (_onReleaseDateAnnouncedHandler != null)
                _productSystem.OnReleaseDateAnnounced -= _onReleaseDateAnnouncedHandler;
            if (_onReleaseDateChangedHandler != null)
                _productSystem.OnReleaseDateChanged -= _onReleaseDateChangedHandler;
        }

        _productSystem = ps;

        if (_productSystem != null)
        {
            _onReleaseDateAnnouncedHandler = OnPlayerReleaseDateAnnounced;
            _onReleaseDateChangedHandler = OnPlayerReleaseDateChanged;
            _productSystem.OnReleaseDateAnnounced += _onReleaseDateAnnouncedHandler;
            _productSystem.OnReleaseDateChanged += _onReleaseDateChangedHandler;
        }
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        _lastKnownTick = tick;
        int ticksPerMonth = TimeState.TicksPerDay * 30;

        _scratchIds.Clear();
        foreach (var kvp in _competitorState.competitors)
        {
            if (!kvp.Value.IsBankrupt && !kvp.Value.IsAbsorbed)
                _scratchIds.Add(kvp.Key);
        }

        int count = _scratchIds.Count;
        for (int i = 0; i < count; i++)
        {
            CompetitorId id = _scratchIds[i];
            if (!_competitorState.competitors.TryGetValue(id, out var comp)) continue;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;

            int offset = comp.Id.Value % ticksPerMonth;
            int dayOfMonth = tick % ticksPerMonth;
            if (dayOfMonth != offset) continue;

            EvaluateStrategicReview(comp, tick);

            CashHealthTier cashHealth = ClassifyCashHealth(comp);
            AIBudgetAllocation budget = AllocateBudget(comp, cashHealth);

            EvaluateFireDecisions(comp, tick, cashHealth);
            EvaluateContractRenewals(comp, tick, cashHealth);

            if (cashHealth >= CashHealthTier.Tight)
                EvaluateHiringDecisions(comp, tick, ref budget, cashHealth);

            EvaluateTeamDecisions(comp, tick);

            if (cashHealth >= CashHealthTier.Stable)
                EvaluateNewProductDecision(comp, tick, budget);

            EvaluateSunsetDecision(comp, tick, cashHealth);
            EvaluateProductPerformance(comp, tick, cashHealth);
            EvaluateMaintenanceDecisions(comp, tick, cashHealth);
            EvaluateMarketingDecisions(comp, tick, ref budget, cashHealth);
            EvaluatePricingDecision(comp, tick);
            RecordPendingOutcomes(comp);
            CheckCompetitorProductDeadlines(comp, tick);
        }
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            PendingAIEvent e = _pendingEvents[i];
            if (e.Type == PendingEventType.HiredCandidate)
                _eventBus.Raise(new CompetitorHiredCandidateEvent(tick, e.CompId, e.CandidateName, e.Role, e.CompanyName));
            else if (e.Type == PendingEventType.ProductSunset)
                _eventBus.Raise(new CompetitorProductSunsetEvent(tick, e.CompId, e.ProductId));
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        if (_productSystem != null)
        {
            if (_onReleaseDateAnnouncedHandler != null)
                _productSystem.OnReleaseDateAnnounced -= _onReleaseDateAnnouncedHandler;
            if (_onReleaseDateChangedHandler != null)
                _productSystem.OnReleaseDateChanged -= _onReleaseDateChangedHandler;
        }
        _onReleaseDateAnnouncedHandler = null;
        _onReleaseDateChangedHandler = null;
        _pendingEvents.Clear();
    }

    private void EvaluateStrategicReview(Competitor comp, int tick)
    {
        if (tick - comp.LastStrategicReviewTick < TimeState.TicksPerDay * 90) return;
        comp.LastStrategicReviewTick = tick;

        if (comp.Memory == null) comp.Memory = CompetitorMemory.CreateNew();

        if (comp.Memory.ConsecutiveFlops >= 3)
            comp.Momentum = CompetitorMomentum.Crisis;
        else if (comp.Memory.ConsecutiveFlops >= 2)
            comp.Momentum = CompetitorMomentum.Declining;
        else if (comp.Memory.ConsecutiveHits >= 2)
            comp.Momentum = CompetitorMomentum.Rising;
        else
            comp.Momentum = CompetitorMomentum.Stable;

        const float driftAmount = 0.02f;
        comp.Personality.ApplyDrift(comp.Momentum, driftAmount);

        int shipped = comp.Memory.TotalProductsShipped;
        if (shipped >= 11)
            comp.CompetitorEra = 3;
        else if (shipped >= 6)
            comp.CompetitorEra = 2;
        else if (shipped >= 3)
            comp.CompetitorEra = 1;
        else
            comp.CompetitorEra = 0;

        if (comp.Momentum == CompetitorMomentum.Crisis)
        {
            if (comp.EmployeeIds != null && comp.EmployeeIds.Count > 0)
            {
                int highestSalary = 0;
                EmployeeId targetId = default;
                bool found = false;

                int empCount = comp.EmployeeIds.Count;
                for (int i = 0; i < empCount; i++)
                {
                    EmployeeId eid = comp.EmployeeIds[i];
                    Employee emp = _employeeSystem.GetEmployee(eid);
                    if (emp == null || !emp.isActive) continue;
                    if (emp.isFounder) continue;
                    if (emp.salary > highestSalary)
                    {
                        highestSalary = emp.salary;
                        targetId = eid;
                        found = true;
                    }
                }

                if (found)
                {
                    _employeeSystem.FireEmployee(targetId);
                    comp.EmployeeIds.Remove(targetId);
                    _logger.Log($"[AIDecisionSystem] {comp.CompanyName} crisis talent drain: fired employee {targetId.Value}.");
                }
            }
        }
    }

    private long GetTaxLiability(Competitor comp)
    {
        long pending = comp.TaxRecord.pendingTaxAmount + comp.TaxRecord.pendingLateFees;
        float taxRate = _taxSystem != null ? _taxSystem.TaxRate : 0.30f;
        long estimated = comp.TaxRecord.profitSinceLastCycle > 0
            ? (long)(comp.TaxRecord.profitSinceLastCycle * taxRate)
            : 0L;
        return pending + estimated;
    }

    private CashHealthTier ClassifyCashHealth(Competitor comp)
    {
        long expenses = comp.Finance.MonthlyExpenses;
        if (expenses <= 0) return CashHealthTier.Flush;
        long taxLiability = GetTaxLiability(comp);
        long availableCash = comp.Finance.Cash - taxLiability;
        if (availableCash < 0) availableCash = 0;
        long runway = availableCash / expenses;
        if (runway < 2) return CashHealthTier.Critical;
        if (runway < 6) return CashHealthTier.Tight;
        if (runway < 12) return CashHealthTier.Stable;
        return CashHealthTier.Flush;
    }

    private AIBudgetAllocation AllocateBudget(Competitor comp, CashHealthTier cashHealth)
    {
        CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
        float salaryRatio = config != null ? config.salaryBudgetRatio : 0.5f;
        float marketingRatio = config != null ? config.marketingBudgetRatio : 0.1f;
        float maintenanceRatio = config != null ? config.maintenanceBudgetRatio : 0.1f;
        float reserveRatio = config != null ? config.reserveBudgetRatio : 0.3f;

        switch (cashHealth)
        {
            case CashHealthTier.Critical:
                salaryRatio = 0f;
                marketingRatio = 0f;
                maintenanceRatio = 0f;
                reserveRatio = 1f;
                break;
            case CashHealthTier.Tight:
            {
                float mktSaved = marketingRatio * 0.7f;
                marketingRatio -= mktSaved;
                reserveRatio += mktSaved;
                break;
            }
            case CashHealthTier.Flush:
                salaryRatio = Math.Min(salaryRatio * 1.3f, 0.7f);
                maintenanceRatio = Math.Min(maintenanceRatio * 1.2f, 0.3f);
                break;
        }

        long taxLiability = GetTaxLiability(comp);
        long available = comp.Finance.Cash - taxLiability;
        if (available < 0) available = 0;
        return new AIBudgetAllocation
        {
            hiringBudget    = (long)(available * salaryRatio),
            marketingBudget = (long)(available * marketingRatio),
            productBudget   = (long)(available * maintenanceRatio),
            reserveBudget   = (long)(available * reserveRatio)
        };
    }

    private void EvaluateHiringDecisions(Competitor comp, int tick, ref AIBudgetAllocation budget, CashHealthTier cashHealth)
    {
        if (_employeeState?.availableCandidates == null || _employeeState.availableCandidates.Count == 0) return;

        if (comp.LastHireTick >= 0 && tick - comp.LastHireTick < TwoWeeksTicks) return;

        int currentCount = _employeeSystem.EmployeeCountForCompany(comp.Id.ToCompanyId());
        int desiredHeadcount = ComputeDesiredHeadcount(comp);
        if (currentCount >= desiredHeadcount) return;

        int maxHires = cashHealth == CashHealthTier.Tight ? 1 : 2;
        int hired = 0;
        int poolCount = _employeeState.availableCandidates.Count;

        float eraHiringMult = 1f + comp.CompetitorEra * 0.15f;

        for (int i = poolCount - 1; i >= 0 && hired < maxHires; i--)
        {
            CandidateData candidate = _employeeState.availableCandidates[i];
            if (candidate == null || candidate.IsPendingReview) continue;

            int expectedSalary = candidate.Salary > 0 ? candidate.Salary : SalaryBand.GetBase(candidate.Role);

            float salaryMult = eraHiringMult;
            if (comp.Personality.RiskTolerance > 0.6f)
                salaryMult *= 1f + (comp.Personality.RiskTolerance - 0.6f) * 0.5f;
            else if (comp.Personality.RiskTolerance < 0.3f)
                salaryMult *= 0.9f;

            if (comp.Personality.PricingAggression > 0.7f && candidate.CurrentAbility > 150)
                salaryMult = System.Math.Max(salaryMult, eraHiringMult * 1.2f);

            int offeredSalary = (int)(expectedSalary * salaryMult);
            long monthlySalaryCost = offeredSalary * 30L;

            if (monthlySalaryCost > budget.hiringBudget) continue;

            var hireCmd = new HireEmployeeCommand
            {
                Tick             = tick,
                CandidateId      = candidate.CandidateId,
                Name             = candidate.Name,
                Gender           = candidate.Gender,
                Age              = candidate.Age,
                Skills           = candidate.Skills,
                HRSkill          = candidate.HRSkill,
                Salary           = offeredSalary,
                Role             = candidate.Role,
                BlindHire        = false,
                Mode             = HiringMode.Manual,
                PotentialAbility = candidate.PotentialAbility,
                CompanyId        = comp.Id.ToCompanyId()
            };

            _employeeSystem.ApplyCommand(hireCmd);

            if (comp.EmployeeIds == null) comp.EmployeeIds = new List<EmployeeId>();
            comp.LastHireTick = tick;
            budget.hiringBudget -= monthlySalaryCost;
            hired++;

            _pendingEvents.Add(new PendingAIEvent
            {
                Type          = PendingEventType.HiredCandidate,
                CompId        = comp.Id,
                CandidateName = candidate.Name,
                Role          = candidate.Role,
                CompanyName   = comp.CompanyName
            });

            _logger.Log($"[AIDecisionSystem] {comp.CompanyName} hired {candidate.Name} ({candidate.Role}) at ${offeredSalary}/mo.");
        }
    }

    private int ComputeDesiredHeadcount(Competitor comp)
    {
        int activeCount = comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0;
        int devCount = comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0;
        int productTotal = activeCount + devCount;
        return Math.Max(3, productTotal * 4);
    }

    private void EvaluateFireDecisions(Competitor comp, int tick, CashHealthTier cashHealth)
    {
        if (comp.EmployeeIds == null || comp.EmployeeIds.Count == 0) return;

        if (cashHealth == CashHealthTier.Critical)
        {
            int highestSalary = 0;
            EmployeeId targetId = default;
            bool found = false;

            int empCount = comp.EmployeeIds.Count;
            for (int i = 0; i < empCount; i++)
            {
                EmployeeId eid = comp.EmployeeIds[i];
                Employee emp = _employeeSystem.GetEmployee(eid);
                if (emp == null || !emp.isActive) continue;
                if (emp.isFounder) continue;
                if (emp.salary > highestSalary)
                {
                    highestSalary = emp.salary;
                    targetId = eid;
                    found = true;
                }
            }

            if (found)
            {
                _employeeSystem.FireEmployee(targetId);
                comp.EmployeeIds.Remove(targetId);
                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} fired employee {targetId.Value} (survival triage).");
            }
            return;
        }

        if (_moraleSystem == null) return;

        int count = comp.EmployeeIds.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            EmployeeId eid = comp.EmployeeIds[i];
            Employee emp = _employeeSystem.GetEmployee(eid);
            if (emp == null || !emp.isActive) continue;
            if (emp.isFounder) continue;

            float morale = _moraleSystem.GetMorale(eid);
            if (morale < 20f)
            {
                _employeeSystem.FireEmployee(eid);
                comp.EmployeeIds.Remove(eid);
                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} fired {emp.name} (low morale: {morale:F0}).");
                break;
            }
        }
    }

    private void EvaluateContractRenewals(Competitor comp, int tick, CashHealthTier cashHealth)
    {
        if (comp.EmployeeIds == null || comp.EmployeeIds.Count == 0) return;

        int empCount = comp.EmployeeIds.Count;
        int topCA = 0;
        EmployeeId topId = default;

        for (int i = 0; i < empCount; i++)
        {
            EmployeeId eid = comp.EmployeeIds[i];
            Employee emp = _employeeSystem.GetEmployee(eid);
            if (emp == null || !emp.isActive || !emp.contractRenewalPending) continue;
            int ca = emp.potentialAbility;
            if (ca > topCA) { topCA = ca; topId = eid; }
        }

        for (int i = empCount - 1; i >= 0; i--)
        {
            EmployeeId eid = comp.EmployeeIds[i];
            Employee emp = _employeeSystem.GetEmployee(eid);
            if (emp == null || !emp.isActive || !emp.contractRenewalPending) continue;

            bool isTopEmployee = eid == topId;

            if (cashHealth <= CashHealthTier.Tight && !isTopEmployee)
            {
                _employeeSystem.ApplyCommand(new DeclineRenewalCommand { Tick = tick, EmployeeId = eid });
                comp.EmployeeIds.RemoveAt(i);
                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} declined renewal for {emp.name} (tight budget).");
                continue;
            }

            if (isTopEmployee || emp.potentialAbility >= 120)
            {
                _employeeSystem.ApplyCommand(new RenewContractCommand { Tick = tick, EmployeeId = eid });
                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} renewed contract for {emp.name}.");
            }
            else
            {
                _employeeSystem.ApplyCommand(new DeclineRenewalCommand { Tick = tick, EmployeeId = eid });
                comp.EmployeeIds.RemoveAt(i);
                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} declined renewal for {emp.name} (low value).");
            }
        }
    }

    private void EvaluateTeamDecisions(Competitor comp, int tick)
    {
        if (_teamSystem == null || comp.EmployeeIds == null || comp.EmployeeIds.Count == 0) return;

        CompanyId companyId = comp.Id.ToCompanyId();

        int empCount = comp.EmployeeIds.Count;
        for (int i = 0; i < empCount; i++)
        {
            EmployeeId eid = comp.EmployeeIds[i];
            Employee emp = _employeeSystem.GetEmployee(eid);
            if (emp == null || !emp.isActive) continue;
            if (_teamSystem.GetEmployeeTeam(eid) != null) continue;

            var freeTeams = _teamSystem.GetFreeTeamsByTypeForCompany(TeamTypeMapping.ToTeamType(RoleToProductTeamRole(emp.role)), companyId);
            if (freeTeams != null && freeTeams.Count > 0)
            {
                _teamSystem.AssignEmployeeToTeam(eid, freeTeams[0]);
            }
        }
    }

    private static ProductTeamRole RoleToProductTeamRole(EmployeeRole role)
    {
        switch (role)
        {
            case EmployeeRole.Designer:      return ProductTeamRole.Design;
            case EmployeeRole.QAEngineer:    return ProductTeamRole.QA;
            case EmployeeRole.SoundEngineer: return ProductTeamRole.SFX;
            case EmployeeRole.VFXArtist:     return ProductTeamRole.VFX;
            default:                         return ProductTeamRole.Programming;
        }
    }

    public void EvaluateNewProductDecision(Competitor comp, int tick, AIBudgetAllocation budget)
    {
        CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
        if (config == null) return;

        int currentInDev = comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0;
        int maxProducts = config.maxSimultaneousProducts;
        if (comp.Momentum == CompetitorMomentum.Rising && comp.CompetitorEra >= 2)
            maxProducts += 1;
        if (comp.Momentum == CompetitorMomentum.Crisis)
            maxProducts = System.Math.Max(1, maxProducts - 1);
        if (currentInDev >= maxProducts) return;

        int activeCount = comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0;
        if (comp.LastProductStartedTick >= 0)
        {
            int cooldownTicks = (int)(config.releaseIntervalMonthsMin * TimeState.TicksPerDay * 30);
            if (activeCount == 0 && currentInDev == 0)
                cooldownTicks /= 2;
            if (tick - comp.LastProductStartedTick < cooldownTicks) return;
        }

        long cashThreshold = (long)config.expansionCashThreshold;
        if (activeCount == 0 && currentInDev == 0)
        {
            cashThreshold /= 4;
            if (cashThreshold < 50000L) cashThreshold = 50000L;
        }

        long taxAdjustedCash = comp.Finance.Cash - GetTaxLiability(comp);
        if (taxAdjustedCash < 0) taxAdjustedCash = 0;
        if (taxAdjustedCash < cashThreshold) return;

        ProductNiche bestNiche;
        if (!TryFindBestNicheForCompetitor(comp, out bestNiche)) return;

        Product newProduct = GenerateCompetitorProduct(comp, bestNiche, tick);
        if (newProduct == null) return;

        comp.LastProductStartedTick = tick;
        _logger.Log($"[AIDecisionSystem] {comp.CompanyName} starts developing product in niche {bestNiche}.");
    }

    private void EvaluateSunsetDecision(Competitor comp, int tick, CashHealthTier cashHealth)
    {
        if (comp.ActiveProductIds == null || comp.ActiveProductIds.Count == 0) return;

        _scratchProductIds.Clear();
        int count = comp.ActiveProductIds.Count;
        for (int i = 0; i < count; i++)
            _scratchProductIds.Add(comp.ActiveProductIds[i]);

        CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
        float sunsetMonths = config != null ? config.sunsetMonths : 6f;
        int sunsetTicks = (int)(sunsetMonths * TimeState.TicksPerDay * 30);

        int divisor = cashHealth == CashHealthTier.Critical ? 3 : 5;

        for (int i = 0; i < _scratchProductIds.Count; i++)
        {
            ProductId pid = _scratchProductIds[i];
            Product product = GetShippedProduct(pid);
            if (product == null || !product.IsOnMarket) continue;

            bool isLastProduct = comp.ActiveProductIds.Count <= 1 &&
                (comp.InDevelopmentProductIds == null || comp.InDevelopmentProductIds.Count == 0);
            if (isLastProduct) continue;

            int mercyTicks = 6 * TimeState.TicksPerDay * 30;
            if (product.MonthlyRevenue < 500L && product.TicksSinceShip > mercyTicks)
            {
                if (product.Category.IsCriticalCategory() && _productState.IsLastOnMarketInCategory(pid))
                {
                    _logger.Log($"[AIDecisionSystem] {comp.CompanyName} blocked from sunsetting last {product.Category} product {pid.Value}.");
                    continue;
                }
                ProcessSunset(comp, pid);
                continue;
            }

            long sunsetRevenueThreshold = comp.Finance.MonthlyExpenses > 0
                ? comp.Finance.MonthlyExpenses / (comp.ActiveProductIds != null && comp.ActiveProductIds.Count > 0
                    ? comp.ActiveProductIds.Count * divisor : divisor)
                : 100L;
            if (sunsetRevenueThreshold < 100L) sunsetRevenueThreshold = 100L;

            if (product.MonthlyRevenue < sunsetRevenueThreshold && product.TicksSinceShip > sunsetTicks)
            {
                if (product.Category.IsCriticalCategory() && _productState.IsLastOnMarketInCategory(pid))
                {
                    _logger.Log($"[AIDecisionSystem] {comp.CompanyName} blocked from sunsetting last {product.Category} product {pid.Value}.");
                    continue;
                }
                ProcessSunset(comp, pid);
            }
        }
    }

    private void EvaluateProductPerformance(Competitor comp, int tick, CashHealthTier cashHealth)
    {
        if (comp.ActiveProductIds == null || comp.ActiveProductIds.Count == 0) return;
        if (_productSystem == null) return;

        _scratchProductIds.Clear();
        int count = comp.ActiveProductIds.Count;
        for (int i = 0; i < count; i++)
            _scratchProductIds.Add(comp.ActiveProductIds[i]);

        int scratchCount = _scratchProductIds.Count;
        for (int i = 0; i < scratchCount; i++)
        {
            ProductId pid = _scratchProductIds[i];
            Product product = GetShippedProduct(pid);
            if (product == null || !product.IsOnMarket) continue;

            bool revenueFalling = product.MonthlyRevenue < product.PreviousMonthlyRevenue;
            bool isFailing = revenueFalling && product.MonthlyRevenue < 2000L;
            bool isDeclining = revenueFalling && !isFailing;

            if (isFailing && cashHealth >= CashHealthTier.Tight)
            {
                long boostBudget = comp.Finance.MonthlyRevenue > 0
                    ? comp.Finance.MonthlyRevenue / System.Math.Max(1, scratchCount)
                    : 5000L;
                long currentMarketing = product.MarketingBudgetMonthly;
                if (boostBudget > currentMarketing)
                    _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = pid, BudgetType = ProductBudgetType.Marketing, MonthlyAllocation = boostBudget });

                if (!product.IsOnSale)
                {
                    bool firstSale = product.TotalSalesTriggered == 0;
                    if (firstSale || product.TicksSinceLastSale >= ProductSystem.SaleEventCooldownTicks)
                    {
                        product.IsOnSale = true;
                        product.SaleTicksRemaining = ProductSystem.SaleEventDurationTicks;
                        product.TotalSalesTriggered++;
                        product.PopularityScore = System.Math.Min(100f, product.PopularityScore + 8f);
                        _logger.Log($"[AIDecisionSystem] {comp.CompanyName} triggered emergency sale on failing product {pid.Value}: boost marketing ${boostBudget}/mo.");
                    }
                    else
                    {
                        _logger.Log($"[AIDecisionSystem] {comp.CompanyName} intervening on failing product {pid.Value}: boost marketing ${boostBudget}/mo (sale on cooldown).");
                    }
                }
            }
            else if (isDeclining && cashHealth >= CashHealthTier.Stable)
            {
                long boostBudget = comp.Finance.MonthlyRevenue > 0
                    ? (long)(comp.Finance.MonthlyRevenue * 0.05f / System.Math.Max(1, scratchCount))
                    : 2000L;
                long currentMarketing = product.MarketingBudgetMonthly;
                if (boostBudget > currentMarketing)
                    _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = pid, BudgetType = ProductBudgetType.Marketing, MonthlyAllocation = boostBudget });

                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} boosting marketing for declining product {pid.Value}: ${boostBudget}/mo.");
            }
        }
    }

    private void EvaluateMaintenanceDecisions(Competitor comp, int tick, CashHealthTier cashHealth)
    {
        if (comp.ActiveProductIds == null || comp.ActiveProductIds.Count == 0) return;
        if (_productSystem == null) return;

        CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
        float maintenanceBudgetRatio = config != null ? config.maintenanceBudgetRatio : 0.1f;

        _scratchProductIds.Clear();
        int activeCount = comp.ActiveProductIds.Count;
        for (int i = 0; i < activeCount; i++)
            _scratchProductIds.Add(comp.ActiveProductIds[i]);

        int scratchCount = _scratchProductIds.Count;
        for (int i = 0; i < scratchCount; i++)
        {
            Product product = GetShippedProduct(_scratchProductIds[i]);
            if (product == null || !product.IsOnMarket) continue;

            long currentBudget = product.MaintenanceBudgetMonthly;

            bool isDecline = product.LifecycleStage == ProductLifecycleStage.Decline;

            if (cashHealth == CashHealthTier.Critical && isDecline)
            {
                if (currentBudget != 0)
                    _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = _scratchProductIds[i], BudgetType = ProductBudgetType.Maintenance, MonthlyAllocation = 0 });
                continue;
            }

            long desiredBudget = (long)(comp.Finance.MonthlyRevenue * maintenanceBudgetRatio / System.Math.Max(1, scratchCount));

            if (isDecline) desiredBudget = desiredBudget * 3 / 4;
            if (cashHealth == CashHealthTier.Tight || cashHealth == CashHealthTier.Critical) desiredBudget /= 2;

            if (desiredBudget != currentBudget)
                _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = _scratchProductIds[i], BudgetType = ProductBudgetType.Maintenance, MonthlyAllocation = desiredBudget });
        }
    }

    private void ProcessSunset(Competitor comp, ProductId productId) {
        Product product = GetShippedProduct(productId);
        if (product == null) return;
        RecordProductOutcome(comp, product);
        comp.Memory ??= CompetitorMemory.CreateNew();
        comp.Memory.TotalProductsSunset++;
        product.IsOnMarket = false;
        comp.ActiveProductIds.Remove(productId);
        _productState.shippedProducts.Remove(productId);
        _productState.archivedProducts[productId] = product;
        _pendingEvents.Add(new PendingAIEvent {
            Type = PendingEventType.ProductSunset,
            CompId = comp.Id,
            ProductId = productId,
            CompanyName = comp.CompanyName
        });
        _logger.Log($"[AIDecisionSystem] {comp.CompanyName} sunset product {productId.Value}.");
    }

    private void EvaluateMarketingDecisions(Competitor comp, int tick, ref AIBudgetAllocation budget, CashHealthTier cashHealth)
    {
        if (_productSystem == null) return;
        if (comp.ActiveProductIds == null || comp.ActiveProductIds.Count == 0) return;

        bool alwaysMarket = comp.Personality.BrandInvestment > 0.6f;
        bool onlyNewest = comp.Personality.BrandInvestment < 0.3f;

        int productCount = comp.ActiveProductIds.Count;
        int newestIdx = productCount - 1;

        for (int i = 0; i < productCount; i++)
        {
            ProductId pid = comp.ActiveProductIds[i];
            Product product = GetShippedProduct(pid);
            if (product == null || !product.IsOnMarket) continue;

            bool shouldMarket = alwaysMarket || (!onlyNewest) || i == newestIdx;
            long currentBudget = product.MarketingBudgetMonthly;

            if (cashHealth == CashHealthTier.Critical || !shouldMarket)
            {
                if (currentBudget != 0)
                {
                    _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = pid, BudgetType = ProductBudgetType.Marketing, MonthlyAllocation = 0 });
                    _logger.Log($"[AIDecisionSystem] {comp.CompanyName} cut marketing budget for product {pid.Value}.");
                }
                continue;
            }

            if (budget.marketingBudget <= 0)
            {
                if (currentBudget != 0)
                    _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = pid, BudgetType = ProductBudgetType.Marketing, MonthlyAllocation = 0 });
                continue;
            }

            long desiredBudget = budget.marketingBudget / System.Math.Max(1, productCount);
            if (cashHealth == CashHealthTier.Tight) desiredBudget /= 2;

            if (desiredBudget != currentBudget)
            {
                _productSystem.ApplyCommand(new SetProductBudgetCommand { Tick = tick, ProductId = pid, BudgetType = ProductBudgetType.Marketing, MonthlyAllocation = desiredBudget });
                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} set marketing budget ${desiredBudget}/month for product {pid.Value}.");
            }
        }
    }

    private void EvaluatePricingDecision(Competitor comp, int tick)
    {
        if (comp.LastPricingReviewTick >= 0)
        {
            int quarterTicks = TimeState.TicksPerDay * 90;
            if (tick - comp.LastPricingReviewTick < quarterTicks) return;
        }
        comp.LastPricingReviewTick = tick;

        if (_productSystem == null) return;
        if (comp.ActiveProductIds == null || comp.ActiveProductIds.Count == 0) return;

        int productCount = comp.ActiveProductIds.Count;
        for (int i = 0; i < productCount; i++)
        {
            ProductId pid = comp.ActiveProductIds[i];
            Product product = GetShippedProduct(pid);
            if (product == null || !product.IsOnMarket) continue;
            if (product.IsOnSale) continue;

            bool usersFalling = product.ActiveUserCount < product.PreviousActiveUsers;
            bool recentUpdate = product.TicksSinceLastUpdate < TimeState.TicksPerDay * 30;

            bool shouldSale = (recentUpdate && usersFalling)
                || (usersFalling && product.ActiveUserCount < (int)(product.PreviousActiveUsers * 0.8f));

            if (!shouldSale) continue;

            bool firstSale = product.TotalSalesTriggered == 0;
            if (!firstSale && product.TicksSinceLastSale < ProductSystem.SaleEventCooldownTicks) continue;

            product.IsOnSale = true;
            product.SaleTicksRemaining = ProductSystem.SaleEventDurationTicks;
            product.TotalSalesTriggered++;
            product.PopularityScore = System.Math.Min(100f, product.PopularityScore + 8f);
            _logger.Log($"[AIDecisionSystem] {comp.CompanyName} triggered sale on product {pid.Value} (recentUpdate={recentUpdate}, usersFalling={usersFalling}).");
        }
    }

    public void EvaluateReleaseDateReaction(Product playerProduct, bool isNewAnnouncement)
    {
        int currentTick = _lastKnownTick;
        int playerLayer = playerProduct.Category.GetLayer();

        foreach (var kvp in _competitorState.competitors)
        {
            Competitor comp = kvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;
            if (comp.InDevelopmentProductIds == null || comp.InDevelopmentProductIds.Count == 0) continue;

            CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
            if (config == null || config.dateShiftReactivity <= 0f) continue;

            float roll = _rng.NextFloat01();
            if (roll > config.dateShiftReactivity) continue;

            int devCount = comp.InDevelopmentProductIds.Count;
            for (int i = 0; i < devCount; i++)
            {
                ProductId pid = comp.InDevelopmentProductIds[i];
                Product competitorProduct = GetDevProduct(pid);
                if (competitorProduct == null) continue;

                if (competitorProduct.Category.GetLayer() != playerLayer) continue;

                bool overlap = competitorProduct.Niche == playerProduct.Niche
                    || competitorProduct.Category == playerProduct.Category;
                if (!overlap) continue;

                int ticksRemaining = competitorProduct.TargetReleaseTick - currentTick;
                if (ticksRemaining <= 0) continue;

                int maxShift = (int)(ticksRemaining * config.maxDateShiftFraction);
                if (maxShift <= 0) continue;

                int halfShift = maxShift / 2;
                int shiftAmount = halfShift < maxShift ? _rng.Range(halfShift, maxShift) : maxShift;
                if (shiftAmount <= 0) continue;

                int newTarget;
                string shiftDirection;
                if (config.prefersRush)
                {
                    newTarget = competitorProduct.TargetReleaseTick - shiftAmount;
                    int minAllowed = currentTick + TimeState.TicksPerDay;
                    if (newTarget < minAllowed) newTarget = minAllowed;
                    shiftDirection = "rush";

                    float compressionRatio = (float)shiftAmount / ticksRemaining;
                    if (competitorProduct.Phases != null && competitorProduct.Phases.Length > 0)
                        competitorProduct.Phases[0].phaseQuality *= (1f - compressionRatio * 0.3f);
                }
                else
                {
                    newTarget = competitorProduct.TargetReleaseTick + shiftAmount;
                    int maxOriginalDevTime = competitorProduct.CreationTick > 0
                        ? 2 * (competitorProduct.TargetReleaseTick - competitorProduct.CreationTick)
                        : competitorProduct.TargetReleaseTick + shiftAmount;
                    int maxAllowed = competitorProduct.CreationTick + maxOriginalDevTime;
                    if (newTarget > maxAllowed) newTarget = maxAllowed;
                    shiftDirection = "delay";
                }

                int shiftDays = shiftAmount / TimeState.TicksPerDay;
                competitorProduct.TargetReleaseTick = newTarget;
                competitorProduct.DateShiftCount++;

                _logger.Log($"[AIDecisionSystem] {comp.CompanyName} shifted {competitorProduct.ProductName} by {shiftDays} days ({shiftDirection}) in response to player.");
                break;
            }
        }
    }

    private bool TryFindBestNicheForCompetitor(Competitor comp, out ProductNiche bestNiche)
    {
        bestNiche = default;
        if (_marketState == null) return false;

        bool found = false;
        float bestScore = 0f;
        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;

        var niches = _marketState.nicheDemand;
        foreach (var kvp in niches)
        {
            ProductNiche niche = kvp.Key;
            float demand = kvp.Value;
            float supply = CountCompetitorProductsInNiche(niche);
            float saturation = supply > 0f ? Clamp(supply / (demand + 1f), 0f, 1f) : 0f;
            float categoryFamiliarity = GetCategoryFamiliarity(comp, niche);

            float opportunity = demand * (1f - saturation) * categoryFamiliarity;

            float innovationFactor = comp.Personality.InnovationBias > 0.5f
                ? 1f + (comp.Personality.InnovationBias - 0.5f) * currentGen * 0.1f
                : 1f - (0.5f - comp.Personality.InnovationBias) * 0.2f;

            float riskFactor = comp.Personality.RiskTolerance > 0.5f
                ? 1f + (comp.Personality.RiskTolerance - 0.5f) * (1f - saturation) * 0.4f
                : Clamp(1f - (0.5f - comp.Personality.RiskTolerance) * saturation, 0.1f, 1f);

            float playerShareBonus = GetPlayerAwarenessModifier(comp, niche);

            float score = opportunity * innovationFactor * riskFactor * playerShareBonus;
            float noise = 0.8f + _rng.NextFloat01() * 0.4f;
            score *= noise;

            float historyMult = GetNicheHistoryMultiplier(comp, niche);
            score *= historyMult;

            if (score > bestScore)
            {
                bestScore = score;
                bestNiche = niche;
                found = true;
            }
        }

        if (_marketState.categoryDemand != null)
        {
            foreach (var kvp in _marketState.categoryDemand)
            {
                ProductCategory cat = kvp.Key;
                float demand = kvp.Value;

                if (!IsTemplateForCategoryTier2(cat)) continue;

                float supply = CountCompetitorProductsInCategory(cat);
                float saturation = supply > 0f ? Clamp(supply / (demand + 1f), 0f, 1f) : 0f;
                float categoryFam = GetCategoryFamiliarityDirect(comp, cat);

                float opportunity = demand * (1f - saturation) * categoryFam;
                float innovationFactor = comp.Personality.InnovationBias > 0.5f
                    ? 1f + (comp.Personality.InnovationBias - 0.5f) * currentGen * 0.1f
                    : 1f - (0.5f - comp.Personality.InnovationBias) * 0.2f;
                float riskFactor = comp.Personality.RiskTolerance > 0.5f
                    ? 1f + (comp.Personality.RiskTolerance - 0.5f) * (1f - saturation) * 0.4f
                    : Clamp(1f - (0.5f - comp.Personality.RiskTolerance) * saturation, 0.1f, 1f);

                float score = opportunity * innovationFactor * riskFactor;
                float noise = 0.8f + _rng.NextFloat01() * 0.4f;
                score *= noise;

                float catHistoryMult = GetCategoryHistoryMultiplier(comp, cat);
                score *= catHistoryMult;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestNiche = ProductNiche.None;
                    found = true;
                }
            }
        }

        return found;
    }

    private float GetPlayerAwarenessModifier(Competitor comp, ProductNiche niche)
    {
        if (_marketState?.currentMarketShares == null) return 1f;
        if (!_marketState.currentMarketShares.TryGetValue(niche, out var entries) || entries == null) return 1f;

        float playerShare = 0f;
        int entryCount = entries.Count;
        for (int i = 0; i < entryCount; i++)
        {
            var entry = entries[i];
            if (!entry.OwnerId.HasValue)
            {
                playerShare += entry.MarketSharePercent;
            }
        }

        if (playerShare > 0.3f)
        {
            if (comp.Personality.RiskTolerance > 0.6f) return 1.3f;
            if (comp.Personality.RiskTolerance < 0.3f) return 0.5f;
        }
        return 1f;
    }

    private void CheckCompetitorProductDeadlines(Competitor comp, int tick)
    {
        if (_competitorSystem == null) return;
        int count = comp.InDevelopmentProductIds.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            ProductId productId = comp.InDevelopmentProductIds[i];
            if (!_productState.developmentProducts.TryGetValue(productId, out _)) continue;
            if (tick > _productState.developmentProducts[productId].TargetReleaseTick)
                _competitorSystem.InstantShipCompetitorProduct(comp, productId, tick);
        }
    }

    private Product GenerateCompetitorProduct(Competitor comp, ProductNiche niche, int tick)
    {
        CompetitorArchetypeConfig config = GetArchetypeConfig(comp.Archetype);
        if (config == null) return null;

        var productId = new ProductId(_productState.nextProductId++);

        int devMonths = (int)(_rng.NextFloat01()
            * (config.releaseIntervalMonthsMax - config.releaseIntervalMonthsMin)
            + config.releaseIntervalMonthsMin);
        if (devMonths < 1) devMonths = 1;
        int devTicks = devMonths * TimeState.TicksPerDay * 30;
        int targetReleaseTick = tick + devTicks;

        ProductCategory resolvedCategory;
        ProductNiche resolvedNiche;

        if (niche == ProductNiche.None)
        {
            resolvedNiche = ProductNiche.None;
            resolvedCategory = comp.Specializations != null && comp.Specializations.Length > 0
                ? comp.Specializations[0] : ProductCategory.VideoGame;
        }
        else
        {
            resolvedCategory = NicheToCategory(niche);
            resolvedNiche = niche;

            if (_templateLookup != null)
            {
                foreach (var kvp in _templateLookup)
                {
                    var tmpl = kvp.Value;
                    if (tmpl.category == resolvedCategory && !tmpl.HasNiches)
                    {
                        resolvedNiche = ProductNiche.None;
                        break;
                    }
                }
            }
        }

        string templateId = resolvedNiche != ProductNiche.None
            ? FindTemplateForNiche(resolvedNiche)
            : FindTemplateForCategory(resolvedCategory);

        ProductId[] selectedToolIds = SelectToolsForCompetitorDesireDriven(comp, resolvedCategory, resolvedNiche, tick);
        ProductId[] selectedPlatformIds = resolvedCategory.IsApplication()
            ? SelectPlatformsForCompetitorDesireDriven(comp, resolvedCategory, resolvedNiche, tick)
            : null;

        string[] selectedFeatureIds = SelectFeaturesForCompetitor(comp, niche, templateId, selectedToolIds, selectedPlatformIds);

        ProductPhaseRuntime[] phases;
        if (_productSystem != null && templateId != null)
        {
            phases = _productSystem.BuildPhasesForTemplate(templateId, selectedFeatureIds, 1f);
        }
        else
        {
            phases = new ProductPhaseRuntime[]
            {
                new ProductPhaseRuntime
                {
                    phaseType = ProductPhaseType.Programming,
                    phaseQuality = 0f,
                    totalWorkRequired = 1000f,
                    workCompleted = 0f,
                    isComplete = false,
                    isUnlocked = true
                }
            };
        }

        float launchPrice = ComputeLaunchPrice(comp, resolvedNiche);

        var product = new Product
        {
            Id = productId,
            ProductName = GenerateProductName(comp, niche, resolvedCategory),
            OwnerCompanyId = comp.Id.ToCompanyId(),
            IsInDevelopment = true,
            IsShipped = false,
            IsOnMarket = false,
            TargetReleaseTick = targetReleaseTick,
            OriginalReleaseTick = targetReleaseTick,
            HasAnnouncedReleaseDate = false,
            CreationTick = tick,
            TemplateId = templateId,
            Phases = phases,
            TeamAssignments = new Dictionary<ProductTeamRole, TeamId>(),
            ActiveUserCount = 0,
            OverallQuality = 0f,
            PopularityScore = 0f,
            MonthlyRevenue = 0,
            LifecycleStage = ProductLifecycleStage.PreLaunch,
            Niche = resolvedNiche,
            Category = resolvedCategory,
            SelectedFeatureIds = selectedFeatureIds,
            TotalDevelopmentTicks = 0,
            DroppedFeatureIds = new List<string>(),
            SequelIds = new List<ProductId>(),
            PriceOverride = launchPrice,
            RequiredToolIds = selectedToolIds,
            TargetPlatformIds = selectedPlatformIds
        };

        if (resolvedCategory.IsTool())
        {
            float pricingAggression = comp.Personality.PricingAggression;
            if (comp.Archetype == CompetitorArchetype.ToolMaker)
            {
                product.DistributionModel = ToolDistributionModel.Licensed;
                product.PlayerLicensingRate = 0.05f + pricingAggression * 0.15f;
            }
            else if (pricingAggression < 0.3f)
            {
                product.DistributionModel = ToolDistributionModel.OpenSource;
                product.PlayerLicensingRate = 0f;
            }
            else
            {
                product.DistributionModel = ToolDistributionModel.Licensed;
                product.PlayerLicensingRate = 0.05f + pricingAggression * 0.10f;
            }

            if (product.IsSubscriptionBased && product.DistributionModel == ToolDistributionModel.Licensed)
                product.MonthlySubscriptionPrice = System.Math.Max(5f, product.PriceOverride * 0.3f);
        }

        _productState.developmentProducts[productId] = product;
        comp.InDevelopmentProductIds.Add(productId);

        long startupCost = Clamp((long)_rng.Range(5000, 20000), 0L, comp.Finance.Cash);
        comp.Finance.Cash -= startupCost;

        return product;
    }

    private float ComputeLaunchPrice(Competitor comp, ProductNiche niche)
    {
        float basePrice = 20f;
        if (_marketSystem != null && niche != ProductNiche.None)
        {
            float demand = _marketSystem.GetNicheDemand(niche);
            basePrice = 10f + demand * 0.5f;
        }

        float pricingAggression = comp.Personality.PricingAggression;
        if (pricingAggression > 0.7f)
            basePrice *= 1f + (pricingAggression - 0.7f) * (2f / 3f);
        else if (pricingAggression < 0.3f)
            basePrice *= 1f - (0.3f - pricingAggression) * (2f / 3f);

        return basePrice;
    }

    private string[] SelectFeaturesForCompetitor(Competitor comp, ProductNiche niche, string templateId, ProductId[] toolIds, ProductId[] platformIds)
    {
        if (_marketSystem == null) return null;

        int baseMin = 2 + (int)(comp.Personality.InnovationBias * 2f);
        int baseMax = 4 + (int)(comp.Personality.InnovationBias * 3f);
        int targetCount = _rng.Range(baseMin, baseMax + 1);

        var candidates = new List<ProductFeatureDefinition>(16);
        if (templateId != null && _templateLookup != null && _templateLookup.TryGetValue(templateId, out var resolvedTemplate) && resolvedTemplate.availableFeatures != null)
        {
            int len = resolvedTemplate.availableFeatures.Length;
            for (int i = 0; i < len; i++)
            {
                var feat = resolvedTemplate.availableFeatures[i];
                if (feat == null || string.IsNullOrEmpty(feat.featureId)) continue;
                candidates.Add(feat);
            }
        }

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (!IsFeatureGateSatisfied(candidates[i], toolIds, platformIds))
                candidates.RemoveAt(i);
        }

        int candidateCount = candidates.Count;
        if (candidateCount == 0) return null;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        var scores = new float[candidateCount];
        for (int i = 0; i < candidateCount; i++)
        {
            var feat = candidates[i];
            float baseDemand = _marketSystem.GetNicheDemand(niche);
            float nicheAffinity = _marketSystem.GetFeatureCategoryAffinity(niche, feat.featureCategory);
            float innovationWeight = comp.Personality.InnovationBias;
            float noise = 0.7f + _rng.NextFloat01() * 0.6f;
            scores[i] = baseDemand * nicheAffinity * innovationWeight * noise;

            float adoptionRate = _marketSystem.GetFeatureAdoptionRate(feat.featureId, niche, "");
            FeatureDemandStage demandStage = FeatureDemandHelper.GetDemandStage(currentGen, feat.demandIntroductionGen, feat.demandMaturitySpeed, feat.isFoundational, adoptionRate);
            float demandMult;
            switch (demandStage)
            {
                case FeatureDemandStage.Emerging:
                    demandMult = comp.Personality.InnovationBias > 0.7f ? 1.3f : 0.7f;
                    break;
                case FeatureDemandStage.Growing:
                    demandMult = 1.2f;
                    break;
                case FeatureDemandStage.Standard:
                    demandMult = 1.0f;
                    break;
                case FeatureDemandStage.Declining:
                    demandMult = 0.4f;
                    break;
                case FeatureDemandStage.Legacy:
                    demandMult = 0.1f;
                    break;
                default:
                    demandMult = 1.0f;
                    break;
            }
            scores[i] *= demandMult;

            float reviewBoost = GetReviewFeedbackBoost(comp, niche, feat);
            scores[i] *= (1f + reviewBoost);
        }

        var indices = new int[candidateCount];
        for (int i = 0; i < candidateCount; i++) indices[i] = i;
        for (int i = 1; i < candidateCount; i++)
        {
            int key = indices[i];
            float keyScore = scores[key];
            int j = i - 1;
            while (j >= 0 && scores[indices[j]] < keyScore)
            {
                indices[j + 1] = indices[j];
                j--;
            }
            indices[j + 1] = key;
        }

        int pickCount = Math.Min(targetCount, candidateCount);
        var result = new string[pickCount];
        for (int i = 0; i < pickCount; i++)
            result[i] = candidates[indices[i]].featureId;

        return result;
    }

    private bool IsFeatureGateSatisfied(ProductFeatureDefinition feat, ProductId[] toolIds, ProductId[] platformIds)
    {
        if (!string.IsNullOrEmpty(feat.requiresToolFeature))
        {
            bool found = false;
            if (toolIds != null)
            {
                int toolCount = toolIds.Length;
                for (int t = 0; t < toolCount && !found; t++)
                {
                    if (!_productState.shippedProducts.TryGetValue(toolIds[t], out var tool)) continue;
                    if (tool.SelectedFeatureIds == null) continue;
                    int fc = tool.SelectedFeatureIds.Length;
                    for (int f = 0; f < fc; f++)
                    {
                        if (tool.SelectedFeatureIds[f] == feat.requiresToolFeature) { found = true; break; }
                    }
                }
            }
            if (!found) return false;
        }

        if (!string.IsNullOrEmpty(feat.requiresPlatformFeature))
        {
            bool found = false;
            if (platformIds != null)
            {
                int platCount = platformIds.Length;
                for (int p = 0; p < platCount && !found; p++)
                {
                    if (!_productState.shippedProducts.TryGetValue(platformIds[p], out var platform)) continue;
                    if (platform.SelectedFeatureIds == null) continue;
                    int fc = platform.SelectedFeatureIds.Length;
                    for (int f = 0; f < fc; f++)
                    {
                        if (platform.SelectedFeatureIds[f] == feat.requiresPlatformFeature) { found = true; break; }
                    }
                }
            }
            if (!found) return false;
        }

        return true;
    }

    private float ScoreToolByDesiredFeatures(Competitor comp, Product tool, ProductNiche targetNiche, int currentGen)
    {
        float toolScore = 0f;
        if (_marketSystem == null || _templateLookup == null) return toolScore;

        float personalityBase = comp.Personality.InnovationBias * 0.5f + 0.5f;
        float pricingAggression = comp.Personality.PricingAggression > 0f ? comp.Personality.PricingAggression : 0.5f;

        foreach (var kvp in _templateLookup)
        {
            var tmpl = kvp.Value;
            if (tmpl.availableFeatures == null) continue;
            int len = tmpl.availableFeatures.Length;
            for (int i = 0; i < len; i++)
            {
                var feat = tmpl.availableFeatures[i];
                if (feat == null || string.IsNullOrEmpty(feat.requiresToolFeature)) continue;

                float nicheDemandAffinity = _marketSystem.GetFeatureCategoryAffinity(targetNiche, feat.featureCategory);
                float generationMod = feat.availableFromGeneration <= currentGen ? 1.0f : 0.3f;
                float trendNoise = 0.7f + _rng.NextFloat01() * 0.6f;
                float weight = personalityBase * nicheDemandAffinity * generationMod * trendNoise;

                bool toolHasFeature = false;
                float upstreamQuality = 0f;
                if (tool.SelectedFeatureIds != null)
                {
                    int fc = tool.SelectedFeatureIds.Length;
                    for (int f = 0; f < fc; f++)
                    {
                        if (tool.SelectedFeatureIds[f] == feat.requiresToolFeature)
                        {
                            toolHasFeature = true;
                            upstreamQuality = tool.OverallQuality;
                            break;
                        }
                    }
                }

                if (toolHasFeature)
                    toolScore += weight * (upstreamQuality / 100f);
                else
                    toolScore -= weight * 0.5f;
            }
        }

        toolScore += tool.OverallQuality * 0.3f;

        float licensingCost = 0f;
        if (!tool.IsCompetitorProduct && tool.DistributionModel == ToolDistributionModel.Licensed)
            licensingCost = tool.PlayerLicensingRate;
        else if (tool.IsCompetitorProduct && tool.OwnerCompanyId != comp.Id.ToCompanyId() && tool.DistributionModel == ToolDistributionModel.Licensed)
            licensingCost = tool.PlayerLicensingRate;

        toolScore -= licensingCost * 100f * pricingAggression;
        return toolScore;
    }

    private ProductId[] SelectToolsForCompetitorDesireDriven(Competitor comp, ProductCategory productCategory, ProductNiche targetNiche, int tick)
    {
        if (_templateLookup == null || _productState == null) return null;

        ProductTemplateDefinition template = null;
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Value.category == productCategory) { template = kvp.Value; break; }
        }

        if (template == null || template.requiredToolTypes == null || template.requiredToolTypes.Length == 0)
            return null;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        float pricingAggression = comp.Personality.PricingAggression > 0f ? comp.Personality.PricingAggression : 0.5f;
        var selectedIds = new List<ProductId>(template.requiredToolTypes.Length);

        for (int ti = 0; ti < template.requiredToolTypes.Length; ti++)
        {
            ProductCategory requiredCategory = template.requiredToolTypes[ti];

            var candidates = new List<ProductId>(8);
            foreach (var kvp in _productState.shippedProducts)
            {
                var tool = kvp.Value;
                if (!tool.IsOnMarket) continue;
                if (tool.Category != requiredCategory) continue;
                if (!tool.IsCompetitorProduct && tool.DistributionModel == ToolDistributionModel.Proprietary) continue;
                candidates.Add(kvp.Key);
            }

            if (candidates.Count == 0) continue;

            float bestScore = float.MinValue;
            ProductId bestId = candidates[0];

            for (int ci = 0; ci < candidates.Count; ci++)
            {
                if (!_productState.shippedProducts.TryGetValue(candidates[ci], out var tool)) continue;
                float ownershipBonus;
                if (tool.IsCompetitorProduct && tool.OwnerCompanyId == comp.Id.ToCompanyId())
                    ownershipBonus = 1.1f;
                else if (!tool.IsCompetitorProduct && tool.DistributionModel == ToolDistributionModel.OpenSource)
                    ownershipBonus = 1.05f;
                else
                    ownershipBonus = 0.95f;

                float desireScore = ScoreToolByDesiredFeatures(comp, tool, targetNiche, currentGen);
                float score = desireScore * ownershipBonus;
                if (score > bestScore) { bestScore = score; bestId = candidates[ci]; }
            }

            selectedIds.Add(bestId);
        }

        return selectedIds.Count > 0 ? selectedIds.ToArray() : null;
    }

    private ProductId[] SelectPlatformsForCompetitorDesireDriven(Competitor comp, ProductCategory productCategory, ProductNiche targetNiche, int tick)
    {
        if (_templateLookup == null || _productState == null) return null;
        if (!productCategory.IsApplication()) return null;

        ProductTemplateDefinition template = null;
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Value.category == productCategory) { template = kvp.Value; break; }
        }

        if (template == null || template.validTargetPlatforms == null || template.validTargetPlatforms.Length == 0)
            return null;

        int currentGen = _generationSystem != null ? _generationSystem.GetCurrentGeneration() : 1;
        var selectedIds = new List<ProductId>(template.validTargetPlatforms.Length);

        for (int ti = 0; ti < template.validTargetPlatforms.Length; ti++)
        {
            ProductCategory requiredCategory = template.validTargetPlatforms[ti];

            var candidates = new List<ProductId>(8);
            foreach (var kvp in _productState.shippedProducts)
            {
                var platform = kvp.Value;
                if (!platform.IsOnMarket) continue;
                if (platform.Category != requiredCategory) continue;
                if (requiredCategory == ProductCategory.OperatingSystem
                    && template.validPlatformNiches != null && template.validPlatformNiches.Length > 0
                    && !AINicheArrayContains(template.validPlatformNiches, platform.Niche)) continue;
                candidates.Add(kvp.Key);
            }

            if (candidates.Count == 0) continue;

            float bestScore = float.MinValue;
            ProductId bestId = candidates[0];

            for (int ci = 0; ci < candidates.Count; ci++)
            {
                if (!_productState.shippedProducts.TryGetValue(candidates[ci], out var platform)) continue;
                float ownershipBonus = (platform.IsCompetitorProduct && platform.OwnerCompanyId == comp.Id.ToCompanyId()) ? 1.1f : 0.95f;

                float platformScore = 0f;
                float personalityBase = comp.Personality.InnovationBias * 0.5f + 0.5f;
                float pricingAggression = comp.Personality.PricingAggression > 0f ? comp.Personality.PricingAggression : 0.5f;

                foreach (var kvp2 in _templateLookup)
                {
                    var tmpl = kvp2.Value;
                    if (tmpl.availableFeatures == null) continue;
                    int len = tmpl.availableFeatures.Length;
                    for (int i = 0; i < len; i++)
                    {
                        var feat = tmpl.availableFeatures[i];
                        if (feat == null || string.IsNullOrEmpty(feat.requiresPlatformFeature)) continue;

                        float nicheDemandAffinity = _marketSystem != null ? _marketSystem.GetFeatureCategoryAffinity(targetNiche, feat.featureCategory) : 1f;
                        float generationMod = feat.availableFromGeneration <= currentGen ? 1.0f : 0.3f;
                        float trendNoise = 0.7f + _rng.NextFloat01() * 0.6f;
                        float weight = personalityBase * nicheDemandAffinity * generationMod * trendNoise;

                        bool platformHasFeature = false;
                        float upstreamQuality = 0f;
                        if (platform.SelectedFeatureIds != null)
                        {
                            int fc = platform.SelectedFeatureIds.Length;
                            for (int f = 0; f < fc; f++)
                            {
                                if (platform.SelectedFeatureIds[f] == feat.requiresPlatformFeature)
                                {
                                    platformHasFeature = true;
                                    upstreamQuality = platform.OverallQuality;
                                    break;
                                }
                            }
                        }

                        if (platformHasFeature)
                            platformScore += weight * (upstreamQuality / 100f);
                        else
                            platformScore -= weight * 0.5f;
                    }
                }

                platformScore += platform.OverallQuality * 0.3f;
                if (_platformState != null && _platformState.platformShares.TryGetValue(candidates[ci], out var entry))
                {
                    platformScore -= entry.LicensingRate * 100f * pricingAggression;
                }

                float score = platformScore * ownershipBonus;
                if (score > bestScore) { bestScore = score; bestId = candidates[ci]; }
            }

            selectedIds.Add(bestId);
        }

        return selectedIds.Count > 0 ? selectedIds.ToArray() : null;
    }

    private static bool AINicheArrayContains(ProductNiche[] arr, ProductNiche value) {
        int len = arr.Length;
        for (int i = 0; i < len; i++) {
            if (arr[i] == value) return true;
        }
        return false;
    }

    private float GetCategoryFamiliarity(Competitor comp, ProductNiche niche)
    {
        if (comp.Specializations == null || comp.Specializations.Length == 0) return 1f;

        ProductCategory nicheCategory = NicheToCategory(niche);
        int len = comp.Specializations.Length;
        for (int i = 0; i < len; i++)
        {
            if (comp.Specializations[i] == nicheCategory) return 1.5f;
        }

        CompetitorArchetypeConfig cfg = GetArchetypeConfig(comp.Archetype);
        if (cfg != null && cfg.secondaryCategories != null)
        {
            int secLen = cfg.secondaryCategories.Length;
            for (int i = 0; i < secLen; i++)
            {
                if (cfg.secondaryCategories[i] == nicheCategory) return 1.1f;
            }
        }
        return 0.6f;
    }

    private float GetCategoryFamiliarityDirect(Competitor comp, ProductCategory cat)
    {
        if (comp.Specializations == null || comp.Specializations.Length == 0) return 1f;
        int len = comp.Specializations.Length;
        for (int i = 0; i < len; i++)
        {
            if (comp.Specializations[i] == cat) return 1.5f;
        }
        CompetitorArchetypeConfig cfg = GetArchetypeConfig(comp.Archetype);
        if (cfg != null && cfg.secondaryCategories != null)
        {
            int secLen = cfg.secondaryCategories.Length;
            for (int i = 0; i < secLen; i++)
            {
                if (cfg.secondaryCategories[i] == cat) return 1.1f;
            }
        }
        return 0.6f;
    }

    private float CountCompetitorProductsInNiche(ProductNiche niche)
    {
        float count = 0f;
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsCompetitorProduct || !product.IsOnMarket) continue;
            if (product.Niche == niche) count += 1f;
        }
        return count;
    }

    private float CountCompetitorProductsInCategory(ProductCategory cat)
    {
        float count = 0f;
        foreach (var kvp in _productState.shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsCompetitorProduct || !product.IsOnMarket) continue;
            if (product.Category == cat && product.Niche == ProductNiche.None) count += 1f;
        }
        return count;
    }

    private bool IsTemplateForCategoryTier2(ProductCategory category)
    {
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Value.category == category) return true;
        }
        return false;
    }

    private static ProductCategory NicheToCategory(ProductNiche niche)
    {
        switch (niche)
        {
            case ProductNiche.DesktopOS:       return ProductCategory.OperatingSystem;
            case ProductNiche.RPG:
            case ProductNiche.FPS:
            case ProductNiche.Strategy:
            case ProductNiche.Puzzle:
            case ProductNiche.Platformer:
            case ProductNiche.Simulation:
            case ProductNiche.Racing:
            case ProductNiche.Sports:
            case ProductNiche.Horror:
            case ProductNiche.Adventure:
            case ProductNiche.MMORPG:
            case ProductNiche.Sandbox:
            case ProductNiche.Fighting:        return ProductCategory.VideoGame;
            default:                           return ProductCategory.VideoGame;
        }
    }

    private string FindTemplateForNiche(ProductNiche niche)
    {
        if (_templateLookup == null) return null;
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Key.Contains(niche.ToString())) return kvp.Key;
        }
        foreach (var kvp in _templateLookup)
            return kvp.Key;
        return null;
    }

    private string FindTemplateForCategory(ProductCategory category)
    {
        if (_templateLookup == null) return null;
        foreach (var kvp in _templateLookup)
        {
            if (kvp.Value.category == category) return kvp.Key;
        }
        return null;
    }

    private string GenerateProductName(Competitor comp, ProductNiche niche, ProductCategory category)
    {
        return CompetitorNameGenerator.GenerateProductName(_nameData, _rng, comp.CompanyName, category, niche);
    }

    private Product GetShippedProduct(ProductId id)
    {
        _productState.shippedProducts.TryGetValue(id, out var p);
        return p;
    }

    private Product GetDevProduct(ProductId id)
    {
        _productState.developmentProducts.TryGetValue(id, out var p);
        return p;
    }

    private CompetitorArchetypeConfig GetArchetypeConfig(CompetitorArchetype archetype)
    {
        if (_archetypeConfigs == null) return null;
        int len = _archetypeConfigs.Length;
        for (int i = 0; i < len; i++)
        {
            if (_archetypeConfigs[i] != null && _archetypeConfigs[i].archetype == archetype)
                return _archetypeConfigs[i];
        }
        return null;
    }

    private void OnPlayerReleaseDateAnnounced(ProductId productId, int targetTick)
    {
        Product playerProduct = null;
        _productState.developmentProducts?.TryGetValue(productId, out playerProduct);
        if (playerProduct == null)
            _productState.shippedProducts?.TryGetValue(productId, out playerProduct);
        if (playerProduct == null) return;

        EvaluateReleaseDateReaction(playerProduct, true);
    }

    private void OnPlayerReleaseDateChanged(ReleaseDateChangedEvent evt)
    {
        Product playerProduct = null;
        _productState.developmentProducts?.TryGetValue(evt.ProductId, out playerProduct);
        if (playerProduct == null)
            _productState.shippedProducts?.TryGetValue(evt.ProductId, out playerProduct);
        if (playerProduct == null) return;

        EvaluateReleaseDateReaction(playerProduct, false);
    }

    private static long Clamp(long value, long min, long max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private void RecordPendingOutcomes(Competitor comp)
    {
        if (comp.ActiveProductIds == null) return;
        int count = comp.ActiveProductIds.Count;
        for (int i = 0; i < count; i++)
        {
            ProductId pid = comp.ActiveProductIds[i];
            Product product = GetShippedProduct(pid);
            if (product == null || product.OutcomeRecorded) continue;
            if (product.ReviewResult == null) continue;
            RecordProductOutcome(comp, product);
        }
    }

    private void RecordProductOutcome(Competitor comp, Product product)
    {
        if (product == null || product.OutcomeRecorded) return;
        product.OutcomeRecorded = true;

        comp.Memory ??= CompetitorMemory.CreateNew();

        bool hasReview = product.ReviewResult != null;
        float reviewScore = hasReview ? product.ReviewResult.AggregateScore : 0f;
        long cost = product.TotalProductionCost;
        long revenue = product.TotalLifetimeRevenue;

        bool isSuccess = hasReview && reviewScore >= 60f && revenue > cost;
        bool isFailure = (hasReview && reviewScore < 40f) || revenue < cost * 0.5f;

        bool useNiche = product.Niche != ProductNiche.None;

        if (useNiche)
        {
            NicheRecord rec;
            if (!comp.Memory.NicheHistory.TryGetValue(product.Niche, out rec))
                rec = default;

            rec.ProductsShipped++;
            if (isSuccess) rec.Successes++;
            else if (isFailure) rec.Failures++;

            if (hasReview && reviewScore > rec.BestScore) rec.BestScore = reviewScore;

            float prevTotal = rec.AverageScore * (rec.ProductsShipped - 1);
            rec.AverageScore = (prevTotal + (hasReview ? reviewScore : 0f)) / rec.ProductsShipped;

            rec.TotalRevenue += revenue;
            rec.TotalInvestment += cost;

            comp.Memory.NicheHistory[product.Niche] = rec;

            if (!comp.Memory.NicheHistory.TryGetValue(comp.Memory.BestNiche, out var bestRec) ||
                rec.Successes > bestRec.Successes)
            {
                comp.Memory.BestNiche = product.Niche;
            }
        }
        else
        {
            NicheRecord rec;
            if (!comp.Memory.CategoryHistory.TryGetValue(product.Category, out rec))
                rec = default;

            rec.ProductsShipped++;
            if (isSuccess) rec.Successes++;
            else if (isFailure) rec.Failures++;

            if (hasReview && reviewScore > rec.BestScore) rec.BestScore = reviewScore;

            float prevTotal = rec.AverageScore * (rec.ProductsShipped - 1);
            rec.AverageScore = (prevTotal + (hasReview ? reviewScore : 0f)) / rec.ProductsShipped;

            rec.TotalRevenue += revenue;
            rec.TotalInvestment += cost;

            comp.Memory.CategoryHistory[product.Category] = rec;

            if (!comp.Memory.CategoryHistory.TryGetValue(comp.Memory.BestCategory, out var bestRec) ||
                rec.Successes > bestRec.Successes)
            {
                comp.Memory.BestCategory = product.Category;
            }
        }

        if (isSuccess) { comp.Memory.ConsecutiveHits++; comp.Memory.ConsecutiveFlops = 0; }
        else if (isFailure) { comp.Memory.ConsecutiveFlops++; comp.Memory.ConsecutiveHits = 0; }
        else { comp.Memory.ConsecutiveHits = 0; comp.Memory.ConsecutiveFlops = 0; }

        comp.Memory.TotalProductsShipped++;

        if (hasReview)
        {
            float prevTotal = comp.Memory.AverageReviewScore * (comp.Memory.TotalProductsShipped - 1);
            comp.Memory.AverageReviewScore = (prevTotal + reviewScore) / comp.Memory.TotalProductsShipped;
            if (reviewScore > comp.Memory.BestReviewScore)
                comp.Memory.BestReviewScore = reviewScore;
        }
    }

    private float GetNicheHistoryMultiplier(Competitor comp, ProductNiche niche)
    {
        if (comp.Memory == null) return 1f;
        NicheRecord rec;
        if (!comp.Memory.NicheHistory.TryGetValue(niche, out rec)) return 1f;
        if (rec.ProductsShipped == 0) return 1f;

        if (comp.Memory.ConsecutiveFlops >= 3 && niche == comp.Memory.BestNiche)
            return 2f;

        float successRate = rec.Successes / (float)rec.ProductsShipped;
        if (successRate > 0.6f)
            return 1.3f + (successRate - 0.6f) * 0.5f;
        if (successRate < 0.3f && rec.Failures >= 2)
            return 0.5f;
        return 1f;
    }

    private float GetCategoryHistoryMultiplier(Competitor comp, ProductCategory cat)
    {
        if (comp.Memory == null) return 1f;
        NicheRecord rec;
        if (!comp.Memory.CategoryHistory.TryGetValue(cat, out rec)) return 1f;
        if (rec.ProductsShipped == 0) return 1f;

        if (comp.Memory.ConsecutiveFlops >= 3 && cat == comp.Memory.BestCategory)
            return 2f;

        float successRate = rec.Successes / (float)rec.ProductsShipped;
        if (successRate > 0.6f)
            return 1.3f + (successRate - 0.6f) * 0.5f;
        if (successRate < 0.3f && rec.Failures >= 2)
            return 0.5f;
        return 1f;
    }

    private float GetReviewFeedbackBoost(Competitor comp, ProductNiche niche, ProductFeatureDefinition feat)
    {
        if (comp.Memory == null) return 0f;

        Product lastProduct = null;
        foreach (var kvp in _productState.archivedProducts)
        {
            Product p = kvp.Value;
            if (!p.IsCompetitorProduct) continue;
            if (p.OwnerCompanyId != comp.Id.ToCompanyId()) continue;
            if (p.Niche != niche) continue;
            if (p.ReviewResult == null) continue;
            if (lastProduct == null || p.ShipTick > lastProduct.ShipTick)
                lastProduct = p;
        }

        if (lastProduct == null) return 0f;

        bool mapped = TryMapFeatureCategoryToDimension(feat.featureCategory, out ReviewDimension dim);
        if (!mapped) return 0f;

        float dimScore = lastProduct.ReviewResult.GetDimensionScore(dim);
        if (dimScore < 50f) return 0.3f;
        if (dimScore > 80f) return -0.1f;
        return 0f;
    }

    private static bool TryMapFeatureCategoryToDimension(FeatureCategory cat, out ReviewDimension dim)
    {
        switch (cat)
        {
            case FeatureCategory.Gameplay:
            case FeatureCategory.Core:
            case FeatureCategory._UnusedMobile0:
            case FeatureCategory._UnusedDesktop0:
            case FeatureCategory._UnusedWebApp0:
            case FeatureCategory._UnusedWebApp1:
            case FeatureCategory._UnusedSaaS0:
            case FeatureCategory._UnusedAI0:
            case FeatureCategory._UnusedCloud0:
            case FeatureCategory.System:
                dim = ReviewDimension.Functionality; return true;

            case FeatureCategory.Rendering:
            case FeatureCategory.Simulation:
            case FeatureCategory.Tooling:
            case FeatureCategory._UnusedSaaS1:
                dim = ReviewDimension.Innovation; return true;

            case FeatureCategory.Presentation:
            case FeatureCategory.Interface:
            case FeatureCategory._UnusedMobile1:
            case FeatureCategory._UnusedDesktop1:
            case FeatureCategory._UnusedAI1:
                dim = ReviewDimension.Polish; return true;

            case FeatureCategory.Social:
            case FeatureCategory._UnusedMobile2:
            case FeatureCategory._UnusedDesktop2:
            case FeatureCategory.Ecosystem:
            case FeatureCategory._UnusedWebApp2:
            case FeatureCategory.Network:
            case FeatureCategory._UnusedCloud1:
            case FeatureCategory._UnusedSaaS2:
            case FeatureCategory._UnusedCloud2:
                dim = ReviewDimension.Quality; return true;

            default:
                dim = default; return false;
        }
    }
}
