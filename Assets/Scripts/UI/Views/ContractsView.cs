using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UIElements;

public class ContractsView : IGameView
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private VisualElement _availableContainer;
    private VisualElement _activeContainer;
    private ElementPool _availablePool;
    private ElementPool _activePool;
    private Button _rerollButton;
    private ContractsOverviewViewModel _viewModel;

    // Empty states
    private VisualElement _availableEmptyState;
    private VisualElement _activeEmptyState;

    // Per-fill tracking for progress tweens
    private readonly Dictionary<VisualElement, float>   _fillPercents = new Dictionary<VisualElement, float>();
    private readonly Dictionary<VisualElement, Tweener> _fillTweeners = new Dictionary<VisualElement, Tweener>();

    // Per-badge semantic class tracking for pop-in
    private readonly Dictionary<VisualElement, string>  _badgeClasses = new Dictionary<VisualElement, string>();

    // Reusable flyout team option button pool per card
    // Key: teamOptions container, Value: list of pre-created buttons
    private readonly Dictionary<VisualElement, List<Button>> _teamOptionPools = new Dictionary<VisualElement, List<Button>>();

    // Stagger scratch list — reused, never allocated in Bind
    private readonly List<VisualElement> _staggerScratch = new List<VisualElement>();

    // Stagger guard
    private bool _hasAnimatedIn;

    public ContractsView(ICommandDispatcher dispatcher, ITooltipProvider tooltipProvider) {
        _dispatcher = dispatcher;
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;

        var layout = new VisualElement();
        layout.AddToClassList("flex-row");
        layout.style.flexGrow = 1;

        // Left: Available contracts
        var availablePanel = new VisualElement();
        availablePanel.style.flexGrow = 1;
        availablePanel.style.flexBasis = 0;
        availablePanel.style.marginRight = 16;

        var availHeader = new VisualElement();
        availHeader.AddToClassList("flex-row");
        availHeader.AddToClassList("justify-between");
        availHeader.AddToClassList("align-center");
        availHeader.style.marginBottom = 8;

        var availTitle = new Label("Available Contracts");
        availTitle.AddToClassList("section-header");
        availHeader.Add(availTitle);

        _rerollButton = new Button { text = "Reroll" };
        _rerollButton.AddToClassList("btn-secondary");
        _rerollButton.AddToClassList("btn-sm");
        _rerollButton.clicked += OnRerollClicked;
        _rerollButton.SetSimpleTooltip("Refresh available contracts for a fee.", _tooltipProvider.TooltipService);
        availHeader.Add(_rerollButton);

        availablePanel.Add(availHeader);

        var availScroll = new ScrollView();
        availScroll.style.flexGrow = 1;
        _availableContainer = availScroll.contentContainer;
        _availablePool = new ElementPool(CreateAvailableCard, _availableContainer);

        _availableEmptyState = UICardHelper.CreateEmptyState("📭", "No contracts available. Wait or reroll.");
        _availableEmptyState.AddToClassList("empty-state--hidden");
        availablePanel.Add(_availableEmptyState);
        availablePanel.Add(availScroll);

        layout.Add(availablePanel);

        // Right: Active contracts
        var activePanel = new VisualElement();
        activePanel.style.flexGrow = 1;
        activePanel.style.flexBasis = 0;

        var activeTitle = new Label("Active Contracts");
        activeTitle.AddToClassList("section-header");
        activeTitle.style.marginBottom = 8;
        activePanel.Add(activeTitle);

        var activeScroll = new ScrollView();
        activeScroll.style.flexGrow = 1;
        _activeContainer = activeScroll.contentContainer;
        _activePool = new ElementPool(CreateActiveCard, _activeContainer);

        _activeEmptyState = UICardHelper.CreateEmptyState("📋", "No active contracts yet.");
        _activeEmptyState.AddToClassList("empty-state--hidden");
        activePanel.Add(_activeEmptyState);
        activePanel.Add(activeScroll);

        layout.Add(activePanel);
        _root.Add(layout);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as ContractsOverviewViewModel;
        if (_viewModel == null) return;

        if (_rerollButton != null) {
            _rerollButton.text = "Reroll (" + _viewModel.RerollCostDisplay + ")";
            _rerollButton.SetEnabled(_viewModel.CanReroll);
        }

        _availablePool.UpdateList(_viewModel.AvailableContracts, BindAvailableCard);
        _activePool.UpdateList(_viewModel.ActiveContracts, BindActiveCard);

        // Empty states
        bool hasAvailable = _viewModel.AvailableContracts != null && _viewModel.AvailableContracts.Count > 0;
        if (_availableEmptyState != null) {
            if (hasAvailable) _availableEmptyState.AddToClassList("empty-state--hidden");
            else _availableEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        bool hasActive = _viewModel.ActiveContracts != null && _viewModel.ActiveContracts.Count > 0;
        if (_activeEmptyState != null) {
            if (hasActive) _activeEmptyState.AddToClassList("empty-state--hidden");
            else _activeEmptyState.RemoveFromClassList("empty-state--hidden");
        }

        // Stagger on first bind
        if (!_hasAnimatedIn) {
            _hasAnimatedIn = true;
            _staggerScratch.Clear();
            int childCount = _availableContainer.childCount;
            for (int i = 0; i < childCount; i++) {
                var el = _availableContainer[i];
                if (el.style.display != DisplayStyle.None) _staggerScratch.Add(el);
            }
            UIAnimator.StaggerIn(_staggerScratch);
        }
    }

    public void Dispose() {
        _hasAnimatedIn = false;
        _fillPercents.Clear();
        _fillTweeners.Clear();
        _badgeClasses.Clear();
        _teamOptionPools.Clear();
        _staggerScratch.Clear();
        _viewModel = null;
        _availablePool = null;
        _activePool = null;
        _rerollButton?.ClearTooltip(_tooltipProvider.TooltipService);
    }

    private void OnRerollClicked() {
        _dispatcher.Dispatch(new RerollContractPoolCommand());
    }

    private VisualElement CreateAvailableCard() {
        var card = new VisualElement();
        card.AddToClassList("card");
        card.AddToClassList("card--hover");
        UICardHelper.ApplyBevel(card);
        UICardHelper.AddGradient(card);
        card.style.marginBottom = 8;

        var topRow = new VisualElement();
        topRow.AddToClassList("flex-row");
        topRow.AddToClassList("justify-between");

        var nameLabel = new Label();
        nameLabel.name = "avail-name";
        nameLabel.AddToClassList("card__title");
        topRow.Add(nameLabel);

        var diffBadge = new Label();
        diffBadge.name = "avail-diff";
        diffBadge.AddToClassList("badge");
        topRow.Add(diffBadge);

        card.Add(topRow);

        var descLabel = new Label();
        descLabel.name = "avail-desc";
        descLabel.AddToClassList("metric-tertiary");
        descLabel.style.marginBottom = 8;
        card.Add(descLabel);

        // Requirements row: skills / quality expectation / staffing
        var reqRow = new VisualElement();
        reqRow.AddToClassList("flex-row");
        reqRow.style.marginBottom = 4;

        var skillBadge = new Label();
        skillBadge.name = "avail-skills";
        skillBadge.AddToClassList("badge");
        skillBadge.AddToClassList("badge--info");
        skillBadge.style.marginRight = 4;
        reqRow.Add(skillBadge);

        var qualityBadge = new Label();
        qualityBadge.name = "avail-quality";
        qualityBadge.AddToClassList("badge");
        qualityBadge.AddToClassList("badge--accent");
        qualityBadge.style.marginRight = 4;
        reqRow.Add(qualityBadge);

        var staffingBadge = new Label();
        staffingBadge.name = "avail-staffing";
        staffingBadge.AddToClassList("badge");
        staffingBadge.AddToClassList("badge--secondary");
        reqRow.Add(staffingBadge);

        // Register tooltip on the row container for a full-width hit area
        reqRow.SetSimpleTooltip("Skill required · Quality threshold · Min team size", _tooltipProvider.TooltipService);

        card.Add(reqRow);

        // Bottom row
        var botRow = new VisualElement();
        botRow.AddToClassList("flex-row");
        botRow.AddToClassList("justify-between");
        botRow.style.marginTop = 4;

        var rewardLabel = new Label();
        rewardLabel.name = "avail-reward";
        rewardLabel.AddToClassList("metric-primary");
        rewardLabel.AddToClassList("text-success");
        botRow.Add(rewardLabel);

        var deadlineLabel = new Label();
        deadlineLabel.name = "avail-deadline";
        deadlineLabel.AddToClassList("metric-tertiary");
        botRow.Add(deadlineLabel);

        // Reward on left, deadline on right — register on botRow for full coverage
        botRow.SetSimpleTooltip("Reward on completion · Days until deadline", _tooltipProvider.TooltipService);

        card.Add(botRow);

        var acceptBtn = new Button { text = "Accept" };
        acceptBtn.name = "avail-accept";
        acceptBtn.AddToClassList("btn-primary");
        acceptBtn.AddToClassList("btn-sm");
        acceptBtn.style.marginTop = 8;
        card.Add(acceptBtn);

        var sourceRow = new VisualElement();
        sourceRow.name = "avail-source-row";
        sourceRow.AddToClassList("flex-row");
        sourceRow.AddToClassList("align-center");
        sourceRow.style.marginTop = 4;
        sourceRow.style.display = DisplayStyle.None;

        var sourceBadge = new Label();
        sourceBadge.name = "avail-source-badge";
        sourceBadge.AddToClassList("badge");
        sourceBadge.AddToClassList("badge--warning");
        sourceBadge.style.marginRight = 6;
        sourceBadge.text = "Competitor";
        sourceRow.Add(sourceBadge);

        var sourceNameLabel = new Label();
        sourceNameLabel.name = "avail-source-name";
        sourceNameLabel.AddToClassList("text-sm");
        sourceNameLabel.AddToClassList("text-muted");
        sourceRow.Add(sourceNameLabel);

        card.Add(sourceRow);

        return card;
    }

    private void BindAvailableCard(VisualElement el, AvailableContractDisplay data) {
        el.Q<Label>("avail-name").text = data.Name;
        el.Q<Label>("avail-desc").text = data.Description;
        el.Q<Label>("avail-reward").text = data.RewardDisplay;
        el.Q<Label>("avail-deadline").text = data.DeadlineDisplay;
        el.Q<Label>("avail-skills").text = data.SkillLabel;
        el.Q<Label>("avail-quality").text = data.QualityExpLabel;
        el.Q<Label>("avail-staffing").text = data.StaffingLabel;

        var diffBadge = el.Q<Label>("avail-diff");

        // Determine new semantic class
        string newClass;
        if (data.Difficulty <= 2) newClass = "badge--success";
        else if (data.Difficulty <= 4) newClass = "badge--warning";
        else newClass = "badge--danger";

        // Badge pop-in when difficulty class changes
        string oldClass = null;
        _badgeClasses.TryGetValue(diffBadge, out oldClass);
        if (oldClass != newClass) {
            diffBadge.RemoveFromClassList("badge--success");
            diffBadge.RemoveFromClassList("badge--warning");
            diffBadge.RemoveFromClassList("badge--danger");
            diffBadge.AddToClassList(newClass);
            _badgeClasses[diffBadge] = newClass;
            UIAnimator.BadgePopIn(diffBadge);
        }

        diffBadge.text = "Lv." + data.Difficulty;

        // Competitor source indicator
        var sourceRow = el.Q<VisualElement>("avail-source-row");
        var sourceNameLabel = el.Q<Label>("avail-source-name");
        if (sourceRow != null) {
            if (data.IsCompetitorSourced) {
                sourceRow.style.display = DisplayStyle.Flex;
                if (sourceNameLabel != null)
                    sourceNameLabel.text = string.IsNullOrEmpty(data.SourceCompetitorName)
                        ? "Competitor-sourced"
                        : "from " + data.SourceCompetitorName;
            } else {
                sourceRow.style.display = DisplayStyle.None;
            }
        }

        var acceptBtn = el.Q<Button>("avail-accept");
        if (acceptBtn != null) {
            if (acceptBtn.userData is System.Action prevAccept)
                acceptBtn.clicked -= prevAccept;
            var capturedId = data.Id;
            System.Action onClick = () => _dispatcher.Dispatch(new AcceptContractCommand { Tick = _dispatcher.CurrentTick, ContractId = capturedId });
            acceptBtn.userData = onClick;
            acceptBtn.clicked += onClick;
        }
    }

    private VisualElement CreateActiveCard() {
        var card = new VisualElement();
        card.AddToClassList("card");
        UICardHelper.ApplyBevel(card);
        UICardHelper.AddGradient(card);
        card.style.marginBottom = 8;

        var topRow = new VisualElement();
        topRow.AddToClassList("flex-row");
        topRow.AddToClassList("justify-between");

        var nameLabel = new Label();
        nameLabel.name = "active-name";
        nameLabel.AddToClassList("card__title");
        topRow.Add(nameLabel);

        var statusBadge = new Label();
        statusBadge.name = "active-status";
        statusBadge.AddToClassList("badge");
        statusBadge.AddToClassList("badge--accent");
        topRow.Add(statusBadge);

        card.Add(topRow);

        var infoRow = new VisualElement();
        infoRow.AddToClassList("flex-row");
        infoRow.AddToClassList("justify-between");
        infoRow.style.marginTop = 4;

        var teamLabel = new Label();
        teamLabel.name = "active-team";
        teamLabel.AddToClassList("metric-tertiary");
        infoRow.Add(teamLabel);

        var deadlineLabel = new Label();
        deadlineLabel.name = "active-deadline";
        deadlineLabel.AddToClassList("metric-tertiary");
        infoRow.Add(deadlineLabel);

        card.Add(infoRow);

        // Skill / staffing / quality / team-fit row
        var metaRow = new VisualElement();
        metaRow.AddToClassList("flex-row");
        metaRow.AddToClassList("align-center");
        metaRow.style.marginTop = 6;
        metaRow.style.marginBottom = 2;

        var skillBadge = new Label();
        skillBadge.name = "active-skill";
        skillBadge.AddToClassList("badge");
        skillBadge.AddToClassList("badge--info");
        skillBadge.style.marginRight = 4;
        metaRow.Add(skillBadge);

        var staffBadge = new Label();
        staffBadge.name = "active-staffing";
        staffBadge.AddToClassList("badge");
        staffBadge.AddToClassList("badge--secondary");
        staffBadge.style.marginRight = 8;
        metaRow.Add(staffBadge);

        var qualityLabel = new Label();
        qualityLabel.name = "active-quality";
        qualityLabel.AddToClassList("metric-tertiary");
        qualityLabel.style.flexGrow = 1;
        metaRow.Add(qualityLabel);

        var fitBadge = new Label();
        fitBadge.name = "active-fit";
        fitBadge.AddToClassList("badge");
        fitBadge.style.display = DisplayStyle.None;
        metaRow.Add(fitBadge);

        card.Add(metaRow);

        // Overall progress bar
        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.marginTop = 6;
        var fill = new VisualElement();
        fill.name = "active-progress";
        fill.AddToClassList("progress-bar__fill");
        progressBar.Add(fill);
        card.Add(progressBar);

        // Assign team button + flyout
        var assignBtn = new Button { text = "Assign Team" };
        assignBtn.name = "active-assign-team-btn";
        assignBtn.AddToClassList("btn-secondary");
        assignBtn.AddToClassList("btn-sm");
        assignBtn.style.marginTop = 8;
        card.Add(assignBtn);

        var teamFlyout = new VisualElement();
        teamFlyout.name = "active-team-flyout";
        teamFlyout.AddToClassList("card");
        teamFlyout.style.display = DisplayStyle.None;
        teamFlyout.style.marginTop = 4;

        var flyoutScroll = new ScrollView();
        flyoutScroll.style.maxHeight = 140;
        var teamOptions = flyoutScroll.contentContainer;
        teamOptions.name = "active-team-options";
        teamFlyout.Add(flyoutScroll);
        card.Add(teamFlyout);

        return card;
    }

    private void BindActiveCard(VisualElement el, ActiveContractDetailDisplay data) {
        el.Q<Label>("active-name").text = data.Name;
        el.Q<Label>("active-status").text = data.Status;
        el.Q<Label>("active-team").text = "Team: " + data.TeamName;

        var deadlineLabel = el.Q<Label>("active-deadline");
        if (deadlineLabel != null) {
            deadlineLabel.text = data.DaysRemaining;
            bool isCritical = data.DaysRemaining == "Overdue" || data.DaysRemaining == "< 1 day";
            if (isCritical) UIAnimator.WarningPulse(deadlineLabel);
        }

        // Skill / staffing / quality
        el.Q<Label>("active-skill").text = data.SkillLabel;
        el.Q<Label>("active-staffing").text = data.StaffingLabel;
        var qualLabel = el.Q<Label>("active-quality");
        if (qualLabel != null) {
            if (data.QualityScore > 0f) {
                qualLabel.text = ((int)data.QualityScore) + "% quality";
                qualLabel.RemoveFromClassList("text-success");
                qualLabel.RemoveFromClassList("text-accent");
                qualLabel.RemoveFromClassList("text-danger");
                if (data.QualityScore >= 75f) qualLabel.AddToClassList("text-success");
                else if (data.QualityScore >= 55f) qualLabel.AddToClassList("text-accent");
                else qualLabel.AddToClassList("text-danger");
            } else {
                qualLabel.text = "";
            }
        }

        // Team fit badge
        var fitBadge = el.Q<Label>("active-fit");
        if (fitBadge != null) {
            bool hasFit = !string.IsNullOrEmpty(data.TeamFitLabel);
            fitBadge.style.display = hasFit ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasFit) {
                fitBadge.text = data.TeamFitLabel;
                string prevClass;
                _badgeClasses.TryGetValue(fitBadge, out prevClass);
                if (prevClass != data.TeamFitClass) {
                    fitBadge.RemoveFromClassList("badge--danger");
                    fitBadge.RemoveFromClassList("badge--warning");
                    fitBadge.RemoveFromClassList("badge--accent");
                    fitBadge.RemoveFromClassList("badge--success");
                    fitBadge.AddToClassList(data.TeamFitClass);
                    _badgeClasses[fitBadge] = data.TeamFitClass;
                    UIAnimator.BadgePopIn(fitBadge);
                }
            }
        }

        // Progress bar — correct tween path (0–1 stored, 0–100 displayed)
        var progressFill = el.Q<VisualElement>("active-progress");
        if (progressFill != null) {
            float targetPercent = data.OverallProgress * 100f;
            if (!_fillPercents.TryGetValue(progressFill, out float cur)) cur = 0f;
            if (_fillTweeners.TryGetValue(progressFill, out var t)) t?.Kill();
            _fillTweeners[progressFill] = UIAnimator.ProgressFill(progressFill, cur, targetPercent);
            _fillPercents[progressFill] = targetPercent;
        }

        // Assign team flyout
        var assignBtn = el.Q<Button>("active-assign-team-btn");
        var teamFlyout = el.Q<VisualElement>("active-team-flyout");
        var teamOptions = el.Q<VisualElement>("active-team-options");

        if (assignBtn != null && teamFlyout != null && teamOptions != null) {
            // Close stale flyout from previous binding
            teamFlyout.style.display = DisplayStyle.None;

            // Wire handler once per pool slot using UnregisterCallback+Register pattern
            assignBtn.UnregisterCallback<ClickEvent>(OnAssignTeamClicked);
            assignBtn.RegisterCallback<ClickEvent>(OnAssignTeamClicked);
            assignBtn.userData = data.Id;

            // Pre-build or resize the team option pool for this container
            var teams = _viewModel?.Teams;
            int tc = teams != null ? teams.Count : 0;

            if (!_teamOptionPools.TryGetValue(teamOptions, out var pool)) {
                pool = new List<Button>();
                _teamOptionPools[teamOptions] = pool;
            }

            // Grow pool as needed (once per slot, not per Bind)
            int poolCount = pool.Count;
            for (int i = poolCount; i < tc; i++) {
                var optBtn = new Button();
                optBtn.AddToClassList("btn-secondary");
                optBtn.AddToClassList("btn-sm");
                optBtn.style.marginBottom = 2;
                optBtn.RegisterCallback<ClickEvent>(OnTeamOptionClicked);
                teamOptions.Add(optBtn);
                pool.Add(optBtn);
            }

            // Bind visible buttons, hide extras
            int poolSize = pool.Count;
            for (int i = 0; i < poolSize; i++) {
                if (i < tc) {
                    var teamData = teams[i];
                    pool[i].text = teamData.Name;
                    pool[i].userData = new TeamOptionData(data.Id, teamData.Id, teamFlyout);
                    pool[i].style.display = DisplayStyle.Flex;
                } else {
                    pool[i].style.display = DisplayStyle.None;
                }
            }

            // Empty label: show when no teams
            var emptyLabel = teamOptions.Q<Label>("active-team-empty");
            if (tc == 0) {
                if (emptyLabel == null) {
                    teamOptions.Add(new Label("No development teams available") { name = "active-team-empty" });
                }
                if (emptyLabel != null) emptyLabel.style.display = DisplayStyle.Flex;
            } else {
                if (emptyLabel != null) emptyLabel.style.display = DisplayStyle.None;
            }
        }
    }

    private readonly struct TeamOptionData
    {
        public readonly ContractId ContractId;
        public readonly TeamId TeamId;
        public readonly VisualElement Flyout;
        public TeamOptionData(ContractId contractId, TeamId teamId, VisualElement flyout) {
            ContractId = contractId;
            TeamId = teamId;
            Flyout = flyout;
        }
    }

    private void OnAssignTeamClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn == null) return;
        var card = btn.parent;
        while (card != null && !card.ClassListContains("card")) card = card.parent;
        if (card == null) return;
        var flyout = card.Q<VisualElement>("active-team-flyout");
        if (flyout == null) return;
        bool isVisible = flyout.style.display == DisplayStyle.Flex;
        flyout.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void OnTeamOptionClicked(ClickEvent evt) {
        var btn = evt.currentTarget as Button;
        if (btn?.userData is TeamOptionData optData) {
            _dispatcher.Dispatch(new AssignTeamToContractCommand {
                Tick = _dispatcher.CurrentTick,
                ContractId = optData.ContractId,
                TeamId = optData.TeamId
            });
            optData.Flyout.style.display = DisplayStyle.None;
        }
    }
}
