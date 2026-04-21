using System;

[Serializable]
public class GameState
{
    public int version = 1;
    public int masterSeed;
    public int currentTick;
    public string companyName;
    
    public TimeState timeState;
    public FinanceState financeState;
    public EmployeeState employeeState;
    public TeamState teamState;
    public ContractState contractState;
    public ReputationState reputationState;
    public MoraleState moraleState;
    public LoanState loanState;
    public InterviewState interviewState;
    public NegotiationState negotiationState;
    public HRState hrState;
    public RecruitmentReputationState recruitmentReputationState;
    public InboxState inboxState;
    public ProductState productState;
    public MarketState marketState;
    public CompetitorState competitorState;
    public StockState stockState;
    public DisruptionState disruptionState;
    public TaxState taxState;
    public PlatformState platformState;
    public GenerationState generationState;
    public DifficultySettings difficultySettings;
    
    public static GameState CreateNew(int seed, string companyName = "New Company")
    {
        int startingHour = 7;
        float hourProgress = startingHour / 24f;
        int startingTick = (int)(hourProgress * TimeState.TicksPerDay);
        
        return new GameState
        {
            masterSeed = seed,
            currentTick = startingTick,
            companyName = companyName,
            timeState = TimeState.CreateNew(),
            financeState = FinanceState.CreateNew(),
            employeeState = EmployeeState.CreateNew(),
            teamState = new TeamState(),
            contractState = ContractState.CreateNew(),
            reputationState = ReputationState.CreateNew(),
            moraleState = MoraleState.CreateNew(),
            loanState = LoanState.CreateNew(),
            interviewState = InterviewState.CreateNew(),
            negotiationState = NegotiationState.CreateNew(),
            hrState = HRState.CreateNew(),
            recruitmentReputationState = RecruitmentReputationState.CreateNew(),
            inboxState = InboxState.CreateNew(),
            productState = ProductState.CreateNew(),
            competitorState = CompetitorState.CreateNew(),
            stockState = StockState.CreateNew(),
            disruptionState = DisruptionState.CreateNew(),
            taxState = TaxState.CreateNew(),
            difficultySettings = DifficultySettings.Default(DifficultyPreset.Normal)
        };
    }
}
