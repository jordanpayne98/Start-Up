using System.Collections.Generic;

public struct RenewalRowDisplay
{
    public EmployeeId Id;
    public string Name;
    public string RoleDisplay;
    public string ArrangementBadge;
    public string ArrangementClass;
    public string SalaryDisplay;
    public string ExpiryDisplay;
    public string ExpiryClass;
    public string UrgencyDisplay;
    public string UrgencyClass;
    public bool HasRequest;
    public string RequestChipText;
    public string RequestChipClass;
    public string RiskDisplay;
    public string RiskClass;
    public int StrikeCount;
    public bool IsResolved;
    public string ResolvedBadge;
    public string ResolvedClass;
}

public struct RenewalOfferDisplay
{
    public string CurrentTermsSummary;
    public string CurrentSalaryDisplay;
    public string CurrentTypeDisplay;
    public string CurrentLengthDisplay;
    public string HiredDateDisplay;
    public bool HasRequest;
    public string RequestTypeDisplay;
    public string RequestLengthDisplay;
    public string RequestReasonDisplay;
    public EmploymentType SelectedType;
    public ContractLengthOption SelectedLength;
    public string OfferSalaryDisplay;
    public int OfferSalaryRaw;
    public string EffOutputDisplay;
    public string ValueEffDisplay;
    public string MarketLabelDisplay;
    public string MarketLabelClass;
    public bool CanRenew;
    public bool CanAcceptRequest;
    public string EscalationDisplay;
    public string EscalationClass;
    public bool ShowEscalation;
}

public class RenewalViewModel : IViewModel
{
    private readonly List<RenewalRowDisplay> _rows = new List<RenewalRowDisplay>(16);
    public List<RenewalRowDisplay> Rows => _rows;

    public int PendingCount { get; private set; }
    public int ExpandedIndex { get; private set; } = -1;
    public RenewalOfferDisplay ExpandedOffer { get; private set; }

    private IReadOnlyGameState _lastState;
    private EmployeeId _autoExpandId;
    private bool _hasAutoExpand;

    public void SetAutoExpand(EmployeeId id) {
        _autoExpandId = id;
        _hasAutoExpand = true;
    }

    private EmploymentType _pendingType;
    private ContractLengthOption _pendingLength;
    private bool _offerInitialized;

    public void ExpandRow(int index) {
        if (index < 0 || index >= _rows.Count) {
            ExpandedIndex = -1;
            ExpandedOffer = default;
            _offerInitialized = false;
            return;
        }
        var row = _rows[index];
        if (row.IsResolved || row.StrikeCount >= 3) {
            ExpandedIndex = -1;
            ExpandedOffer = default;
            _offerInitialized = false;
            return;
        }
        if (ExpandedIndex != index) {
            _offerInitialized = false;
        }
        ExpandedIndex = index;
        RebuildOffer();
    }

    public void SetOfferType(EmploymentType type) {
        if (ExpandedIndex < 0 || ExpandedIndex >= _rows.Count) return;
        _pendingType = type;
        RebuildOffer();
    }

    public void SetOfferLength(ContractLengthOption length) {
        if (ExpandedIndex < 0 || ExpandedIndex >= _rows.Count) return;
        _pendingLength = length;
        RebuildOffer();
    }

    public void AcceptRequest() {
        if (ExpandedIndex < 0 || ExpandedIndex >= _rows.Count) return;
        var row = _rows[ExpandedIndex];
        if (!row.HasRequest) return;
        var emp = FindEmployee(row.Id);
        if (emp == null) return;
        _pendingType   = emp.Renewal.RequestedType;
        _pendingLength = emp.Renewal.RequestedLength;
        RebuildOffer();
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        _lastState = state;

        _rows.Clear();
        PendingCount = 0;

        var employees = state.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            var emp = employees[i];
            if (!emp.isActive) continue;
            var phase = emp.Renewal.Phase;
            if (phase != RenewalPhase.WindowOpen &&
                phase != RenewalPhase.Renewed &&
                phase != RenewalPhase.Departed) continue;

            var row = BuildRow(emp, state);
            _rows.Add(row);
            if (!row.IsResolved) PendingCount++;
        }

        SortRows();

        if (_hasAutoExpand) {
            _hasAutoExpand = false;
            int autoIdx = FindRowIndex(_autoExpandId);
            if (autoIdx >= 0) ExpandRow(autoIdx);
        } else {
            if (ExpandedIndex >= _rows.Count) {
                ExpandedIndex = -1;
                ExpandedOffer = default;
            } else if (ExpandedIndex >= 0) {
                RebuildOffer();
            }
        }
    }

    private void SortRows() {
        _rows.Sort((a, b) => {
            bool aResolved = a.IsResolved;
            bool bResolved = b.IsResolved;
            if (aResolved != bResolved) return aResolved ? 1 : -1;

            int aStrike = a.StrikeCount;
            int bStrike = b.StrikeCount;
            if (aStrike != bStrike) return bStrike - aStrike;

            return 0;
        });
    }

    private RenewalRowDisplay BuildRow(Employee emp, IReadOnlyGameState state) {
        bool isResolved = emp.Renewal.Phase == RenewalPhase.Renewed || emp.Renewal.Phase == RenewalPhase.Departed;

        string roleDisplay = UIFormatting.FormatRole(emp.role);

        string arrangementBadge;
        string arrangementClass;
        if (emp.Contract.Type == EmploymentType.FullTime) {
            arrangementBadge = "Full-Time";
            arrangementClass = "badge--accent";
        } else {
            arrangementBadge = "Part-Time";
            arrangementClass = "badge--info";
        }

        string salaryDisplay = UIFormatting.FormatMoney(emp.salary) + "/mo";

        string expiryDisplay = UIFormatting.FormatDaysRemaining(emp.Renewal.ExpiryTick, state.CurrentTick);
        int daysLeft = (emp.Renewal.ExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
        string expiryClass = daysLeft <= 3 ? "text-danger" : daysLeft <= 14 ? "text-warning" : "text-muted";

        bool isUrgent = emp.Renewal.IsUrgent(state.CurrentTick);
        int strikes = emp.Renewal.StrikeCount;
        string urgencyDisplay;
        string urgencyClass;
        if (strikes >= 3) {
            urgencyDisplay = "Refusing";
            urgencyClass = "chip--urgent";
        } else if (strikes == 2) {
            urgencyDisplay = "Strike 2";
            urgencyClass = "chip--urgent";
        } else if (strikes == 1 || isUrgent) {
            urgencyDisplay = strikes == 1 ? "Strike 1" : "Urgent";
            urgencyClass = "chip--expiring";
        } else {
            urgencyDisplay = "Pending";
            urgencyClass = "badge--neutral";
        }

        bool hasRequest = emp.Renewal.HasChangeRequest;
        string requestChipText = "";
        string requestChipClass = "badge--info";
        if (hasRequest) {
            string reqType = emp.Renewal.RequestedTypeChange
                ? (emp.Renewal.RequestedType == EmploymentType.FullTime ? "→ FT" : "→ PT") : "";
            string reqLen = emp.Renewal.RequestedLengthChange
                ? ("→ " + FormatLength(emp.Renewal.RequestedLength)) : "";
            requestChipText = (reqType + " " + reqLen).Trim();
            if (string.IsNullOrEmpty(requestChipText)) requestChipText = "Has Request";
        }

        int marketRate = SalaryBand.GetBase(emp.role);
        string riskDisplay;
        string riskClass;
        float salaryRatio = marketRate > 0 ? (float)emp.salary / marketRate : 1f;
        if (salaryRatio > 1.10f) {
            riskDisplay = "High";
            riskClass = "badge--danger";
        } else if (salaryRatio > 0.90f) {
            riskDisplay = "Med";
            riskClass = "badge--warning";
        } else {
            riskDisplay = "Low";
            riskClass = "badge--success";
        }

        string resolvedBadge = "";
        string resolvedClass = "badge--neutral";
        if (isResolved) {
            if (emp.Renewal.Phase == RenewalPhase.Renewed) {
                string typeLabel = emp.Contract.Type == EmploymentType.FullTime ? "FT" : "PT";
                resolvedBadge = "Renewed " + typeLabel + " " + emp.Contract.ContractMonths + "mo";
                resolvedClass = "badge--success";
            } else {
                resolvedBadge = "Departed";
                resolvedClass = "badge--danger";
            }
        }

        return new RenewalRowDisplay {
            Id               = emp.id,
            Name             = emp.name,
            RoleDisplay      = roleDisplay,
            ArrangementBadge = arrangementBadge,
            ArrangementClass = arrangementClass,
            SalaryDisplay    = salaryDisplay,
            ExpiryDisplay    = expiryDisplay,
            ExpiryClass      = expiryClass,
            UrgencyDisplay   = urgencyDisplay,
            UrgencyClass     = urgencyClass,
            HasRequest       = hasRequest,
            RequestChipText  = requestChipText,
            RequestChipClass = requestChipClass,
            RiskDisplay      = riskDisplay,
            RiskClass        = riskClass,
            StrikeCount      = strikes,
            IsResolved       = isResolved,
            ResolvedBadge    = resolvedBadge,
            ResolvedClass    = resolvedClass
        };
    }

    private void RebuildOffer() {
        if (ExpandedIndex < 0 || ExpandedIndex >= _rows.Count || _lastState == null) {
            ExpandedOffer = default;
            return;
        }

        var row = _rows[ExpandedIndex];
        var emp = FindEmployee(row.Id);
        if (emp == null) {
            ExpandedOffer = default;
            return;
        }

        if (!_offerInitialized) {
            _pendingType   = emp.Contract.Type;
            _pendingLength = emp.Contract.Length;
            _offerInitialized = true;
        }

        EmploymentType selType       = _pendingType;
        ContractLengthOption selLength = _pendingLength;

        int marketRate  = SalaryBand.GetBase(emp.role);
        int offerSalary = SalaryModifierCalculator.ComputeRenewalDemand(emp, marketRate, selType, selLength, emp.Renewal.StrikeCount);

        float effOutput = SalaryModifierCalculator.GetEffectiveOutput(selType);
        string effOutputDisplay = effOutput.ToString("F2");

        float valueEff = offerSalary > 0 ? (float)marketRate / offerSalary * 100f : 100f;
        string valueEffDisplay = (int)System.Math.Round(valueEff) + "%";

        string marketLabel;
        string marketLabelClass;
        MapMarketPosition(offerSalary, marketRate, out marketLabel, out marketLabelClass);

        string currentTypeDisplay   = emp.Contract.Type == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
        string currentLengthDisplay = FormatLength(emp.Contract.Length);
        string currentTermsSummary  = currentTypeDisplay + " / " + currentLengthDisplay + " / " + UIFormatting.FormatMoney(emp.salary) + "/mo";
        string hiredDateDisplay     = "Day " + (emp.hireDate / TimeState.TicksPerDay);

        bool hasRequest = emp.Renewal.HasChangeRequest;
        string requestTypeDisplay   = "";
        string requestLengthDisplay = "";
        string requestReasonDisplay = "";
        bool canAcceptRequest = false;
        if (hasRequest) {
            requestTypeDisplay   = emp.Renewal.RequestedTypeChange
                ? (emp.Renewal.RequestedType == EmploymentType.FullTime ? "Full-Time" : "Part-Time") : currentTypeDisplay;
            requestLengthDisplay = emp.Renewal.RequestedLengthChange
                ? FormatLength(emp.Renewal.RequestedLength) : currentLengthDisplay;
            requestReasonDisplay = "Employee has requested different terms";
            canAcceptRequest     = true;
        }

        int strikes = emp.Renewal.StrikeCount;
        bool showEscalation = strikes > 0;
        string escalationDisplay = "";
        string escalationClass   = "badge--neutral";
        if (strikes >= 3) {
            escalationDisplay = "Strike 3 — Refusing renewal";
            escalationClass   = "badge--danger";
        } else if (strikes == 2) {
            escalationDisplay = "Strike 2 of 3 — Will leave soon";
            escalationClass   = "badge--warning";
        } else if (strikes == 1) {
            escalationDisplay = "Strike 1 of 3";
            escalationClass   = "badge--warning";
        }

        bool canRenew = strikes < 3 && !row.IsResolved && _lastState.Money >= offerSalary;

        ExpandedOffer = new RenewalOfferDisplay {
            CurrentTermsSummary  = currentTermsSummary,
            CurrentSalaryDisplay = UIFormatting.FormatMoney(emp.salary) + "/mo",
            CurrentTypeDisplay   = currentTypeDisplay,
            CurrentLengthDisplay = currentLengthDisplay,
            HiredDateDisplay     = hiredDateDisplay,
            HasRequest           = hasRequest,
            RequestTypeDisplay   = requestTypeDisplay,
            RequestLengthDisplay = requestLengthDisplay,
            RequestReasonDisplay = requestReasonDisplay,
            SelectedType         = selType,
            SelectedLength       = selLength,
            OfferSalaryDisplay   = UIFormatting.FormatMoney(offerSalary) + "/mo",
            OfferSalaryRaw       = offerSalary,
            EffOutputDisplay     = effOutputDisplay,
            ValueEffDisplay      = valueEffDisplay,
            MarketLabelDisplay   = marketLabel,
            MarketLabelClass     = marketLabelClass,
            CanRenew             = canRenew,
            CanAcceptRequest     = canAcceptRequest,
            EscalationDisplay    = escalationDisplay,
            EscalationClass      = escalationClass,
            ShowEscalation       = showEscalation
        };
    }

    private Employee FindEmployee(EmployeeId id) {
        if (_lastState == null) return null;
        var employees = _lastState.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            if (employees[i].id.Equals(id)) return employees[i];
        }
        return null;
    }

    private int FindRowIndex(EmployeeId id) {
        int count = _rows.Count;
        for (int i = 0; i < count; i++) {
            if (_rows[i].Id.Equals(id)) return i;
        }
        return -1;
    }

    private static string FormatLength(ContractLengthOption length) {
        switch (length) {
            case ContractLengthOption.Short:    return "Short";
            case ContractLengthOption.Standard: return "Standard";
            case ContractLengthOption.Long:     return "Long";
            default:                            return "Standard";
        }
    }

    private static void MapMarketPosition(int salary, int marketRate, out string label, out string labelClass) {
        float ratio = marketRate > 0 ? (float)salary / marketRate : 1f;
        if (ratio <= 0.80f)      { label = "Far Below Market";  labelClass = "badge--danger";  }
        else if (ratio <= 0.93f) { label = "Below Market";      labelClass = "badge--warning"; }
        else if (ratio <= 1.07f) { label = "At Market";         labelClass = "badge--neutral"; }
        else if (ratio <= 1.20f) { label = "Above Market";      labelClass = "badge--success"; }
        else                     { label = "Well Above Market";  labelClass = "badge--accent";  }
    }
}
