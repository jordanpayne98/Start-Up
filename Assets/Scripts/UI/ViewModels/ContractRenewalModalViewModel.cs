public class ContractRenewalModalViewModel : IViewModel
{
    private EmployeeId _employeeId;
    private IReadOnlyGameState _lastState;

    private EmploymentType _selectedType;
    private ContractLengthOption _selectedLength;
    private bool _initialized;

    // Current terms
    public string CurrentType { get; private set; }
    public string CurrentLength { get; private set; }
    public string CurrentSalary { get; private set; }
    public string CurrentRemaining { get; private set; }

    // Employee request
    public bool HasRequest { get; private set; }
    public string RequestText { get; private set; }
    public string RequestConsequence { get; private set; }

    // Offer selectors
    public EmploymentType SelectedType => _selectedType;
    public ContractLengthOption SelectedLength => _selectedLength;

    // Offer summary
    public string OfferSalaryText { get; private set; }
    public string OfferEffOutputText { get; private set; }
    public string OfferValueEffText { get; private set; }
    public string MarketPositionText { get; private set; }
    public string MarketPositionClass { get; private set; }

    // Delta
    public string SalaryDeltaText { get; private set; }
    public string SalaryDeltaClass { get; private set; }

    // Escalation warning
    public bool ShowEscalationWarning { get; private set; }
    public string EscalationWarningText { get; private set; }
    public string EscalationWarningClass { get; private set; }

    // Founder
    public bool IsFounder { get; private set; }

    // Can confirm
    public bool CanConfirm { get; private set; }

    // Raw for command dispatch
    public int OfferSalaryRaw { get; private set; }

    public void SetEmployeeId(EmployeeId id) {
        _employeeId = id;
        _initialized = false;
    }

    public void SetSelectedType(EmploymentType type) {
        _selectedType = type;
        RebuildOffer();
    }

    public void SetSelectedLength(ContractLengthOption length) {
        _selectedLength = length;
        RebuildOffer();
    }

    public void AcceptRequest() {
        if (!HasRequest || _lastState == null) return;
        var emp = FindEmployee();
        if (emp == null) return;
        if (emp.Renewal.RequestedTypeChange) _selectedType = emp.Renewal.RequestedType;
        if (emp.Renewal.RequestedLengthChange) _selectedLength = emp.Renewal.RequestedLength;
        RebuildOffer();
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        _lastState = state;

        var emp = FindEmployee();
        if (emp == null) return;

        if (!_initialized) {
            _selectedType   = emp.Contract.Type;
            _selectedLength = emp.Contract.Length;
            _initialized = true;
        }

        IsFounder = emp.isFounder;

        // Current terms
        CurrentType      = emp.Contract.Type == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
        CurrentLength    = FormatLength(emp.Contract.Length);
        CurrentSalary    = UIFormatting.FormatMoney(emp.salary) + "/mo";

        if (emp.isFounder) {
            CurrentRemaining = "Permanent";
        } else if (emp.contractExpiryTick <= 0) {
            CurrentRemaining = "--";
        } else {
            int days = (emp.contractExpiryTick - state.CurrentTick) / TimeState.TicksPerDay;
            if (days < 0) days = 0;
            CurrentRemaining = days + " days";
        }

        // Request
        HasRequest = emp.Renewal.HasChangeRequest;
        if (HasRequest) {
            string reqType = emp.Renewal.RequestedTypeChange
                ? (emp.Renewal.RequestedType == EmploymentType.FullTime ? "Full-Time" : "Part-Time") : "";
            string reqLen = emp.Renewal.RequestedLengthChange
                ? FormatLength(emp.Renewal.RequestedLength) : "";
            string parts = (reqType + " " + reqLen).Trim();
            RequestText = string.IsNullOrEmpty(parts) ? "Change request" : parts;
            RequestConsequence = "Rejecting this request will add a strike";
        } else {
            RequestText = "";
            RequestConsequence = "";
        }

        // Escalation
        int strikes = emp.Renewal.StrikeCount;
        ShowEscalationWarning = strikes >= 2;
        if (strikes >= 3) {
            EscalationWarningText = "Strike 3 — Employee refusing renewal";
            EscalationWarningClass = "badge--danger";
        } else if (strikes == 2) {
            EscalationWarningText = "Strike 2 of 3 — Final warning";
            EscalationWarningClass = "badge--warning";
        } else {
            EscalationWarningText = "";
            EscalationWarningClass = "badge--neutral";
        }

        RebuildOffer();
    }

    private void RebuildOffer() {
        if (_lastState == null) return;
        var emp = FindEmployee();
        if (emp == null) return;

        int marketRate = SalaryBand.GetBase(emp.role);
        int newSalary = SalaryModifierCalculator.ComputeRenewalDemand(emp, marketRate, _selectedType, _selectedLength, emp.Renewal.StrikeCount);
        OfferSalaryRaw = newSalary;

        float effOutput = SalaryModifierCalculator.GetEffectiveOutput(_selectedType);
        OfferSalaryText    = UIFormatting.FormatMoney(newSalary) + "/mo";
        OfferEffOutputText = effOutput.ToString("F2");

        float valueEff = newSalary > 0 ? (float)marketRate / newSalary * 100f : 100f;
        OfferValueEffText = System.Math.Round(valueEff).ToString("F0") + "%";

        MapMarketPosition(newSalary, marketRate, out string pos, out string posClass);
        MarketPositionText  = pos;
        MarketPositionClass = posClass;

        // Delta
        int delta = newSalary - emp.salary;
        string sign = delta >= 0 ? "+" : "";
        SalaryDeltaText  = sign + UIFormatting.FormatMoney(delta) + "/mo";
        SalaryDeltaClass = delta > 0 ? "text-danger" : delta < 0 ? "text-success" : "text-muted";

        int strikes = emp.Renewal.StrikeCount;
        bool isResolved = emp.Renewal.Phase == RenewalPhase.Renewed || emp.Renewal.Phase == RenewalPhase.Departed;
        CanConfirm = strikes < 3 && !isResolved && _lastState.Money >= newSalary;
    }

    private Employee FindEmployee() {
        if (_lastState == null) return null;
        var employees = _lastState.ActiveEmployees;
        int count = employees.Count;
        for (int i = 0; i < count; i++) {
            if (employees[i].id.Equals(_employeeId)) return employees[i];
        }
        return null;
    }

    public EmployeeId EmployeeId => _employeeId;

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
