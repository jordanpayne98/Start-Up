using System.Collections.Generic;

public struct AvailableContractDisplay
{
    public ContractId Id;
    public string Name;
    public string Description;
    public int Difficulty;
    public string RewardDisplay;
    public string ReputationRewardDisplay;
    public string DeadlineDisplay;
    public float TotalWork;
    public bool HasStretchGoal;
    public string SkillLabel;
    public string QualityExpLabel;
    public string StaffingLabel;
    public bool IsCompetitorSourced;
    public CompetitorId? SourceCompetitorId;
    public string SourceCompetitorName;
    public string SourceProductName;
    public bool IsNicheConflict;
}

public struct ActiveContractDetailDisplay
{
    public ContractId Id;
    public string Name;
    public string Description;
    public string Status;
    public string TeamName;
    public float OverallProgress;
    public string DaysRemaining;
    public string RewardDisplay;
    public float QualityScore;
    public string SkillLabel;
    public string StaffingLabel;
    public bool HasStretchGoal;
    public string TeamFitLabel;   // "Excellent", "Good", "Moderate", "Low", "Critical", or ""
    public string TeamFitClass;   // USS badge class
}

public class ContractsOverviewViewModel : IViewModel
{
    private readonly List<AvailableContractDisplay> _availableContracts = new List<AvailableContractDisplay>();
    public List<AvailableContractDisplay> AvailableContracts => _availableContracts;

    private readonly List<ActiveContractDetailDisplay> _activeContracts = new List<ActiveContractDetailDisplay>();
    public List<ActiveContractDetailDisplay> ActiveContracts => _activeContracts;

    private readonly List<TeamSummaryDisplay> _teams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> Teams => _teams;

    public bool CanReroll { get; private set; }
    public string RerollCostDisplay { get; private set; }

    public ContractsOverviewViewModel()
    {
        CanReroll = false;
        RerollCostDisplay = "$0";
    }

    public void Refresh(IReadOnlyGameState state)
    {
        var snapshot = state as GameStateSnapshot;
        Refresh(state, snapshot?.CompetitorState);
    }

    public void Refresh(IReadOnlyGameState state, CompetitorState compState)
    {
        if (state == null) return;

        CanReroll = state.CanRerollContracts;
        RerollCostDisplay = UIFormatting.FormatMoney(state.RerollCost);

        // Contracts teams for assignment flyout
        _teams.Clear();
        var activeTeams = state.ActiveTeams;
        int teamCount = activeTeams.Count;
        for (int t = 0; t < teamCount; t++)
        {
            var team = activeTeams[t];
            if (state.GetTeamType(team.id) != TeamType.Contracts) continue;
            _teams.Add(new TeamSummaryDisplay {
                Id = team.id,
                Name = team.name,
                MemberCount = team.members?.Count ?? 0,
                ContractName = "",
                TeamType = UIFormatting.FormatTeamType(TeamType.Contracts),
                AvgMorale = 0
            });
        }

        // Available contracts
        _availableContracts.Clear();
        foreach (var c in state.GetAvailableContracts())
        {
            bool isCompetitorSourced = c.SourceCompetitorId.HasValue;
            string sourceCompetitorName = "";
            string sourceProductName = "";

            if (isCompetitorSourced && compState?.competitors != null)
            {
                if (compState.competitors.TryGetValue(c.SourceCompetitorId.Value, out var comp))
                    sourceCompetitorName = comp.CompanyName;

                if (c.SourceProductId.HasValue)
                    sourceProductName = "";
            }

            _availableContracts.Add(new AvailableContractDisplay {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Difficulty = c.Difficulty,
                RewardDisplay = UIFormatting.FormatMoney(c.RewardMoney),
                ReputationRewardDisplay = "+" + c.ReputationReward + " rep",
                DeadlineDisplay = UIFormatting.FormatTickDuration(c.DeadlineDurationTicks),
                TotalWork = c.TotalWorkRequired,
                HasStretchGoal = c.HasStretchGoal,
                SkillLabel = UIFormatting.FormatContractSkills(c.Requirements),
                StaffingLabel = c.MinContributors + "–" + c.OptimalContributors + " staff",
                IsCompetitorSourced = isCompetitorSourced,
                SourceCompetitorId = c.SourceCompetitorId,
                SourceCompetitorName = sourceCompetitorName,
                SourceProductName = sourceProductName,
                IsNicheConflict = false
            });
        }

        // Active contracts
        _activeContracts.Clear();
        foreach (var c in state.GetActiveContracts())
        {
            string teamName = "Unassigned";
            TeamId? assignedTeamId = c.AssignedTeamId;
            if (assignedTeamId.HasValue)
            {
                var teams = state.ActiveTeams;
                int teamsCount = teams.Count;
                for (int t = 0; t < teamsCount; t++)
                {
                    if (teams[t].id == assignedTeamId.Value)
                    {
                        teamName = teams[t].name;
                        break;
                    }
                }
            }

            // Team fit prediction — only shown when a team is assigned
            string fitLabel = "";
            string fitClass = "";
            if (assignedTeamId.HasValue)
            {
                var fit = state.GetTeamFitPrediction(c.Id, assignedTeamId.Value);
                fitLabel = fit.Label;
                fitClass = fit.CssClass;
            }

            _activeContracts.Add(new ActiveContractDetailDisplay {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Status = UIFormatting.FormatContractStatus(c.Status),
                TeamName = teamName,
                OverallProgress = c.ProgressPercent,
                DaysRemaining = c.DeadlineTick < 0
                    ? UIFormatting.FormatTickDuration(c.DeadlineDurationTicks)
                    : UIFormatting.FormatDaysRemaining(c.DeadlineTick, state.CurrentTick),
                RewardDisplay = UIFormatting.FormatMoney(c.RewardMoney),
                QualityScore = c.QualityScore,
                SkillLabel = UIFormatting.FormatContractSkills(c.Requirements),
                HasStretchGoal = c.HasStretchGoal,
                TeamFitLabel = fitLabel,
                TeamFitClass = fitClass
            });
        }
    }

    private static string FormatQualityExpectation(QualityExpectation q)
    {
        switch (q)
        {
            case QualityExpectation.High:    return "High Quality";
            case QualityExpectation.Premium: return "Premium";
            default:                          return "Standard";
        }
    }
}
