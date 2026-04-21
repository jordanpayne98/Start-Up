using UnityEngine.UIElements;

public class InterviewProgressView : IGameView
{
    private VisualElement _root;
    private InterviewProgressViewModel _viewModel;

    // Header
    private Label _nameLabel;
    private Label _roleLabel;
    private Button _dismissButton;

    // Stage pipeline
    private VisualElement _stage1;
    private VisualElement _stage2;
    private VisualElement _stage3;

    // Status area
    private VisualElement _inProgressSection;
    private Label _inProgressLabel;
    private Label _completesLabel;

    // Start section
    private VisualElement _startSection;
    private Label _nextStageLabel;
    private Label _costLabel;
    private Label _durationLabel;
    private Button _startButton;

    // Hireable section
    private VisualElement _hireableSection;

    public void Initialize(VisualElement root) {
        _root = root;

        // ── Header ───────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var titleLabel = new Label("Interview Process");
        titleLabel.AddToClassList("h4");
        header.Add(titleLabel);

        _dismissButton = new Button { text = "X" };
        _dismissButton.AddToClassList("btn-sm");
        _dismissButton.style.minWidth = 30;
        header.Add(_dismissButton);

        _root.Add(header);

        // ── Scrollable body ───────────────────────────────────────────────────────
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // Candidate summary
        var infoRow = new VisualElement();
        infoRow.style.flexDirection = FlexDirection.Row;
        infoRow.style.alignItems = Align.Center;
        infoRow.style.marginBottom = 16;

        _nameLabel = new Label();
        _nameLabel.AddToClassList("text-bold");
        _nameLabel.AddToClassList("text-lg");
        _nameLabel.style.marginRight = 8;
        infoRow.Add(_nameLabel);

        _roleLabel = new Label();
        _roleLabel.AddToClassList("badge");
        _roleLabel.AddToClassList("badge--primary");
        infoRow.Add(_roleLabel);

        body.Add(infoRow);

        // Stage pipeline
        var pipelineCard = new VisualElement();
        pipelineCard.AddToClassList("card");
        pipelineCard.style.marginBottom = 16;

        var pipelineTitle = new Label("Interview Stages");
        pipelineTitle.AddToClassList("text-bold");
        pipelineTitle.style.marginBottom = 10;
        pipelineCard.Add(pipelineTitle);

        var pipeline = new VisualElement();
        pipeline.style.flexDirection = FlexDirection.Row;
        pipeline.style.alignItems = Align.Center;

        _stage1 = CreateStageNode("1", "Phone Screen", "Free");
        pipeline.Add(_stage1);
        pipeline.Add(CreateConnector());
        _stage2 = CreateStageNode("2", "Skills Interview", "$25");
        pipeline.Add(_stage2);
        pipeline.Add(CreateConnector());
        _stage3 = CreateStageNode("3", "Final Interview", "$75");
        pipeline.Add(_stage3);

        pipelineCard.Add(pipeline);
        body.Add(pipelineCard);

        // In-progress section
        _inProgressSection = new VisualElement();
        _inProgressSection.AddToClassList("card");
        _inProgressSection.style.marginBottom = 16;
        _inProgressSection.style.display = DisplayStyle.None;

        _inProgressLabel = new Label();
        _inProgressLabel.AddToClassList("text-bold");
        _inProgressLabel.AddToClassList("text-accent");
        _inProgressSection.Add(_inProgressLabel);

        _completesLabel = new Label();
        _completesLabel.AddToClassList("text-sm");
        _completesLabel.AddToClassList("text-muted");
        _completesLabel.style.marginTop = 4;
        _inProgressSection.Add(_completesLabel);

        body.Add(_inProgressSection);

        // Start next stage section
        _startSection = new VisualElement();
        _startSection.AddToClassList("card");
        _startSection.style.marginBottom = 16;
        _startSection.style.display = DisplayStyle.None;

        _nextStageLabel = new Label();
        _nextStageLabel.AddToClassList("text-bold");
        _nextStageLabel.style.marginBottom = 8;
        _startSection.Add(_nextStageLabel);

        var detailsRow = new VisualElement();
        detailsRow.style.flexDirection = FlexDirection.Row;
        detailsRow.style.marginBottom = 12;

        _costLabel = new Label();
        _costLabel.AddToClassList("badge");
        _costLabel.AddToClassList("badge--info");
        _costLabel.style.marginRight = 8;
        detailsRow.Add(_costLabel);

        _durationLabel = new Label();
        _durationLabel.AddToClassList("text-sm");
        _durationLabel.AddToClassList("text-muted");
        detailsRow.Add(_durationLabel);

        _startSection.Add(detailsRow);

        _startButton = new Button { text = "Start Interview" };
        _startButton.AddToClassList("btn-primary");
        _startSection.Add(_startButton);

        body.Add(_startSection);

        // Hireable section
        _hireableSection = new VisualElement();
        _hireableSection.AddToClassList("card");
        _hireableSection.style.marginBottom = 16;
        _hireableSection.style.display = DisplayStyle.None;

        var hireableLabel = new Label("All stages complete. Candidate is ready for an offer.");
        hireableLabel.AddToClassList("text-success");
        hireableLabel.AddToClassList("text-bold");
        _hireableSection.Add(hireableLabel);

        var makeOfferBtn = new Button { text = "Make Offer" };
        makeOfferBtn.name = "btn-make-offer";
        makeOfferBtn.AddToClassList("btn-primary");
        makeOfferBtn.style.marginTop = 8;
        _hireableSection.Add(makeOfferBtn);

        body.Add(_hireableSection);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as InterviewProgressViewModel;
        if (_viewModel == null) return;

        _nameLabel.text = _viewModel.Name;
        _roleLabel.text = _viewModel.Role;

        // Wire buttons once (guard with userData pattern)
        if (_dismissButton.userData == null) {
            _dismissButton.userData = true;
            _dismissButton.clicked += () => _viewModel?.Dismiss();
            _startButton.clicked += () => _viewModel?.StartInterview();

            var makeOfferBtn = _root.Q<Button>("btn-make-offer");
            if (makeOfferBtn != null) {
                makeOfferBtn.clicked += () => _viewModel?.Dismiss();
            }
        }

        UpdateStages(_viewModel.InterviewStage, _viewModel.IsInProgress);
        UpdateSections();
    }

    public void Dispose() {
        _viewModel = null;
    }

    private void UpdateStages(int completedStage, bool inProgress) {
        ApplyStageStyle(_stage1, 1, completedStage, inProgress);
        ApplyStageStyle(_stage2, 2, completedStage, inProgress);
        ApplyStageStyle(_stage3, 3, completedStage, inProgress);
    }

    private void ApplyStageStyle(VisualElement node, int stageIndex, int completedStage, bool inProgress) {
        var dot = node.Q<VisualElement>("stage-dot");
        var label = node.Q<Label>("stage-label");
        if (dot == null || label == null) return;

        dot.RemoveFromClassList("stage-dot--complete");
        dot.RemoveFromClassList("stage-dot--active");
        dot.RemoveFromClassList("stage-dot--pending");

        if (stageIndex <= completedStage) {
            dot.AddToClassList("stage-dot--complete");
        } else if (stageIndex == completedStage + 1 && inProgress) {
            dot.AddToClassList("stage-dot--active");
        } else {
            dot.AddToClassList("stage-dot--pending");
        }

        label.RemoveFromClassList("text-success");
        label.RemoveFromClassList("text-accent");
        label.RemoveFromClassList("text-muted");
        if (stageIndex <= completedStage) label.AddToClassList("text-success");
        else if (stageIndex == completedStage + 1 && inProgress) label.AddToClassList("text-accent");
        else label.AddToClassList("text-muted");
    }

    private void UpdateSections() {
        if (_viewModel == null) return;

        if (_viewModel.IsHireable) {
            _inProgressSection.style.display = DisplayStyle.None;
            _startSection.style.display = DisplayStyle.None;
            _hireableSection.style.display = DisplayStyle.Flex;
        } else if (_viewModel.IsInProgress) {
            _inProgressLabel.text = _viewModel.InProgressStageName + " in progress...";
            _completesLabel.text = _viewModel.CompletesInDisplay;
            _inProgressSection.style.display = DisplayStyle.Flex;
            _startSection.style.display = DisplayStyle.None;
            _hireableSection.style.display = DisplayStyle.None;
        } else {
            _nextStageLabel.text = "Next: " + _viewModel.NextStageName;
            _costLabel.text = "Cost: " + _viewModel.NextStageCostDisplay;
            _durationLabel.text = "Duration: " + _viewModel.NextStageDurationDisplay;
            _startButton.SetEnabled(_viewModel.CanStartNext);
            _startButton.text = _viewModel.CanStartNext
                ? "Start " + _viewModel.NextStageName
                : "Insufficient funds";
            _startSection.style.display = DisplayStyle.Flex;
            _inProgressSection.style.display = DisplayStyle.None;
            _hireableSection.style.display = DisplayStyle.None;
        }
    }

    // --- Helpers ---

    private VisualElement CreateStageNode(string number, string title, string cost) {
        var node = new VisualElement();
        node.style.alignItems = Align.Center;
        node.style.flexGrow = 1;

        var dot = new VisualElement();
        dot.name = "stage-dot";
        dot.AddToClassList("stage-dot");
        dot.AddToClassList("stage-dot--pending");
        dot.style.width = 28;
        dot.style.height = 28;
        dot.style.borderTopLeftRadius = 14;
        dot.style.borderTopRightRadius = 14;
        dot.style.borderBottomLeftRadius = 14;
        dot.style.borderBottomRightRadius = 14;
        dot.style.alignItems = Align.Center;
        dot.style.justifyContent = Justify.Center;
        dot.style.marginBottom = 4;

        var numLabel = new Label(number);
        numLabel.AddToClassList("text-sm");
        numLabel.AddToClassList("text-bold");
        dot.Add(numLabel);

        node.Add(dot);

        var titleLabel = new Label(title);
        titleLabel.name = "stage-label";
        titleLabel.AddToClassList("text-xs");
        titleLabel.AddToClassList("text-muted");
        titleLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        node.Add(titleLabel);

        var costLabel = new Label(cost);
        costLabel.AddToClassList("text-xs");
        costLabel.AddToClassList("text-muted");
        costLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        node.Add(costLabel);

        return node;
    }

    private VisualElement CreateConnector() {
        var line = new VisualElement();
        line.style.flexGrow = 0;
        line.style.width = 24;
        line.style.height = 2;
        line.style.marginTop = 13;
        line.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color(0.3f, 0.4f, 0.35f));
        return line;
    }
}
