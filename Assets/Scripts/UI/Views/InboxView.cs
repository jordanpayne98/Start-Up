using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InboxView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IModalPresenter _modal;
    private readonly INavigationService _nav;
    private VisualElement _root;
    private VisualElement _filterBar;
    private VisualElement _messageList;
    private Label _countLabel;
    private ElementPool _messagePool;
    private InboxViewModel _viewModel;
    private readonly List<Button> _filterButtons = new List<Button>();
    private Button _markAllReadBtn;

    // Max pre-created action buttons per inbox row
    private const int MaxActionsPerRow = 3;

    // Cached filter enum array — avoids per-Bind allocation
    private static readonly InboxFilter[] _filterEnums =
    {
        InboxFilter.All, InboxFilter.Unread, InboxFilter.Alert,
        InboxFilter.Contract, InboxFilter.Recruitment, InboxFilter.HR,
        InboxFilter.Finance, InboxFilter.Technology, InboxFilter.Operations,
        InboxFilter.News
    };

    // Priority indicator colours
    private static readonly Color _colourCriticalUnread  = new Color(0.90f, 0.22f, 0.27f, 1f);
    private static readonly Color _colourWarningUnread   = new Color(0.95f, 0.65f, 0.10f, 1f);
    private static readonly Color _colourInfoUnread      = new Color(0.30f, 0.79f, 0.69f, 1f);
    private static readonly Color _colourRead            = new Color(0.40f, 0.40f, 0.40f, 0.3f);

    public InboxView(ICommandDispatcher dispatcher, IModalPresenter modal, INavigationService nav)
    {
        _dispatcher = dispatcher;
        _modal = modal;
        _nav = nav;
    }

    public void Initialize(VisualElement root)
    {
        _root = root;

        // Header
        var header = new VisualElement();
        header.AddToClassList("flex-row");
        header.AddToClassList("justify-between");
        header.AddToClassList("align-center");
        header.style.marginBottom = 12;

        var title = new Label("Inbox");
        title.AddToClassList("section-header");
        header.Add(title);

        var headerRight = new VisualElement();
        headerRight.AddToClassList("flex-row");
        headerRight.AddToClassList("align-center");

        _countLabel = new Label("0 messages");
        _countLabel.AddToClassList("text-muted");
        _countLabel.style.marginRight = 12;
        headerRight.Add(_countLabel);

        _markAllReadBtn = new Button { text = "Mark All Read" };
        _markAllReadBtn.AddToClassList("btn");
        _markAllReadBtn.AddToClassList("btn--sm");
        _markAllReadBtn.clicked += OnMarkAllReadClicked;
        headerRight.Add(_markAllReadBtn);

        header.Add(headerRight);

        _root.Add(header);

        // Filter bar — 9 buttons
        _filterBar = new VisualElement();
        _filterBar.AddToClassList("flex-row");
        _filterBar.style.marginBottom = 12;
        _filterBar.style.flexWrap = Wrap.Wrap;

        var filterLabels = new[] { "All", "Unread", "Alerts", "Contract", "Recruit", "HR", "Finance", "Technology", "Ops", "News" };
        var filterEnums = new[]
        {
            InboxFilter.All, InboxFilter.Unread, InboxFilter.Alert,
            InboxFilter.Contract, InboxFilter.Recruitment, InboxFilter.HR,
            InboxFilter.Finance, InboxFilter.Technology, InboxFilter.Operations,
            InboxFilter.News
        };

        for (int i = 0; i < filterLabels.Length; i++)
        {
            var btn = new Button { text = filterLabels[i] };
            btn.AddToClassList("sub-tab");
            if (i == 0) btn.AddToClassList("sub-tab--active");

            var capturedFilter = filterEnums[i];
            btn.clicked += () => OnFilterClicked(capturedFilter);

            _filterBar.Add(btn);
            _filterButtons.Add(btn);
        }
        _root.Add(_filterBar);

        // Message list with scroll
        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        _messageList = scrollView.contentContainer;
        _messagePool = new ElementPool(CreateMessageItem, _messageList);
        _root.Add(scrollView);
    }

    public void Bind(IViewModel viewModel)
    {
        _viewModel = viewModel as InboxViewModel;
        if (_viewModel == null) return;

        if (_countLabel != null)
            _countLabel.text = _viewModel.TotalCount + " messages (" + _viewModel.UnreadCount + " unread)";

        // Update filter button active states (index-based match)
        int btnCount = _filterButtons.Count;
        for (int i = 0; i < btnCount; i++)
        {
            if (_filterEnums[i] == _viewModel.ActiveFilter)
                _filterButtons[i].AddToClassList("sub-tab--active");
            else
                _filterButtons[i].RemoveFromClassList("sub-tab--active");
        }

        _messagePool.UpdateList(_viewModel.FilteredMessages, BindMessageItem);
    }

    public void Dispose()
    {
        if (_markAllReadBtn != null) _markAllReadBtn.clicked -= OnMarkAllReadClicked;
        _viewModel = null;
        _messagePool = null;
        _markAllReadBtn = null;
    }

    private void OnFilterClicked(InboxFilter filter)
    {
        if (_viewModel == null) return;
        _viewModel.SetFilter(filter);
        Bind(_viewModel);
    }

    private VisualElement CreateMessageItem()
    {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.style.flexDirection = FlexDirection.Column;
        item.style.paddingTop = 0;
        item.style.paddingBottom = 0;

        // Header row — clickable
        var headerRow = new VisualElement();
        headerRow.name = "header-row";
        headerRow.AddToClassList("flex-row");
        headerRow.AddToClassList("align-center");
        headerRow.style.paddingTop = 8;
        headerRow.style.paddingBottom = 8;

        var indicator = new VisualElement();
        indicator.name = "read-indicator";
        indicator.style.width = 8;
        indicator.style.height = 8;
        indicator.style.borderTopLeftRadius = 4;
        indicator.style.borderTopRightRadius = 4;
        indicator.style.borderBottomLeftRadius = 4;
        indicator.style.borderBottomRightRadius = 4;
        indicator.style.marginRight = 10;
        indicator.style.flexShrink = 0;
        indicator.style.alignSelf = Align.Center;
        headerRow.Add(indicator);

        var categoryBadge = new Label();
        categoryBadge.name = "category-badge";
        categoryBadge.AddToClassList("badge");
        categoryBadge.style.marginRight = 10;
        categoryBadge.style.minWidth = 56;
        headerRow.Add(categoryBadge);

        var msgTitle = new Label();
        msgTitle.name = "msg-title";
        msgTitle.style.flexGrow = 1;
        headerRow.Add(msgTitle);

        var msgTime = new Label();
        msgTime.name = "msg-time";
        msgTime.AddToClassList("text-sm");
        msgTime.AddToClassList("text-muted");
        msgTime.style.minWidth = 60;
        msgTime.style.marginLeft = 8;
        headerRow.Add(msgTime);

        var chevron = new Label("▶");
        chevron.name = "expand-chevron";
        chevron.style.marginLeft = 8;
        chevron.style.width = 16;
        chevron.style.flexShrink = 0;
        headerRow.Add(chevron);

        item.Add(headerRow);

        // Detail container — hidden by default
        var detail = new VisualElement();
        detail.name = "detail-container";
        detail.style.display = DisplayStyle.None;
        detail.style.paddingLeft = 30;
        detail.style.paddingBottom = 8;

        var msgBody = new Label();
        msgBody.name = "msg-body";
        msgBody.AddToClassList("text-sm");
        msgBody.AddToClassList("text-muted");
        detail.Add(msgBody);

        var actionBar = new VisualElement();
        actionBar.name = "action-bar";
        actionBar.AddToClassList("flex-row");
        actionBar.style.marginTop = 6;
        detail.Add(actionBar);

        // Pre-create fixed pool of action buttons — wired once, rebound in Bind
        for (int i = 0; i < MaxActionsPerRow; i++) {
            var btn = new Button();
            btn.AddToClassList("btn");
            btn.AddToClassList("btn--sm");
            btn.style.marginRight = 6;
            btn.style.display = DisplayStyle.None;
            btn.RegisterCallback<ClickEvent>(OnActionButtonClicked);
            actionBar.Add(btn);
        }

        item.Add(detail);

        return item;
    }

    private readonly struct MailActionUserData
    {
        public readonly MailAction Action;
        public readonly int MailId;
        public MailActionUserData(MailAction action, int mailId) {
            Action = action;
            MailId = mailId;
        }
    }

    private void BindMessageItem(VisualElement el, InboxItemDisplay data)
    {
        var headerRow  = el.Q<VisualElement>("header-row");
        var indicator  = el.Q<VisualElement>("read-indicator");
        var badge      = el.Q<Label>("category-badge");
        var titleLabel = el.Q<Label>("msg-title");
        var timeLabel  = el.Q<Label>("msg-time");
        var chevron    = el.Q<Label>("expand-chevron");
        var detail     = el.Q<VisualElement>("detail-container");
        var bodyLabel  = el.Q<Label>("msg-body");
        var actionBar  = el.Q<VisualElement>("action-bar");

        // News article distinct styling
        if (data.IsNewsArticle) el.AddToClassList("news-article-item");
        else el.RemoveFromClassList("news-article-item");

        // Priority indicator colour
        if (indicator != null)
        {
            Color indicatorColor;
            if (data.IsRead)
                indicatorColor = _colourRead;
            else
            {
                switch (data.Priority)
                {
                    case MailPriority.Critical: indicatorColor = _colourCriticalUnread; break;
                    case MailPriority.Warning:  indicatorColor = _colourWarningUnread;  break;
                    default:                    indicatorColor = _colourInfoUnread;      break;
                }
            }
            indicator.style.backgroundColor = new StyleColor(indicatorColor);
        }

        if (badge != null) badge.text = UIFormatting.FormatMailCategory(data.Category);

        if (titleLabel != null)
        {
            titleLabel.text = data.Title;
            if (!data.IsRead) titleLabel.AddToClassList("text-bold");
            else titleLabel.RemoveFromClassList("text-bold");
        }

        if (timeLabel != null) timeLabel.text = data.Timestamp;
        if (chevron != null)   chevron.text = data.IsExpanded ? "▼" : "▶";
        if (detail != null)    detail.style.display = data.IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;

        if (bodyLabel != null)
        {
            var reportContainer = detail?.Q<VisualElement>("report-sections");

            if (data.IsNewsArticle && data.IsExpanded && data.AttachedReport != null)
            {
                bodyLabel.style.display = DisplayStyle.None;
                if (reportContainer == null)
                {
                    reportContainer = new VisualElement();
                    reportContainer.name = "report-sections";
                    detail.Insert(0, reportContainer);
                }
                reportContainer.Clear();
                RenderReportSections(reportContainer, data.AttachedReport);
            }
            else
            {
                bodyLabel.style.display = DisplayStyle.Flex;
                bodyLabel.text = data.Body;
                if (reportContainer != null)
                    reportContainer.Clear();
            }
        }

        // Rebind pre-created action buttons — show/hide, set userData
        if (actionBar != null)
        {
            var actions = data.Actions;
            int actionCount = actions != null ? actions.Length : 0;
            int btnCount = actionBar.childCount;
            for (int i = 0; i < btnCount; i++)
            {
                var btn = actionBar[i] as Button;
                if (btn == null) continue;
                if (i < actionCount)
                {
                    btn.text = actions[i].Label;
                    btn.userData = new MailActionUserData(actions[i], data.Id);
                    btn.style.display = DisplayStyle.Flex;
                }
                else
                {
                    btn.style.display = DisplayStyle.None;
                }
            }
        }

        // Header row click: toggle expand + mark read
        if (headerRow != null)
        {
            headerRow.UnregisterCallback<ClickEvent>(OnHeaderClicked);
            headerRow.userData = data;
            headerRow.RegisterCallback<ClickEvent>(OnHeaderClicked);
        }
    }

    private void OnActionButtonClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        if (btn?.userData is MailActionUserData ud)
            OnActionClicked(ud.Action, ud.MailId);
    }

    private void OnHeaderClicked(ClickEvent evt)
    {
        var el = evt.currentTarget as VisualElement;
        if (el?.userData is InboxItemDisplay data && _viewModel != null)
        {
            _viewModel.ToggleExpanded(data.Id);
            if (!data.IsRead)
            {
                _viewModel.OptimisticMarkRead(data.Id);
                _dispatcher?.Dispatch(new MarkMailReadCommand(_dispatcher.CurrentTick, data.Id));
            }
            Bind(_viewModel);
        }
    }

    private void OnMarkAllReadClicked()
    {
        if (_viewModel == null) return;
        _viewModel.OptimisticMarkAllRead();
        _dispatcher?.Dispatch(new MarkMailReadCommand(_dispatcher.CurrentTick, null));
        Bind(_viewModel);
    }

    private void OnActionClicked(MailAction action, int mailId)
    {
        if (_dispatcher == null) return;

        switch (action.Type)
        {
            case MailActionType.Navigate:
                var navTarget = action.NavTarget;
                if (navTarget.HasValue) {
                    if (action.TabHint >= 0)
                        _nav.NavigateTo(navTarget.Value, action.TabHint);
                    else
                        _nav.NavigateTo(navTarget.Value);
                }
                break;

            case MailActionType.OpenModal:
                ParseAndOpenModal(action.ModalKey);
                break;

            case MailActionType.DismissOnly:
                _dispatcher.Dispatch(new DismissMailCommand(_dispatcher.CurrentTick, mailId));
                if (_viewModel != null) Bind(_viewModel);
                break;
        }
    }

    private static void RenderReportSections(VisualElement container, MonthlyNewsReport report)
    {
        var reportTitle = new Label(UIFormatting.FormatMonthName(report.ReportMonth) + " " + report.ReportYear);
        reportTitle.AddToClassList("text-bold");
        reportTitle.style.marginBottom = 8;
        container.Add(reportTitle);

        if (report.NicheTrends != null && report.NicheTrends.Count > 0) {
            var section = CreateSection("Market Trends");
            int count = report.NicheTrends.Count;
            for (int i = 0; i < count; i++) {
                var entry = report.NicheTrends[i];
                var row = new Label(entry.Niche + ": " + UIFormatting.FormatMarketTrend(entry.Trend)
                    + " (" + UIFormatting.FormatPercent(entry.Demand) + ")");
                row.AddToClassList("text-sm");
                section.Add(row);
            }
            container.Add(section);
        }

        if (report.TopGainers != null && report.TopGainers.Count > 0) {
            var section = CreateSection("Top Gainers");
            int count = report.TopGainers.Count;
            for (int i = 0; i < count; i++) {
                var entry = report.TopGainers[i];
                var row = new Label(entry.CompanyName + "  +" + UIFormatting.FormatPercent(entry.MarketShareDelta));
                row.AddToClassList("text-sm");
                row.AddToClassList("text-success");
                section.Add(row);
            }
            container.Add(section);
        }

        if (report.TopLosers != null && report.TopLosers.Count > 0) {
            var section = CreateSection("Top Losers");
            int count = report.TopLosers.Count;
            for (int i = 0; i < count; i++) {
                var entry = report.TopLosers[i];
                var row = new Label(entry.CompanyName + "  " + UIFormatting.FormatPercent(entry.MarketShareDelta));
                row.AddToClassList("text-sm");
                row.AddToClassList("text-danger");
                section.Add(row);
            }
            container.Add(section);
        }

        if (report.TopProducts != null && report.TopProducts.Count > 0) {
            var section = CreateSection("Top Products");
            int count = report.TopProducts.Count;
            for (int i = 0; i < count; i++) {
                var entry = report.TopProducts[i];
                var row = new Label(entry.ProductName + " by " + entry.CompanyName
                    + " — +" + entry.NewUsers.ToString("N0") + " users");
                row.AddToClassList("text-sm");
                section.Add(row);
            }
            container.Add(section);
        }

        if (report.IndustryEvents != null && report.IndustryEvents.Count > 0) {
            var section = CreateSection("Industry Events");
            int count = report.IndustryEvents.Count;
            for (int i = 0; i < count; i++) {
                var row = new Label("• " + report.IndustryEvents[i]);
                row.AddToClassList("text-sm");
                section.Add(row);
            }
            container.Add(section);
        }
    }

    private static VisualElement CreateSection(string sectionTitle)
    {
        var section = new VisualElement();
        section.style.marginBottom = 12;

        var header = new Label(sectionTitle);
        header.AddToClassList("text-bold");
        header.style.marginBottom = 4;
        section.Add(header);

        var divider = new VisualElement();
        divider.AddToClassList("divider");
        divider.style.marginBottom = 4;
        section.Add(divider);

        return section;
    }

    private void ParseAndOpenModal(string modalKey)
    {
        if (string.IsNullOrEmpty(modalKey)) return;

        if (modalKey.StartsWith("ProductDetail:"))
        {
            var idStr = modalKey.Substring("ProductDetail:".Length);
            if (int.TryParse(idStr, out int rawId))
                _modal.OpenProductDetail(new ProductId(rawId));
        }

        if (modalKey == "ContractRenewal")
        {
            _modal.OpenRenewalModal();
        }

        if (modalKey.StartsWith("ContractRenewal:"))
        {
            var idStr = modalKey.Substring("ContractRenewal:".Length);
            if (int.TryParse(idStr, out int rawId))
                _modal.OpenRenewalModal(new EmployeeId(rawId));
        }

        if (modalKey.StartsWith("CounterOffer:"))
        {
            var idStr = modalKey.Substring("CounterOffer:".Length);
            if (int.TryParse(idStr, out int candidateId))
            {
                _nav.NavigateTo(ScreenId.HRCandidates, (int)HRTab.Candidates);
                _modal.ShowCandidateDetailModal(candidateId, showCounterOffer: true);
            }
        }
    }
}
