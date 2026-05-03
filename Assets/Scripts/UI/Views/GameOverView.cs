using UnityEngine.UIElements;

public class GameOverView : IGameView
{
    private readonly INavigationService _navigation;
    private VisualElement _root;
    private GameOverViewModel _vm;

    private Label _companyNameLabel;
    private Label _reasonLabel;
    private Label _timeSurvivedLabel;
    private Label _peakRevenueLabel;
    private Label _peakMarketShareLabel;
    private Label _productsShippedLabel;
    private Label _employeesHiredLabel;
    private Label _biggestCompetitorLabel;
    private Button _mainMenuButton;

    public GameOverView(INavigationService navigation) {
        _navigation = navigation;
    }

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;
        _root.AddToClassList("game-over-screen");
        _root.style.flexGrow = 1;
        _root.style.justifyContent = Justify.Center;
        _root.style.alignItems = Align.Center;

        var container = new VisualElement();
        container.style.maxWidth = 600;
        container.style.width = Length.Percent(90);

        // Game over banner
        var banner = new Label("GAME OVER");
        banner.AddToClassList("text-bold");
        banner.AddToClassList("text-danger");
        banner.style.fontSize = 48;
        banner.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        banner.style.marginBottom = 8;
        container.Add(banner);

        _companyNameLabel = new Label();
        _companyNameLabel.AddToClassList("text-2xl");
        _companyNameLabel.AddToClassList("text-bold");
        _companyNameLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        _companyNameLabel.style.marginBottom = 4;
        container.Add(_companyNameLabel);

        _reasonLabel = new Label();
        _reasonLabel.AddToClassList("text-lg");
        _reasonLabel.AddToClassList("text-muted");
        _reasonLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        _reasonLabel.style.marginBottom = 24;
        container.Add(_reasonLabel);

        // Stats card
        var statsCard = new VisualElement();
        statsCard.AddToClassList("card");
        statsCard.style.marginBottom = 24;

        var statsTitle = new Label("Final Statistics");
        statsTitle.AddToClassList("text-bold");
        statsTitle.AddToClassList("text-lg");
        statsTitle.style.marginBottom = 12;
        statsCard.Add(statsTitle);

        _timeSurvivedLabel = CreateStatRow(statsCard, "Time Survived");
        _peakRevenueLabel = CreateStatRow(statsCard, "Peak Revenue");
        _peakMarketShareLabel = CreateStatRow(statsCard, "Peak Market Share");
        _productsShippedLabel = CreateStatRow(statsCard, "Products Shipped");
        _employeesHiredLabel = CreateStatRow(statsCard, "Employees Hired");
        _biggestCompetitorLabel = CreateStatRow(statsCard, "Biggest Competitor");

        container.Add(statsCard);

        // Buttons
        var buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.justifyContent = Justify.Center;

        _mainMenuButton = new Button { text = "Main Menu" };
        _mainMenuButton.AddToClassList("btn-secondary");
        _mainMenuButton.style.marginRight = 12;
        _mainMenuButton.style.minWidth = 120;
        buttonRow.Add(_mainMenuButton);

        container.Add(buttonRow);
        _root.Add(container);

        _mainMenuButton.clicked += OnMainMenuClicked;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as GameOverViewModel;
        if (_vm == null) return;

        _companyNameLabel.text = _vm.CompanyName;
        _reasonLabel.text = _vm.GameOverReason;
        _timeSurvivedLabel.text = _vm.TimeSurvived;
        _peakRevenueLabel.text = _vm.PeakRevenue;
        _peakMarketShareLabel.text = _vm.PeakMarketShare;
        _productsShippedLabel.text = _vm.TotalProductsShipped.ToString();
        _employeesHiredLabel.text = _vm.TotalEmployeesHired.ToString();
        _biggestCompetitorLabel.text = _vm.BiggestCompetitor;
    }

    public void Dispose() {
        if (_mainMenuButton != null) _mainMenuButton.clicked -= OnMainMenuClicked;
        _vm = null;
    }

    private void OnMainMenuClicked() {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private Label CreateStatRow(VisualElement parent, string labelText) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 6;

        var label = new Label(labelText);
        label.AddToClassList("text-muted");
        row.Add(label);

        var value = new Label("--");
        value.AddToClassList("text-bold");
        row.Add(value);

        parent.Add(row);
        return value;
    }
}
