using System;
using System.Collections.Generic;

public class HRSearchConfiguratorViewModel : IViewModel
{
    public TeamId TargetTeamId { get; }
    public string TeamName { get; private set; }
    public EmployeeRole SelectedRole { get; private set; }
    public int MinAbility { get; private set; }
    public int MaxAbility { get; private set; }
    public int MinPAStars { get; private set; }
    public int MaxPAStars { get; private set; }
    public bool[] DesiredSkills { get; private set; }
    public int SearchCount { get; private set; }
    public string CostPreview { get; private set; }
    public string DurationPreview { get; private set; }
    public string SuccessChancePreview { get; private set; }
    public bool CanLaunch { get; private set; }

    public event Action<TeamId, StartHRSearchCommand> OnLaunchSearch;
    public event Action OnDismiss;

    public HRSearchConfiguratorViewModel(TeamId teamId)
    {
        TargetTeamId = teamId;
        TeamName = "";
        SelectedRole = EmployeeRole.Developer;
        MinAbility = 0;
        MaxAbility = 0;
        MinPAStars = 0;
        MaxPAStars = 0;
        DesiredSkills = new bool[SkillTypeHelper.SkillTypeCount];
        SearchCount = 1;
        CostPreview = "$2,500";
        DurationPreview = "7 days";
        SuccessChancePreview = "30%";
        CanLaunch = false;
    }

    public void Refresh(IReadOnlyGameState state)
    {
        if (state == null) return;

        // Resolve team name
        TeamName = "";
        var teams = state.ActiveTeams;
        int tc = teams.Count;
        for (int i = 0; i < tc; i++)
        {
            if (teams[i].id == TargetTeamId)
            {
                TeamName = teams[i].name;
                break;
            }
        }

        int desiredSkillCount = 0;
        for (int i = 0; i < DesiredSkills.Length; i++)
            if (DesiredSkills[i]) desiredSkillCount++;

        var preview = state.GetHRSearchPreview(TargetTeamId, MinAbility, MinPAStars, desiredSkillCount, SearchCount);
        CostPreview = UIFormatting.FormatMoney(preview.Cost);
        DurationPreview = preview.DurationDays + " days";
        SuccessChancePreview = ((int)(preview.SuccessChance * 100f)) + "%";
        CanLaunch = !state.HasActiveHRSearch(TargetTeamId) && preview.CanAfford;
    }

    public void SetRole(EmployeeRole role)
    {
        SelectedRole = role;
    }

    public void SetCARange(int min, int max)
    {
        MinAbility = min;
        MaxAbility = max;
    }

    public void SetPARange(int min, int max)
    {
        MinPAStars = min;
        MaxPAStars = max;
    }

    public void SetDesiredSkill(int skillIndex, bool value)
    {
        if (skillIndex >= 0 && skillIndex < DesiredSkills.Length)
            DesiredSkills[skillIndex] = value;
    }

    public void SetSearchCount(int count)
    {
        if (count < 1) count = 1;
        if (count > 3) count = 3;
        SearchCount = count;
    }

    public void LaunchSearch()
    {
        var cmd = new StartHRSearchCommand
        {
            TeamId = TargetTeamId,
            TargetRole = SelectedRole,
            MinCA = MinAbility,
            MaxCA = MaxAbility,
            MinPAStars = MinPAStars,
            MaxPAStars = MaxPAStars,
            DesiredSkills = (bool[])DesiredSkills.Clone(),
            SearchCount = SearchCount
        };
        OnLaunchSearch?.Invoke(TargetTeamId, cmd);
    }

    public void Dismiss()
    {
        OnDismiss?.Invoke();
    }
}
