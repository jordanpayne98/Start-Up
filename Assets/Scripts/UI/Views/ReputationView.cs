using System.Collections.Generic;
using UnityEngine.UIElements;

public class ReputationView : IGameView
{
    private readonly ITooltipProvider _tooltipProvider;
    private VisualElement _root;
    private VisualElement _reputationCard;
    private Label _tierLabel;
    private Label _scoreLabel;
    private VisualElement _progressFill;
    private Label _progressText;
    private VisualElement _milestoneContainer;
    private ElementPool _milestonePool;

    // Company Fans card
    private Label _fansCountLabel;
    private VisualElement _sentimentFill;
    private Label _sentimentLabel;
    private Label _fanLaunchBonusLabel;
    private Label _fanWomBonusLabel;

    // Category Reputation card
    private VisualElement _categoryContainer;
    private ElementPool _categoryPool;

    // Stats card
    private Label _contractsCompletedLabel;
    private Label _productsShippedLabel;

    // Industry Rankings card
    private VisualElement _rankingsContainer;
    private ElementPool _rankingsPool;

    public ReputationView(ITooltipProvider tooltipProvider) {
        _tooltipProvider = tooltipProvider;
    }

    public void Initialize(VisualElement root) {
        _root = root;

        var title = new Label("Reputation");
        title.AddToClassList("section-header");
        _root.Add(title);

        // Main reputation card
        _reputationCard = new VisualElement();
        _reputationCard.AddToClassList("card");
        _reputationCard.style.marginBottom = 16;
        _reputationCard.SetRichTooltip("topbar.reputation", _tooltipProvider.TooltipService);

        _tierLabel = new Label("Unknown");
        _tierLabel.AddToClassList("text-2xl");
        _tierLabel.AddToClassList("text-bold");
        _tierLabel.AddToClassList("text-accent");
        _reputationCard.Add(_tierLabel);

        _scoreLabel = new Label("0");
        _scoreLabel.AddToClassList("text-muted");
        _scoreLabel.style.marginTop = 4;
        _scoreLabel.style.marginBottom = 12;
        _reputationCard.Add(_scoreLabel);

        // Progress bar
        var progressBar = new VisualElement();
        progressBar.AddToClassList("progress-bar");
        progressBar.style.height = 12;
        _progressFill = new VisualElement();
        _progressFill.AddToClassList("progress-bar__fill");
        progressBar.Add(_progressFill);
        _reputationCard.Add(progressBar);

        _progressText = new Label("0%");
        _progressText.AddToClassList("text-sm");
        _progressText.AddToClassList("text-muted");
        _progressText.style.marginTop = 4;
        _reputationCard.Add(_progressText);

        _root.Add(_reputationCard);

        // Milestones
        var milestonesCard = new VisualElement();
        milestonesCard.AddToClassList("card");

        var milestonesTitle = new Label("Milestones");
        milestonesTitle.AddToClassList("text-bold");
        milestonesTitle.style.marginBottom = 8;
        milestonesCard.Add(milestonesTitle);

        _milestoneContainer = new VisualElement();
        _milestonePool = new ElementPool(CreateMilestoneItem, _milestoneContainer);
        milestonesCard.Add(_milestoneContainer);

        _root.Add(milestonesCard);

        // Industry Rankings card
        var rankingsCard = new VisualElement();
        rankingsCard.AddToClassList("card");
        rankingsCard.style.marginTop = 16;

        var rankingsTitle = new Label("Industry Rankings");
        rankingsTitle.AddToClassList("text-bold");
        rankingsTitle.style.marginBottom = 4;
        rankingsCard.Add(rankingsTitle);

        // Column header row
        var rankHeader = new VisualElement();
        rankHeader.AddToClassList("flex-row");
        rankHeader.style.marginBottom = 6;

        var rankNoHdr = new Label("#");
        rankNoHdr.AddToClassList("text-xs");
        rankNoHdr.AddToClassList("text-muted");
        rankNoHdr.style.width = 24;
        rankHeader.Add(rankNoHdr);

        var nameHdr = new Label("Company");
        nameHdr.AddToClassList("text-xs");
        nameHdr.AddToClassList("text-muted");
        nameHdr.style.flexGrow = 1;
        rankHeader.Add(nameHdr);

        var repHdr = new Label("Rep");
        repHdr.AddToClassList("text-xs");
        repHdr.AddToClassList("text-muted");
        repHdr.style.width = 48;
        repHdr.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
        rankHeader.Add(repHdr);

        var shareHdr = new Label("Market");
        shareHdr.AddToClassList("text-xs");
        shareHdr.AddToClassList("text-muted");
        shareHdr.style.width = 56;
        shareHdr.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
        rankHeader.Add(shareHdr);

        rankingsCard.Add(rankHeader);

        _rankingsContainer = new VisualElement();
        _rankingsPool = new ElementPool(CreateRankingItem, _rankingsContainer);
        rankingsCard.Add(_rankingsContainer);

        _root.Add(rankingsCard);

        // Company Fans card
        var fansCard = new VisualElement();
        fansCard.AddToClassList("card");
        fansCard.style.marginTop = 16;

        var fansTitle = new Label("Company Fans");
        fansTitle.AddToClassList("text-bold");
        fansTitle.style.marginBottom = 8;
        fansCard.Add(fansTitle);

        _fansCountLabel = new Label("0");
        _fansCountLabel.AddToClassList("text-2xl");
        _fansCountLabel.AddToClassList("text-accent");
        fansCard.Add(_fansCountLabel);

        var sentimentRow = new VisualElement();
        sentimentRow.AddToClassList("flex-row");
        sentimentRow.AddToClassList("align-center");
        sentimentRow.style.marginTop = 8;

        var sentimentLabelTitle = new Label("Sentiment");
        sentimentLabelTitle.AddToClassList("text-sm");
        sentimentLabelTitle.style.marginRight = 8;
        sentimentRow.Add(sentimentLabelTitle);

        var sentimentBar = new VisualElement();
        sentimentBar.AddToClassList("progress-bar");
        sentimentBar.style.height = 8;
        sentimentBar.style.flexGrow = 1;
        _sentimentFill = new VisualElement();
        _sentimentFill.AddToClassList("progress-bar__fill");
        sentimentBar.Add(_sentimentFill);
        sentimentRow.Add(sentimentBar);

        _sentimentLabel = new Label("50%");
        _sentimentLabel.AddToClassList("text-sm");
        _sentimentLabel.AddToClassList("text-muted");
        _sentimentLabel.style.marginLeft = 8;
        sentimentRow.Add(_sentimentLabel);

        fansCard.Add(sentimentRow);

        _fanLaunchBonusLabel = new Label("Launch Bonus: +0%");
        _fanLaunchBonusLabel.AddToClassList("text-sm");
        _fanLaunchBonusLabel.AddToClassList("text-muted");
        _fanLaunchBonusLabel.style.marginTop = 4;
        fansCard.Add(_fanLaunchBonusLabel);

        _fanWomBonusLabel = new Label("Word of Mouth: +0");
        _fanWomBonusLabel.AddToClassList("text-sm");
        _fanWomBonusLabel.AddToClassList("text-muted");
        _fanWomBonusLabel.style.marginTop = 2;
        fansCard.Add(_fanWomBonusLabel);

        _root.Add(fansCard);

        // Category Reputation card
        var categoryCard = new VisualElement();
        categoryCard.AddToClassList("card");
        categoryCard.style.marginTop = 16;

        var categoryTitle = new Label("Category Reputation");
        categoryTitle.AddToClassList("text-bold");
        categoryTitle.style.marginBottom = 8;
        categoryCard.Add(categoryTitle);

        _categoryContainer = new VisualElement();
        _categoryPool = new ElementPool(CreateCategoryItem, _categoryContainer);
        categoryCard.Add(_categoryContainer);

        _root.Add(categoryCard);

        // Stats card
        var statsCard = new VisualElement();
        statsCard.AddToClassList("card");
        statsCard.style.marginTop = 16;

        var statsTitle = new Label("Statistics");
        statsTitle.AddToClassList("text-bold");
        statsTitle.style.marginBottom = 8;
        statsCard.Add(statsTitle);

        _contractsCompletedLabel = new Label("Contracts Completed: 0");
        _contractsCompletedLabel.AddToClassList("text-sm");
        statsCard.Add(_contractsCompletedLabel);

        _productsShippedLabel = new Label("Products Shipped: 0");
        _productsShippedLabel.AddToClassList("text-sm");
        _productsShippedLabel.style.marginTop = 4;
        statsCard.Add(_productsShippedLabel);

        _root.Add(statsCard);
    }

    public void Bind(IViewModel viewModel) {
        var vm = viewModel as ReputationViewModel;
        if (vm == null) return;

        _tierLabel.text = vm.TierName;
        _scoreLabel.text = "Score: " + vm.Score + " / " + vm.NextTierThreshold;
        _progressFill.style.width = Length.Percent(vm.ProgressPercent * 100f);
        _progressText.text = UIFormatting.FormatPercent(vm.ProgressPercent) + " to next tier";

        _milestonePool.UpdateList(vm.Milestones, BindMilestoneItem);

        _rankingsPool.UpdateList(vm.IndustryRankings, BindRankingItem);

        _fansCountLabel.text = vm.CompanyFans.ToString("N0");
        _sentimentFill.style.width = Length.Percent(vm.FanSentiment);
        _sentimentFill.EnableInClassList("sentiment-low", vm.FanSentiment < 30f);
        _sentimentFill.EnableInClassList("sentiment-mid", vm.FanSentiment >= 30f && vm.FanSentiment < 70f);
        _sentimentFill.EnableInClassList("sentiment-high", vm.FanSentiment >= 70f);
        _sentimentLabel.text = ((int)vm.FanSentiment) + "%";
        _fanLaunchBonusLabel.text = vm.LaunchBonusText;
        _fanWomBonusLabel.text = vm.WomBonusText;

        _categoryPool.UpdateList(vm.TopCategories, BindCategoryItem);

        _contractsCompletedLabel.text = "Contracts Completed: " + vm.ContractsCompleted;
        _productsShippedLabel.text = "Products Shipped: " + vm.ProductsShipped;
    }

    public void Dispose() {
        _reputationCard?.ClearTooltip(_tooltipProvider.TooltipService);
        _milestonePool = null;
        _categoryPool = null;
        _rankingsPool = null;
        _fansCountLabel = null;
        _sentimentFill = null;
        _sentimentLabel = null;
        _fanLaunchBonusLabel = null;
        _fanWomBonusLabel = null;
        _categoryContainer = null;
        _rankingsContainer = null;
        _contractsCompletedLabel = null;
        _productsShippedLabel = null;
    }

    private VisualElement CreateMilestoneItem() {
        var item = new VisualElement();
        item.AddToClassList("flex-row");
        item.AddToClassList("align-center");
        item.style.marginBottom = 8;

        var indicator = new VisualElement();
        indicator.name = "milestone-indicator";
        indicator.style.width = 12;
        indicator.style.height = 12;
        indicator.style.borderTopLeftRadius = 6;
        indicator.style.borderTopRightRadius = 6;
        indicator.style.borderBottomLeftRadius = 6;
        indicator.style.borderBottomRightRadius = 6;
        indicator.style.marginRight = 12;
        item.Add(indicator);

        var nameLabel = new Label();
        nameLabel.name = "milestone-name";
        nameLabel.style.flexGrow = 1;
        item.Add(nameLabel);

        var thresholdLabel = new Label();
        thresholdLabel.name = "milestone-threshold";
        thresholdLabel.AddToClassList("text-sm");
        thresholdLabel.AddToClassList("text-muted");
        item.Add(thresholdLabel);

        return item;
    }

    private void BindMilestoneItem(VisualElement el, ReputationMilestoneDisplay data) {
        el.Q<Label>("milestone-name").text = data.TierName;
        el.Q<Label>("milestone-threshold").text = data.Threshold.ToString();
        var indicator = el.Q<VisualElement>("milestone-indicator");
        if (indicator != null) {
            indicator.style.backgroundColor = data.IsUnlocked
                ? new StyleColor(new UnityEngine.Color(0.32f, 0.72f, 0.53f, 1f))
                : new StyleColor(new UnityEngine.Color(0.4f, 0.4f, 0.4f, 0.5f));
        }
    }

    private VisualElement CreateRankingItem() {
        var row = new VisualElement();
        row.AddToClassList("flex-row");
        row.AddToClassList("align-center");
        row.style.marginBottom = 6;

        var rankLabel = new Label();
        rankLabel.name = "rank-no";
        rankLabel.AddToClassList("text-sm");
        rankLabel.AddToClassList("text-muted");
        rankLabel.style.width = 24;
        row.Add(rankLabel);

        var nameLabel = new Label();
        nameLabel.name = "rank-name";
        nameLabel.AddToClassList("text-sm");
        nameLabel.style.flexGrow = 1;
        row.Add(nameLabel);

        var repLabel = new Label();
        repLabel.name = "rank-rep";
        repLabel.AddToClassList("text-sm");
        repLabel.style.width = 48;
        repLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
        row.Add(repLabel);

        var shareLabel = new Label();
        shareLabel.name = "rank-share";
        shareLabel.AddToClassList("text-sm");
        shareLabel.AddToClassList("text-muted");
        shareLabel.style.width = 56;
        shareLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
        row.Add(shareLabel);

        var badgeLabel = new Label();
        badgeLabel.name = "rank-badge";
        badgeLabel.AddToClassList("badge");
        badgeLabel.AddToClassList("text-xs");
        badgeLabel.style.display = DisplayStyle.None;
        badgeLabel.style.marginLeft = 4;
        row.Add(badgeLabel);

        return row;
    }

    private static void BindRankingItem(VisualElement el, IndustryRankingDisplay data) {
        el.Q<Label>("rank-no").text = "#" + data.Rank;
        el.Q<Label>("rank-rep").text = data.ReputationDisplay;
        el.Q<Label>("rank-share").text = data.MarketShareDisplay;

        var nameLabel = el.Q<Label>("rank-name");
        if (nameLabel != null) {
            nameLabel.text = data.EntityName;
            if (data.IsPlayer) {
                nameLabel.RemoveFromClassList("text-muted");
                nameLabel.AddToClassList("text-accent");
                nameLabel.AddToClassList("text-bold");
            } else {
                nameLabel.RemoveFromClassList("text-accent");
                nameLabel.RemoveFromClassList("text-bold");
                nameLabel.AddToClassList("text-muted");
            }
        }

        var badge = el.Q<Label>("rank-badge");
        if (badge != null) {
            if (data.IsBankrupt) {
                badge.style.display = DisplayStyle.Flex;
                badge.text = "Bankrupt";
                badge.AddToClassList("badge--danger");
                badge.RemoveFromClassList("badge--warning");
            } else if (data.IsAbsorbed) {
                badge.style.display = DisplayStyle.Flex;
                badge.text = "Absorbed";
                badge.AddToClassList("badge--warning");
                badge.RemoveFromClassList("badge--danger");
            } else {
                badge.style.display = DisplayStyle.None;
            }
        }
    }

    private VisualElement CreateCategoryItem() {
        var item = new VisualElement();
        item.AddToClassList("flex-row");
        item.AddToClassList("align-center");
        item.style.marginBottom = 6;

        var nameLabel = new Label();
        nameLabel.name = "category-name";
        nameLabel.style.flexGrow = 1;
        item.Add(nameLabel);

        var scoreLabel = new Label();
        scoreLabel.name = "category-score";
        scoreLabel.AddToClassList("text-sm");
        scoreLabel.AddToClassList("text-muted");
        scoreLabel.style.marginRight = 8;
        item.Add(scoreLabel);

        var tierLabel = new Label();
        tierLabel.name = "category-tier";
        tierLabel.AddToClassList("text-sm");
        tierLabel.AddToClassList("text-accent");
        item.Add(tierLabel);

        return item;
    }

    private void BindCategoryItem(VisualElement el, CategoryReputationDisplay data) {
        el.Q<Label>("category-name").text = data.CategoryName;
        el.Q<Label>("category-score").text = data.Score.ToString();
        el.Q<Label>("category-tier").text = data.TierName;
    }
}
