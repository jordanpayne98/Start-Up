using System;
using System.Collections.Generic;

public class NegotiationViewModel : IViewModel
{
    public int CandidateId { get; private set; }
    public string CandidateName { get; private set; }
    public string CandidateRole { get; private set; }
    public int AskingSalary { get; private set; }
    public string AskingSalaryDisplay { get; private set; }

    // Current negotiation
    public bool HasNegotiation { get; private set; }
    public NegotiationStatus Status { get; private set; }
    public string OfferedSalaryDisplay { get; private set; }
    public int OfferedSalary { get; private set; }

    // Deterministic salary demand
    public int SalaryDemand { get; private set; }
    public string SalaryDemandDisplay { get; private set; }
    public bool IsSalaryRevealed { get; private set; }
    public string HiringPathLabel { get; private set; }

    public event Action<int, int> OnMakeOffer;  // candidateId, salary
    public event Action OnDismiss;

    public NegotiationViewModel(int candidateId) {
        CandidateId = candidateId;
        CandidateName = "";
        CandidateRole = "";
        AskingSalaryDisplay = "$0";
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;

        // Find candidate
        var candidates = state.AvailableCandidates;
        int count = candidates.Count;
        CandidateData found = null;
        for (int i = 0; i < count; i++) {
            if (candidates[i].CandidateId == CandidateId) {
                var c = candidates[i];
                CandidateName = c.Name;
                CandidateRole = UIFormatting.FormatRole(c.Role);
                AskingSalary = c.Salary;
                AskingSalaryDisplay = UIFormatting.FormatMoney(c.Salary);
                found = c;
                break;
            }
        }

        // Negotiation state
        var neg = state.GetNegotiation(CandidateId);
        HasNegotiation = neg.HasValue;
        if (HasNegotiation) {
            var n = neg.Value;
            Status = n.status;
            OfferedSalary = n.offeredSalary;
            OfferedSalaryDisplay = UIFormatting.FormatMoney(n.offeredSalary);
        }

        // Deterministic salary demand
        SalaryDemand = state.GetEffectiveSalaryDemand(CandidateId);
        IsSalaryRevealed = state.IsSalaryRevealed(CandidateId);
        SalaryDemandDisplay = IsSalaryRevealed
            ? UIFormatting.FormatMoney(SalaryDemand) + "/mo"
            : "Interview required";

        if (found != null) {
            if (found.IsTargeted)
                HiringPathLabel = "HR Sourced";
            else if (found.InterviewStage >= 3)
                HiringPathLabel = "Interviewed";
            else
                HiringPathLabel = "Direct Hire";
        }
    }

    public void SubmitOffer(int salary) {
        OnMakeOffer?.Invoke(CandidateId, salary);
    }

    public void RequestDismiss() {
        OnDismiss?.Invoke();
    }
}
