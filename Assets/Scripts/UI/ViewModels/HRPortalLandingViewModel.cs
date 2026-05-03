using System.Collections.Generic;

public struct CandidateMiniItem
{
    public string Name;
    public string Role;
    public string DaysLeft;
}

public struct SearchMiniItem
{
    public string Role;
    public string Status;
}

public struct EmployeeMiniItem
{
    public string Name;
    public string Role;
    public string ContractDaysLeft;
}

public struct TeamMiniItem
{
    public string Name;
    public string Issue;
}

public class HRPortalLandingViewModel : IViewModel
{
    // Candidates card
    public int TotalCandidates { get; private set; }
    public int ShortlistedCount { get; private set; }
    public int DaysToRefresh { get; private set; }
    public int CandidateAlertCount { get; private set; }

    // Assignments card
    public int ActiveSearches { get; private set; }
    public int SourcedCount { get; private set; }
    public int AssignmentAlertCount { get; private set; }

    // Employees card
    public int EmployeeCount { get; private set; }
    public string EffectiveCapacity { get; private set; }
    public int PendingRenewals { get; private set; }
    public int EmployeeAlertCount { get; private set; }

    // Teams card
    public int TeamCount { get; private set; }
    public string AverageChemistry { get; private set; }
    public int TeamAlertCount { get; private set; }

    // Total alert count
    public int TotalAlertCount { get; private set; }

    // Mini-lists
    private readonly List<CandidateMiniItem> _topCandidates = new List<CandidateMiniItem>(3);
    public List<CandidateMiniItem> TopCandidates => _topCandidates;

    private readonly List<SearchMiniItem> _topSearches = new List<SearchMiniItem>(3);
    public List<SearchMiniItem> TopSearches => _topSearches;

    private readonly List<EmployeeMiniItem> _topRenewals = new List<EmployeeMiniItem>(3);
    public List<EmployeeMiniItem> TopRenewals => _topRenewals;

    private readonly List<TeamMiniItem> _teamsNeedingAttention = new List<TeamMiniItem>(3);
    public List<TeamMiniItem> TeamsNeedingAttention => _teamsNeedingAttention;

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;

        int currentTick = state.CurrentTick;

        // --- Candidates card ---
        var candidates = state.AvailableCandidates;
        int candCount = candidates.Count;
        TotalCandidates = candCount;
        ShortlistedCount = state.PendingReviewCandidates.Count;

        int currentDay = currentTick / TimeState.TicksPerDay;
        int dayOfMonth = TimeState.GetDayOfMonth(currentDay);
        int currentMonth = TimeState.GetMonth(currentDay);
        int daysInCurrentMonth = TimeState.DaysInMonth[currentMonth];
        DaysToRefresh = daysInCurrentMonth - dayOfMonth + 1;

        _topCandidates.Clear();
        int candLimit = candCount < 3 ? candCount : 3;
        for (int i = 0; i < candLimit; i++) {
            var c = candidates[i];
            int daysLeft = (c.ExpiryTick - currentTick) / 8;
            if (daysLeft < 0) daysLeft = 0;
            _topCandidates.Add(new CandidateMiniItem {
                Name = c.Name,
                Role = UIFormatting.FormatRole(c.Role),
                DaysLeft = daysLeft + "d"
            });
        }

        CandidateAlertCount = ShortlistedCount;

        // --- Assignments card ---
        var searches = state.ActiveHRSearches;
        ActiveSearches = searches.Count;
        SourcedCount = state.PendingReviewCandidates.Count;

        _topSearches.Clear();
        int searchCount = searches.Count;
        int searchLimit = searchCount < 3 ? searchCount : 3;
        for (int i = 0; i < searchLimit; i++) {
            var s = searches[i];
            int daysRemaining = (s.completionTick - currentTick) / 8;
            string status = daysRemaining > 0 ? daysRemaining + "d left" : "Completing";
            _topSearches.Add(new SearchMiniItem {
                Role = UIFormatting.FormatRole(s.targetRole),
                Status = status
            });
        }

        AssignmentAlertCount = SourcedCount;

        // --- Employees card ---
        var employees = state.ActiveEmployees;
        int empCount = employees.Count;
        EmployeeCount = state.TotalEmployees;

        float totalCapacity = 0f;
        int renewalCount = 0;
        _topRenewals.Clear();

        for (int i = 0; i < empCount; i++) {
            var emp = employees[i];
            totalCapacity += emp.EffectiveOutput;
            int daysToExpiry = (emp.contractExpiryTick - currentTick) / 8;
            if (emp.contractExpiryTick > 0 && daysToExpiry <= 60) {
                renewalCount++;
                if (_topRenewals.Count < 3) {
                    _topRenewals.Add(new EmployeeMiniItem {
                        Name = emp.name,
                        Role = UIFormatting.FormatRole(emp.role),
                        ContractDaysLeft = daysToExpiry + "d"
                    });
                }
            }
        }

        PendingRenewals = renewalCount;
        EffectiveCapacity = empCount > 0 ? (totalCapacity / empCount * 100f).ToString("F0") + "%" : "--";
        EmployeeAlertCount = renewalCount;

        // --- Teams card ---
        var teams = state.ActiveTeams;
        int teamCount = teams.Count;
        TeamCount = teamCount;

        float totalChemScore = 0f;
        int attentionCount = 0;
        _teamsNeedingAttention.Clear();

        for (int i = 0; i < teamCount; i++) {
            var team = teams[i];
            var chemistry = state.GetTeamChemistry(team.id);
            totalChemScore += chemistry.Score;

            bool needsAttention = chemistry.Band == ChemistryBand.Poor || chemistry.Band == ChemistryBand.Toxic
                || team.MemberCount == 0;
            if (needsAttention) {
                attentionCount++;
                if (_teamsNeedingAttention.Count < 3) {
                    string issue = team.MemberCount == 0 ? "Understaffed" : "Low chemistry";
                    _teamsNeedingAttention.Add(new TeamMiniItem {
                        Name = team.name,
                        Issue = issue
                    });
                }
            }
        }

        AverageChemistry = teamCount > 0
            ? UIFormatting.FormatPercent(totalChemScore / teamCount / 100f)
            : "--";
        TeamAlertCount = attentionCount;

        // --- Totals ---
        TotalAlertCount = CandidateAlertCount + AssignmentAlertCount + EmployeeAlertCount + TeamAlertCount;
    }
}
