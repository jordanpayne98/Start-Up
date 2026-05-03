using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UIElements;

public class OverviewView : IGameView
{
    private VisualElement _root;

    // Finance widget
    private Label _moneyLabel;
    private Label _incomeLabel;
    private Label _expensesLabel;
    private VisualElement _moneyRow;
    private VisualElement _incomeRow;
    private VisualElement _expensesRow;

    // Stats widget
    private Label _employeeCountLabel;
    private Label _teamCountLabel;
    private Label _contractCountLabel;

    // Reputation widget
    private Label _reputationTierLabel;
    private Label _reputationScoreLabel;
    private VisualElement _reputationProgressFill;

    // Contracts list
    private VisualElement _contractsContainer;
    private ElementPool _contractPool;
    private VisualElement _contractsEmptyState;

    // Inbox feed
    private VisualElement _inboxContainer;
    private ElementPool _inboxPool;
    private VisualElement _inboxEmptyState;

    // Per-fill tracking for progress tweens
    private readonly Dictionary<VisualElement, float>   _fillPercents = new Dictionary<VisualElement, float>();
    private readonly Dictionary<VisualElement, Tweener> _fillTweeners = new Dictionary<VisualElement, Tweener>();

    // Stagger scratch list — reused, never allocated in Bind
    private readonly List<VisualElement> _staggerScratch = new List<VisualElement>();

    // Stagger guard
    private bool _hasAnimatedIn;

    private readonly ICommandDispatcher _dispatcher;
    private readonly ITooltipProvider _tooltipProvider;

    public OverviewView(ICommandDispatcher dispatcher, ITooltipProvider tooltipProvider) {
        _dispatcher = dispatcher;
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;

        // Build the dashboard layout
        var dashboard = new VisualElement();
        dashboard.name = "overview-dashboard";
        dashboard.AddToClassList("flex-row");
        dashboard.style.flexGrow = 1;

        // Left column: Inbox feed
        var leftCol = CreateColumn("overview-col-left");
        var inboxCard = CreateCard("Recent Messages");
        UICardHelper.ApplyBevel(inboxCard);
        UICardHelper.AddGradient(inboxCard);
        _inboxContainer = new VisualElement();
        _inboxContainer.name = "inbox-list";
        inboxCard.Add(_inboxContainer);
        _inboxEmptyState = UICardHelper.CreateEmptyState("✉", "No recent messages.");
        _inboxEmptyState.AddToClassList("empty-state--hidden");
        inboxCard.Add(_inboxEmptyState);
        leftCol.Add(UICardHelper.WrapWithShadow(inboxCard));
        _inboxPool = new ElementPool(CreateInboxItem, _inboxContainer);

        // Center column: Active contracts
        var centerCol = CreateColumn("overview-col-center");
        var contractsCard = CreateCard("Active Contracts");
        UICardHelper.ApplyBevel(contractsCard);
        UICardHelper.AddGradient(contractsCard);
        _contractsContainer = new VisualElement();
        _contractsContainer.name = "contracts-list";
        contractsCard.Add(_contractsContainer);
        _contractsEmptyState = UICardHelper.CreateEmptyState("📋", "No active contracts.");
        _contractsEmptyState.AddToClassList("empty-state--hidden");
        contractsCard.Add(_contractsEmptyState);
        centerCol.Add(UICardHelper.WrapWithShadow(contractsCard));
        _contractPool = new ElementPool(CreateContractItem, _contractsContainer);

        // Right column: Finance + Reputation
        var rightCol = CreateColumn("overview-col-right");

        // Finance card
        var financeCard = CreateCard("Finance");
        UICardHelper.ApplyBevel(financeCard);
        UICardHelper.AddGradient(financeCard);
        var financeGrid = new VisualElement();
        financeGrid.AddToClassList("flex-col");
        (_moneyRow, _moneyLabel)     = CreateStatRow(financeGrid, "Balance");
        (_incomeRow, _incomeLabel)   = CreateStatRow(financeGrid, "Revenue");
        (_expensesRow, _expensesLabel) = CreateStatRow(financeGrid, "Expenses");
        financeCard.Add(financeGrid);
        rightCol.Add(UICardHelper.WrapWithShadow(financeCard));

        var tooltips = _tooltipProvider.TooltipService;
        _moneyRow.SetRichTooltip("topbar.balance", tooltips);
        _incomeRow.SetRichTooltip("finance.revenue", tooltips);
        _expensesRow.SetRichTooltip("finance.expenses", tooltips);

        // Stats card
        var statsCard = CreateCard("Company");
        UICardHelper.ApplyBevel(statsCard);
        UICardHelper.AddGradient(statsCard);
        var statsGrid = new VisualElement();
        statsGrid.AddToClassList("flex-col");
        (_, _employeeCountLabel) = CreateStatRow(statsGrid, "Employees");
        (_, _teamCountLabel)     = CreateStatRow(statsGrid, "Teams");
        (_, _contractCountLabel) = CreateStatRow(statsGrid, "Contracts");
        statsCard.Add(statsGrid);
        rightCol.Add(UICardHelper.WrapWithShadow(statsCard));

        // Reputation card
        var repCard = CreateCard("Reputation");
        UICardHelper.ApplyBevel(repCard);
        UICardHelper.AddGradient(repCard);
        var repContent = new VisualElement();
        repContent.AddToClassList("flex-col");
        _reputationTierLabel = new Label("Unknown");
        _reputationTierLabel.AddToClassList("metric-primary");
        _reputationTierLabel.AddToClassList("text-accent");
        repContent.Add(_reputationTierLabel);

        _reputationScoreLabel = new Label("0");
        _reputationScoreLabel.AddToClassList("metric-tertiary");
        repContent.Add(_reputationScoreLabel);

        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.marginTop = 8;
        _reputationProgressFill = new VisualElement();
        _reputationProgressFill.AddToClassList("progress-bar__fill");
        progressBar.Add(_reputationProgressFill);
        repContent.Add(progressBar);

        repCard.Add(repContent);
        rightCol.Add(UICardHelper.WrapWithShadow(repCard));

        dashboard.Add(leftCol);
        dashboard.Add(centerCol);
        dashboard.Add(rightCol);
        _root.Add(dashboard);
    }

    public void Bind(IViewModel viewModel) {
        var vm = viewModel as OverviewViewModel;
        if (vm == null) return;

        // Finance
        if (_moneyLabel != null) _moneyLabel.text = vm.MoneyDisplay;
        if (_incomeLabel != null) _incomeLabel.text = vm.MonthlyIncomeDisplay;
        if (_expensesLabel != null) _expensesLabel.text = vm.MonthlyExpensesDisplay;

        // Stats
        if (_employeeCountLabel != null) _employeeCountLabel.text = vm.EmployeeCount.ToString();
        if (_teamCountLabel != null) _teamCountLabel.text = vm.TeamCount.ToString();
        if (_contractCountLabel != null) _contractCountLabel.text = vm.ActiveContractCount.ToString();

        // Reputation
        if (_reputationTierLabel != null) _reputationTierLabel.text = vm.Reputation.TierName;
        if (_reputationScoreLabel != null) _reputationScoreLabel.text = vm.Reputation.Score + " / " + vm.Reputation.NextTierThreshold;
        if (_reputationProgressFill != null) {
            float targetPercent = vm.Reputation.ProgressPercent * 100f;
            if (!_fillPercents.TryGetValue(_reputationProgressFill, out float cur)) cur = 0f;
            if (_fillTweeners.TryGetValue(_reputationProgressFill, out var t)) t?.Kill();
            _fillTweeners[_reputationProgressFill] = UIAnimator.ProgressFill(_reputationProgressFill, cur, targetPercent);
            _fillPercents[_reputationProgressFill] = targetPercent;
        }

        // Contracts
        _contractPool.UpdateList(vm.ActiveContracts, BindContractItem);
        bool hasContracts = vm.ActiveContracts != null && vm.ActiveContracts.Count > 0;
        _contractsContainer.style.display = hasContracts ? DisplayStyle.Flex : DisplayStyle.None;
        if (_contractsEmptyState != null) {
            if (hasContracts) _contractsEmptyState.AddToClassList("empty-state--hidden");
            else _contractsEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        // Inbox
        _inboxPool.UpdateList(vm.RecentMessages, BindInboxItem);
        bool hasMessages = vm.RecentMessages != null && vm.RecentMessages.Count > 0;
        _inboxContainer.style.display = hasMessages ? DisplayStyle.Flex : DisplayStyle.None;
        if (_inboxEmptyState != null) {
            if (hasMessages) _inboxEmptyState.AddToClassList("empty-state--hidden");
            else _inboxEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        // Stagger in on first bind
        if (!_hasAnimatedIn) {
            _hasAnimatedIn = true;
            _staggerScratch.Clear();
            int childCount = _contractsContainer.childCount;
            for (int i = 0; i < childCount; i++) {
                var el = _contractsContainer[i];
                if (el.style.display != DisplayStyle.None) _staggerScratch.Add(el);
            }
            UIAnimator.StaggerIn(_staggerScratch);
        }
    }

    public void Dispose() {
        var tooltips = _tooltipProvider.TooltipService;
        _moneyRow?.ClearTooltip(tooltips);
        _incomeRow?.ClearTooltip(tooltips);
        _expensesRow?.ClearTooltip(tooltips);
        _hasAnimatedIn = false;
        _fillPercents.Clear();
        _fillTweeners.Clear();
        _staggerScratch.Clear();
        _contractPool = null;
        _inboxPool = null;
    }

    // --- Element factories ---

    private VisualElement CreateColumn(string name) {
        var col = new VisualElement();
        col.name = name;
        col.style.flexGrow = 1;
        col.style.flexBasis = 0;
        col.style.marginRight = 12;
        return col;
    }

    private VisualElement CreateCard(string title) {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.style.marginBottom = 12;

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("card__title");
        card.Add(titleLabel);

        return card;
    }

    private (VisualElement row, Label value) CreateStatRow(VisualElement parent, string label) {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("justify-between");
        row.style.marginBottom = 4;

        var labelEl = new Label(label);
        labelEl.AddToClassList("metric-tertiary");
        row.Add(labelEl);

        var valueEl = new Label("--");
        valueEl.AddToClassList("metric-secondary");
        row.Add(valueEl);

        parent.Add(row);
        return (row, valueEl);
    }

    private VisualElement CreateContractItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");
        item.AddToClassList("flex-col");
        item.style.paddingTop = 6;
        item.style.paddingBottom = 6;

        var topRow = new VisualElement();
        topRow.AddToClassList("flex-row");
        topRow.AddToClassList("justify-between");

        var nameLabel = new Label();
        nameLabel.name = "contract-name";
        nameLabel.AddToClassList("metric-secondary");
        topRow.Add(nameLabel);

        var deadlineLabel = new Label();
        deadlineLabel.name = "contract-deadline";
        deadlineLabel.AddToClassList("metric-tertiary");
        topRow.Add(deadlineLabel);

        item.Add(topRow);

        var botRow = new VisualElement();
        botRow.AddToClassList("flex-row");
        botRow.AddToClassList("justify-between");
        botRow.style.marginTop = 4;

        var teamLabel = new Label();
        teamLabel.name = "contract-team";
        teamLabel.AddToClassList("metric-tertiary");
        botRow.Add(teamLabel);

        var phaseLabel = new Label();
        phaseLabel.name = "contract-phase";
        phaseLabel.AddToClassList("badge");
        phaseLabel.AddToClassList("badge--accent");
        botRow.Add(phaseLabel);

        item.Add(botRow);

        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.marginTop = 4;
        var fill = new VisualElement();
        fill.name = "contract-progress";
        fill.AddToClassList("progress-bar__fill");
        progressBar.Add(fill);
        item.Add(progressBar);

        return item;
    }

    private void BindContractItem(VisualElement el, ActiveContractDisplay data) {
        var nameLabel    = el.Q<Label>("contract-name");
        var deadlineLabel = el.Q<Label>("contract-deadline");
        var teamLabel    = el.Q<Label>("contract-team");
        var phaseLabel   = el.Q<Label>("contract-phase");
        var progressFill = el.Q<VisualElement>("contract-progress");

        if (nameLabel != null) nameLabel.text = data.Name;
        if (deadlineLabel != null) deadlineLabel.text = data.DaysRemaining;
        if (teamLabel != null) teamLabel.text = data.TeamName;
        if (phaseLabel != null) phaseLabel.text = data.PhaseName;

        if (progressFill != null) {
            float targetPercent = data.ProgressPercent * 100f;
            if (!_fillPercents.TryGetValue(progressFill, out float cur)) cur = 0f;
            if (_fillTweeners.TryGetValue(progressFill, out var t)) t?.Kill();
            _fillTweeners[progressFill] = UIAnimator.ProgressFill(progressFill, cur, targetPercent);
            _fillPercents[progressFill] = targetPercent;
        }
    }

    private VisualElement CreateInboxItem() {
        var item = new VisualElement();
        item.AddToClassList("list-item");

        var icon = new VisualElement();
        icon.name = "mail-icon";
        icon.AddToClassList("badge");
        icon.style.width = 8;
        icon.style.height = 8;
        icon.style.borderTopLeftRadius = 4;
        icon.style.borderTopRightRadius = 4;
        icon.style.borderBottomLeftRadius = 4;
        icon.style.borderBottomRightRadius = 4;
        icon.style.marginRight = 8;
        item.Add(icon);

        var content = new VisualElement();
        content.style.flexGrow = 1;

        var titleLabel = new Label();
        titleLabel.name = "mail-title";
        titleLabel.AddToClassList("metric-secondary");
        content.Add(titleLabel);

        var timeLabel = new Label();
        timeLabel.name = "mail-time";
        timeLabel.AddToClassList("metric-tertiary");
        content.Add(timeLabel);

        item.Add(content);
        return item;
    }

    private void BindInboxItem(VisualElement el, InboxItemDisplay data) {
        var icon = el.Q<VisualElement>("mail-icon");
        var titleLabel = el.Q<Label>("mail-title");
        var timeLabel  = el.Q<Label>("mail-time");

        if (icon != null) {
            icon.style.backgroundColor = data.IsRead
                ? new StyleColor(new UnityEngine.Color(0.66f, 0.71f, 0.68f, 0.5f))
                : new StyleColor(new UnityEngine.Color(0.3f, 0.79f, 0.69f, 1f));
        }
        if (titleLabel != null) titleLabel.text = data.Title;
        if (timeLabel != null) timeLabel.text = data.Timestamp;
    }
}
