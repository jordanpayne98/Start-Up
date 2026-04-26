using System;
using System.Text;
using UnityEngine.UIElements;

public class ProductPlanReviewModalView : IGameView
{
    private readonly IModalPresenter _modal;

    public event Action OnConfirmed;

    private VisualElement _root;
    private CreateProductViewModel _vm;

    // Header
    private Button _closeButton;

    // Summary labels
    private Label _summaryName;
    private Label _summaryType;
    private Label _summaryPlatform;
    private Label _summaryReach;
    private Label _summaryGenre;

    // Fit
    private Label _expectedInterestLabel;
    private Label _wastedInterestLabel;
    private Label _scopeLabel;

    // Estimates
    private Label _etaLabel;
    private Label _estQualityLabel;
    private Label _upfrontCostLabel;

    // Tool costs
    private Label _royaltyCutLabel;

    // Pricing
    private Label _basePriceLabel;
    private Label _featurePriceLabel;
    private Label _marketPriceLabel;
    private Label _yourPriceLabel;

    // Plan review
    private readonly VisualElement[] _planReviewCards = new VisualElement[3];
    private readonly Label[] _planReviewStatusChips = new Label[3];
    private readonly Label[] _planReviewTitles = new Label[3];
    private readonly Label[] _planReviewWhyLabels = new Label[3];
    private readonly Label[] _planReviewEffectLabels = new Label[3];
    private readonly Label[] _planReviewChangeLabels = new Label[3];
    private Label _planReviewEmptyLabel;

    // Footer
    private Button _goBackButton;
    private Button _startDevButton;

    public ProductPlanReviewModalView(IModalPresenter modal) {
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("plan-review-modal");

        // Header
        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        var title = new Label("Confirm Development Plan");
        title.AddToClassList("text-xl");
        title.AddToClassList("text-bold");
        header.Add(title);

        _closeButton = new Button { text = "X" };
        _closeButton.AddToClassList("btn-sm");
        _closeButton.style.minWidth = 30;
        header.Add(_closeButton);
        _root.Add(header);

        // Body
        var bodyScroll = new ScrollView();
        bodyScroll.AddToClassList("modal-body");
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.flexShrink = 1;
        var body = bodyScroll.contentContainer;

        BuildProductSummaryCard(body);
        BuildMetricRow(body, "PRODUCT FIT", out var fitRow);
        _expectedInterestLabel = AddMetricField(fitRow, "Expected Interest");
        _wastedInterestLabel = AddMetricField(fitRow, "Wasted Effort");
        _scopeLabel = AddMetricField(fitRow, "Scope");

        BuildMetricRow(body, "ESTIMATES", out var estRow);
        _etaLabel = AddMetricField(estRow, "Dev Time");
        _estQualityLabel = AddMetricField(estRow, "Est. Quality");
        _upfrontCostLabel = AddMetricField(estRow, "Upfront Cost");

        BuildMetricRow(body, "TOOL COSTS", out var toolRow);
        _royaltyCutLabel = AddMetricField(toolRow, "Royalty Cut");

        BuildMetricRow(body, "PRICING", out var pricingRow);
        _basePriceLabel = AddMetricField(pricingRow, "Base Price");
        _featurePriceLabel = AddMetricField(pricingRow, "Feature Value");
        _marketPriceLabel = AddMetricField(pricingRow, "Market Price");
        _yourPriceLabel = AddMetricField(pricingRow, "Your Price");

        BuildPlanReviewSection(body);

        _root.Add(bodyScroll);

        // Footer
        var footer = new VisualElement();
        footer.AddToClassList("modal-footer");
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;

        _goBackButton = new Button { text = "Go Back" };
        _goBackButton.AddToClassList("btn-secondary");
        footer.Add(_goBackButton);

        _startDevButton = new Button { text = "Start Development" };
        _startDevButton.AddToClassList("btn-primary");
        footer.Add(_startDevButton);

        _root.Add(footer);

        _closeButton.clicked += OnCloseClicked;
        _goBackButton.clicked += OnGoBackClicked;
        _startDevButton.clicked += OnStartDevConfirmed;
    }

    public void Bind(IViewModel viewModel) {
        _vm = viewModel as CreateProductViewModel;
        if (_vm == null) return;

        BindProductSummary();
        BindProductFit();
        BindEstimates();
        BindToolCosts();
        BindPricingSummary();
        BindPlanReview();
    }

    public void Dispose() {
        if (_closeButton != null) _closeButton.clicked -= OnCloseClicked;
        if (_goBackButton != null) _goBackButton.clicked -= OnGoBackClicked;
        if (_startDevButton != null) _startDevButton.clicked -= OnStartDevConfirmed;
        _vm = null;
        _summaryName = null;
        _summaryType = null;
        _summaryPlatform = null;
        _summaryReach = null;
        _summaryGenre = null;
        _expectedInterestLabel = null;
        _wastedInterestLabel = null;
        _scopeLabel = null;
        _etaLabel = null;
        _estQualityLabel = null;
        _upfrontCostLabel = null;
        _royaltyCutLabel = null;
        _basePriceLabel = null;
        _featurePriceLabel = null;
        _marketPriceLabel = null;
        _yourPriceLabel = null;
        _planReviewEmptyLabel = null;
        for (int i = 0; i < 3; i++) {
            _planReviewCards[i] = null;
            _planReviewStatusChips[i] = null;
            _planReviewTitles[i] = null;
            _planReviewWhyLabels[i] = null;
            _planReviewEffectLabels[i] = null;
            _planReviewChangeLabels[i] = null;
        }
    }

    private void OnCloseClicked() {
        _modal?.DismissModal();
    }

    private void OnGoBackClicked() {
        _modal?.DismissModal();
    }

    private void OnStartDevConfirmed() {
        OnConfirmed?.Invoke();
        _modal?.DismissModal();
    }

    private void BuildProductSummaryCard(VisualElement parent) {
        var card = new VisualElement();
        card.AddToClassList("plan-review-modal__summary-card");

        var sectionTitle = new Label("PRODUCT SUMMARY");
        sectionTitle.AddToClassList("plan-review__title");
        card.Add(sectionTitle);

        _summaryName = AddLabelRow(card, "Name");
        _summaryType = AddLabelRow(card, "Type");
        _summaryPlatform = AddLabelRow(card, "Platform");
        _summaryReach = AddLabelRow(card, "Reach");
        _summaryGenre = AddLabelRow(card, "Genre");

        parent.Add(card);
    }

    private Label AddLabelRow(VisualElement parent, string labelText) {
        var row = new VisualElement();
        row.AddToClassList("plan-review-modal__metric-row");

        var key = new Label(labelText);
        key.AddToClassList("text-sm");
        key.AddToClassList("text-muted");
        row.Add(key);

        var value = new Label("—");
        value.AddToClassList("text-sm");
        value.AddToClassList("text-bold");
        row.Add(value);

        parent.Add(row);
        return value;
    }

    private void BuildMetricRow(VisualElement parent, string sectionTitle, out VisualElement container) {
        var card = new VisualElement();
        card.AddToClassList("plan-review-modal__summary-card");

        var title = new Label(sectionTitle);
        title.AddToClassList("plan-review__title");
        card.Add(title);

        container = card;
        parent.Add(card);
    }

    private Label AddMetricField(VisualElement parent, string labelText) {
        return AddLabelRow(parent, labelText);
    }

    private void BuildPlanReviewSection(VisualElement parent) {
        var container = new VisualElement();
        container.AddToClassList("plan-review-container");

        var sectionTitle = new Label("PLAN REVIEW");
        sectionTitle.AddToClassList("plan-review__title");
        container.Add(sectionTitle);

        for (int i = 0; i < 3; i++) {
            var card = new VisualElement();
            card.AddToClassList("plan-review__card");
            card.style.display = DisplayStyle.None;

            var statusChip = new Label();
            statusChip.AddToClassList("plan-review__status-chip");
            card.Add(statusChip);

            var cardTitle = new Label();
            cardTitle.AddToClassList("plan-review__card-title");
            card.Add(cardTitle);

            var whyLabel = new Label();
            whyLabel.AddToClassList("plan-review__why");
            card.Add(whyLabel);

            var effectLabel = new Label();
            effectLabel.AddToClassList("plan-review__effect");
            card.Add(effectLabel);

            var changeLabel = new Label();
            changeLabel.AddToClassList("plan-review__change");
            card.Add(changeLabel);

            _planReviewCards[i] = card;
            _planReviewStatusChips[i] = statusChip;
            _planReviewTitles[i] = cardTitle;
            _planReviewWhyLabels[i] = whyLabel;
            _planReviewEffectLabels[i] = effectLabel;
            _planReviewChangeLabels[i] = changeLabel;

            container.Add(card);
        }

        _planReviewEmptyLabel = new Label();
        _planReviewEmptyLabel.AddToClassList("plan-review__empty-label");
        _planReviewEmptyLabel.style.display = DisplayStyle.None;
        container.Add(_planReviewEmptyLabel);

        parent.Add(container);
    }

    private void BindProductSummary() {
        _summaryName.text = string.IsNullOrEmpty(_vm.ProductName) ? "—" : _vm.ProductName;

        string typeName = "—";
        int tc = _vm.Templates.Count;
        for (int i = 0; i < tc; i++) {
            if (_vm.Templates[i].TemplateId == _vm.SelectedTemplateId) {
                typeName = _vm.Templates[i].DisplayName;
                break;
            }
        }
        _summaryType.text = typeName;

        string platformNames = "—";
        var platIds = _vm.SelectedPlatformIds;
        if (platIds.Count > 0) {
            int pc = _vm.AvailablePlatforms.Count;
            var sb = new StringBuilder();
            int found = 0;
            for (int pi = 0; pi < pc; pi++) {
                for (int si = 0; si < platIds.Count; si++) {
                    if (_vm.AvailablePlatforms[pi].PlatformId == platIds[si]) {
                        if (found > 0) sb.Append(", ");
                        sb.Append(_vm.AvailablePlatforms[pi].DisplayName);
                        found++;
                        break;
                    }
                }
            }
            if (found > 0) platformNames = sb.ToString();
        }
        _summaryPlatform.text = platformNames;

        int totalReach = _vm.TotalPlatformUserReach;
        _summaryReach.text = totalReach > 0
            ? UIFormatting.FormatUserCount(totalReach) + " users"
            : "—";

        string nicheName = "—";
        if (_vm.SelectedNiche.HasValue) {
            int nc = _vm.AvailableNiches.Count;
            for (int i = 0; i < nc; i++) {
                if (_vm.AvailableNiches[i].Niche == _vm.SelectedNiche.Value) {
                    nicheName = _vm.AvailableNiches[i].DisplayName;
                    break;
                }
            }
        }
        _summaryGenre.text = nicheName;
    }

    private void BindProductFit() {
        _expectedInterestLabel.text = (_vm.ExpectedInterest * 100f).ToString("F1") + "%";
        _wastedInterestLabel.text = ((int)_vm.WastedInterest) + "%";
        _scopeLabel.text = _vm.ScopeDisplay + " | " + _vm.ScopeEfficiencyLabel;
    }

    private void BindEstimates() {
        _etaLabel.text = _vm.EstimatedCompletionLabel;
        _estQualityLabel.text = _vm.EstimatedQualityLabel;
        _upfrontCostLabel.text = UIFormatting.FormatMoney(_vm.CalculatedCost);
    }

    private void BindToolCosts() {
        bool hasDeps = _vm.RequiredToolCategories.Count > 0;
        _royaltyCutLabel.text = hasDeps
            ? (_vm.TotalRoyaltyCut * 100f).ToString("F1") + "%"
            : "—";
    }

    private void BindPricingSummary() {
        int basePrice = _vm.BaseProductPrice;
        int featureTotal = _vm.FeaturePriceTotal;

        _basePriceLabel.text = UIFormatting.FormatMoney(basePrice);
        _featurePriceLabel.text = featureTotal > 0
            ? "+" + UIFormatting.FormatMoney(featureTotal)
            : "$0";
        _marketPriceLabel.text = _vm.SweetSpotPrice > 0f
            ? UIFormatting.FormatMoney((long)_vm.SweetSpotPrice)
            : "—";
        _yourPriceLabel.text = _vm.Price > 0f
            ? UIFormatting.FormatMoney((long)_vm.Price)
            : "—";
    }

    private void BindPlanReview() {
        var display = _vm.PlanReviewDisplay;
        if (!display.HasCards) {
            for (int i = 0; i < 3; i++)
                _planReviewCards[i].style.display = DisplayStyle.None;
            _planReviewEmptyLabel.style.display = DisplayStyle.Flex;
            _planReviewEmptyLabel.text = display.EmptyText ?? "";
            return;
        }
        _planReviewEmptyLabel.style.display = DisplayStyle.None;
        for (int i = 0; i < 3; i++) {
            if (i < display.CardCount) {
                var card = display.Cards[i];
                _planReviewCards[i].style.display = DisplayStyle.Flex;
                _planReviewTitles[i].text = card.Title ?? "";
                _planReviewStatusChips[i].text = card.StatusText ?? "";
                _planReviewStatusChips[i].EnableInClassList("plan-review__status-chip--risk",        card.Status == ProductPlanReviewStatus.Risk);
                _planReviewStatusChips[i].EnableInClassList("plan-review__status-chip--opportunity", card.Status == ProductPlanReviewStatus.Opportunity);
                _planReviewStatusChips[i].EnableInClassList("plan-review__status-chip--tradeoff",    card.Status == ProductPlanReviewStatus.Tradeoff);
                _planReviewWhyLabels[i].text    = card.WhyText ?? "";
                _planReviewEffectLabels[i].text = card.EffectText ?? "";
                _planReviewChangeLabels[i].text = card.ChangeText ?? "";
            } else {
                _planReviewCards[i].style.display = DisplayStyle.None;
            }
        }
    }
}
