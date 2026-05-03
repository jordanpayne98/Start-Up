using UnityEngine;
using UnityEngine.UIElements;

public class LoanApplicationView : IGameView
{
    private VisualElement _root;
    private LoanApplicationViewModel _viewModel;

    private Label _maxLabel;
    private Label _debtLabel;
    private Label _daysLabel;
    private Button _dismissButton;

    // Amount input
    private SliderInt _amountSlider;
    private Label _sliderValueLabel;
    private TextField _amountInput;

    // Duration slider
    private SliderInt _durationSlider;
    private Label _durationValueLabel;

    // Preview section
    private VisualElement _previewSection;
    private Label _previewRateLabel;
    private Label _previewTotalLabel;
    private Label _previewMonthlyLabel;
    private Label _previewInterestCostLabel;
    private Label _previewRiskLabel;
    private Label _previewUtilizationLabel;

    private Button _previewButton;
    private Button _submitButton;

    public void Initialize(VisualElement root, UIServices services) {
        _root = root;

        // ── Header ───────────────────────────────────────────────────────────────
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var title = new Label("Loan Application");
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

        // Loan info
        var infoCard = new VisualElement();
        infoCard.AddToClassList("card");
        infoCard.style.marginBottom = 16;

        _maxLabel = CreateInfoRow(infoCard, "Maximum Loan");
        _debtLabel = CreateInfoRow(infoCard, "Current Debt");

        body.Add(infoCard);

        // Amount section
        var amountSection = new VisualElement();
        amountSection.style.marginBottom = 16;

        var amountTitle = new Label("Loan Amount:");
        amountTitle.AddToClassList("text-bold");
        amountTitle.style.marginBottom = 4;
        amountSection.Add(amountTitle);

        var amountSliderRow = new VisualElement();
        amountSliderRow.style.flexDirection = FlexDirection.Row;
        amountSliderRow.style.alignItems = Align.Center;
        amountSliderRow.style.marginBottom = 8;

        _amountSlider = new SliderInt(0, 100000, SliderDirection.Horizontal, 1000);
        _amountSlider.style.flexGrow = 1;
        amountSliderRow.Add(_amountSlider);

        _sliderValueLabel = new Label("$0");
        _sliderValueLabel.AddToClassList("text-bold");
        _sliderValueLabel.style.minWidth = 80;
        _sliderValueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        amountSliderRow.Add(_sliderValueLabel);
        amountSection.Add(amountSliderRow);

        var manualRow = new VisualElement();
        manualRow.style.flexDirection = FlexDirection.Row;
        manualRow.style.alignItems = Align.Center;

        var dollar = new Label("$");
        dollar.style.marginRight = 4;
        manualRow.Add(dollar);

        _amountInput = new TextField();
        _amountInput.style.flexGrow = 1;
        _amountInput.style.marginRight = 8;
        manualRow.Add(_amountInput);

        _previewButton = new Button { text = "Preview" };
        _previewButton.AddToClassList("btn-secondary");
        _previewButton.AddToClassList("btn-sm");
        manualRow.Add(_previewButton);
        amountSection.Add(manualRow);
        body.Add(amountSection);

        // Duration section
        var durationSection = new VisualElement();
        durationSection.style.marginBottom = 16;

        var durationTitle = new Label("Loan Duration:");
        durationTitle.AddToClassList("text-bold");
        durationTitle.style.marginBottom = 4;
        durationSection.Add(durationTitle);

        var durationRow = new VisualElement();
        durationRow.style.flexDirection = FlexDirection.Row;
        durationRow.style.alignItems = Align.Center;

        _durationSlider = new SliderInt(1, 12, SliderDirection.Horizontal, 1);
        _durationSlider.style.flexGrow = 1;
        durationRow.Add(_durationSlider);

        _durationValueLabel = new Label("3 months");
        _durationValueLabel.AddToClassList("text-bold");
        _durationValueLabel.style.minWidth = 70;
        _durationValueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        durationRow.Add(_durationValueLabel);
        durationSection.Add(durationRow);
        body.Add(durationSection);

        // Preview section
        _previewSection = new VisualElement();
        _previewSection.AddToClassList("card");
        _previewSection.style.marginBottom = 16;
        _previewSection.style.display = DisplayStyle.None;

        var previewTitle = new Label("Loan Terms Preview");
        previewTitle.AddToClassList("text-bold");
        previewTitle.style.marginBottom = 8;
        _previewSection.Add(previewTitle);

        _previewRateLabel = CreateInfoRow(_previewSection, "Interest Rate");
        _previewTotalLabel = CreateInfoRow(_previewSection, "Total Owed");
        _previewMonthlyLabel = CreateInfoRow(_previewSection, "Monthly Payment");
        _previewInterestCostLabel = CreateInfoRow(_previewSection, "Interest Cost");
        _previewRiskLabel = CreateInfoRow(_previewSection, "Risk Level");
        _previewUtilizationLabel = CreateInfoRow(_previewSection, "Utilization");

        body.Add(_previewSection);

        // ── Footer ───────────────────────────────────────────────────────────────
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.justifyContent = Justify.FlexEnd;
        _root.Add(footer);

        _submitButton = new Button { text = "Take Loan" };
        _submitButton.AddToClassList("btn-primary");
        footer.Add(_submitButton);

        _dismissButton.clicked += OnDismissClicked;
        _previewButton.clicked += TriggerPreview;
        _submitButton.clicked += OnSubmitClicked;
        _amountSlider.RegisterValueChangedCallback(OnAmountSliderChanged);
        _durationSlider.RegisterValueChangedCallback(OnDurationSliderChanged);
    }

    public void Bind(IViewModel viewModel) {
        _viewModel = viewModel as LoanApplicationViewModel;
        if (_viewModel == null) return;

        _maxLabel.text = _viewModel.MaxAmountDisplay;
        _debtLabel.text = _viewModel.CurrentDebtDisplay;

        _amountSlider.highValue = _viewModel.MaxAmount > 0 ? _viewModel.MaxAmount : 100000;
        _durationSlider.lowValue = _viewModel.MinDuration;
        _durationSlider.highValue = _viewModel.MaxDuration;
        _durationSlider.value = _viewModel.SelectedDuration;
        _durationValueLabel.text = _viewModel.SelectedDuration + " months";

        _submitButton.SetEnabled(_viewModel.CanTakeLoan);

        if (string.IsNullOrEmpty(_amountInput.value) && _viewModel.MaxAmount > 0) {
            int defaultAmount = _viewModel.MaxAmount / 2;
            _amountInput.value = defaultAmount.ToString();
            _amountSlider.value = defaultAmount;
            _sliderValueLabel.text = UIFormatting.FormatMoney(defaultAmount);
        }
    }

    public void Dispose() {
        _dismissButton.clicked -= OnDismissClicked;
        _previewButton.clicked -= TriggerPreview;
        _submitButton.clicked -= OnSubmitClicked;
        _amountSlider.UnregisterValueChangedCallback(OnAmountSliderChanged);
        _durationSlider.UnregisterValueChangedCallback(OnDurationSliderChanged);
        _viewModel = null;
    }

    private void OnDismissClicked() {
        _viewModel?.RequestDismiss();
    }

    private void OnSubmitClicked() {
        if (_viewModel == null) return;
        if (int.TryParse(_amountInput.value, out int amount)) {
            _viewModel.SubmitLoan(amount, _durationSlider.value);
        }
    }

    private void OnAmountSliderChanged(ChangeEvent<int> evt) {
        _amountInput.value = evt.newValue.ToString();
        _sliderValueLabel.text = UIFormatting.FormatMoney(evt.newValue);
        TriggerPreview();
    }

    private void OnDurationSliderChanged(ChangeEvent<int> evt) {
        _viewModel?.SetDuration(evt.newValue);
        _durationValueLabel.text = evt.newValue + " months";
        TriggerPreview();
    }

    private void TriggerPreview() {
        if (_viewModel == null) return;
        if (int.TryParse(_amountInput.value, out int amount) && amount > 0) {
            _viewModel.PreviewLoan(amount, _durationSlider.value);
            RefreshPreview();
        }
    }

    private void RefreshPreview() {
        if (_viewModel == null || _viewModel.PreviewAmount <= 0) {
            _previewSection.style.display = DisplayStyle.None;
            return;
        }

        _previewSection.style.display = DisplayStyle.Flex;
        _previewRateLabel.text = _viewModel.PreviewInterestRateDisplay;
        _previewTotalLabel.text = _viewModel.PreviewTotalOwedDisplay;
        _previewMonthlyLabel.text = _viewModel.PreviewMonthlyPaymentDisplay;
        _previewInterestCostLabel.text = _viewModel.PreviewInterestCostDisplay;

        _previewRiskLabel.text = _viewModel.PreviewRiskBandDisplay;
        _previewRiskLabel.RemoveFromClassList("risk-safe");
        _previewRiskLabel.RemoveFromClassList("risk-standard");
        _previewRiskLabel.RemoveFromClassList("risk-aggressive");
        _previewRiskLabel.RemoveFromClassList("risk-extreme");
        _previewRiskLabel.AddToClassList(_viewModel.PreviewRiskBandClass);

        _previewUtilizationLabel.text = _viewModel.PreviewUtilizationDisplay;
    }

    private Label CreateInfoRow(VisualElement parent, string label) {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        var labelEl = new Label(label);
        labelEl.AddToClassList("text-secondary");
        row.Add(labelEl);

        var valueEl = new Label("--");
        valueEl.AddToClassList("text-bold");
        row.Add(valueEl);

        parent.Add(row);
        return valueEl;
    }
}
