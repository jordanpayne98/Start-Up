using System.Collections.Generic;

public struct CandidateRowDisplay
{
    public int CandidateId;
    public string Name;
    public string RoleName;
    public string RolePillClass;
    public string SourceBadge;
    public bool IsInterviewed;
    public bool IsInterviewing;
    public string SalaryChip;
    public string SalaryChipClass;
    public string ExpiryText;
    public string ExpiryClass;
    public bool HasPendingCounter;
    public int PatienceCurrent;
    public int PatienceMax;
}

public class CandidateMarketViewModel : IViewModel
{
    public const int MaxPoolCapacity = 20;
    private const int PoolNearFullThreshold = 18;
    private const int ExpiryWarningDays = 5;

    private readonly List<CandidateRowDisplay> _marketPool = new List<CandidateRowDisplay>(8);
    private readonly List<CandidateRowDisplay> _sourcedPool = new List<CandidateRowDisplay>(4);
    private readonly List<CandidateRowDisplay> _shortlist  = new List<CandidateRowDisplay>(4);

    public List<CandidateRowDisplay> MarketPool => _marketPool;
    public List<CandidateRowDisplay> SourcedPool => _sourcedPool;
    public List<CandidateRowDisplay> Shortlist   => _shortlist;

    public int MarketCount   => _marketPool.Count;
    public int SourcedCount  => _sourcedPool.Count;
    public int ShortlistCount => _shortlist.Count;

    public int TotalPoolCount    { get; private set; }
    public int DaysToRefresh     { get; private set; }
    public string PoolCapacityText { get; private set; }
    public bool IsPoolNearFull   { get; private set; }
    public bool IsPoolFull       { get; private set; }

    public CandidateMarketViewModel() {
        PoolCapacityText = "0 / 20 candidates";
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;

        _marketPool.Clear();
        _sourcedPool.Clear();
        _shortlist.Clear();

        var available = state.AvailableCandidates;
        var pending   = state.PendingReviewCandidates;
        int currentTick = state.CurrentTick;

        int avCount = available.Count;
        for (int i = 0; i < avCount; i++) {
            var c = available[i];
            var row = BuildRow(c, state, currentTick);
            if (c.IsTargeted)
                _shortlist.Add(row);
            else
                _marketPool.Add(row);
        }

        int pendCount = pending.Count;
        for (int i = 0; i < pendCount; i++) {
            var c = pending[i];
            var row = BuildRow(c, state, currentTick);
            _sourcedPool.Add(row);
        }

        TotalPoolCount = _marketPool.Count + _sourcedPool.Count + _shortlist.Count;

        int currentDay = currentTick / TimeState.TicksPerDay;
        int dayOfMonth = TimeState.GetDayOfMonth(currentDay);
        int currentMonth = TimeState.GetMonth(currentDay);
        int daysInCurrentMonth = TimeState.DaysInMonth[currentMonth];
        int daysUntilRefresh = daysInCurrentMonth - dayOfMonth + 1;
        DaysToRefresh = daysUntilRefresh;

        IsPoolFull     = _marketPool.Count >= MaxPoolCapacity;
        IsPoolNearFull = _marketPool.Count >= PoolNearFullThreshold;
        PoolCapacityText = _marketPool.Count + " / " + MaxPoolCapacity + " candidates";
    }

    private static CandidateRowDisplay BuildRow(CandidateData c, IReadOnlyGameState state, int currentTick) {
        bool isInterviewed = state.IsFinalReportReady(c.CandidateId);
        bool isInterviewing = !isInterviewed && state.IsInterviewInProgress(c.CandidateId);
        int salary = state.GetEffectiveSalaryDemand(c.CandidateId);
        int marketRate = SalaryBand.GetBase(c.Role);
        string salaryChip;
        string salaryChipClass;

        if (salary <= (int)(marketRate * 0.9f)) {
            salaryChip = "Cheap";
            salaryChipClass = "badge--success";
        } else if (salary >= (int)(marketRate * 1.15f)) {
            salaryChip = "Expensive";
            salaryChipClass = "badge--danger";
        } else {
            salaryChip = "Fair";
            salaryChipClass = "badge--accent";
        }

        string expiryText;
        string expiryClass;
        if (!c.IsTargeted || c.ExpiryTick <= 0) {
            expiryText = "";
            expiryClass = "text-muted";
        } else if (c.ExpiryTick == int.MaxValue) {
            expiryText = "Indefinite";
            expiryClass = "text-muted";
        } else {
            int daysLeft = (c.ExpiryTick - currentTick) / TimeState.TicksPerDay;
            if (daysLeft < 0) daysLeft = 0;
            expiryText = daysLeft + "d";
            expiryClass = daysLeft <= ExpiryWarningDays ? "text-danger" : "text-muted";
        }

        string sourceBadge = c.IsPendingReview ? "Sourced" : (c.IsTargeted ? "Shortlist" : "Market");

        // Counter-offer state
        bool hasPendingCounter = false;
        int patienceCurrent = 0;
        int patienceMax = 0;
        var neg = state.GetNegotiation(c.CandidateId);
        if (neg.HasValue && neg.Value.status == NegotiationStatus.CounterOffered && neg.Value.hasCounterOffer) {
            hasPendingCounter = true;
            patienceCurrent = neg.Value.currentPatience;
            patienceMax = neg.Value.maxPatience;
        }

        return new CandidateRowDisplay {
            CandidateId   = c.CandidateId,
            Name          = c.Name,
            RoleName      = UIFormatting.FormatRole(c.Role),
            RolePillClass = UIFormatting.RolePillClass(c.Role),
            SourceBadge   = sourceBadge,
            IsInterviewed = isInterviewed,
            IsInterviewing = isInterviewing,
            SalaryChip    = salaryChip,
            SalaryChipClass = salaryChipClass,
            ExpiryText    = expiryText,
            ExpiryClass   = expiryClass,
            HasPendingCounter = hasPendingCounter,
            PatienceCurrent = patienceCurrent,
            PatienceMax   = patienceMax
        };
    }
}
