public class InboxSystem
{
    private readonly InboxState _state;
    private readonly GameEventBus _eventBus;
    private readonly ILogger _logger;
    private int _nextId = 1;

    // Cached unread counts — invalidated on any read/dismiss mutation
    private int _unreadCount = -1;
    private int _criticalUnreadCount = -1;

    public int UnreadCount
    {
        get
        {
            if (_state == null) return 0;
            if (_unreadCount >= 0) return _unreadCount;
            ComputeCounts();
            return _unreadCount;
        }
    }

    public int CriticalUnreadCount
    {
        get
        {
            if (_state == null) return 0;
            if (_criticalUnreadCount >= 0) return _criticalUnreadCount;
            ComputeCounts();
            return _criticalUnreadCount;
        }
    }

    public InboxSystem(InboxState state, GameEventBus eventBus, ILogger logger = null)
    {
        _logger = logger;
        if (state == null)
        {
            _logger?.LogError("[InboxSystem] InboxState is null. All operations will be no-ops.");
        }
        _state = state;
        _eventBus = eventBus;

        // Determine next id from existing persisted state.
        if (_state != null && _state.Items != null)
        {
            int count = _state.Items.Count;
            for (int i = 0; i < count; i++)
            {
                if (_state.Items[i].Id >= _nextId)
                    _nextId = _state.Items[i].Id + 1;
            }
        }
    }

    public void Initialize()
    {
        if (_eventBus == null) return;

        _eventBus.Subscribe<CandidatesGeneratedEvent>(OnCandidatesGenerated);
        _eventBus.Subscribe<ContractCompletedEvent>(OnContractCompleted);
        _eventBus.Subscribe<ContractFailedEvent>(OnContractFailed);
        _eventBus.Subscribe<ContractExpiredEvent>(OnContractExpired);
        _eventBus.Subscribe<SalaryPaidEvent>(OnSalaryPaid);
        _eventBus.Subscribe<CandidateFollowUpEvent>(OnCandidateFollowUp);
        _eventBus.Subscribe<CandidateWithdrewEvent>(OnCandidateWithdrew);
        _eventBus.Subscribe<CandidateDeclinedEvent>(OnCandidateDeclined);
        _eventBus.Subscribe<CandidateHardRejectedEvent>(OnCandidateHardRejected);
        _eventBus.Subscribe<InterviewFinalReportEvent>(OnInterviewFinalReport);
        _eventBus.Subscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReadyForReview);
        _eventBus.Subscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);
        _eventBus.Subscribe<ContractRenewalRequestedEvent>(OnContractRenewalRequested);
        _eventBus.Subscribe<TaxReminderEvent>(OnTaxReminder);
        _eventBus.Subscribe<TaxDueEvent>(OnTaxDue);
        _eventBus.Subscribe<TaxOverdueEvent>(OnTaxOverdue);
        _eventBus.Subscribe<TaxBankruptcyEvent>(OnTaxBankruptcyWarning);
        _eventBus.Subscribe<ProductCrisisEvent>(OnProductCrisisEvent);
        // TODO: Subscribe<ContractDeadlineWarningEvent>(OnContractDeadlineWarning) when event is added
        // TODO: Subscribe<LowCashWarningEvent>(OnLowCashWarning) when event is added
    }

    public void Dispose()
    {
        if (_eventBus == null) return;

        _eventBus.Unsubscribe<CandidatesGeneratedEvent>(OnCandidatesGenerated);
        _eventBus.Unsubscribe<ContractCompletedEvent>(OnContractCompleted);
        _eventBus.Unsubscribe<ContractFailedEvent>(OnContractFailed);
        _eventBus.Unsubscribe<ContractExpiredEvent>(OnContractExpired);
        _eventBus.Unsubscribe<SalaryPaidEvent>(OnSalaryPaid);
        _eventBus.Unsubscribe<CandidateFollowUpEvent>(OnCandidateFollowUp);
        _eventBus.Unsubscribe<CandidateWithdrewEvent>(OnCandidateWithdrew);
        _eventBus.Unsubscribe<CandidateDeclinedEvent>(OnCandidateDeclined);
        _eventBus.Unsubscribe<CandidateHardRejectedEvent>(OnCandidateHardRejected);
        _eventBus.Unsubscribe<InterviewFinalReportEvent>(OnInterviewFinalReport);
        _eventBus.Unsubscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReadyForReview);
        _eventBus.Unsubscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);
        _eventBus.Unsubscribe<ContractRenewalRequestedEvent>(OnContractRenewalRequested);
        _eventBus.Unsubscribe<TaxReminderEvent>(OnTaxReminder);
        _eventBus.Unsubscribe<TaxDueEvent>(OnTaxDue);
        _eventBus.Unsubscribe<TaxOverdueEvent>(OnTaxOverdue);
        _eventBus.Unsubscribe<TaxBankruptcyEvent>(OnTaxBankruptcyWarning);
        _eventBus.Unsubscribe<ProductCrisisEvent>(OnProductCrisisEvent);
    }

    public void DismissItem(int mailId)
    {
        if (_state == null) return;
        int count = _state.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = _state.Items[i];
            if (item.Id == mailId)
            {
                item.IsDismissed = true;
                item.IsRead = true;
                _state.Items[i] = item;
                Invalidate();
                return;
            }
        }
    }

    public void DismissAll()
    {
        if (_state == null) return;
        int count = _state.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = _state.Items[i];
            item.IsDismissed = true;
            item.IsRead = true;
            _state.Items[i] = item;
        }
        Invalidate();
    }

    // Marks a single item read. Overload: if mailId is null, marks all read.
    public void MarkRead(int? mailId)
    {
        if (mailId.HasValue)
            MarkRead(mailId.Value);
        else
            MarkAllRead();
    }

    public void MarkRead(int mailId)
    {
        if (_state == null) return;
        int count = _state.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = _state.Items[i];
            if (item.Id == mailId)
            {
                item.IsRead = true;
                _state.Items[i] = item;
                Invalidate();
                return;
            }
        }
    }

    public void MarkAllRead()
    {
        if (_state == null) return;
        int count = _state.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = _state.Items[i];
            if (!item.IsDismissed)
            {
                item.IsRead = true;
                _state.Items[i] = item;
            }
        }
        Invalidate();
    }

    private void Invalidate()
    {
        _unreadCount = -1;
        _criticalUnreadCount = -1;
    }

    private void ComputeCounts()
    {
        _unreadCount = 0;
        _criticalUnreadCount = 0;
        if (_state == null) return;
        int total = _state.Items.Count;
        for (int i = 0; i < total; i++)
        {
            var item = _state.Items[i];
            if (item.IsDismissed || item.IsRead) continue;
            _unreadCount++;
            if (item.Priority == MailPriority.Critical)
                _criticalUnreadCount++;
        }
    }

    public void AddMail(MailItem item)
    {
        Prepend(item);
    }

    public void PurgeExpiredMessages(int currentTick, int expiryTicks)
    {
        if (_state == null) return;
        var items = _state.Items;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item.IsRead || item.IsDismissed)
            {
                if ((currentTick - item.Tick) > expiryTicks)
                    items.RemoveAt(i);
            }
        }
        Invalidate();
    }

    private void Prepend(MailItem item)
    {
        if (_state == null) return;
        item.Id = _nextId++;
        _state.AddItem(item);
        Invalidate();
    }

    public void AddMonthlyNewsReport(int tick, MonthlyNewsReport report)
    {
        string monthName = UIFormatting.FormatMonthName(report.ReportMonth);
        Prepend(new MailItem {
            Tick = tick,
            Category = MailCategory.NewsArticle,
            Priority = MailPriority.Info,
            Title = monthName + " " + report.ReportYear + " Market Report",
            Body = "Monthly market report with trends, rankings, and industry events.",
            AttachedReport = report,
            Actions = new MailAction[0]
        });
    }

    private static MailAction Nav(string label, ScreenId target)
    {
        var action = new MailAction { Label = label, Type = MailActionType.Navigate, ModalKey = null };
        action.NavTarget = target;
        return action;
    }

    private static MailAction Modal(string label, string modalKey)
    {
        var action = new MailAction { Label = label, Type = MailActionType.OpenModal, ModalKey = modalKey };
        action.NavTargetInt = -1;
        return action;
    }

    // --- Event handlers ---

    private void OnCandidatesGenerated(CandidatesGeneratedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Recruitment,
            Priority = MailPriority.Info,
            Title = "New Candidates Available",
            Body = $"{evt.Count} candidates are ready to interview",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnContractCompleted(ContractCompletedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Contract,
            Priority = MailPriority.Info,
            Title = "Contract Completed",
            Body = $"Earned {MoneyFormatter.FormatShort(evt.Reward)}",
            Actions = new[] { Nav("View Contracts", ScreenId.ProductionContracts) }
        });
    }

    private void OnContractFailed(ContractFailedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Warning,
            Title = "Contract Failed",
            Body = $"{evt.ContractName}: {evt.Reason}",
            Actions = new[] { Nav("View Contracts", ScreenId.ProductionContracts) }
        });
    }

    private void OnContractExpired(ContractExpiredEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Warning,
            Title = "Contract Expired",
            Body = $"Contract #{evt.ContractId.Value} has expired",
            Actions = new[] { Nav("View Contracts", ScreenId.ProductionContracts) }
        });
    }

    private void OnSalaryPaid(SalaryPaidEvent evt)
    {
        int ticksPerDay = TimeState.TicksPerDay;
        bool throttled = (evt.Tick - _state.LastSalaryMailTick) < (7 * ticksPerDay);
        bool lowCash = evt.CashAfterPayment < evt.TotalAmount * 2;

        if (throttled && !lowCash) return;

        var category = lowCash ? MailCategory.Alert : MailCategory.Finance;
        var priority = lowCash ? MailPriority.Critical : MailPriority.Info;
        string title = lowCash ? "Low Cash Warning" : "Salaries Paid";
        string body = lowCash
            ? $"Monthly payroll: {MoneyFormatter.FormatShort(evt.TotalAmount)} — cash is critically low"
            : $"Monthly payroll: {MoneyFormatter.FormatShort(evt.TotalAmount)} deducted";

        _state.LastSalaryMailTick = evt.Tick;

        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = category,
            Priority = priority,
            Title = title,
            Body = body,
            Actions = new[] { Nav("View Finance", ScreenId.FinanceOverview) }
        });
    }

    private void OnCandidateFollowUp(CandidateFollowUpEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Candidate Following Up",
            Body = $"{evt.CandidateName} is waiting for a decision. They will withdraw in 3 days.",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnCandidateDeclined(CandidateDeclinedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Offer Declined",
            Body = $"{evt.CandidateName} declined your offer. {evt.ConditionText}. You have 7 days to re-offer.",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnCandidateWithdrew(CandidateWithdrewEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Candidate Withdrew",
            Body = $"{evt.CandidateName} accepted another offer.",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnCandidateHardRejected(CandidateHardRejectedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Candidate Closed Door",
            Body = "A candidate has exhausted their patience and will no longer consider offers.",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnInterviewFinalReport(InterviewFinalReportEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "Interview Complete",
            Body = $"{evt.CandidateName} is ready for an offer.",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnHRCandidatesReadyForReview(HRCandidatesReadyForReviewEvent evt)
    {
        string modalKey = "HRCandidateReview:"
            + string.Join(",", evt.CandidateIds)
            + ":" + evt.TeamId.Value
            + ":" + evt.CriteriaLabel;

        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "HR Search Complete",
            Body = $"{evt.CandidateCount} candidate(s) found by {evt.TeamName}. Ready for review.",
            Actions = new[]
            {
                Modal("Review Candidates", modalKey),
                Nav("Go to HR", ScreenId.HRCandidates)
            }
        });
    }

    private void OnTeamIdleMoraleAlert(TeamIdleMoraleAlertEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Operations,
            Priority = MailPriority.Warning,
            Title = "Team Morale Warning",
            Body = $"{evt.TeamName} is losing morale due to having no assigned contract.",
            Actions = new[] { Nav("View Contracts", ScreenId.ProductionContracts) }
        });
    }

    public void AddNewsArticle(HypeEventType eventType, ProductId productId, string productName,
        string headline, string body, int tick, bool wasMitigated = false)
    {
        Prepend(new MailItem
        {
            Tick = tick,
            Category = MailCategory.NewsArticle,
            Priority = MailPriority.Info,
            Title = headline,
            Body = body,
            EventType = eventType,
            RelatedProductId = productId,
            Actions = new[] { Nav("View Products", ScreenId.ProductionProductsLive) }
        });

        _eventBus?.Raise(new NewsArticleAddedEvent(
            tick, eventType, productId, productName, headline, wasMitigated));
    }

    private void OnContractRenewalRequested(ContractRenewalRequestedEvent evt)
    {
        int graceDays = (evt.DeadlineTick - evt.Tick) / TimeState.TicksPerDay;
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Contract Renewal Request",
            Body = $"{evt.EmployeeName} wants a new contract at {MoneyFormatter.FormatShort(evt.Demand)}/mo. They will leave in {graceDays} days if ignored.",
            Actions = new[] { Nav("View Employees", ScreenId.HREmployees) }
        });
    }

    private void OnTaxReminder(TaxReminderEvent evt)
    {
        if (evt.IsFinalWarning)
        {
            Prepend(new MailItem
            {
                Tick = evt.Tick,
                Category = MailCategory.Alert,
                Priority = MailPriority.Critical,
                Title = "BANKRUPTCY WARNING: Unpaid Tax",
                Body = $"Pay your overdue tax of {UIFormatting.FormatMoney(evt.EstimatedTaxOwed)} immediately or face bankruptcy in {evt.DaysUntilDue} days.",
                Actions = new[] { Nav("View Finances", ScreenId.FinanceOverview) }
            });
        }
        else if (evt.DaysUntilDue <= 7)
        {
            Prepend(new MailItem
            {
                Tick = evt.Tick,
                Category = MailCategory.Finance,
                Priority = MailPriority.Critical,
                Title = "URGENT: Tax Due Soon",
                Body = $"Your tax payment of {UIFormatting.FormatMoney(evt.EstimatedTaxOwed)} is due in {evt.DaysUntilDue} days.",
                Actions = new[] { Nav("View Finances", ScreenId.FinanceOverview) }
            });
        }
        else
        {
            Prepend(new MailItem
            {
                Tick = evt.Tick,
                Category = MailCategory.Finance,
                Priority = MailPriority.Warning,
                Title = "Tax Reminder",
                Body = $"Your tax payment of {UIFormatting.FormatMoney(evt.EstimatedTaxOwed)} is due in {evt.DaysUntilDue} days.",
                Actions = new[] { Nav("View Finances", ScreenId.FinanceOverview) }
            });
        }
    }

    private void OnTaxDue(TaxDueEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Finance,
            Priority = MailPriority.Critical,
            Title = "Tax Payment Due",
            Body = $"Your annual tax of {UIFormatting.FormatMoney(evt.TaxOwed)} is now due. Pay from the Tax Report screen.",
            Actions = new[] { Nav("View Finances", ScreenId.FinanceOverview) }
        });
    }

    private void OnTaxOverdue(TaxOverdueEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Critical,
            Title = "Late Tax Fee Applied",
            Body = $"A late fee of {UIFormatting.FormatMoney(evt.LateFees)} has been added. Total owed: {UIFormatting.FormatMoney(evt.TaxOwed + evt.LateFees)}. {evt.MonthsOverdue} month(s) overdue.",
            Actions = new[] { Nav("View Finances", ScreenId.FinanceOverview) }
        });
    }

    private void OnTaxBankruptcyWarning(TaxBankruptcyEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Critical,
            Title = "BANKRUPTCY: Unpaid Taxes",
            Body = $"Your company has been declared bankrupt due to unpaid taxes of {UIFormatting.FormatMoney(evt.UnpaidAmount)}."
        });
    }

    private void OnProductCrisisEvent(ProductCrisisEvent evt)
    {
        if (evt.ProductId == default) return;

        string name = evt.ProductName ?? "Unknown Product";
        string title;
        string body;
        MailPriority priority;

        switch (evt.CrisisType)
        {
            case CrisisEventType.Catastrophic:
                title = "CATASTROPHIC FAILURE — " + name;
                body = name + " is failing catastrophically. Users are abandoning rapidly and a 50% revenue penalty is now active. Assign an emergency QA team and fund the maintenance budget immediately or the product may become unrecoverable.";
                priority = MailPriority.Critical;
                break;
            case CrisisEventType.ModerateBreach:
                title = "Security Breach — " + name;
                body = "A security vulnerability has been detected in " + name + ". User trust is declining and churn is accelerating. Increase the maintenance budget and assign a QA team to contain the damage.";
                priority = MailPriority.Warning;
                break;
            default:
                title = "Bug Spike — " + name;
                body = "Bug reports are increasing for " + name + " due to insufficient maintenance coverage. Assign a QA team or increase the maintenance budget to prevent escalation to a full security breach.";
                priority = MailPriority.Info;
                break;
        }

        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Alert,
            Priority = priority,
            Title = title,
            Body = body,
            RelatedProductId = evt.ProductId,
            Actions = new[] { Nav("View Products", ScreenId.ProductionProductsLive) }
        });
    }
}
