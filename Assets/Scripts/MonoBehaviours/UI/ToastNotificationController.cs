using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToastNotificationController : MonoBehaviour
{
    private struct ToastEntry
    {
        public int MailId;
        public string Message;
        public MailPriority Priority;
        public bool IsNewsArticle;
        public string NewsHeadline;
        public ScreenId NavTarget;
    }

    private readonly Queue<ToastEntry> _queue = new Queue<ToastEntry>();
    private bool _isShowing;
    private float _currentTimer;
    private ToastEntry _current;
    private VisualElement _currentToastEl;

    private GameEventBus _eventBus;
    private INavigationService _nav;
    private Func<bool> _isInboxOpen;
    private VisualElement _toastContainer;

    // Dedicated news article toast elements
    private VisualElement _newsToastContainer;
    private Label _newsToastCategory;
    private Label _newsToastHeadline;
    private bool _newsToastShowing;
    private float _newsToastTimer;
    private const float NewsToastDuration = 5f;

    private int _lastSalaryToastTick = 0;

    private const float DurationInfo     = 3f;
    private const float DurationWarning  = 4f;
    private const float DurationCritical = 5f;

    public void Initialize(GameEventBus bus, INavigationService nav, VisualElement toastContainer, Func<bool> isInboxOpen)
    {
        _eventBus = bus;
        _nav = nav;
        _toastContainer = toastContainer;
        _isInboxOpen = isInboxOpen;

        var root = toastContainer?.parent;
        if (root != null)
        {
            _newsToastContainer = root.Q<VisualElement>("news-toast-container");
            _newsToastCategory  = _newsToastContainer?.Q<Label>("news-toast-category");
            _newsToastHeadline  = _newsToastContainer?.Q<Label>("news-toast-headline");
        }

        if (_eventBus == null) return;

        _eventBus.Subscribe<ContractFailedEvent>(OnContractFailed);
        _eventBus.Subscribe<ContractExpiredEvent>(OnContractExpired);
        _eventBus.Subscribe<SalaryPaidEvent>(OnSalaryPaid);
        _eventBus.Subscribe<CandidateFollowUpEvent>(OnCandidateFollowUp);
        _eventBus.Subscribe<CandidateDeclinedEvent>(OnCandidateDeclined);
        _eventBus.Subscribe<CandidateHardRejectedEvent>(OnCandidateHardRejected);
        _eventBus.Subscribe<CandidatesGeneratedEvent>(OnCandidatesGenerated);
        _eventBus.Subscribe<InterviewFinalReportEvent>(OnInterviewFinalReport);
        _eventBus.Subscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReadyForReview);
        _eventBus.Subscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);
        _eventBus.Subscribe<NewsArticleAddedEvent>(OnNewsArticleAdded);
        _eventBus.Subscribe<CompetitorProductLaunchedEvent>(OnCompetitorProductLaunched);
        _eventBus.Subscribe<CompetitorBankruptEvent>(OnCompetitorBankrupt);
        _eventBus.Subscribe<CompanyAcquiredEvent>(OnCompanyAcquired);
        _eventBus.Subscribe<ProductCrisisEvent>(OnProductCrisis);
        _eventBus.Subscribe<DividendPaidEvent>(OnDividendPaid);
        _eventBus.Subscribe<StockPurchasedEvent>(OnStockPurchased);
        _eventBus.Subscribe<MinorDisruptionStartedEvent>(OnMinorDisruptionStarted);
        _eventBus.Subscribe<MonthlyNewsReportEvent>(OnMonthlyNewsReport);
    }

    public void Dispose()
    {
        if (_eventBus == null) return;

        _eventBus.Unsubscribe<ContractFailedEvent>(OnContractFailed);
        _eventBus.Unsubscribe<ContractExpiredEvent>(OnContractExpired);
        _eventBus.Unsubscribe<SalaryPaidEvent>(OnSalaryPaid);
        _eventBus.Unsubscribe<CandidateFollowUpEvent>(OnCandidateFollowUp);
        _eventBus.Unsubscribe<CandidateDeclinedEvent>(OnCandidateDeclined);
        _eventBus.Unsubscribe<CandidateHardRejectedEvent>(OnCandidateHardRejected);
        _eventBus.Unsubscribe<CandidatesGeneratedEvent>(OnCandidatesGenerated);
        _eventBus.Unsubscribe<InterviewFinalReportEvent>(OnInterviewFinalReport);
        _eventBus.Unsubscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReadyForReview);
        _eventBus.Unsubscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);
        _eventBus.Unsubscribe<NewsArticleAddedEvent>(OnNewsArticleAdded);
        _eventBus.Unsubscribe<CompetitorProductLaunchedEvent>(OnCompetitorProductLaunched);
        _eventBus.Unsubscribe<CompetitorBankruptEvent>(OnCompetitorBankrupt);
        _eventBus.Unsubscribe<CompanyAcquiredEvent>(OnCompanyAcquired);
        _eventBus.Unsubscribe<ProductCrisisEvent>(OnProductCrisis);
        _eventBus.Unsubscribe<DividendPaidEvent>(OnDividendPaid);
        _eventBus.Unsubscribe<StockPurchasedEvent>(OnStockPurchased);
        _eventBus.Unsubscribe<MinorDisruptionStartedEvent>(OnMinorDisruptionStarted);
        _eventBus.Unsubscribe<MonthlyNewsReportEvent>(OnMonthlyNewsReport);
    }

    private void OnDestroy() => Dispose();

    private void Update()
    {
        if (_newsToastShowing)
        {
            _newsToastTimer -= Time.deltaTime;
            if (_newsToastTimer <= 0f)
                HideNewsToast();
        }

        if (!_isShowing) { ShowNext(); return; }

        _currentTimer -= Time.deltaTime;
        if (_currentTimer <= 0f)
            HideCurrentAndShowNext();
    }

    private void OnContractFailed(ContractFailedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "Contract Failed: " + evt.ContractName, Priority = MailPriority.Warning, NavTarget = ScreenId.ProductionContracts });

    private void OnContractExpired(ContractExpiredEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "Contract Expired #" + evt.ContractId.Value, Priority = MailPriority.Warning, NavTarget = ScreenId.ProductionContracts });

    private void OnSalaryPaid(SalaryPaidEvent evt)
    {
        int ticksPerDay = TimeState.TicksPerDay;
        bool throttled = (evt.Tick - _lastSalaryToastTick) < (7 * ticksPerDay);
        bool lowCash = evt.CashAfterPayment < evt.TotalAmount * 2;
        if (throttled && !lowCash) return;

        _lastSalaryToastTick = evt.Tick;
        var priority = lowCash ? MailPriority.Critical : MailPriority.Info;
        string msg = lowCash
            ? "Low cash warning! Salaries of " + MoneyFormatter.FormatShort(evt.TotalAmount) + " paid"
            : "Salaries paid: " + MoneyFormatter.FormatShort(evt.TotalAmount);
        Enqueue(new ToastEntry { MailId = 0, Message = msg, Priority = priority, NavTarget = ScreenId.FinanceOverview });
    }

    private void OnCandidateFollowUp(CandidateFollowUpEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = evt.CandidateName + " is following up", Priority = MailPriority.Warning, NavTarget = ScreenId.HRCandidates });

    private void OnCandidateDeclined(CandidateDeclinedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = evt.CandidateName + " declined your offer", Priority = MailPriority.Warning, NavTarget = ScreenId.HRCandidates });

    private void OnCandidateHardRejected(CandidateHardRejectedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "A candidate has closed the door", Priority = MailPriority.Warning, NavTarget = ScreenId.HRCandidates });

    private void OnCandidatesGenerated(CandidatesGeneratedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = evt.Count + " new candidates available", Priority = MailPriority.Info, NavTarget = ScreenId.HRCandidates });

    private void OnInterviewFinalReport(InterviewFinalReportEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = evt.CandidateName + " interview complete", Priority = MailPriority.Info, NavTarget = ScreenId.HRCandidates });

    private void OnHRCandidatesReadyForReview(HRCandidatesReadyForReviewEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "HR Search Complete: " + evt.CandidateCount + " candidates found", Priority = MailPriority.Info, NavTarget = ScreenId.HRCandidates });

    private void OnTeamIdleMoraleAlert(TeamIdleMoraleAlertEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = evt.TeamName + " morale warning: no contract assigned", Priority = MailPriority.Warning, NavTarget = ScreenId.ProductionContracts });

    private void OnNewsArticleAdded(NewsArticleAddedEvent evt)
    {
        ShowNewsToast(evt.Headline, evt.WasMitigated);
    }

    private void OnCompetitorProductLaunched(CompetitorProductLaunchedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "A competitor released a new product!", Priority = MailPriority.Info, NavTarget = ScreenId.CompetitorsList });

    private void OnCompetitorBankrupt(CompetitorBankruptEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "A competitor has gone bankrupt!", Priority = MailPriority.Warning, NavTarget = ScreenId.CompetitorsList });

    private void OnCompanyAcquired(CompanyAcquiredEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "Corporate acquisition — check inbox for details", Priority = MailPriority.Warning, NavTarget = ScreenId.DashboardInbox });

    private void OnProductCrisis(ProductCrisisEvent evt)
    {
        string name = string.IsNullOrEmpty(evt.ProductName) ? "Unknown Product" : evt.ProductName;
        string msg = evt.CrisisType switch
        {
            CrisisEventType.Catastrophic   => "CRITICAL: " + name + " — catastrophic failure detected!",
            CrisisEventType.ModerateBreach => "WARNING: " + name + " — security breach detected",
            _                              => "Alert: " + name + " — bug spike reported"
        };
        MailPriority priority = evt.CrisisType switch
        {
            CrisisEventType.Catastrophic   => MailPriority.Critical,
            CrisisEventType.ModerateBreach => MailPriority.Warning,
            _                              => MailPriority.Info
        };
        Enqueue(new ToastEntry { MailId = 0, Message = msg, Priority = priority, NavTarget = ScreenId.ProductionProductsInDev });
    }

    private void OnDividendPaid(DividendPaidEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "Dividend received: " + MoneyFormatter.FormatShort((int)evt.Amount), Priority = MailPriority.Info, NavTarget = ScreenId.FinanceOverview });

    private void OnStockPurchased(StockPurchasedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "Stock purchase confirmed", Priority = MailPriority.Info, NavTarget = ScreenId.FinanceMyInvestments });

    private void OnMinorDisruptionStarted(MinorDisruptionStartedEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = evt.Description, Priority = MailPriority.Info, NavTarget = ScreenId.DashboardInbox });

    private void OnMonthlyNewsReport(MonthlyNewsReportEvent evt) =>
        Enqueue(new ToastEntry { MailId = 0, Message = "Monthly Market Report available", Priority = MailPriority.Info, NavTarget = ScreenId.DashboardInbox });

    private void ShowNewsToast(string headline, bool wasMitigated)
    {
        if (_newsToastContainer == null) return;

        if (_newsToastHeadline != null)
            _newsToastHeadline.text = wasMitigated ? "[Mitigated] " + headline : headline;

        _newsToastContainer.AddToClassList("news-toast-visible");
        _newsToastShowing = true;
        _newsToastTimer = NewsToastDuration;
    }

    private void HideNewsToast()
    {
        if (_newsToastContainer == null) return;
        _newsToastContainer.RemoveFromClassList("news-toast-visible");
        _newsToastShowing = false;
        _newsToastTimer = 0f;
    }

    private void Enqueue(ToastEntry entry)
    {
        bool suppress = entry.Priority == MailPriority.Info && _isInboxOpen != null && _isInboxOpen();
        if (suppress) return;

        if (entry.Priority == MailPriority.Critical)
        {
            var temp = new ToastEntry[_queue.Count + 1];
            temp[0] = entry;
            int idx = 1;
            while (_queue.Count > 0) temp[idx++] = _queue.Dequeue();
            for (int i = 0; i < temp.Length; i++) _queue.Enqueue(temp[i]);
        }
        else
        {
            _queue.Enqueue(entry);
        }

        if (_isShowing)
            ForceHideAndShowNext();
    }

    private void ShowNext()
    {
        if (_queue.Count == 0) return;

        _current = _queue.Dequeue();
        _isShowing = true;

        _currentTimer = _current.Priority switch
        {
            MailPriority.Critical => DurationCritical,
            MailPriority.Warning  => DurationWarning,
            _                     => DurationInfo
        };

        BuildToastElement(_current);
    }

    private void HideCurrentAndShowNext()
    {
        RemoveCurrentToastElement();
        _isShowing = false;
        _currentToastEl = null;
        ShowNext();
    }

    private void ForceHideAndShowNext()
    {
        RemoveCurrentToastElement();
        _isShowing = false;
        _currentToastEl = null;
        ShowNext();
    }

    private void BuildToastElement(ToastEntry entry)
    {
        if (_toastContainer == null) return;

        RemoveCurrentToastElement();

        var toast = new VisualElement();
        toast.AddToClassList("toast");
        string typeClass = entry.Priority switch
        {
            MailPriority.Critical => "toast--danger",
            MailPriority.Warning  => "toast--warning",
            _                     => "toast--info"
        };
        toast.AddToClassList(typeClass);

        var label = new Label(entry.Message);
        label.AddToClassList("toast__message");
        toast.Add(label);

        toast.RegisterCallback<ClickEvent>(_ => {
            if (_nav != null)
                _nav.NavigateTo(_current.NavTarget);
            HideCurrentAndShowNext();
        });

        _toastContainer.Add(toast);
        _currentToastEl = toast;
    }

    private void RemoveCurrentToastElement()
    {
        if (_currentToastEl != null && _toastContainer != null)
        {
            if (_currentToastEl.parent == _toastContainer)
                _toastContainer.Remove(_currentToastEl);
            _currentToastEl = null;
        }
    }
}
