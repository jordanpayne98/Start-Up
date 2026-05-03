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
        _eventBus.Subscribe<InterviewThresholdEvent>(OnInterviewThresholdReached);
        _eventBus.Subscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReadyForReview);
        _eventBus.Subscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);
        _eventBus.Subscribe<ContractRenewalRequestedEvent>(OnContractRenewalRequested);
        _eventBus.Subscribe<TaxReminderEvent>(OnTaxReminder);
        _eventBus.Subscribe<TaxDueEvent>(OnTaxDue);
        _eventBus.Subscribe<TaxOverdueEvent>(OnTaxOverdue);
        _eventBus.Subscribe<TaxBankruptcyEvent>(OnTaxBankruptcyWarning);
        _eventBus.Subscribe<ProductCrisisEvent>(OnProductCrisisEvent);
        // Renewal notifications
        _eventBus.Subscribe<RenewalWindowOpenedEvent>(OnRenewalWindowOpened);
        _eventBus.Subscribe<RenewalChangeRequestedEvent>(OnRenewalChangeRequested);
        _eventBus.Subscribe<RenewalEscalationEvent>(OnRenewalEscalation);
        _eventBus.Subscribe<RenewalRequestRejectedEvent>(OnRenewalRequestRejected);
        _eventBus.Subscribe<EmployeeDepartedEvent>(OnEmployeeDeparted);
        _eventBus.Subscribe<ContractRenewedEvent>(OnContractRenewed);
        // Hiring notifications
        _eventBus.Subscribe<CandidatePoolFullEvent>(OnCandidatePoolFull);
        _eventBus.Subscribe<EmployeeHiredEvent>(OnEmployeeHired);
        // Negotiation notifications
        _eventBus.Subscribe<CounterOfferReceivedEvent>(OnCounterOfferReceived);
        _eventBus.Subscribe<PatienceLowEvent>(OnPatienceLow);
        _eventBus.Subscribe<CandidateLostPatienceEvent>(OnCandidateLostPatience);
        _eventBus.Subscribe<EmployeeFrustratedEvent>(OnEmployeeFrustrated);
        _eventBus.Subscribe<EmployeeCooldownExpiredEvent>(OnEmployeeCooldownExpired);
        _eventBus.Subscribe<CounterOfferExpiredEvent>(OnCounterOfferExpired);
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
        _eventBus.Unsubscribe<InterviewThresholdEvent>(OnInterviewThresholdReached);
        _eventBus.Unsubscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReadyForReview);
        _eventBus.Unsubscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);
        _eventBus.Unsubscribe<ContractRenewalRequestedEvent>(OnContractRenewalRequested);
        _eventBus.Unsubscribe<TaxReminderEvent>(OnTaxReminder);
        _eventBus.Unsubscribe<TaxDueEvent>(OnTaxDue);
        _eventBus.Unsubscribe<TaxOverdueEvent>(OnTaxOverdue);
        _eventBus.Unsubscribe<TaxBankruptcyEvent>(OnTaxBankruptcyWarning);
        _eventBus.Unsubscribe<ProductCrisisEvent>(OnProductCrisisEvent);
        _eventBus.Unsubscribe<RenewalWindowOpenedEvent>(OnRenewalWindowOpened);
        _eventBus.Unsubscribe<RenewalChangeRequestedEvent>(OnRenewalChangeRequested);
        _eventBus.Unsubscribe<RenewalEscalationEvent>(OnRenewalEscalation);
        _eventBus.Unsubscribe<RenewalRequestRejectedEvent>(OnRenewalRequestRejected);
        _eventBus.Unsubscribe<EmployeeDepartedEvent>(OnEmployeeDeparted);
        _eventBus.Unsubscribe<ContractRenewedEvent>(OnContractRenewed);
        _eventBus.Unsubscribe<CandidatePoolFullEvent>(OnCandidatePoolFull);
        _eventBus.Unsubscribe<EmployeeHiredEvent>(OnEmployeeHired);
        // Negotiation notifications
        _eventBus.Unsubscribe<CounterOfferReceivedEvent>(OnCounterOfferReceived);
        _eventBus.Unsubscribe<PatienceLowEvent>(OnPatienceLow);
        _eventBus.Unsubscribe<CandidateLostPatienceEvent>(OnCandidateLostPatience);
        _eventBus.Unsubscribe<EmployeeFrustratedEvent>(OnEmployeeFrustrated);
        _eventBus.Unsubscribe<EmployeeCooldownExpiredEvent>(OnEmployeeCooldownExpired);
        _eventBus.Unsubscribe<CounterOfferExpiredEvent>(OnCounterOfferExpired);
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

    public void DismissMailByModalKey(string modalKeyPrefix)
    {
        if (_state == null || string.IsNullOrEmpty(modalKeyPrefix)) return;
        int count = _state.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var item = _state.Items[i];
            if (item.IsDismissed) continue;
            if (item.Actions == null) continue;
            int actionCount = item.Actions.Length;
            for (int j = 0; j < actionCount; j++)
            {
                if (item.Actions[j].ModalKey != null && item.Actions[j].ModalKey.StartsWith(modalKeyPrefix))
                {
                    item.IsDismissed = true;
                    item.IsRead = true;
                    _state.Items[i] = item;
                    break;
                }
            }
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
        var action = new MailAction { Label = label, Type = MailActionType.Navigate, ModalKey = null, TabHint = -1 };
        action.NavTarget = target;
        return action;
    }

    private static MailAction NavTab(string label, ScreenId target, int tabHint)
    {
        var action = new MailAction { Label = label, Type = MailActionType.Navigate, ModalKey = null, TabHint = tabHint };
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
            Body = $"{evt.Count} new candidates in the hiring pool.",
            Actions = new[] { NavTab("View Candidates", ScreenId.HRCandidates, (int)HRTab.Candidates) }
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
            Actions = new[] { NavTab("View Employees", ScreenId.HRCandidates, (int)HRTab.Employees) }
        });
    }

    private void OnCandidateDeclined(CandidateDeclinedEvent evt)
    {
        string body = evt.Reason switch
        {
            DeclineReason.SalaryTooLow        => $"{evt.CandidateName} declined your offer — the salary was below their expectation. You have 7 days to re-offer.",
            DeclineReason.ArrangementMismatch => $"{evt.CandidateName} declined your offer — the arrangement doesn't match their preferences. You have 7 days to re-offer.",
            _                                 => $"{evt.CandidateName} declined your offer. You have 7 days to re-offer."
        };
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Offer Declined",
            Body = body,
            Actions = new[] { NavTab("View Candidates", ScreenId.HRCandidates, (int)HRTab.Candidates) }
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
            Actions = new[] { NavTab("View Employees", ScreenId.HRCandidates, (int)HRTab.Employees) }
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
            Actions = new[] { NavTab("View Employees", ScreenId.HRCandidates, (int)HRTab.Employees) }
        });
    }

    private void OnInterviewThresholdReached(InterviewThresholdEvent evt)
    {
        if (evt.ThresholdReached == 100)
        {
            Prepend(new MailItem
            {
                Tick = evt.Tick,
                Category = MailCategory.HR,
                Priority = MailPriority.Info,
                Title = "Interview Complete",
                Body = $"{evt.CandidateName}'s interview is complete. Preferences and exact salary expectation are now known. Ready for an offer.",
                Actions = new[] { NavTab("View Candidates", ScreenId.HRCandidates, (int)HRTab.Candidates) }
            });
        }
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
                NavTab("Go to HR", ScreenId.HRCandidates, (int)HRTab.Candidates)
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
        string modalKey = "ContractRenewal:" + evt.EmployeeId.Value;
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Contract Renewal Request",
            Body = $"{evt.EmployeeName} wants a new contract at {MoneyFormatter.FormatShort(evt.Demand)}/mo. They will leave in {graceDays} days if ignored.",
            Actions = new[] { Modal("Review Renewals", modalKey) }
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

    // R1
    private void OnRenewalWindowOpened(RenewalWindowOpenedEvent evt)
    {
        string typeStr = evt.CurrentType == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "Contract Expiring Soon",
            Body = $"{evt.Name}'s {typeStr} contract expires in {evt.DaysUntilExpiry} days. Review their renewal terms.",
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // R2
    private void OnRenewalChangeRequested(RenewalChangeRequestedEvent evt)
    {
        string body;
        if (evt.RequestsTypeChange && evt.RequestsLengthChange)
        {
            string typeStr = evt.RequestedType == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
            string lengthStr = ContractLengthToDisplay(evt.RequestedLength);
            body = $"{evt.Name} is requesting a change to {typeStr} and a {lengthStr} contract length for their renewal.";
        }
        else if (evt.RequestsTypeChange)
        {
            string typeStr = evt.RequestedType == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
            body = $"{evt.Name} is requesting a switch to {typeStr} for their renewal.";
        }
        else
        {
            string lengthStr = ContractLengthToDisplay(evt.RequestedLength);
            body = $"{evt.Name} is requesting a {lengthStr} contract length for their renewal.";
        }
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Renewal Request: " + evt.Name,
            Body = body,
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // R3 / R4
    private void OnRenewalEscalation(RenewalEscalationEvent evt)
    {
        if (evt.IsFinalStrike)
        {
            Prepend(new MailItem
            {
                Tick = evt.Tick,
                Category = MailCategory.Alert,
                Priority = MailPriority.Critical,
                Title = evt.Name + " Refusing Renewal",
                Body = $"{evt.Name} has refused to renew their contract. Their contract will expire without renewal unless immediate action is taken.",
                Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
            });
        }
        else
        {
            Prepend(new MailItem
            {
                Tick = evt.Tick,
                Category = MailCategory.HR,
                Priority = MailPriority.Warning,
                Title = "Urgent: " + evt.Name + " Demands Change",
                Body = $"{evt.Name} is increasingly dissatisfied with their current arrangement. Failure to address their renewal request may result in them refusing to renew.",
                Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
            });
        }
    }

    // R5
    private void OnRenewalRequestRejected(RenewalRequestRejectedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = evt.Name + ": High Departure Risk",
            Body = $"{evt.Name} is dissatisfied with their current arrangement. Ignoring their renewal preferences again may lead to them refusing renewal entirely.",
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // R6
    private void OnEmployeeDeparted(EmployeeDepartedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.Alert,
            Priority = MailPriority.Warning,
            Title = evt.Name + " Has Left the Company",
            Body = $"{evt.Name}'s contract expired without renewal and they have left the company.",
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // R7
    private void OnContractRenewed(ContractRenewedEvent evt)
    {
        string body = evt.NewSalary != evt.OldSalary
            ? $"Their contract has been renewed. Salary adjusted from {MoneyFormatter.FormatShort(evt.OldSalary)}/mo to {MoneyFormatter.FormatShort(evt.NewSalary)}/mo."
            : $"Their contract has been renewed at {MoneyFormatter.FormatShort(evt.NewSalary)}/mo with no changes.";
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "Contract Renewed",
            Body = body,
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // H2
    private void OnCandidatePoolFull(CandidatePoolFullEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Candidate Pool Full",
            Body = $"Your HR team found candidates, but the pool is full ({evt.PoolCount}/{evt.PoolMax}). Hire or dismiss existing candidates to make room.",
            Actions = new[] { NavTab("View Candidates", ScreenId.HRCandidates, (int)HRTab.Candidates) }
        });
    }

    // H4
    private void OnEmployeeHired(EmployeeHiredEvent evt)
    {
        string typeStr = evt.Type == EmploymentType.FullTime ? "Full-Time" : "Part-Time";
        string lengthStr = ContractLengthToDisplay(evt.Length);
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "Welcome: " + evt.Name,
            Body = $"{evt.Name} accepted your offer. Hired as {typeStr}, {lengthStr} contract at {MoneyFormatter.FormatShort(evt.Salary)}/mo.",
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // N1 — Counter-offer received
    private void OnCounterOfferReceived(CounterOfferReceivedEvent evt)
    {
        string modalKey = "CounterOffer:" + evt.CandidateId;
        DismissMailByModalKey(modalKey);
        string roleName = RecommendationLabelBuilder.RoleLabel(evt.Counter.CounterRole);
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Counter Offer Received",
            Body = $"{evt.CandidateName} has counter-offered: {MoneyFormatter.FormatShort(evt.Counter.CounterSalary)}/mo as {roleName}",
            Actions = new[] { Modal("View Counter Offer", modalKey) }
        });
    }

    // N2 — Patience low
    private void OnPatienceLow(PatienceLowEvent evt)
    {
        string title = evt.IsEmployee ? "Employee Losing Patience" : "Candidate Losing Patience";
        string body = $"{evt.Name} is losing patience. {evt.RemainingPatience} round(s) left.";
        MailAction action = evt.IsEmployee
            ? NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees)
            : NavTab("View Candidates", ScreenId.HRCandidates, (int)HRTab.Candidates);
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = title,
            Body = body,
            Actions = new[] { action }
        });
    }

    // N3 — Candidate lost patience
    private void OnCandidateLostPatience(CandidateLostPatienceEvent evt)
    {
        DismissMailByModalKey("CounterOffer:" + evt.CandidateId);
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Candidate Left",
            Body = $"{evt.CandidateName} has lost patience and left the hiring pool.",
            Actions = new MailAction[0]
        });
    }

    // N4 — Employee frustrated / cooldown started
    private void OnEmployeeFrustrated(EmployeeFrustratedEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Warning,
            Title = "Employee Frustrated",
            Body = $"{evt.EmployeeName} won't discuss renewal for 30 days.",
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // N5 — Employee cooldown expired
    private void OnEmployeeCooldownExpired(EmployeeCooldownExpiredEvent evt)
    {
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "Employee Ready to Negotiate",
            Body = $"{evt.EmployeeName} is willing to discuss renewal terms again.",
            Actions = new[] { NavTab("View Employees", ScreenId.HREmployees, (int)HRTab.Employees) }
        });
    }

    // N6 — Counter-offer expired (without patience exhaustion)
    private void OnCounterOfferExpired(CounterOfferExpiredEvent evt)
    {
        DismissMailByModalKey("CounterOffer:" + evt.CandidateId);
        Prepend(new MailItem
        {
            Tick = evt.Tick,
            Category = MailCategory.HR,
            Priority = MailPriority.Info,
            Title = "Counter Offer Expired",
            Body = $"{evt.CandidateName}'s counter-offer has expired.",
            Actions = new MailAction[0]
        });
    }

    private static string ContractLengthToDisplay(ContractLengthOption length)
    {
        switch (length)
        {
            case ContractLengthOption.Short:    return "Short";
            case ContractLengthOption.Long:     return "Long";
            default:                            return "Standard";
        }
    }

    /// <summary>
    /// Sends friendly tutorial-style startup messages based on company background and missing role families.
    /// Called once at game start after founders are created.
    /// </summary>
    public void SendStartupMessages(
        string companyName,
        CompanyBackgroundDefinition background,
        RoleFamily[] missingFamilies,
        int currentTick)
    {
        // Welcome message — friendly, not critical
        string welcomeBody = BuildWelcomeBody(companyName, background);
        Prepend(new MailItem
        {
            Tick     = currentTick,
            Category = MailCategory.Operations,
            Priority = MailPriority.Info,
            Title    = $"Welcome to {companyName}",
            Body     = welcomeBody,
            Actions  = new MailAction[0]
        });

        // Hiring hint if there are uncovered role families
        if (missingFamilies != null && missingFamilies.Length > 0)
        {
            string missingFamilyName = RoleFamilyDisplayName(missingFamilies[0]);
            string hintBody = BuildHiringHintBody(background, missingFamilyName);
            Prepend(new MailItem
            {
                Tick     = currentTick,
                Category = MailCategory.Recruitment,
                Priority = MailPriority.Info,
                Title    = "Hiring Suggestion",
                Body     = hintBody,
                Actions  = new[] { NavTab("View Candidates", ScreenId.HRCandidates, (int)HRTab.Candidates) }
            });
        }

        // First action hint from background definition
        if (background != null
            && background.SuggestedFirstActions != null
            && background.SuggestedFirstActions.Length > 0)
        {
            Prepend(new MailItem
            {
                Tick     = currentTick,
                Category = MailCategory.Operations,
                Priority = MailPriority.Info,
                Title    = "First Steps",
                Body     = background.SuggestedFirstActions[0],
                Actions  = new[] { Nav("View Contracts", ScreenId.ProductionContracts) }
            });
        }
    }

    private static string BuildWelcomeBody(string companyName, CompanyBackgroundDefinition background)
    {
        if (background != null && !string.IsNullOrEmpty(background.WelcomeMessage))
            return $"{companyName} is ready. {background.WelcomeMessage}";

        return $"{companyName} is ready. Check the candidate pool and pick up your first contract to get started.";
    }

    private static string BuildHiringHintBody(CompanyBackgroundDefinition background, string missingFamilyName)
    {
        if (background != null && !string.IsNullOrEmpty(background.HiringHint))
            return background.HiringHint;

        return $"Consider hiring a {missingFamilyName} specialist to cover gaps in your founding team's skills.";
    }

    private static string RoleFamilyDisplayName(RoleFamily family)
    {
        switch (family)
        {
            case RoleFamily.Engineering:       return "Engineering";
            case RoleFamily.Hardware:          return "Hardware";
            case RoleFamily.Product:           return "Product";
            case RoleFamily.Creative:          return "Creative";
            case RoleFamily.QualityAndSupport: return "Quality & Support";
            case RoleFamily.Commercial:        return "Commercial";
            case RoleFamily.Operations:        return "Operations";
            default:                           return family.ToString();
        }
    }
}
