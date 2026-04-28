using System.Collections.Generic;

public struct TeamMemberRoleData
{
    public EmployeeId EmployeeId;
    public string Name;
    public RoleId EmployeeRole;
}

public interface IReadOnlyGameState : IAbilityReadModel
{
    int CurrentTick { get; }
    string CompanyName { get; }
    int Money { get; }
    int MonthlyExpenses { get; }
    int TotalRevenue { get; }
    int CurrentDay { get; }
    int DayOfMonth { get; }
    int CurrentMonth { get; }
    int CurrentYear { get; }
    int CurrentHour { get; }
    int CurrentMinute { get; }
    bool IsAdvancing { get; }
    int TotalEmployees { get; }
    int IdleEmployees { get; }
    int WorkingEmployees { get; }
    IReadOnlyList<Employee> ActiveEmployees { get; }
    IReadOnlyList<CandidateData> AvailableCandidates { get; }
    IReadOnlyList<Team> ActiveTeams { get; }
    int Reputation { get; }
    ReputationTier CurrentReputationTier { get; }
    int CompanyFans { get; }
    float FanSentiment { get; }
    float FanLaunchMultiplier { get; }
    int FanWomBonus { get; }
    int GetCategoryReputation(ProductCategory category);
    List<(string category, int score)> TopReputationCategories { get; }
    int ContractsCompleted { get; }
    int ProductsShipped { get; }
    TeamId? GetEmployeeTeam(EmployeeId employeeId);

    // Role accessors
    IReadOnlyList<TeamMemberRoleData> GetTeamMemberRoles(TeamId teamId);
    int LastCandidateGenerationTick { get; }
    float CandidateGenerationSpeedMultiplier { get; }
    bool CanRerollCandidates { get; }
    int CandidateRerollCost { get; }

    int LastPoolRefreshTick { get; }
    int PoolRefreshIntervalTicks { get; }
    int RerollsUsedThisCycle { get; }
    bool CanRerollContracts { get; }
    int RerollCost { get; }

    // Finance
    FinancialHealthState FinancialHealth { get; }
    int ConsecutiveDaysNegativeCash { get; }
    int DailyObligations { get; }
    int RunwayDays { get; }
    int TotalSalaryCost { get; }
    int LoanRepaymentCost { get; }
    int TotalLoanDebt { get; }
    bool CanTakeLoan { get; }
    int MaxLoanAmount { get; }
    float LoanInterestRate { get; }
    ActiveLoan? ActiveLoan { get; }

    // Credit score
    int CreditScore { get; }
    CreditTier CreditTier { get; }

    // Loan extended fields
    int LoanDurationMonths { get; }
    int LoanRemainingMonths { get; }
    LoanRiskBand LoanRiskBand { get; }
    float LoanUtilization { get; }

    // Legacy - kept for backward compat
    int DaysInDebt { get; }

    // Interview System
    int GetInterviewStage(int candidateId);
    bool IsInterviewInProgress(int candidateId);
    bool IsCandidateHireable(int candidateId);
    int GetCandidateConfidence(int candidateId);
    int GetCandidateRoleFitScore(int candidateId);
    bool GetCandidateHasSentFollowUp(int candidateId);
    int GetCandidateWithdrawalDeadlineTick(int candidateId);

    // New interview pipeline (knowledge-based)
    bool IsFirstReportReady(int candidateId);
    bool IsFinalReportReady(int candidateId);
    bool CanStartInterview(int candidateId);
    bool CanStartInterview(int candidateId, HiringMode mode);
    float GetInterviewProgressPercent(int candidateId);
    float GetInterviewKnowledgeLevel(int candidateId);
    int GetAbilityStarEstimate(int candidateId);
    int GetPotentialStarEstimate(int candidateId);
    string GetInterviewConfidenceLabel(int candidateId);
    string GetInterviewConfidenceClass(int candidateId);
    string GetInterviewReliabilityLabel(int candidateId);
    string GetInterviewReliabilityClass(int candidateId);
    TeamId GetInterviewingTeamId(int candidateId);
    bool IsCandidateHardRejected(int candidateId);

    // New negotiation: deterministic salary demand
    int GetEffectiveSalaryDemand(int candidateId);
    bool IsSalaryRevealed(int candidateId);
    bool IsOfferOnCooldown(int candidateId);
    int GetNegotiationSkillAverage(TeamId teamId);
    string GetRecommendationLabel(int candidateId);
    HRTeamStatus GetHRTeamStatus(TeamId teamId);

    // Competing Offers
    string GetCandidateUrgency(int candidateId);
    float GetCandidateTimeRemaining(int candidateId);

    // Recruitment Reputation
    int RecruitmentScore { get; }

    // HR System
    IReadOnlyList<ActiveHRSearch> ActiveHRSearches { get; }
    bool CanStartHRSearch(TeamId teamId);
    int GetHRSkillAverage(TeamId teamId);
    float GetHRSearchSuccessChance(TeamId teamId);
    IReadOnlyList<Employee> HRSpecialists { get; }
    ActiveHRSearch GetActiveSearchForTeam(TeamId teamId);
    int ComputeSearchCost(int minAbility, int minPotentialStars, int desiredSkillCount = 0, int searchCount = 1);
    int ComputeSearchDurationDays(TeamId teamId);
    TeamType GetTeamType(TeamId teamId);
    IReadOnlyList<CandidateData> PendingReviewCandidates { get; }
    bool HasActiveHRSearch(TeamId teamId);

    // Negotiation
    ActiveNegotiation? GetNegotiation(int candidateId);
    bool HasActiveNegotiation(int candidateId);
    int GetCandidateMaxPatience(int candidateId);
    int GetCandidateCurrentPatience(int candidateId);
    bool HasPendingCounterOffer(int candidateId);
    CounterOffer? GetPendingCounterOffer(int candidateId);
    NegotiationStatus GetNegotiationStatus(int candidateId);

    // Employee Negotiation (renewal)
    bool HasEmployeeNegotiation(EmployeeId id);
    EmployeeNegotiation? GetEmployeeNegotiation(EmployeeId id);
    bool IsEmployeeOnNegotiationCooldown(EmployeeId id);

    // HiringMode-aware ability estimate
    CandidatePotentialEstimate GetCandidatePotentialEstimate(int candidateId, HiringMode mode);

    // Contract V2
    IEnumerable<Contract> GetAvailableContracts();
    IEnumerable<Contract> GetActiveContracts();
    Contract GetContractForTeam(TeamId teamId);
    Contract GetContract(ContractId contractId);
    TeamFitResult GetTeamFitPrediction(ContractId contractId, TeamId teamId);

    // Inbox
    IReadOnlyList<MailItem> InboxItems { get; }

    // Product System
    IReadOnlyDictionary<ProductId, Product> DevelopmentProducts { get; }
    IReadOnlyDictionary<ProductId, Product> ShippedProducts { get; }
    IReadOnlyDictionary<ProductId, Product> ArchivedProducts { get; }
    bool IsTeamAssignedToProduct(TeamId teamId);
    ProductId? GetProductForTeam(TeamId teamId);

    // Product economy read model
    int GetProductMonthlyRevenue(ProductId productId);
    long GetProductTotalLifetimeRevenue(ProductId productId);
    long GetProductTotalProductionCost(ProductId productId);
    float GetProductPopularity(ProductId productId);
    ProductLifecycleStage GetProductLifecycleStage(ProductId productId);
    int GetProductActiveUsers(ProductId productId);
    bool IsProductUpdating(ProductId productId);
    int GetProductPreviousActiveUsers(ProductId productId);
    int GetProductPreviousMonthlyRevenue(ProductId productId);
    int GetProductProjectedActiveUsers(ProductId productId);
    int GetProductProjectedMonthlyRevenue(ProductId productId);

    // Market System
    int GetCurrentGeneration();
    float GetFeatureAdoptionRate(string featureId, ProductNiche niche, string templateId);
    float GetNicheDemand(ProductNiche niche);
    MarketTrend GetNicheTrend(ProductNiche niche);
    float GetNicheDevTimeMultiplier(ProductNiche niche);
    List<float> GetNicheDemandProjection(ProductNiche niche, int ticks);
    float GetCategoryDemand(ProductCategory category);
    MarketTrend GetCategoryTrend(ProductCategory category);
    List<float> GetCategoryDemandProjection(ProductCategory category, int dataPoints);

    // Pivot Info
    int GetProductPivotsRemaining(ProductId productId);
    bool CanPivotProduct(ProductId productId);

    // Sale System
    bool IsProductOnSale(ProductId productId);
    int GetSaleCooldownRemainingTicks(ProductId productId);
    int GetSaleTimeRemainingTicks(ProductId productId);

    // Marketing / Hype
    float GetProductHype(ProductId productId);
    bool IsProductMarketingActive(ProductId productId);
    int GetProductTotalMarketingSpend(ProductId productId);
    float GetProductHypeAtShip(ProductId productId);
    bool IsProductRunningAds(ProductId productId);
    bool HasProductAnnouncedUpdate(ProductId productId);
    float GetProductUpdateHype(ProductId productId);
    bool HasMarketingTeamAssigned(ProductId productId);

    // Review System
    ProductReviewResult GetProductReviewResult(ProductId productId);

    // Tuning
    float ProductBaseWorkMultiplier { get; }

    // Personality & Chemistry
    Personality GetEmployeePersonality(EmployeeId employeeId);
    TeamChemistrySnapshot GetTeamChemistry(TeamId teamId);
    int GetProjectedChemistryChange(TeamId teamId, EmployeeId candidate);

    // Energy / Fatigue
    float GetEmployeeEnergy(EmployeeId employeeId);
    EnergyBand GetEmployeeEnergyBand(EmployeeId employeeId);
    float GetTeamAverageEnergy(TeamId teamId);
}
