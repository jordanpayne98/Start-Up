using System;
using System.Collections.Generic;

public class HireConfirmationViewModel : IViewModel
{
    public int CandidateId { get; private set; }
    public string Name { get; private set; }
    public string Role { get; private set; }
    public string SalaryDisplay { get; private set; }
    public int Salary { get; private set; }
    public int AbilityStars { get; private set; }
    public int PotentialStars { get; private set; }
    public string ExpiryDisplay { get; private set; }

    private readonly List<SkillDisplay> _skills = new List<SkillDisplay>();
    public List<SkillDisplay> Skills => _skills;

    // Negotiation state
    public bool HasActiveNegotiation { get; private set; }
    public NegotiationStatus NegotiationStatus { get; private set; }
    public string OfferedSalaryDisplay { get; private set; }

    // Deterministic salary demand
    public int SalaryDemand { get; private set; }
    public string SalaryDemandDisplay { get; private set; }
    public bool IsSalaryRevealed { get; private set; }
    public string HiringPathLabel { get; private set; }

    // Hiring mode
    public HiringMode Mode { get; private set; }
    public string HiringModeWarning { get; private set; }

    public event Action<int, int> OnMakeOffer;  // candidateId, offeredSalary
    public event Action OnDismiss;

    public HireConfirmationViewModel(int candidateId) {
        CandidateId = candidateId;
        Name = "";
        Role = "";
        SalaryDisplay = "$0";
        Mode = HiringMode.HR;
        HiringModeWarning = "";
    }

    public void SetMode(HiringMode mode)
    {
        Mode = mode;
        HiringModeWarning = mode == HiringMode.Manual
            ? "Manual hire: salary demand increased"
            : "";
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
        Salary = candidate.Salary;
        SalaryDisplay = UIFormatting.FormatMoney(candidate.Salary);
        AbilityStars = AbilityCalculator.AbilityToStars(candidate.CurrentAbility);
        PotentialStars = AbilityCalculator.PotentialToStars(candidate.PotentialAbility);
        ExpiryDisplay = UIFormatting.FormatDaysRemaining(candidate.ExpiryTick, state.CurrentTick);

        // Skills
        _skills.Clear();
        for (int s = 0; s < SkillTypeHelper.SkillTypeCount; s++) {
            int val = candidate.GetSkill((SkillType)s);
            if (val > 0) {
                _skills.Add(new SkillDisplay {
                    Name = SkillTypeHelper.GetName((SkillType)s),
                    Value = val,
                    MaxValue = 20
                });
            }
        }

        // Negotiation
        var negotiation = state.GetNegotiation(CandidateId);
        HasActiveNegotiation = negotiation.HasValue;
        if (HasActiveNegotiation) {
            var neg = negotiation.Value;
            NegotiationStatus = neg.status;
            OfferedSalaryDisplay = UIFormatting.FormatMoney(neg.offeredSalary);
        }

        // Deterministic salary demand
        SalaryDemand = state.GetEffectiveSalaryDemand(CandidateId);
        SalaryDemandDisplay = UIFormatting.FormatMoney(SalaryDemand) + "/mo";
        IsSalaryRevealed = state.IsSalaryRevealed(CandidateId);

        if (candidate.IsTargeted)
            HiringPathLabel = "HR Sourced";
        else if (candidate.InterviewStage >= 3)
            HiringPathLabel = "Interviewed";
        else
            HiringPathLabel = "Direct Hire";
    }

    public void SubmitOffer(int offeredSalary) {
        OnMakeOffer?.Invoke(CandidateId, offeredSalary);
    }

    public void Dismiss() {
        OnDismiss?.Invoke();
    }
}
