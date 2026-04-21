using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HireConfirmationView : IGameView
{
    private VisualElement _root;
    private HireConfirmationViewModel _viewModel;

    // Candidate info
    private Label _nameLabel;
    private Label _roleLabel;
    private Label _salaryLabel;
    private Label _expiryLabel;
    private VisualElement _abilityStars;
    private VisualElement _potentialStars;
    private VisualElement _skillsContainer;
    private ElementPool _skillPool;

    // Offer section
    private VisualElement _offerSection;
    private TextField _offerInput;
    private Button _previewButton;
    private Button _submitOfferButton;

    // Probability preview
    private VisualElement _probSection;
    private Label _acceptLabel;
    private Label _counterLabel;
    private Label _declineLabel;
    private VisualElement _acceptBar;
    private VisualElement _counterBar;
    private VisualElement _declineBar;

    // Counter-offer section
    private VisualElement _counterSection;
    private Label _counterSalaryLabel;
    private Button _acceptCounterButton;
    private Button _rejectCounterButton;

    // Negotiation status
    private Label _statusLabel;

    // Actions
    private Button _dismissButton;

    public void Initialize(VisualElement root) {
        _root = root;

        // ── Header ───────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var title = new Label("Hire Candidate");
        title.AddToClassList("text-xl");
        title.AddToClassList("text-bold");
        header.Add(title);

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

        // Candidate info card
        var infoCard = new VisualElement();
        infoCard.AddToClassList("card");
        infoCard.style.marginBottom = 16;

        _nameLabel = new Label();
        _nameLabel.AddToClassList("text-lg");
        _nameLabel.AddToClassList("text-bold");
        infoCard.Add(_nameLabel);

        var infoRow = new VisualElement();
        infoRow.style.flexDirection = FlexDirection.Row;
        infoRow.style.marginTop = 4;

        _roleLabel = new Label();
        _roleLabel.AddToClassList("badge");
        _roleLabel.AddToClassList("badge--primary");
        _roleLabel.style.marginRight = 12;
        infoRow.Add(_roleLabel);

        _salaryLabel = new Label();
        _salaryLabel.AddToClassList("text-muted");
        infoRow.Add(_salaryLabel);

        infoCard.Add(infoRow);

        // Stars
        var starRow = new VisualElement();
        starRow.style.flexDirection = FlexDirection.Row;
        starRow.style.marginTop = 8;

        var caLabel = new Label("Ability: ");
        caLabel.AddToClassList("text-sm");
        caLabel.AddToClassList("text-muted");
        starRow.Add(caLabel);
        _abilityStars = CreateStarRow();
        starRow.Add(_abilityStars);

        var paLabel = new Label("  Potential: ");
        paLabel.AddToClassList("text-sm");
        paLabel.AddToClassList("text-muted");
        paLabel.style.marginLeft = 16;
        starRow.Add(paLabel);
        _potentialStars = CreateStarRow();
        starRow.Add(_potentialStars);

        infoCard.Add(starRow);

        _expiryLabel = new Label();
        _expiryLabel.AddToClassList("text-sm");
        _expiryLabel.AddToClassList("text-warning");
        _expiryLabel.style.marginTop = 4;
        infoCard.Add(_expiryLabel);

        // Skills
        _skillsContainer = new VisualElement();
        _skillsContainer.style.marginTop = 8;
        _skillPool = new ElementPool(CreateSkillBar, _skillsContainer);
        infoCard.Add(_skillsContainer);

        body.Add(infoCard);

        // Status
        _statusLabel = new Label();
        _statusLabel.AddToClassList("text-bold");
        _statusLabel.AddToClassList("text-accent");
        _statusLabel.style.marginBottom = 8;
        body.Add(_statusLabel);

        // Offer section
        _offerSection = new VisualElement();
        _offerSection.style.marginBottom = 16;

        var offerLabel = new Label("Make an offer:");
        offerLabel.AddToClassList("text-bold");
        offerLabel.style.marginBottom = 4;
        _offerSection.Add(offerLabel);

        var offerRow = new VisualElement();
        offerRow.style.flexDirection = FlexDirection.Row;
        offerRow.style.alignItems = Align.Center;

        _offerInput = new TextField();
        _offerInput.style.flexGrow = 1;
        _offerInput.style.marginRight = 8;
        offerRow.Add(_offerInput);

        _previewButton = new Button { text = "Preview" };
        _previewButton.AddToClassList("btn-secondary");
        _previewButton.AddToClassList("btn-sm");
        _previewButton.style.marginRight = 4;
        offerRow.Add(_previewButton);

        _submitOfferButton = new Button { text = "Send Offer" };
        _submitOfferButton.AddToClassList("btn-primary");
        _submitOfferButton.AddToClassList("btn-sm");
        offerRow.Add(_submitOfferButton);

        _offerSection.Add(offerRow);
        body.Add(_offerSection);

        // Probability preview
        _probSection = new VisualElement();
        _probSection.AddToClassList("card");
        _probSection.style.marginBottom = 16;
        _probSection.style.display = DisplayStyle.None;

        var probTitle = new Label("Outcome Probabilities");
        probTitle.AddToClassList("text-sm");
        probTitle.AddToClassList("text-bold");
        probTitle.style.marginBottom = 8;
        _probSection.Add(probTitle);

        _acceptLabel = new Label();
        _acceptBar = CreateProbBar(_probSection, _acceptLabel, "Accept", new Color(0.32f, 0.72f, 0.53f));

        _counterLabel = new Label();
        _counterBar = CreateProbBar(_probSection, _counterLabel, "Counter", new Color(0.96f, 0.64f, 0.38f));

        _declineLabel = new Label();
        _declineBar = CreateProbBar(_probSection, _declineLabel, "Decline", new Color(0.9f, 0.22f, 0.27f));

        body.Add(_probSection);

        // Counter-offer section
        _counterSection = new VisualElement();
        _counterSection.AddToClassList("card");
        _counterSection.style.marginBottom = 16;
        _counterSection.style.display = DisplayStyle.None;

        var counterTitle = new Label("Counter Offer Received!");
        counterTitle.AddToClassList("text-bold");
        counterTitle.AddToClassList("text-warning");
        counterTitle.style.marginBottom = 8;
        _counterSection.Add(counterTitle);

        _counterSalaryLabel = new Label();
        _counterSalaryLabel.AddToClassList("text-lg");
        _counterSalaryLabel.style.marginBottom = 12;
        _counterSection.Add(_counterSalaryLabel);

        var counterButtons = new VisualElement();
        counterButtons.style.flexDirection = FlexDirection.Row;
        counterButtons.style.justifyContent = Justify.FlexEnd;

        _rejectCounterButton = new Button { text = "Reject" };
        _rejectCounterButton.AddToClassList("btn-danger");
        _rejectCounterButton.AddToClassList("btn-sm");
        _rejectCounterButton.style.marginRight = 8;
        counterButtons.Add(_rejectCounterButton);

        _acceptCounterButton = new Button { text = "Accept Counter" };
        _acceptCounterButton.AddToClassList("btn-primary");
        _acceptCounterButton.AddToClassList("btn-sm");
        counterButtons.Add(_acceptCounterButton);

        _counterSection.Add(counterButtons);
        body.Add(_counterSection);

        _dismissButton.clicked += OnDismissClicked;
        _submitOfferButton.clicked += OnSubmitOfferClicked;
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as HireConfirmationViewModel;
        if (_viewModel == null) return;

        _nameLabel.text = _viewModel.Name;
        _roleLabel.text = _viewModel.Role;
        _salaryLabel.text = "Asking: " + _viewModel.SalaryDisplay;
        _expiryLabel.text = "Expires: " + _viewModel.ExpiryDisplay;

        UpdateStars(_abilityStars, _viewModel.AbilityStars);
        UpdateStars(_potentialStars, _viewModel.PotentialStars);
        _skillPool.UpdateList(_viewModel.Skills, BindSkillBar);

        if (string.IsNullOrEmpty(_offerInput.value)) {
            _offerInput.value = _viewModel.Salary.ToString();
        }

        UpdateNegotiationState();
    }

    public void Dispose() {
        _dismissButton.clicked -= OnDismissClicked;
        _submitOfferButton.clicked -= OnSubmitOfferClicked;
        _viewModel = null;
        _skillPool = null;
    }

    private void OnDismissClicked() {
        _viewModel?.Dismiss();
    }

    private void OnSubmitOfferClicked() {
        if (_viewModel == null) return;
        if (int.TryParse(_offerInput.value, out int offer)) {
            _viewModel.SubmitOffer(offer);
        }
    }

    private void UpdateNegotiationState() {
        if (_viewModel == null) return;

        if (_viewModel.HasActiveNegotiation) {
            switch (_viewModel.NegotiationStatus) {
                case NegotiationStatus.Pending:
                    _statusLabel.text = "Offer sent - waiting for response...";
                    _offerSection.style.display = DisplayStyle.None;
                    _counterSection.style.display = DisplayStyle.None;
                    break;
                case NegotiationStatus.Accepted:
                    _statusLabel.text = "Offer accepted!";
                    _offerSection.style.display = DisplayStyle.None;
                    _counterSection.style.display = DisplayStyle.None;
                    break;
                case NegotiationStatus.Rejected:
                    _statusLabel.text = "Offer rejected.";
                    _offerSection.style.display = DisplayStyle.Flex;
                    _counterSection.style.display = DisplayStyle.None;
                    break;
                default:
                    _offerSection.style.display = DisplayStyle.Flex;
                    _counterSection.style.display = DisplayStyle.None;
                    break;
            }
        } else {
            _statusLabel.text = "";
            _offerSection.style.display = DisplayStyle.Flex;
            _counterSection.style.display = DisplayStyle.None;
        }
    }

    private void UpdateProbabilities() {
        if (_probSection != null) _probSection.style.display = DisplayStyle.None;
    }

    // --- Helpers ---

    private VisualElement CreateStarRow() {
        var container = new VisualElement();
        container.AddToClassList("star-rating");
        for (int i = 0; i < 5; i++) {
            var star = new VisualElement();
            star.AddToClassList("star");
            star.AddToClassList("star-empty");
            container.Add(star);
        }
        return container;
    }

    private void UpdateStars(VisualElement container, int filled) {
        int childCount = container.childCount;
        for (int i = 0; i < childCount && i < 5; i++) {
            var star = container[i];
            star.RemoveFromClassList("star-filled");
            star.RemoveFromClassList("star-empty");
            star.AddToClassList(i < filled ? "star-filled" : "star-empty");
        }
    }

    private VisualElement CreateSkillBar() {
        var row = new VisualElement();
        row.AddToClassList("stat-bar");

        var label = new Label();
        label.name = "skill-label";
        label.AddToClassList("stat-bar__label");
        row.Add(label);

        var track = new VisualElement();
        track.AddToClassList("stat-bar__track");
        var fill = new VisualElement();
        fill.name = "skill-fill";
        fill.AddToClassList("stat-bar__fill");
        track.Add(fill);
        row.Add(track);

        var value = new Label();
        value.name = "skill-value";
        value.AddToClassList("stat-bar__value");
        row.Add(value);

        return row;
    }

    private void BindSkillBar(VisualElement el, SkillDisplay data) {
        el.Q<Label>("skill-label").text = data.Name;
        el.Q<Label>("skill-value").text = data.Value.ToString();
        var fill = el.Q<VisualElement>("skill-fill");
        if (fill != null) {
            float percent = data.MaxValue > 0 ? (float)data.Value / data.MaxValue * 100f : 0f;
            fill.style.width = Length.Percent(percent);
        }
    }

    private VisualElement CreateProbBar(VisualElement parent, Label label, string name, Color color) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;

        label.text = name + ": 0%";
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
        fill.style.height = new StyleLength(StyleKeyword.Auto);
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
