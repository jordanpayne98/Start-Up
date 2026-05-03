using UnityEngine;
using UnityEngine.UIElements;

public class NegotiationView : IGameView
{
    private VisualElement _root;
    private NegotiationViewModel _viewModel;

    private Label _titleLabel;
    private Label _candidateLabel;
    private Label _roleLabel;
    private Label _askingLabel;
    private Label _statusLabel;
    private Button _dismissButton;

    // Offer controls
    private VisualElement _offerSection;
    private TextField _salaryInput;
    private Button _previewButton;
    private Button _sendButton;

    // Probability display
    private VisualElement _probSection;
    private Label _acceptPctLabel;
    private Label _counterPctLabel;
    private Label _declinePctLabel;
    private VisualElement _acceptFill;
    private VisualElement _counterFill;
    private VisualElement _declineFill;
    private Label _salaryModLabel;
    private Label _repModLabel;

    // Counter section
    private VisualElement _counterSection;
    private Label _counterSalaryLabel;
    private Label _rejectionCountLabel;
    private Button _acceptCounterBtn;
    private Button _rejectCounterBtn;

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;

        // Header
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 12;

        _titleLabel = new Label("Salary Negotiation");
        _titleLabel.AddToClassList("text-xl");
        _titleLabel.AddToClassList("text-bold");
        header.Add(_titleLabel);

        _dismissButton = new Button { text = "X" };
        _dismissButton.AddToClassList("btn-sm");
        _dismissButton.style.minWidth = 30;
        header.Add(_dismissButton);
        _root.Add(header);

        // Candidate summary
        var candidateRow = new VisualElement();
        candidateRow.style.flexDirection = FlexDirection.Row;
        candidateRow.style.alignItems = Align.Center;
        candidateRow.style.marginBottom = 12;

        _candidateLabel = new Label();
        _candidateLabel.AddToClassList("text-bold");
        _candidateLabel.style.marginRight = 8;
        candidateRow.Add(_candidateLabel);

        _roleLabel = new Label();
        _roleLabel.AddToClassList("badge");
        _roleLabel.AddToClassList("badge--primary");
        _roleLabel.style.marginRight = 12;
        candidateRow.Add(_roleLabel);

        _askingLabel = new Label();
        _askingLabel.AddToClassList("text-muted");
        candidateRow.Add(_askingLabel);

        _root.Add(candidateRow);

        // Status
        _statusLabel = new Label();
        _statusLabel.AddToClassList("text-bold");
        _statusLabel.style.marginBottom = 12;
        _root.Add(_statusLabel);

        // Offer section
        _offerSection = new VisualElement();
        _offerSection.style.marginBottom = 12;

        var offerLabel = new Label("Your salary offer:");
        offerLabel.AddToClassList("text-sm");
        offerLabel.AddToClassList("text-muted");
        offerLabel.style.marginBottom = 4;
        _offerSection.Add(offerLabel);

        var offerRow = new VisualElement();
        offerRow.style.flexDirection = FlexDirection.Row;
        offerRow.style.alignItems = Align.Center;

        var dollarSign = new Label("$");
        dollarSign.style.marginRight = 4;
        offerRow.Add(dollarSign);

        _salaryInput = new TextField();
        _salaryInput.style.flexGrow = 1;
        _salaryInput.style.marginRight = 8;
        offerRow.Add(_salaryInput);

        _previewButton = new Button { text = "Preview" };
        _previewButton.AddToClassList("btn-secondary");
        _previewButton.AddToClassList("btn-sm");
        _previewButton.style.marginRight = 4;
        offerRow.Add(_previewButton);

        _sendButton = new Button { text = "Send Offer" };
        _sendButton.AddToClassList("btn-primary");
        _sendButton.AddToClassList("btn-sm");
        offerRow.Add(_sendButton);

        _offerSection.Add(offerRow);
        _root.Add(_offerSection);

        // Probability section
        _probSection = new VisualElement();
        _probSection.AddToClassList("card");
        _probSection.style.marginBottom = 12;
        _probSection.style.display = DisplayStyle.None;

        var probHeader = new Label("Outcome Forecast");
        probHeader.AddToClassList("text-sm");
        probHeader.AddToClassList("text-bold");
        probHeader.style.marginBottom = 8;
        _probSection.Add(probHeader);

        _acceptPctLabel = new Label();
        _acceptFill = CreateProbRow(_probSection, _acceptPctLabel, new Color(0.32f, 0.72f, 0.53f));

        _counterPctLabel = new Label();
        _counterFill = CreateProbRow(_probSection, _counterPctLabel, new Color(0.96f, 0.64f, 0.38f));

        _declinePctLabel = new Label();
        _declineFill = CreateProbRow(_probSection, _declinePctLabel, new Color(0.9f, 0.22f, 0.27f));

        // Modifiers
        var modRow = new VisualElement();
        modRow.style.flexDirection = FlexDirection.Row;
        modRow.style.marginTop = 8;

        _salaryModLabel = new Label();
        _salaryModLabel.AddToClassList("text-sm");
        _salaryModLabel.AddToClassList("text-muted");
        _salaryModLabel.style.marginRight = 16;
        modRow.Add(_salaryModLabel);

        _repModLabel = new Label();
        _repModLabel.AddToClassList("text-sm");
        _repModLabel.AddToClassList("text-muted");
        modRow.Add(_repModLabel);

        _probSection.Add(modRow);
        _root.Add(_probSection);

        // Counter-offer section
        _counterSection = new VisualElement();
        _counterSection.AddToClassList("card");
        _counterSection.style.marginBottom = 12;
        _counterSection.style.display = DisplayStyle.None;

        var counterHeader = new Label("Counter Offer Received");
        counterHeader.AddToClassList("text-bold");
        counterHeader.AddToClassList("text-warning");
        counterHeader.style.marginBottom = 4;
        _counterSection.Add(counterHeader);

        _counterSalaryLabel = new Label();
        _counterSalaryLabel.AddToClassList("text-lg");
        _counterSection.Add(_counterSalaryLabel);

        _rejectionCountLabel = new Label();
        _rejectionCountLabel.AddToClassList("text-sm");
        _rejectionCountLabel.AddToClassList("text-muted");
        _rejectionCountLabel.style.marginBottom = 12;
        _counterSection.Add(_rejectionCountLabel);

        var counterBtns = new VisualElement();
        counterBtns.style.flexDirection = FlexDirection.Row;
        counterBtns.style.justifyContent = Justify.FlexEnd;

        _rejectCounterBtn = new Button { text = "Reject" };
        _rejectCounterBtn.AddToClassList("btn-danger");
        _rejectCounterBtn.AddToClassList("btn-sm");
        _rejectCounterBtn.style.marginRight = 8;
        counterBtns.Add(_rejectCounterBtn);

        _acceptCounterBtn = new Button { text = "Accept" };
        _acceptCounterBtn.AddToClassList("btn-primary");
        _acceptCounterBtn.AddToClassList("btn-sm");
        counterBtns.Add(_acceptCounterBtn);

        _counterSection.Add(counterBtns);
        _root.Add(_counterSection);

        _dismissButton.clicked += OnDismissClicked;
        _sendButton.clicked += OnSendOfferClicked;
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as NegotiationViewModel;
        if (_viewModel == null) return;

        _candidateLabel.text = _viewModel.CandidateName;
        _roleLabel.text = _viewModel.CandidateRole;
        _askingLabel.text = "Asking: " + _viewModel.AskingSalaryDisplay;

        if (string.IsNullOrEmpty(_salaryInput.value)) {
            _salaryInput.value = _viewModel.AskingSalary.ToString();
        }

        RefreshState();
    }

    public void Dispose() {
        _dismissButton.clicked -= OnDismissClicked;
        _sendButton.clicked -= OnSendOfferClicked;
        _viewModel = null;
    }

    private void OnDismissClicked() {
        _viewModel?.RequestDismiss();
    }

    private void OnSendOfferClicked() {
        if (_viewModel == null) return;
        if (int.TryParse(_salaryInput.value, out int salary)) {
            _viewModel.SubmitOffer(salary);
        }
    }

    private void RefreshState() {
        if (_viewModel == null) return;

        if (!_viewModel.HasNegotiation) {
            _statusLabel.text = "";
            _offerSection.style.display = DisplayStyle.Flex;
            _counterSection.style.display = DisplayStyle.None;
            return;
        }

        switch (_viewModel.Status) {
            case NegotiationStatus.Pending:
                _statusLabel.text = "Offer sent (" + _viewModel.OfferedSalaryDisplay + ") - awaiting response...";
                _statusLabel.RemoveFromClassList("text-success");
                _statusLabel.RemoveFromClassList("text-danger");
                _statusLabel.AddToClassList("text-accent");
                _offerSection.style.display = DisplayStyle.None;
                _counterSection.style.display = DisplayStyle.None;
                break;
            case NegotiationStatus.Accepted:
                _statusLabel.text = "Offer accepted! Employee hired.";
                _statusLabel.RemoveFromClassList("text-accent");
                _statusLabel.RemoveFromClassList("text-danger");
                _statusLabel.AddToClassList("text-success");
                _offerSection.style.display = DisplayStyle.None;
                _counterSection.style.display = DisplayStyle.None;
                break;
            case NegotiationStatus.Rejected:
                _statusLabel.text = "Offer rejected. Try a different amount.";
                _statusLabel.RemoveFromClassList("text-accent");
                _statusLabel.RemoveFromClassList("text-success");
                _statusLabel.AddToClassList("text-danger");
                _offerSection.style.display = DisplayStyle.Flex;
                _counterSection.style.display = DisplayStyle.None;
                break;
        }
    }

    private void RefreshProbabilities() {
        if (_probSection != null) _probSection.style.display = DisplayStyle.None;
    }

    private VisualElement CreateProbRow(VisualElement parent, Label label, Color color) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;

        label.AddToClassList("text-sm");
        label.style.width = 100;
        row.Add(label);

        var track = new VisualElement();
        track.style.flexGrow = 1;
        track.style.height = 8;
        track.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.14f));
        track.style.borderTopLeftRadius = 4;
        track.style.borderTopRightRadius = 4;
        track.style.borderBottomLeftRadius = 4;
        track.style.borderBottomRightRadius = 4;
        track.style.overflow = Overflow.Hidden;

        var fill = new VisualElement();
        fill.style.height = 8;
        fill.style.backgroundColor = new StyleColor(color);
        fill.style.borderTopLeftRadius = 4;
        fill.style.borderTopRightRadius = 4;
        fill.style.borderBottomLeftRadius = 4;
        fill.style.borderBottomRightRadius = 4;
        fill.style.width = 0;
        track.Add(fill);

        row.Add(track);
        parent.Add(row);
        return fill;
    }
}
