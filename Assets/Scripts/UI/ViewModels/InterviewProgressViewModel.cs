using System;

public class InterviewProgressViewModel : IViewModel
{
    public int CandidateId { get; private set; }
    public string Name { get; private set; }
    public string Role { get; private set; }
    public int InterviewStage { get; private set; }
    public bool IsInProgress { get; private set; }
    public bool IsHireable { get; private set; }

    // Next stage info
    public int NextStage { get; private set; }
    public string NextStageName { get; private set; }
    public string NextStageCostDisplay { get; private set; }
    public int NextStageCost { get; private set; }
    public string NextStageDurationDisplay { get; private set; }
    public bool CanStartNext { get; private set; }

    // In-progress info
    public string InProgressStageName { get; private set; }
    public string CompletesInDisplay { get; private set; }

    public event Action<int> OnStartInterview; // candidateId
    public event Action OnDismiss;

    private static readonly string[] _stageNames = { "", "Phone Screen", "Skills Interview", "Final Interview" };
    private static readonly int[] _stageCosts = { 0, 0, 25, 75 };

    public InterviewProgressViewModel(int candidateId) {
        CandidateId = candidateId;
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;

        CandidateData candidate = default;
        bool found = false;
        var candidates = state.AvailableCandidates;
        int count = candidates.Count;
        for (int i = 0; i < count; i++) {
            if (candidates[i].CandidateId == CandidateId) {
                candidate = candidates[i];
                found = true;
                break;
            }
        }
        if (!found) return;

        Name = candidate.Name;
        Role = UIFormatting.FormatRole(candidate.Role);
        InterviewStage = state.GetInterviewStage(CandidateId);
        IsInProgress = state.IsInterviewInProgress(CandidateId);
        IsHireable = state.IsCandidateHireable(CandidateId);

        if (IsInProgress) {
            InProgressStageName = InterviewStage > 0 && InterviewStage <= 3
                ? _stageNames[InterviewStage]
                : "Interview";
            // CompletionTick comes from ActiveInterview; approximate from stage duration midpoints
            CompletesInDisplay = "In progress...";
        }

        NextStage = InterviewStage + 1;
        if (NextStage >= 1 && NextStage <= 3) {
            NextStageName = _stageNames[NextStage];
            NextStageCost = _stageCosts[NextStage];
            NextStageCostDisplay = NextStageCost == 0 ? "Free" : UIFormatting.FormatMoney(NextStageCost);
            NextStageDurationDisplay = NextStage == 1 ? "4–12 sim-hours"
                : NextStage == 2 ? "1–3 days"
                : "12 hours–2 days";
            CanStartNext = !IsInProgress && !IsHireable && state.Money >= NextStageCost;
        } else {
            CanStartNext = false;
        }
    }

    public void StartInterview() {
        OnStartInterview?.Invoke(CandidateId);
    }

    public void Dismiss() {
        OnDismiss?.Invoke();
    }
}
