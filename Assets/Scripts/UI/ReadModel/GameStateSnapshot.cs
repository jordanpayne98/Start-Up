using System.Collections.Generic;

public class GameStateSnapshot : IReadOnlyGameState, IAbilityReadModel
{
    public int CurrentTick { get; private set; }
    public string CompanyName { get; private set; }
    public int Money { get; private set; }
    public int MonthlyExpenses { get; private set; }
    public int TotalRevenue { get; private set; }
    public int CurrentDay { get; private set; }
    public int DayOfMonth { get; private set; }
    public int CurrentMonth { get; private set; }
    public int CurrentYear { get; private set; }
    public int CurrentHour { get; private set; }
    public int CurrentMinute { get; private set; }
    public bool IsAdvancing { get; private set; }
    public int TotalEmployees { get; private set; }
    public int IdleEmployees { get; private set; }
    public int WorkingEmployees { get; private set; }
    public IReadOnlyList<Employee> ActiveEmployees => _activeEmployees;
    public IReadOnlyList<CandidateData> AvailableCandidates => _availableCandidates;
    public IReadOnlyList<Team> ActiveTeams => _activeTeams;
    public int Reputation { get; private set; }
    public ReputationTier CurrentReputationTier { get; private set; }
    public int CompanyFans { get; private set; }
    public float FanSentiment { get; private set; }
    public float FanLaunchMultiplier { get; private set; }
    public int FanWomBonus { get; private set; }
    public int ContractsCompleted { get; private set; }
    public int ProductsShipped { get; private set; }

    private readonly List<(string category, int score)> _topCategories = new List<(string, int)>(8);
    public List<(string category, int score)> TopReputationCategories => _topCategories;

    private ReputationSystem _reputationSystem;

    private TaxSystem _taxSystem;

    public long TaxAccumulatedProfit => _taxSystem?.CurrentProfitAccumulated ?? 0;
    public long TaxEstimatedOwed => _taxSystem?.EstimatedTaxOwed ?? 0;
    public long TaxPendingAmount => _taxSystem?.PendingTaxAmount ?? 0;
    public long TaxPendingLateFees => _taxSystem?.PendingLateFees ?? 0;
    public long TaxTotalPending => _taxSystem?.TotalPendingPayment ?? 0;
    public bool TaxHasPending => _taxSystem?.HasPendingTax ?? false;
    public int TaxNextDueTick => _taxSystem?.NextDueTick ?? 0;
    public int TaxDaysUntilDue => _taxSystem?.DaysUntilDue ?? 0;
    public float TaxRate => _taxSystem?.TaxRate ?? 0.30f;
    public int TaxOverdueMonths => _taxSystem?.OverdueMonths ?? 0;

    public void SetTaxSystem(TaxSystem ts) { _taxSystem = ts; }

    public int LastCandidateGenerationTick { get; private set; }
    public int CandidateGenerationInterval { get; private set; }
    public float CandidateGenerationSpeedMultiplier { get; private set; }
    public bool CanRerollCandidates { get; private set; }
    public int CandidateRerollCost { get; private set; }

    public int LastPoolRefreshTick { get; private set; }
    public int PoolRefreshIntervalTicks { get; private set; }
    public int RerollsUsedThisCycle { get; private set; }
    public bool CanRerollContracts { get; private set; }
    public int RerollCost { get; private set; }

    public int DaysInDebt { get; private set; }
    public int LoanRepaymentCost { get; private set; }
    public int TotalLoanDebt { get; private set; }
    public bool CanTakeLoan { get; private set; }
    public int MaxLoanAmount { get; private set; }
    public float LoanInterestRate { get; private set; }
    public ActiveLoan? ActiveLoan { get; private set; }

    // Finance health & obligations
    public FinancialHealthState FinancialHealth { get; private set; }
    public int ConsecutiveDaysNegativeCash { get; private set; }
    public int DailyObligations { get; private set; }
    public int RunwayDays { get; private set; }
    public int TotalSalaryCost { get; private set; }

    // Credit score
    public int CreditScore { get; private set; }
    public CreditTier CreditTier { get; private set; }

    // Loan extended fields
    public int LoanDurationMonths { get; private set; }
    public int LoanRemainingMonths { get; private set; }
    public LoanRiskBand LoanRiskBand { get; private set; }
    public float LoanUtilization { get; private set; }

    // Hiring pipeline
    public int RecruitmentScore { get; private set; }
    public IReadOnlyList<ActiveHRSearch> ActiveHRSearches => _activeHRSearches;
    public IReadOnlyList<Employee> HRSpecialists => _hrSpecialists;
    public IReadOnlyList<CandidateData> PendingReviewCandidates => _pendingReviewCandidates;

    // Contract V2
    // (ActiveClients removed — ClientSystem decoupled from snapshot)

    // Inbox
    public IReadOnlyList<MailItem> InboxItems => _inboxItems;

    // Product System
    public IReadOnlyDictionary<ProductId, Product> DevelopmentProducts { get; private set; }
    public IReadOnlyDictionary<ProductId, Product> ShippedProducts { get; private set; }
    public IReadOnlyDictionary<ProductId, Product> ArchivedProducts { get; private set; }

    // Competitor System
    public CompetitorState CompetitorState { get; private set; }
    public StockState StockState { get; private set; }
    public MarketState MarketStateRef { get; private set; }
    public DisruptionState DisruptionStateRef { get; private set; }
    public ProductState ProductStateRef { get; private set; }
    public EmployeeState EmployeeStateRef => _employeeState;

    // Pre-allocated backing lists for PopulateFrom reuse
    private readonly List<Employee> _activeEmployees = new List<Employee>(16);
    private readonly List<CandidateData> _availableCandidates = new List<CandidateData>(8);
    private readonly List<Team> _activeTeams = new List<Team>(8);
    private readonly List<ActiveHRSearch> _activeHRSearches = new List<ActiveHRSearch>(4);
    private readonly List<Employee> _hrSpecialists = new List<Employee>(4);
    private readonly List<CandidateData> _pendingReviewCandidates = new List<CandidateData>(4);
    private readonly List<MailItem> _inboxItems = new List<MailItem>(8);

    // Scratch lists for company-scoped queries — callers must consume immediately
    private readonly List<Employee> _companyEmployeesScratch = new List<Employee>(16);
    private readonly List<Team> _companyTeamsScratch = new List<Team>(8);

    private ProductSystem _productSystem;
    private MarketSystem _marketSystem;
    private GenerationSystem _generationSystem;

    private TeamSystem _teamSystem;
    private ContractSystem _contractSystem;
    private Dictionary<EmployeeId, TeamId> _employeeToTeamMap;
    private TeamState _teamState;
    private EmployeeState _employeeState;
    private InterviewSystem _interviewSystem;
    private NegotiationSystem _negotiationSystem;
    private HRSystem _hrSystem;
    private AbilitySystem _abilitySystem;
    private TuningConfig _tuning;

    public int GetCategoryReputation(ProductCategory category) {
        if (_reputationSystem != null)
            return _reputationSystem.GetReputation(category.ToString());
        return 0;
    }

    public TeamId? GetEmployeeTeam(EmployeeId employeeId) {
        if (_employeeToTeamMap != null && _employeeToTeamMap.TryGetValue(employeeId, out var teamId)) {
            return teamId;
        }
        return null;
    }

    public IReadOnlyList<TeamMemberRoleData> GetTeamMemberRoles(TeamId teamId) {
        var result = new List<TeamMemberRoleData>();
        if (_teamState == null) return result;
        if (!_teamState.teams.TryGetValue(teamId, out var team)) return result;
        int count = team.members.Count;
        for (int i = 0; i < count; i++) {
            var memberId = team.members[i];
            string memberName = "";
            EmployeeRole empRole = EmployeeRole.Developer;
            int empCount = ActiveEmployees.Count;
            for (int e = 0; e < empCount; e++) {
                if (ActiveEmployees[e].id == memberId) {
                    memberName = ActiveEmployees[e].name;
                    empRole = ActiveEmployees[e].role;
                    break;
                }
            }
            result.Add(new TeamMemberRoleData {
                EmployeeId = memberId,
                Name = memberName,
                EmployeeRole = empRole
            });
        }
        return result;
    }

    public int GetInterviewStage(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.GetInterviewStage(candidateId);
        return 0;
    }

    public bool IsInterviewInProgress(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.IsInterviewInProgress(candidateId);
        return false;
    }

    public bool IsCandidateHireable(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.IsHireable(candidateId);
        return true;
    }

    public int GetCandidateConfidence(int candidateId) {
        return 0;
    }

    public int GetCandidateRoleFitScore(int candidateId) {
        if (AvailableCandidates == null) return 0;
        int count = AvailableCandidates.Count;
        for (int i = 0; i < count; i++) {
            if (AvailableCandidates[i].CandidateId == candidateId)
                return AvailableCandidates[i].RoleFitScore;
        }
        return 0;
    }

    public bool GetCandidateHasSentFollowUp(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.GetCandidateHasSentFollowUp(candidateId);
        return false;
    }

    public int GetCandidateWithdrawalDeadlineTick(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.GetCandidateWithdrawalDeadlineTick(candidateId);
        return 0;
    }

    public string GetCandidateUrgency(int candidateId) {
        if (_employeeState != null) return CandidateExpiryHelper.GetUrgencyDisplay(_employeeState, candidateId, CurrentTick, _tuning);
        return "Plenty of time";
    }

    public float GetCandidateTimeRemaining(int candidateId) {
        if (_employeeState != null) return CandidateExpiryHelper.GetTimeRemainingPercent(_employeeState, candidateId, CurrentTick, _tuning);
        return 1f;
    }

    public ActiveNegotiation? GetNegotiation(int candidateId) {
        if (_negotiationSystem != null) return _negotiationSystem.GetNegotiation(candidateId);
        return null;
    }

    public bool HasActiveNegotiation(int candidateId) {
        return _negotiationSystem?.HasActiveNegotiation(candidateId) ?? false;
    }

    public bool CanStartHRSearch(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.CanStartSearch(teamId);
        return false;
    }

    public int GetHRSkillAverage(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.GetHRSkillAverage(teamId);
        return 0;
    }

    public float GetHRSearchSuccessChance(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.ComputeSuccessChance(teamId);
        return HRSearchConfig.BaseSuccessChance;
    }

    public ActiveHRSearch GetActiveSearchForTeam(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.GetActiveSearchForTeam(teamId);
        return null;
    }

    public int ComputeSearchCost(int minAbility, int minPotentialStars, int desiredSkillCount = 0, int searchCount = 1) {
        if (_hrSystem != null) return _hrSystem.ComputeSearchCost(minAbility, minPotentialStars, desiredSkillCount, searchCount);
        return HRSearchConfig.BaseSearchCost;
    }

    public int ComputeSearchDurationDays(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.ComputeDurationTicks(teamId) / TimeState.TicksPerDay;
        return HRSearchConfig.BaseDurationDays;
    }

    public bool HasActiveHRSearch(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.GetActiveSearchForTeam(teamId) != null;
        return false;
    }

    public HRSearchPreviewData GetHRSearchPreview(TeamId teamId, int minAbility, int minPotentialStars, int desiredSkillCount = 0, int searchCount = 1) {
        if (_hrSystem == null) return new HRSearchPreviewData {
            Cost = HRSearchConfig.BaseSearchCost,
            DurationDays = HRSearchConfig.BaseDurationDays,
            SuccessChance = HRSearchConfig.BaseSuccessChance,
            CanAfford = false
        };
        int cost = _hrSystem.ComputeSearchCost(minAbility, minPotentialStars, desiredSkillCount, searchCount);
        int durationDays = _hrSystem.ComputeDurationTicks(teamId) / TimeState.TicksPerDay;
        float successChance = _hrSystem.ComputeSuccessChance(teamId);
        return new HRSearchPreviewData {
            Cost = cost,
            DurationDays = durationDays,
            SuccessChance = successChance,
            CanAfford = Money >= cost
        };
    }

    public TeamType GetTeamType(TeamId teamId) {
        if (_teamSystem != null) return _teamSystem.GetTeamType(teamId);
        return TeamType.Contracts;
    }

    public IEnumerable<Contract> GetAvailableContracts() {
        if (_contractSystem != null) return _contractSystem.GetAvailableContracts();
        return System.Array.Empty<Contract>();
    }

    public IEnumerable<Contract> GetActiveContracts() {
        if (_contractSystem != null) return _contractSystem.GetActiveContracts();
        return System.Array.Empty<Contract>();
    }

    public Contract GetContractForTeam(TeamId teamId) {
        if (_contractSystem != null) return _contractSystem.GetContractForTeam(teamId);
        return null;
    }

    public Contract GetContract(ContractId contractId) {
        if (_contractSystem != null) return _contractSystem.GetContract(contractId);
        return null;
    }
    
    public static GameStateSnapshot CreateFrom(GameState gameState,
        ILoanReadModel loanReadModel = null,
        InterviewSystem interviewSystem = null,
        NegotiationSystem negotiationSystem = null,
        HRSystem hrSystem = null,
        RecruitmentReputationSystem recruitmentReputationSystem = null,
        GameController gameController = null,
        TeamSystem teamSystem = null,
        ContractSystem contractSystem = null,
        AbilitySystem abilitySystem = null,
        TuningConfig tuning = null,
        ProductSystem productSystem = null,
        MarketSystem marketSystem = null) {
        var snapshot = new GameStateSnapshot();
        snapshot.PopulateFrom(gameState, loanReadModel, interviewSystem,
            negotiationSystem, hrSystem, recruitmentReputationSystem, gameController,
            teamSystem, contractSystem, abilitySystem, tuning, productSystem, marketSystem);
        return snapshot;
    }

    public void PopulateFrom(GameState gameState,
        ILoanReadModel loanReadModel = null,
        InterviewSystem interviewSystem = null,
        NegotiationSystem negotiationSystem = null,
        HRSystem hrSystem = null,
        RecruitmentReputationSystem recruitmentReputationSystem = null,
        GameController gameController = null,
        TeamSystem teamSystem = null,
        ContractSystem contractSystem = null,
        AbilitySystem abilitySystem = null,
        TuningConfig tuning = null,
        ProductSystem productSystem = null,
        MarketSystem marketSystem = null,
        ReputationSystem reputationSystem = null,
        GenerationSystem generationSystem = null) {
        _activeEmployees.Clear();
        _availableCandidates.Clear();
        _activeTeams.Clear();
        _activeHRSearches.Clear();
        _hrSpecialists.Clear();
        _pendingReviewCandidates.Clear();
        _inboxItems.Clear();
        _topCategories.Clear();

        if (gameState == null) {
            CurrentTick = 0;
            CompanyName = "New Company";
            Money = 0;
            MonthlyExpenses = 0;
            TotalRevenue = 0;
            CurrentDay = 0;
            DayOfMonth = 1;
            CurrentMonth = 1;
            CurrentYear = 1;
            CurrentHour = 0;
            CurrentMinute = 0;
            IsAdvancing = false;
            TotalEmployees = 0;
            IdleEmployees = 0;
            WorkingEmployees = 0;
            Reputation = 0;
            CurrentReputationTier = ReputationTier.Unknown;
            _employeeToTeamMap = new Dictionary<EmployeeId, TeamId>();
            _teamState = null;
            LastCandidateGenerationTick = 0;
            CandidateGenerationInterval = 33600;
            CandidateGenerationSpeedMultiplier = 1f;
            CanRerollCandidates = false;
            CandidateRerollCost = 0;
            LastPoolRefreshTick = 0;
            PoolRefreshIntervalTicks = 7 * TimeState.TicksPerDay;
            RerollsUsedThisCycle = 0;
            CanRerollContracts = false;
            RerollCost = 0;
            DaysInDebt = 0;
            LoanRepaymentCost = 0;
            TotalLoanDebt = 0;
            CanTakeLoan = false;
            MaxLoanAmount = 0;
            LoanInterestRate = 0f;
            ActiveLoan = null;
            FinancialHealth = FinancialHealthState.Stable;
            ConsecutiveDaysNegativeCash = 0;
            DailyObligations = 0;
            RunwayDays = 0;
            CreditScore = 40;
            CreditTier = CreditTier.Fair;
            LoanDurationMonths = 0;
            LoanRemainingMonths = 0;
            LoanRiskBand = LoanRiskBand.Safe;
            LoanUtilization = 0f;
            RecruitmentScore = 50;
            _interviewSystem = null;
            _negotiationSystem = null;
            _hrSystem = null;
            _abilitySystem = null;
            _teamSystem = null;
            _contractSystem = null;
            _employeeState = null;
            _productSystem = null;
            _marketSystem = null;
            _reputationSystem = null;
            _generationSystem = null;
            CompanyFans = 0;
            FanSentiment = 50f;
            FanLaunchMultiplier = 1f;
            FanWomBonus = 0;
            ContractsCompleted = 0;
            ProductsShipped = 0;
            DevelopmentProducts = new Dictionary<ProductId, Product>();
            ShippedProducts = new Dictionary<ProductId, Product>();
            ArchivedProducts = new Dictionary<ProductId, Product>();
            CompetitorState = null;
            StockState = null;
            MarketStateRef = null;
            DisruptionStateRef = null;
            ProductStateRef = null;
            return;
        }

        if (gameState.employeeState != null && gameState.employeeState.employees != null) {
            foreach (var kvp in gameState.employeeState.employees) {
                if (kvp.Value.isActive && kvp.Value.ownerCompanyId.IsPlayer)
                    _activeEmployees.Add(kvp.Value);
            }
        }

        if (gameState.employeeState != null && gameState.employeeState.availableCandidates != null) {
            int candCount = gameState.employeeState.availableCandidates.Count;
            for (int i = 0; i < candCount; i++) {
                var c = gameState.employeeState.availableCandidates[i];
                if (!c.IsPendingReview) _availableCandidates.Add(c);
                else _pendingReviewCandidates.Add(c);
            }
        }

        if (gameState.teamState != null) {
            if (gameState.teamState.teams != null) {
                foreach (var kvp in gameState.teamState.teams) {
                    if (kvp.Value.isActive && kvp.Value.ownerCompanyId.IsPlayer)
                        _activeTeams.Add(kvp.Value);
                }
            }
            if (gameState.teamState.employeeToTeam != null) {
                if (_employeeToTeamMap == null)
                    _employeeToTeamMap = new Dictionary<EmployeeId, TeamId>(gameState.teamState.employeeToTeam);
                else {
                    _employeeToTeamMap.Clear();
                    foreach (var kvp in gameState.teamState.employeeToTeam)
                        _employeeToTeamMap[kvp.Key] = kvp.Value;
                }
            } else {
                if (_employeeToTeamMap == null) _employeeToTeamMap = new Dictionary<EmployeeId, TeamId>();
                else _employeeToTeamMap.Clear();
            }
        } else {
            if (_employeeToTeamMap == null) _employeeToTeamMap = new Dictionary<EmployeeId, TeamId>();
            else _employeeToTeamMap.Clear();
        }

        int reputation = 0;
        ReputationTier reputationTier = ReputationTier.Unknown;
        if (gameState.reputationState != null && gameState.reputationState.reputationScores != null) {
            if (gameState.reputationState.reputationScores.TryGetValue("global", out int globalRep))
                reputation = globalRep;
            reputationTier = ReputationSystem.CalculateTier(reputation, tuning);
        }

        BuildHRSpecialistsInto(_activeEmployees, _hrSpecialists);
        BuildInboxItemsInto(gameState, _inboxItems);

        if (hrSystem != null) {
            var searches = hrSystem.GetActiveSearches();
            int scount = searches.Count;
            for (int i = 0; i < scount; i++) _activeHRSearches.Add(searches[i]);
        }

        CurrentTick = gameState.currentTick;
        CompanyName = gameState.companyName ?? "New Company";
        Money = gameState.financeState?.money ?? 0;
        MonthlyExpenses = gameState.financeState != null ? ComputeMonthlyExpenses(gameState.financeState) : 0;
        TotalRevenue = gameState.financeState != null ? ComputeTotalRevenue(gameState.financeState) : 0;
        CurrentDay = gameState.timeState?.currentDay ?? 0;
        DayOfMonth = TimeState.GetDayOfMonth(gameState.timeState?.currentDay ?? 0);
        CurrentMonth = gameState.timeState?.currentMonth ?? 1;
        CurrentYear = gameState.timeState?.currentYear ?? 1;
        CurrentHour = gameController?.TimeSystem?.CurrentHour ?? 0;
        CurrentMinute = gameController?.TimeSystem?.CurrentMinute ?? 0;
        IsAdvancing = gameController?.IsAdvancing ?? false;
        TotalEmployees = _activeEmployees.Count;
        IdleEmployees = _activeEmployees.Count;
        WorkingEmployees = 0;
        Reputation = reputation;
        CurrentReputationTier = reputationTier;
        _teamState = gameState.teamState;
        LastCandidateGenerationTick = gameState.employeeState?.lastCandidateGenerationTick ?? 0;
        CandidateGenerationInterval = gameState.employeeState?.candidateGenerationInterval ?? 33600;
        CandidateGenerationSpeedMultiplier = 1f;
        CanRerollCandidates = (gameState.employeeState?.candidateRerollsUsedThisCycle ?? 1) < 1;
        CandidateRerollCost = 1000;
        LastPoolRefreshTick = gameState.contractState?.lastPoolRefreshTick ?? 0;
        PoolRefreshIntervalTicks = gameState.contractState?.poolRefreshIntervalTicks > 0
            ? gameState.contractState.poolRefreshIntervalTicks
            : 7 * TimeState.TicksPerDay;
        RerollsUsedThisCycle = gameState.contractState?.rerollsUsedThisCycle ?? 0;
        CanRerollContracts = (gameState.contractState?.rerollsUsedThisCycle ?? 1) < 1;
        RerollCost = 2000;
        DaysInDebt = gameState.financeState?.consecutiveDaysNegativeCash ?? 0;
        LoanRepaymentCost = loanReadModel != null ? loanReadModel.GetTotalMonthlyRepayment() : 0;
        TotalLoanDebt = loanReadModel != null ? loanReadModel.GetTotalRemainingDebt() : 0;
        CanTakeLoan = loanReadModel != null && loanReadModel.CanTakeLoan();
        MaxLoanAmount = loanReadModel != null ? loanReadModel.GetMaxLoanAmount() : 0;
        LoanInterestRate = loanReadModel?.GetActiveLoan()?.interestRate ?? 0f;
        ActiveLoan = loanReadModel?.GetActiveLoan();
        FinancialHealth = gameState.financeState?.financialHealth ?? FinancialHealthState.Stable;
        ConsecutiveDaysNegativeCash = gameState.financeState?.consecutiveDaysNegativeCash ?? 0;
        DailyObligations = gameState.financeState != null ? SumDailyObligations(gameState.financeState) + SumMonthlyObligations(gameState.financeState) / 30 : 0;
        RunwayDays = gameState.financeState != null ? ComputeRunway(gameState.financeState) : 0;
        TotalSalaryCost = gameState.financeState != null ? SumMonthlySalaryCosts(gameState.financeState) : 0;
        CreditScore = loanReadModel != null ? loanReadModel.GetCreditScore() : 40;
        CreditTier = loanReadModel != null ? loanReadModel.GetCreditTier() : CreditTier.Fair;
        LoanDurationMonths = loanReadModel?.GetActiveLoan()?.durationMonths ?? 0;
        LoanRemainingMonths = loanReadModel?.GetActiveLoan()?.remainingMonths ?? 0;
        LoanRiskBand = loanReadModel?.GetActiveLoan()?.riskBand ?? LoanRiskBand.Safe;
        LoanUtilization = loanReadModel?.GetActiveLoan()?.utilization ?? 0f;
        RecruitmentScore = recruitmentReputationSystem != null ? recruitmentReputationSystem.Score : 50;
        _interviewSystem = interviewSystem;
        _negotiationSystem = negotiationSystem;
        _hrSystem = hrSystem;
        _abilitySystem = abilitySystem;
        _teamSystem = teamSystem;
        _contractSystem = contractSystem;
        _employeeState = gameState.employeeState;
        _tuning = tuning;
        _productSystem = productSystem;
        _marketSystem = marketSystem;
        _reputationSystem = reputationSystem;
        _generationSystem = generationSystem;
        DevelopmentProducts = gameState.productState?.developmentProducts ?? new Dictionary<ProductId, Product>();
        ShippedProducts = gameState.productState?.shippedProducts ?? new Dictionary<ProductId, Product>();
        ArchivedProducts = gameState.productState?.archivedProducts ?? new Dictionary<ProductId, Product>();

        if (reputationSystem != null) {
            CompanyFans = reputationSystem.CompanyFans;
            FanSentiment = reputationSystem.FanSentiment;
            FanLaunchMultiplier = reputationSystem.GetFanLaunchMultiplier();
            FanWomBonus = reputationSystem.GetFanWomBonus();
            var top = reputationSystem.GetTopCategories(5);
            int topCount = top.Count;
            for (int i = 0; i < topCount; i++)
                _topCategories.Add(top[i]);
        } else {
            CompanyFans = 0;
            FanSentiment = 50f;
            FanLaunchMultiplier = 1f;
            FanWomBonus = 0;
        }

        if (gameState.reputationState != null) {
            ContractsCompleted = gameState.reputationState.contractsCompletedCount;
            ProductsShipped = gameState.reputationState.productsShippedCount;
        } else {
            ContractsCompleted = 0;
            ProductsShipped = 0;
        }

        CompetitorState = gameState.competitorState;
        StockState = gameState.stockState;
        MarketStateRef = gameState.marketState;
        DisruptionStateRef = gameState.disruptionState;
        ProductStateRef = gameState.productState;
    }

    public IReadOnlyList<Employee> GetEmployeesForCompany(CompanyId companyId) {
        _companyEmployeesScratch.Clear();
        if (_employeeState == null) return _companyEmployeesScratch;
        foreach (var kvp in _employeeState.employees) {
            if (kvp.Value.isActive && kvp.Value.ownerCompanyId == companyId)
                _companyEmployeesScratch.Add(kvp.Value);
        }
        return _companyEmployeesScratch;
    }

    public IReadOnlyList<Team> GetTeamsForCompany(CompanyId companyId) {
        _companyTeamsScratch.Clear();
        if (_teamState == null) return _companyTeamsScratch;
        foreach (var kvp in _teamState.teams) {
            if (kvp.Value.isActive && kvp.Value.ownerCompanyId == companyId)
                _companyTeamsScratch.Add(kvp.Value);
        }
        return _companyTeamsScratch;
    }

    private static int SumDailyObligations(FinanceState state)
    {
        if (state?.recurringCosts == null) return 0;
        int total = 0;
        int count = state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var e = state.recurringCosts[i];
            if (e.isActive && e.interval == RecurringInterval.Daily)
                total += e.amount;
        }
        return total;
    }

    private static int ComputeMonthlyExpenses(FinanceState state)
    {
        if (state?.recurringCosts == null) return 0;
        int monthly = 0;
        int daily = 0;
        int count = state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var e = state.recurringCosts[i];
            if (!e.isActive) continue;
            if (e.interval == RecurringInterval.Monthly) monthly += e.amount;
            else if (e.interval == RecurringInterval.Daily) daily += e.amount;
        }
        return monthly + daily * 30;
    }

    private static int ComputeTotalRevenue(FinanceState state)
    {
        if (state?.transactions == null) return 0;
        int total = 0;
        int count = state.transactions.Count;
        for (int i = 0; i < count; i++)
        {
            if (state.transactions[i].amount > 0)
                total += state.transactions[i].amount;
        }
        return total;
    }

    private static int ComputeRunway(FinanceState state)
    {
        int daily = SumDailyObligations(state) + SumMonthlyObligations(state) / 30;
        if (daily <= 0) return int.MaxValue;
        int runway = state.money / daily;
        return runway > 0 ? runway : 0;
    }

    private static int SumMonthlyObligations(FinanceState state)
    {
        if (state?.recurringCosts == null) return 0;
        int total = 0;
        int count = state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var e = state.recurringCosts[i];
            if (e.isActive && e.interval == RecurringInterval.Monthly)
                total += e.amount;
        }
        return total;
    }

    private static int SumMonthlySalaryCosts(FinanceState state)
    {
        if (state?.recurringCosts == null) return 0;
        int total = 0;
        int count = state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var e = state.recurringCosts[i];
            if (e.isActive && e.category == FinanceCategory.Salary)
                total += e.amount;
        }
        return total;
    }

    public TeamFitResult GetTeamFitPrediction(ContractId contractId, TeamId teamId)
    {
        if (_contractSystem == null) return default;
        return _contractSystem.GetTeamFitPrediction(contractId, teamId);
    }

    public bool IsTeamAssignedToProduct(TeamId teamId) {
        if (_productSystem != null) return _productSystem.IsTeamAssignedToProduct(teamId);
        return false;
    }

    public ProductId? GetProductForTeam(TeamId teamId) {
        if (_productSystem != null) return _productSystem.GetProductForTeam(teamId);
        return null;
    }

    public int GetProductMonthlyRevenue(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.MonthlyRevenue;
        return 0;
    }

    public long GetProductTotalLifetimeRevenue(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.TotalLifetimeRevenue;
        return 0;
    }

    public long GetProductTotalProductionCost(ProductId productId) {
        if (DevelopmentProducts != null && DevelopmentProducts.TryGetValue(productId, out var dp))
            return dp.TotalProductionCost;
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var sp))
            return sp.TotalProductionCost;
        return 0;
    }

    public float GetProductPopularity(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.PopularityScore;
        return 0f;
    }

    public ProductLifecycleStage GetProductLifecycleStage(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.LifecycleStage;
        return ProductLifecycleStage.PreLaunch;
    }

    public int GetProductActiveUsers(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.ActiveUserCount;
        return 0;
    }

    public bool IsProductUpdating(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p))
            return p.CurrentUpdate != null && p.CurrentUpdate.isUpdating;
        return false;
    }

    public int GetProductPreviousActiveUsers(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.PreviousActiveUsers;
        return 0;
    }

    public int GetProductPreviousMonthlyRevenue(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.PreviousMonthlyRevenue;
        return 0;
    }

    public int GetProductProjectedActiveUsers(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.ProjectedActiveUsers;
        return 0;
    }

    public int GetProductProjectedMonthlyRevenue(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.ProjectedMonthlyRevenue;
        return 0;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private static void BuildHRSpecialistsInto(List<Employee> activeEmployees, List<Employee> result)
    {
        int count = activeEmployees.Count;
        for (int i = 0; i < count; i++)
        {
            if (activeEmployees[i].role == EmployeeRole.HR)
                result.Add(activeEmployees[i]);
        }
    }

    private static void BuildInboxItemsInto(GameState gameState, List<MailItem> result)
    {
        if (gameState?.inboxState?.Items == null) return;
        int count = gameState.inboxState.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = gameState.inboxState.Items[i];
            if (!item.IsDismissed)
                result.Add(item);
        }
    }

    // IAbilityReadModel implementation — delegates to AbilitySystem
    public int GetEmployeeAbility(EmployeeId id) {
        if (_abilitySystem == null) return 0;
        int empCount = ActiveEmployees.Count;
        for (int i = 0; i < empCount; i++) {
            if (ActiveEmployees[i].id == id)
                return _abilitySystem.GetCA(id, ActiveEmployees[i].role);
        }
        return 0;
    }

    public int GetEmployeePotential(EmployeeId id) {
        int empCount = ActiveEmployees.Count;
        for (int i = 0; i < empCount; i++) {
            if (ActiveEmployees[i].id == id)
                return ActiveEmployees[i].potentialAbility;
        }
        return 0;
    }

    public int GetEmployeePotentialStars(EmployeeId id) {
        return AbilityCalculator.PotentialToStars(GetEmployeePotential(id));
    }

    public HiddenAttributes GetEmployeeHiddenAttributes(EmployeeId id) {
        int empCount = ActiveEmployees.Count;
        for (int i = 0; i < empCount; i++) {
            if (ActiveEmployees[i].id == id)
                return ActiveEmployees[i].hiddenAttributes;
        }
        return new HiddenAttributes();
    }

    public CandidatePotentialEstimate GetCandidatePotentialEstimate(int candidateId) {
        if (_abilitySystem == null || AvailableCandidates == null)
            return new CandidatePotentialEstimate { PotentialStarsMin = 1, PotentialStarsMax = 5 };
        int count = AvailableCandidates.Count;
        for (int i = 0; i < count; i++) {
            if (AvailableCandidates[i].CandidateId == candidateId) {
                var cand = AvailableCandidates[i];
                int hrSkillAvg = cand.IsTargeted
                    ? GetSourcingTeamHRSkillAverage(cand.SourcingTeamId)
                    : GetAllHREmployeesSkillAverage();
                return _abilitySystem.GetCandidateEstimate(cand, hrSkillAvg, cand.IsTargeted ? HiringMode.HR : HiringMode.Manual);
            }
        }
        return new CandidatePotentialEstimate { PotentialStarsMin = 1, PotentialStarsMax = 5 };
    }

    public CandidatePotentialEstimate GetCandidatePotentialEstimate(int candidateId, HiringMode mode) {
        if (_abilitySystem == null || AvailableCandidates == null)
            return new CandidatePotentialEstimate { PotentialStarsMin = 1, PotentialStarsMax = 5 };
        int count = AvailableCandidates.Count;
        for (int i = 0; i < count; i++) {
            if (AvailableCandidates[i].CandidateId == candidateId) {
                var cand = AvailableCandidates[i];
                int hrSkillAvg = mode == HiringMode.HR
                    ? GetSourcingTeamHRSkillAverage(cand.SourcingTeamId)
                    : GetAllHREmployeesSkillAverage();
                return _abilitySystem.GetCandidateEstimate(cand, hrSkillAvg, mode);
            }
        }
        return new CandidatePotentialEstimate { PotentialStarsMin = 1, PotentialStarsMax = 5 };
    }

    private int GetSourcingTeamHRSkillAverage(TeamId teamId) {
        if (_hrSystem == null) return 0;
        return _hrSystem.GetHRSkillAverage(teamId);
    }

    private int GetAllHREmployeesSkillAverage() {
        if (_hrSystem != null) return _hrSystem.GetAllHREmployeesSkillAverage();
        if (_employeeState == null) return -1;
        int total = 0;
        int count = 0;
        foreach (var kvp in _employeeState.employees) {
            var emp = kvp.Value;
            if (!emp.isActive) continue;
            if (emp.role != EmployeeRole.HR) continue;
            total += emp.hrSkill;
            count++;
        }
        return count > 0 ? total / count : -1;
    }

    // ─── New interview/negotiation pipeline accessors ─────────────────────────

    public bool IsFirstReportReady(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.IsFirstReportReady(candidateId);
        return false;
    }

    public bool IsFinalReportReady(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.IsFinalReportReady(candidateId);
        return false;
    }

    public bool CanStartInterview(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.CanStartInterview(candidateId);
        return false;
    }

    public bool CanStartInterview(int candidateId, HiringMode mode) {
        if (_interviewSystem != null) return _interviewSystem.CanStartInterview(candidateId, mode);
        return false;
    }

    public float GetInterviewProgressPercent(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.GetInterviewProgressPercent(candidateId, CurrentTick);
        return 0f;
    }

    public TeamId GetInterviewingTeamId(int candidateId) {
        if (_interviewSystem != null) return _interviewSystem.GetAssignedTeamId(candidateId);
        return default;
    }

    public bool IsCandidateHardRejected(int candidateId) {
        return false;
    }

    public int GetEffectiveSalaryDemand(int candidateId) {
        if (_negotiationSystem != null) return _negotiationSystem.GetEffectiveSalaryDemand(candidateId);
        return 0;
    }

    public bool IsSalaryRevealed(int candidateId) {
        if (_negotiationSystem != null) return _negotiationSystem.IsSalaryRevealed(candidateId);
        return false;
    }

    public int GetNegotiationSkillAverage(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.GetNegotiationSkillAverage(teamId);
        return 0;
    }

    public string GetRecommendationLabel(int candidateId) {
        if (AvailableCandidates == null || _abilitySystem == null) return null;
        int count = AvailableCandidates.Count;
        for (int i = 0; i < count; i++) {
            var c = AvailableCandidates[i];
            if (c.CandidateId != candidateId) continue;
            if (!IsFinalReportReady(candidateId)) return null;
            int abilityMax = AbilityCalculator.MaxAbility(c.Role);
            int potentialMax = AbilityCalculator.MaxPotential(c.Role);
            return RecommendationLabelBuilder.Build(
                c.CurrentAbility, c.PotentialAbility, abilityMax, potentialMax,
                c.Role, _tuning);
        }
        return null;
    }

    public HRTeamStatus GetHRTeamStatus(TeamId teamId) {
        if (_hrSystem != null) return _hrSystem.GetTeamStatus(teamId);
        return HRTeamStatus.Idle;
    }

    public int GetCurrentGeneration() {
        if (_generationSystem != null) return _generationSystem.GetCurrentGeneration();
        return 1;
    }

    public float GetFeatureAdoptionRate(string featureId, ProductNiche niche, string templateId) {
        if (_marketSystem != null) return _marketSystem.GetFeatureAdoptionRate(featureId, niche, templateId);
        return 0f;
    }

    public float GetNicheDemand(ProductNiche niche) {
        if (_marketSystem != null) return _marketSystem.GetNicheDemand(niche);
        return 50f;
    }

    public MarketTrend GetNicheTrend(ProductNiche niche) {
        if (_marketSystem != null) return _marketSystem.GetNicheTrend(niche);
        return MarketTrend.Stable;
    }

    public float GetNicheDevTimeMultiplier(ProductNiche niche) {
        if (_marketSystem != null) return _marketSystem.GetNicheDevTimeMultiplier(niche);
        return 1f;
    }

    public List<float> GetNicheDemandProjection(ProductNiche niche, int ticks) {
        if (_marketSystem != null) return _marketSystem.ProjectNicheDemand(niche, ticks);
        return new List<float>();
    }

    public float GetCategoryDemand(ProductCategory category) {
        if (_marketSystem != null) return _marketSystem.GetCategoryDemand(category);
        return 50f;
    }

    public MarketTrend GetCategoryTrend(ProductCategory category) {
        if (_marketSystem != null) return _marketSystem.GetCategoryTrend(category);
        return MarketTrend.Stable;
    }

    public List<float> GetCategoryDemandProjection(ProductCategory category, int dataPoints) {
        if (_marketSystem != null) return _marketSystem.ProjectCategoryDemand(category, dataPoints);
        return new List<float>();
    }

    // ── Pivot Info ─────────────────────────────────────────────────────────────

    public int GetProductPivotsRemaining(ProductId productId) {
        if (DevelopmentProducts != null && DevelopmentProducts.TryGetValue(productId, out var p))
            return p.MaxPivots - p.PivotsUsed;
        return 0;
    }

    public bool CanPivotProduct(ProductId productId) {
        if (DevelopmentProducts == null || !DevelopmentProducts.TryGetValue(productId, out var p)) return false;
        if (p.PivotsUsed >= p.MaxPivots) return false;
        if (p.Phases == null || p.Phases.Length == 0) return true;
        var finalPhase = p.Phases[p.Phases.Length - 1];
        return !(finalPhase.isUnlocked && !finalPhase.isComplete);
    }

    public bool IsProductOnSale(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.IsOnSale;
        return false;
    }

    public int GetSaleCooldownRemainingTicks(ProductId productId) {
        if (ShippedProducts == null || !ShippedProducts.TryGetValue(productId, out var p)) return 0;
        if (p.TotalSalesTriggered == 0) return 0;
        int remaining = ProductSystem.SaleEventCooldownTicks - p.TicksSinceLastSale;
        return remaining > 0 ? remaining : 0;
    }

    public int GetSaleTimeRemainingTicks(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p))
            return p.IsOnSale ? p.SaleTicksRemaining : 0;
        return 0;
    }

    // ── Marketing / Hype ───────────────────────────────────────────────────────

    public float GetProductHype(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.HypeScore;
        if (DevelopmentProducts != null && DevelopmentProducts.TryGetValue(productId, out var d)) return d.HypeScore;
        return 0f;
    }

    public bool IsProductMarketingActive(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.IsMarketingActive;
        if (DevelopmentProducts != null && DevelopmentProducts.TryGetValue(productId, out var d)) return d.IsMarketingActive;
        return false;
    }

    public int GetProductTotalMarketingSpend(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.TotalMarketingSpend;
        if (DevelopmentProducts != null && DevelopmentProducts.TryGetValue(productId, out var d)) return d.TotalMarketingSpend;
        return 0;
    }

    public float GetProductHypeAtShip(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.HypeAtShip;
        return 0f;
    }

    public bool IsProductRunningAds(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.IsRunningAds;
        return false;
    }

    public bool HasProductAnnouncedUpdate(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.HasAnnouncedUpdate;
        return false;
    }

    public float GetProductUpdateHype(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p)) return p.UpdateHype;
        return 0f;
    }

    public bool HasMarketingTeamAssigned(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p))
            return p.TeamAssignments != null && p.TeamAssignments.ContainsKey(ProductTeamRole.Marketing);
        if (DevelopmentProducts != null && DevelopmentProducts.TryGetValue(productId, out var d))
            return d.TeamAssignments != null && d.TeamAssignments.ContainsKey(ProductTeamRole.Marketing);
        return false;
    }

    public ProductReviewResult GetProductReviewResult(ProductId productId) {
        if (ShippedProducts != null && ShippedProducts.TryGetValue(productId, out var p))
            return p.ReviewResult;
        if (ArchivedProducts != null && ArchivedProducts.TryGetValue(productId, out var a))
            return a.ReviewResult;
        return null;
    }

    public float ProductBaseWorkMultiplier => _tuning?.ProductBaseWorkMultiplier ?? 100f;
}