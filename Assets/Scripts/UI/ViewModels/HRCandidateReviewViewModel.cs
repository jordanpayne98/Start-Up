using System;
using System.Collections.Generic;

public class HRCandidateReviewViewModel : IViewModel
{
    public struct ReviewCandidateDisplay
    {
        public int CandidateId;
        public string Name;
        public string Role;
        public string SalaryDisplay;
        public int AbilityStars;
        public int PotentialStars;
        public string SkillTierLabel;
    }

    private readonly List<ReviewCandidateDisplay> _pendingCandidates = new List<ReviewCandidateDisplay>();
    public List<ReviewCandidateDisplay> PendingCandidates => _pendingCandidates;

    public string TeamName { get; private set; }
    public string SearchCriteriaLabel { get; private set; }

    private readonly IReadOnlyList<int> _pendingCandidateIds;

    public event Action<int> OnAcceptCandidate;
    public event Action<int> OnDeclineCandidate;
    public event Action OnDismiss;

    public HRCandidateReviewViewModel(IReadOnlyList<int> pendingCandidateIds, string teamName, string criteriaLabel)
    {
        _pendingCandidateIds = pendingCandidateIds ?? new List<int>();
        TeamName = teamName ?? "";
        SearchCriteriaLabel = criteriaLabel ?? "";
    }

    public void Refresh(IReadOnlyGameState state)
    {
        if (state == null) return;

        _pendingCandidates.Clear();

        var pending = state.PendingReviewCandidates;
        int pendingCount = pending.Count;
        int idCount = _pendingCandidateIds.Count;

        for (int i = 0; i < idCount; i++)
        {
            int targetId = _pendingCandidateIds[i];
            for (int j = 0; j < pendingCount; j++)
            {
                var c = pending[j];
                if (c.CandidateId != targetId) continue;

                int abilityStars = AbilityCalculator.AbilityToStars(c.CurrentAbility);
                int potentialStars = AbilityCalculator.PotentialToStars(c.PotentialAbility);

                _pendingCandidates.Add(new ReviewCandidateDisplay
                {
                    CandidateId = c.CandidateId,
                    Name = c.Name,
                    Role = UIFormatting.FormatRole(c.Role),
                    SalaryDisplay = UIFormatting.FormatMoney(c.Salary),
                    AbilityStars = abilityStars,
                    PotentialStars = potentialStars,
                    SkillTierLabel = c.SkillLevel
                });
                break;
            }
        }
    }

    public void AcceptCandidate(int candidateId)
    {
        OnAcceptCandidate?.Invoke(candidateId);
    }

    public void DeclineCandidate(int candidateId)
    {
        OnDeclineCandidate?.Invoke(candidateId);
    }

    public void Dismiss()
    {
        OnDismiss?.Invoke();
    }
}
