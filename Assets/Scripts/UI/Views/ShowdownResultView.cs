using UnityEngine.UIElements;

public class ShowdownResultView : IGameView
{
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private ShowdownResultViewModel _vm;

    private Label _nicheLabel;
    private Label _winnerProductLabel;
    private Label _winnerCompanyLabel;
    private Label _winnerQualityLabel;
    private VisualElement _winnerCard;
    private Label _loserProductLabel;
    private Label _loserCompanyLabel;
    private Label _loserQualityLabel;
    private VisualElement _loserCard;
    private Label _resultBannerLabel;
    private Label _churnPenaltyLabel;
    private Button _dismissButton;

    public ShowdownResultView(IModalPresenter modal) {
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("showdown-result-modal");

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var title = new Label("Showdown Result");
        title.AddToClassList("text-xl");
        title.AddToClassList("text-bold");
        header.Add(title);

        _nicheLabel = new Label();
        _nicheLabel.AddToClassList("badge");
        _nicheLabel.AddToClassList("badge--primary");
        header.Add(_nicheLabel);

        _root.Add(header);

        // Result banner
        _resultBannerLabel = new Label();
        _resultBannerLabel.AddToClassList("text-2xl");
        _resultBannerLabel.AddToClassList("text-bold");
        _resultBannerLabel.style.marginTop = 12;
        _resultBannerLabel.style.marginBottom = 12;
        _resultBannerLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        _root.Add(_resultBannerLabel);

        // Side by side cards
        var cardsRow = new VisualElement();
        cardsRow.style.flexDirection = FlexDirection.Row;
        cardsRow.style.flexGrow = 1;

        _winnerCard = new VisualElement();
        _winnerCard.AddToClassList("card");
        _winnerCard.style.flexGrow = 1;
        _winnerCard.style.marginRight = 8;
        _winnerCard.style.borderTopWidth = 2;
        _winnerCard.style.borderTopColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color(0.2f, 0.83f, 0.6f));

        var winnerBadge = new Label("WINNER");
        winnerBadge.AddToClassList("text-bold");
        winnerBadge.AddToClassList("text-success");
        winnerBadge.style.marginBottom = 8;
        _winnerCard.Add(winnerBadge);

        _winnerProductLabel = new Label();
        _winnerProductLabel.AddToClassList("text-lg");
        _winnerProductLabel.AddToClassList("text-bold");
        _winnerProductLabel.style.marginBottom = 4;
        _winnerCard.Add(_winnerProductLabel);

        _winnerCompanyLabel = new Label();
        _winnerCompanyLabel.AddToClassList("text-sm");
        _winnerCompanyLabel.AddToClassList("text-muted");
        _winnerCompanyLabel.style.marginBottom = 8;
        _winnerCard.Add(_winnerCompanyLabel);

        var winnerQRow = new VisualElement();
        winnerQRow.style.flexDirection = FlexDirection.Row;
        winnerQRow.style.justifyContent = Justify.SpaceBetween;
        var winQLabel = new Label("Quality");
        winQLabel.AddToClassList("text-sm");
        winQLabel.AddToClassList("text-muted");
        winnerQRow.Add(winQLabel);
        _winnerQualityLabel = new Label();
        _winnerQualityLabel.AddToClassList("text-sm");
        _winnerQualityLabel.AddToClassList("text-bold");
        winnerQRow.Add(_winnerQualityLabel);
        _winnerCard.Add(winnerQRow);

        cardsRow.Add(_winnerCard);

        _loserCard = new VisualElement();
        _loserCard.AddToClassList("card");
        _loserCard.style.flexGrow = 1;
        _loserCard.style.borderTopWidth = 2;
        _loserCard.style.borderTopColor = new UnityEngine.UIElements.StyleColor(new UnityEngine.Color(0.98f, 0.44f, 0.52f));

        var loserBadge = new Label("DEFEATED");
        loserBadge.AddToClassList("text-bold");
        loserBadge.AddToClassList("text-danger");
        loserBadge.style.marginBottom = 8;
        _loserCard.Add(loserBadge);

        _loserProductLabel = new Label();
        _loserProductLabel.AddToClassList("text-lg");
        _loserProductLabel.AddToClassList("text-bold");
        _loserProductLabel.style.marginBottom = 4;
        _loserCard.Add(_loserProductLabel);

        _loserCompanyLabel = new Label();
        _loserCompanyLabel.AddToClassList("text-sm");
        _loserCompanyLabel.AddToClassList("text-muted");
        _loserCompanyLabel.style.marginBottom = 8;
        _loserCard.Add(_loserCompanyLabel);

        var loserQRow = new VisualElement();
        loserQRow.style.flexDirection = FlexDirection.Row;
        loserQRow.style.justifyContent = Justify.SpaceBetween;
        var loseQLabel = new Label("Quality");
        loseQLabel.AddToClassList("text-sm");
        loseQLabel.AddToClassList("text-muted");
        loserQRow.Add(loseQLabel);
        _loserQualityLabel = new Label();
        _loserQualityLabel.AddToClassList("text-sm");
        _loserQualityLabel.AddToClassList("text-bold");
        loserQRow.Add(_loserQualityLabel);
        _loserCard.Add(loserQRow);

        cardsRow.Add(_loserCard);
        _root.Add(cardsRow);

        // Churn penalty
        var churnCard = new VisualElement();
        churnCard.AddToClassList("card");
        churnCard.style.marginTop = 12;

        var churnTitle = new Label("Churn Penalty");
        churnTitle.AddToClassList("text-bold");
        churnTitle.style.marginBottom = 4;
        churnCard.Add(churnTitle);

        _churnPenaltyLabel = new Label();
        _churnPenaltyLabel.AddToClassList("text-sm");
        churnCard.Add(_churnPenaltyLabel);
        _root.Add(churnCard);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.justifyContent = Justify.FlexEnd;
        _root.Add(footer);

        _dismissButton = new Button { text = "Dismiss" };
        _dismissButton.AddToClassList("btn-primary");
        footer.Add(_dismissButton);

        _dismissButton.clicked += OnDismissClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as ShowdownResultViewModel;
        if (_vm == null) return;

        _nicheLabel.text = _vm.Niche;
        _winnerProductLabel.text = _vm.WinnerProductName;
        _winnerCompanyLabel.text = _vm.WinnerCompanyName;
        _winnerQualityLabel.text = _vm.WinnerQuality;
        _loserProductLabel.text = _vm.LoserProductName;
        _loserCompanyLabel.text = _vm.LoserCompanyName;
        _loserQualityLabel.text = _vm.LoserQuality;
        _churnPenaltyLabel.text = _vm.ChurnPenaltyDescription;

        if (_vm.PlayerWon) {
            _resultBannerLabel.text = "You Won!";
            _resultBannerLabel.RemoveFromClassList("text-danger");
            _resultBannerLabel.AddToClassList("text-success");
        } else {
            _resultBannerLabel.text = "You Lost";
            _resultBannerLabel.RemoveFromClassList("text-success");
            _resultBannerLabel.AddToClassList("text-danger");
        }
    }

    public void Dispose() {
        if (_dismissButton != null) _dismissButton.clicked -= OnDismissClicked;
        _vm = null;
    }

    private void OnDismissClicked() {
        _modal?.DismissModal();
    }
}
