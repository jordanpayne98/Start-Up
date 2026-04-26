using System.Collections.Generic;

public struct SearchAssignmentDisplay
{
    public HRSearchId SearchId;
    public string RoleName;
    public string AbilityFilter;
    public string PotentialFilter;
    public string BatchSize;
    public string Status;
    public string StatusClass;
    public float Progress;
    public bool IsComplete;
}

public struct SourcedCandidateDisplay
{
    public int CandidateId;
    public string Name;
    public string Role;
    public string AbilityEstimate;
    public string PotentialEstimate;
}

public class HRAssignmentsViewModel : IViewModel
{
    public int ActiveSearchCount { get; private set; }
    public int SourcedCount { get; private set; }
    public bool HasHRTeam { get; private set; }
    public string NoHRMessage { get; private set; }

    private readonly List<SearchAssignmentDisplay> _activeSearches = new List<SearchAssignmentDisplay>(8);
    public List<SearchAssignmentDisplay> ActiveSearches => _activeSearches;

    private readonly List<SourcedCandidateDisplay> _sourcedCandidates = new List<SourcedCandidateDisplay>(16);
    public List<SourcedCandidateDisplay> SourcedCandidates => _sourcedCandidates;

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;

        int currentTick = state.CurrentTick;

        bool hasHRTeamType = false;
        var teams = state.ActiveTeams;
        int teamCount = teams.Count;
        for (int i = 0; i < teamCount; i++) {
            if (state.GetTeamType(teams[i].id) == TeamType.HR) {
                hasHRTeamType = true;
                break;
            }
        }

        if (!hasHRTeamType) {
            HasHRTeam = false;
            NoHRMessage = "No HR Team \u2014 Create an HR team in the Teams screen to begin searching for candidates.";
        } else if (state.HRSpecialists.Count == 0) {
            HasHRTeam = true;
            NoHRMessage = "HR Team needs HR Specialists \u2014 Assign employees with the HR role to conduct searches.";
        } else {
            HasHRTeam = true;
            NoHRMessage = "";
        }

        var searches = state.ActiveHRSearches;
        int searchCount = searches.Count;
        ActiveSearchCount = searchCount;

        _activeSearches.Clear();
        for (int i = 0; i < searchCount; i++) {
            var s = searches[i];
            int duration = s.completionTick - s.startTick;
            int elapsed = currentTick - s.startTick;
            float progress = duration > 0 ? (float)elapsed / duration : 1f;
            if (progress < 0f) progress = 0f;
            if (progress > 1f) progress = 1f;

            bool isComplete = currentTick >= s.completionTick;
            string status = isComplete ? "Complete" : "Searching";
            string statusClass = isComplete ? "badge--success" : "badge--accent";

            string abilityFilter = s.minCA > 0 ? s.minCA + "+ CA" : "Any CA";
            string potentialFilter = s.minPAStars > 0 ? s.minPAStars + "+ Stars" : "Any PA";

            _activeSearches.Add(new SearchAssignmentDisplay {
                SearchId    = s.searchId,
                RoleName    = UIFormatting.FormatRole(s.targetRole),
                AbilityFilter  = abilityFilter,
                PotentialFilter = potentialFilter,
                BatchSize   = s.searchCount.ToString(),
                Status      = status,
                StatusClass = statusClass,
                Progress    = progress,
                IsComplete  = isComplete
            });
        }

        var sourced = state.PendingReviewCandidates;
        int sourcedCount = sourced.Count;
        SourcedCount = sourcedCount;

        _sourcedCandidates.Clear();
        for (int i = 0; i < sourcedCount; i++) {
            var c = sourced[i];
            string ability = c.CurrentAbility.ToString();
            int stars = AbilityCalculator.PotentialToStars(c.PotentialAbility);
            string potential = AbilityCalculator.PotentialStarsDisplay(stars);
            _sourcedCandidates.Add(new SourcedCandidateDisplay {
                CandidateId       = c.CandidateId,
                Name              = c.Name,
                Role              = UIFormatting.FormatRole(c.Role),
                AbilityEstimate   = ability,
                PotentialEstimate = potential
            });
        }
    }
}
