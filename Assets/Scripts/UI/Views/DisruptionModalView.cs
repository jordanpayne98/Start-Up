using UnityEngine.UIElements;

public class DisruptionModalView : IGameView
{
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private DisruptionModalViewModel _vm;

    private Label _titleLabel;
    private Label _descriptionLabel;
    private Label _durationLabel;
    private Label _impactLabel;
    private VisualElement _boostedContainer;
    private ElementPool _boostedPool;
    private VisualElement _penalizedContainer;
    private ElementPool _penalizedPool;
    private Button _dismissButton;

    public DisruptionModalView(IModalPresenter modal) {
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("disruption-modal");

        // Title section
        var titleSection = new VisualElement();
        titleSection.style.marginBottom = 16;
        titleSection.style.paddingBottom = 12;
        titleSection.style.borderBottomWidth = 1;
        titleSection.style.borderBottomColor = new UnityEngine.UIElements.StyleColor(
            new UnityEngine.Color(1f, 0.73f, 0.14f, 0.3f));

        var majorBadge = new Label("MAJOR DISRUPTION");
        majorBadge.AddToClassList("badge");
        majorBadge.AddToClassList("badge--warning");
        majorBadge.style.marginBottom = 8;
        titleSection.Add(majorBadge);

        _titleLabel = new Label();
        _titleLabel.AddToClassList("text-2xl");
        _titleLabel.AddToClassList("text-bold");
        _titleLabel.style.marginBottom = 8;
        _titleLabel.style.whiteSpace = WhiteSpace.Normal;
        titleSection.Add(_titleLabel);

        _descriptionLabel = new Label();
        _descriptionLabel.AddToClassList("text-sm");
        _descriptionLabel.AddToClassList("text-muted");
        _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
        titleSection.Add(_descriptionLabel);

        _root.Add(titleSection);

        // Body scroll
        var bodyScroll = new ScrollView();
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;
        _root.Add(bodyScroll);

        // Impact summary
        var summaryCard = new VisualElement();
        summaryCard.AddToClassList("card");
        summaryCard.style.marginBottom = 12;
        var summaryTitle = new Label("Impact");
        summaryTitle.AddToClassList("text-bold");
        summaryTitle.style.marginBottom = 4;
        summaryCard.Add(summaryTitle);
        _impactLabel = new Label();
        _impactLabel.AddToClassList("text-bold");
        _impactLabel.AddToClassList("text-warning");
        summaryCard.Add(_impactLabel);
        var durationRow = new VisualElement();
        durationRow.style.flexDirection = FlexDirection.Row;
        durationRow.style.marginTop = 4;
        var durationKey = new Label("Duration: ");
        durationKey.AddToClassList("text-sm");
        durationKey.AddToClassList("text-muted");
        durationRow.Add(durationKey);
        _durationLabel = new Label();
        _durationLabel.AddToClassList("text-sm");
        _durationLabel.AddToClassList("text-bold");
        durationRow.Add(_durationLabel);
        summaryCard.Add(durationRow);
        body.Add(summaryCard);

        // Boosted niches
        var boostedCard = new VisualElement();
        boostedCard.AddToClassList("card");
        boostedCard.style.marginBottom = 12;
        var boostedTitle = new Label("Boosted Niches");
        boostedTitle.AddToClassList("text-bold");
        boostedTitle.AddToClassList("text-success");
        boostedTitle.style.marginBottom = 8;
        boostedCard.Add(boostedTitle);
        _boostedContainer = new VisualElement();
        _boostedPool = new ElementPool(() => CreateNicheTag("text-success"), _boostedContainer);
        boostedCard.Add(_boostedContainer);
        body.Add(boostedCard);

        // Penalized niches
        var penalizedCard = new VisualElement();
        penalizedCard.AddToClassList("card");
        penalizedCard.style.marginBottom = 12;
        var penalizedTitle = new Label("Penalized Niches");
        penalizedTitle.AddToClassList("text-bold");
        penalizedTitle.AddToClassList("text-danger");
        penalizedTitle.style.marginBottom = 8;
        penalizedCard.Add(penalizedTitle);
        _penalizedContainer = new VisualElement();
        _penalizedPool = new ElementPool(() => CreateNicheTag("text-danger"), _penalizedContainer);
        penalizedCard.Add(_penalizedContainer);
        body.Add(penalizedCard);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.justifyContent = Justify.FlexEnd;
        _root.Add(footer);

        _dismissButton = new Button { text = "Acknowledge" };
        _dismissButton.AddToClassList("btn-primary");
        footer.Add(_dismissButton);

        _dismissButton.clicked += OnDismissClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as DisruptionModalViewModel;
        if (_vm == null) return;

        _titleLabel.text = _vm.Title;
        _descriptionLabel.text = _vm.Description;
        _durationLabel.text = _vm.Duration;
        _impactLabel.text = _vm.ImpactSummary;

        _boostedPool.UpdateList(_vm.NichesBoosted, BindNicheTag);
        _penalizedPool.UpdateList(_vm.NichesPenalized, BindNicheTag);
    }

    public void Dispose() {
        if (_dismissButton != null) _dismissButton.clicked -= OnDismissClicked;
        _boostedPool = null;
        _penalizedPool = null;
        _vm = null;
    }

    private void OnDismissClicked() {
        _modal?.DismissModal();
    }

    private VisualElement CreateNicheTag(string cssClass) {
        var label = new Label();
        label.name = "niche-tag";
        label.AddToClassList("badge");
        label.AddToClassList(cssClass);
        label.style.marginBottom = 4;
        label.style.marginRight = 4;
        return label;
    }

    private void BindNicheTag(VisualElement el, string nicheName) {
        var label = el.Q<Label>("niche-tag");
        if (label != null) label.text = nicheName;
    }
}
