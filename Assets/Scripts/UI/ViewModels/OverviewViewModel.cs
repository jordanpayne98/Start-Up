using System.Collections.Generic;

public struct ActiveContractDisplay
{
    public ContractId Id;
    public string Name;
    public string TeamName;
    public string PhaseName;
    public float ProgressPercent;
    public string DaysRemaining;
}

public struct InboxItemDisplay
{
    public int Id;
    public MailCategory Category;
    public MailPriority Priority;
    public string Title;
    public string Body;
    public string Timestamp;
    public bool IsRead;
    public int Tick;
    public bool IsExpanded;
    public MailAction[] Actions;
    public bool IsNewsArticle;
    public MonthlyNewsReport AttachedReport;
}

public struct ReputationDisplay
{
    public string TierName;
    public int Score;
    public int NextTierThreshold;
    public float ProgressPercent;
}

public class OverviewViewModel : IViewModel
{
    public string MoneyDisplay { get; private set; }
    public string MonthlyIncomeDisplay { get; private set; }
    public string MonthlyExpensesDisplay { get; private set; }
    public int ActiveContractCount { get; private set; }
    public int EmployeeCount { get; private set; }
    public int TeamCount { get; private set; }
    public ReputationDisplay Reputation { get; private set; }

    private readonly List<InboxItemDisplay> _recentMessages = new List<InboxItemDisplay>();
    public List<InboxItemDisplay> RecentMessages => _recentMessages;

    private readonly List<ActiveContractDisplay> _activeContracts = new List<ActiveContractDisplay>();
    public List<ActiveContractDisplay> ActiveContracts => _activeContracts;

    public OverviewViewModel() {
        MoneyDisplay = "$0";
        MonthlyIncomeDisplay = "$0";
        MonthlyExpensesDisplay = "$0";
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;

        MoneyDisplay = UIFormatting.FormatMoney(state.Money);
        MonthlyIncomeDisplay = UIFormatting.FormatMoney(state.TotalRevenue);
        MonthlyExpensesDisplay = UIFormatting.FormatMoney(state.MonthlyExpenses);
        EmployeeCount = state.TotalEmployees;
        TeamCount = state.ActiveTeams.Count;

        // Reputation
        int score = state.Reputation;
        int nextThreshold = GetNextTierThreshold(state.CurrentReputationTier);
        float repProgress = nextThreshold > 0 ? (float)score / nextThreshold : 1f;
        Reputation = new ReputationDisplay {
            TierName = UIFormatting.FormatReputationTier(state.CurrentReputationTier),
            Score = score,
            NextTierThreshold = nextThreshold,
            ProgressPercent = repProgress
        };

        // Active contracts
        _activeContracts.Clear();
        int contractCount = 0;
        foreach (var contract in state.GetActiveContracts()) {
            contractCount++;
            string teamName = "Unassigned";
            if (contract.AssignedTeamId.HasValue) {
                var teams = state.ActiveTeams;
                int teamLen = teams.Count;
                for (int t = 0; t < teamLen; t++) {
                    if (teams[t].id == contract.AssignedTeamId.Value) {
                        teamName = teams[t].name;
                        break;
                    }
                }
            }
            string phaseName = "";
            _activeContracts.Add(new ActiveContractDisplay {
                Id = contract.Id,
                Name = contract.Name,
                TeamName = teamName,
                PhaseName = phaseName,
                ProgressPercent = contract.ProgressPercent,
                DaysRemaining = contract.DeadlineTick < 0
                    ? UIFormatting.FormatTickDuration(contract.DeadlineDurationTicks)
                    : UIFormatting.FormatDaysRemaining(contract.DeadlineTick, state.CurrentTick)
            });
        }
        ActiveContractCount = contractCount;

        // Recent inbox messages (last 5)
        _recentMessages.Clear();
        var inbox = state.InboxItems;
        int msgCount = inbox.Count;
        int limit = msgCount < 5 ? msgCount : 5;
        for (int i = 0; i < limit; i++) {
            var mail = inbox[i];
            if (mail.IsDismissed) continue;
            _recentMessages.Add(new InboxItemDisplay {
                Id = mail.Id,
                Category = mail.Category,
                Priority = mail.Priority,
                Title = mail.Title,
                Body = mail.Body,
                Timestamp = UIFormatting.FormatMailAge(mail.Tick, state.CurrentTick),
                IsRead = mail.IsRead,
                Tick = mail.Tick,
                Actions = mail.Actions
            });
        }
    }

    private static int GetNextTierThreshold(ReputationTier current) {
        int nextIndex = (int)current + 1;
        var thresholds = new int[] { 0, 200, 1500, 5000, 15000 };
        if (nextIndex < thresholds.Length) return thresholds[nextIndex];
        return thresholds[thresholds.Length - 1];
    }
}
