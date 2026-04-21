using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProductReviewModalView : IGameView
{
    private readonly IModalPresenter _modal;
    private VisualElement _root;
    private ProductReviewViewModel _vm;

    private Label _titleLabel;
    private Label _aggregateScoreLabel;
    private Label _aggregateRatingLabel;
    private VisualElement _dimensionsContainer;
    private VisualElement _outletsContainer;
    private Button _dismissButton;

    private readonly List<DimensionRow> _dimensionRows = new List<DimensionRow>(6);
    private readonly List<OutletCard> _outletCards = new List<OutletCard>(7);

    private struct DimensionRow
    {
        public VisualElement Container;
        public Label NameLabel;
        public VisualElement Bar;
        public Label ScoreLabel;
    }

    private struct OutletCard
    {
        public VisualElement Root;
        public Label OutletNameLabel;
        public Label OutletStyleLabel;
        public Label ScoreLabel;
        public Label RatingLabel;
        public Label TopLabel;
        public Label WeakLabel;
    }

    public ProductReviewModalView(IModalPresenter modal) {
        _modal = modal;
    }

    public void Initialize(VisualElement root) {
        _root = root;
        _root.AddToClassList("product-review-modal");

        var header = new VisualElement();
        header.AddToClassList("modal-header");
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;

        _titleLabel = new Label();
        _titleLabel.AddToClassList("text-xl");
        _titleLabel.AddToClassList("text-bold");
        header.Add(_titleLabel);

        var scoreBadge = new VisualElement();
        scoreBadge.style.flexDirection = FlexDirection.Row;
        scoreBadge.style.alignItems = Align.Center;

        _aggregateScoreLabel = new Label();
        _aggregateScoreLabel.AddToClassList("badge");
        _aggregateScoreLabel.AddToClassList("badge--primary");
        _aggregateScoreLabel.AddToClassList("text-bold");
        scoreBadge.Add(_aggregateScoreLabel);

        _aggregateRatingLabel = new Label();
        _aggregateRatingLabel.AddToClassList("text-sm");
        _aggregateRatingLabel.AddToClassList("text-muted");
        _aggregateRatingLabel.style.marginLeft = 6;
        scoreBadge.Add(_aggregateRatingLabel);

        header.Add(scoreBadge);
        _root.Add(header);

        var dimensionsCard = new VisualElement();
        dimensionsCard.AddToClassList("card");
        dimensionsCard.style.marginTop = 12;

        var dimensionsTitle = new Label("Scores by Dimension");
        dimensionsTitle.AddToClassList("text-bold");
        dimensionsTitle.style.marginBottom = 8;
        dimensionsCard.Add(dimensionsTitle);

        _dimensionsContainer = new VisualElement();
        dimensionsCard.Add(_dimensionsContainer);
        _root.Add(dimensionsCard);

        for (int i = 0; i < 6; i++) {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            var nameLabel = new Label();
            nameLabel.AddToClassList("text-sm");
            nameLabel.style.width = 110;

            var barTrack = new VisualElement();
            barTrack.style.flexGrow = 1;
            barTrack.style.height = 8;
            barTrack.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
            barTrack.style.borderTopLeftRadius = 4;
            barTrack.style.borderTopRightRadius = 4;
            barTrack.style.borderBottomLeftRadius = 4;
            barTrack.style.borderBottomRightRadius = 4;
            barTrack.style.marginLeft = 8;
            barTrack.style.marginRight = 8;

            var bar = new VisualElement();
            bar.style.height = 8;
            bar.style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 1f));
            bar.style.borderTopLeftRadius = 4;
            bar.style.borderTopRightRadius = 4;
            bar.style.borderBottomLeftRadius = 4;
            bar.style.borderBottomRightRadius = 4;
            barTrack.Add(bar);

            var scoreLabel = new Label();
            scoreLabel.AddToClassList("text-sm");
            scoreLabel.AddToClassList("text-bold");
            scoreLabel.style.width = 32;
            scoreLabel.style.unityTextAlign = TextAnchor.MiddleRight;

            row.Add(nameLabel);
            row.Add(barTrack);
            row.Add(scoreLabel);
            _dimensionsContainer.Add(row);

            _dimensionRows.Add(new DimensionRow {
                Container = row,
                NameLabel = nameLabel,
                Bar = bar,
                ScoreLabel = scoreLabel
            });
        }

        var outletsCard = new VisualElement();
        outletsCard.AddToClassList("card");
        outletsCard.style.marginTop = 12;

        var outletsTitle = new Label("Outlet Reviews");
        outletsTitle.AddToClassList("text-bold");
        outletsTitle.style.marginBottom = 8;
        outletsCard.Add(outletsTitle);

        _outletsContainer = new VisualElement();
        outletsCard.Add(_outletsContainer);
        _root.Add(outletsCard);

        for (int i = 0; i < 7; i++) {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.marginBottom = 6;
            card.style.paddingBottom = 6;

            var leftCol = new VisualElement();
            leftCol.style.flexGrow = 1;

            var outletNameLabel = new Label();
            outletNameLabel.AddToClassList("text-sm");
            outletNameLabel.AddToClassList("text-bold");
            leftCol.Add(outletNameLabel);

            var outletStyleLabel = new Label();
            outletStyleLabel.AddToClassList("text-sm");
            outletStyleLabel.AddToClassList("text-muted");
            leftCol.Add(outletStyleLabel);

            var dimRow = new VisualElement();
            dimRow.style.flexDirection = FlexDirection.Row;
            dimRow.style.marginTop = 2;

            var topLabel = new Label();
            topLabel.AddToClassList("text-sm");
            topLabel.AddToClassList("text-success");
            topLabel.style.marginRight = 10;
            dimRow.Add(topLabel);

            var weakLabel = new Label();
            weakLabel.AddToClassList("text-sm");
            weakLabel.AddToClassList("text-danger");
            dimRow.Add(weakLabel);

            leftCol.Add(dimRow);
            card.Add(leftCol);

            var rightCol = new VisualElement();
            rightCol.style.alignItems = Align.FlexEnd;

            var scoreLabel = new Label();
            scoreLabel.AddToClassList("text-bold");
            rightCol.Add(scoreLabel);

            var ratingLabel = new Label();
            ratingLabel.AddToClassList("text-sm");
            rightCol.Add(ratingLabel);

            card.Add(rightCol);
            _outletsContainer.Add(card);

            _outletCards.Add(new OutletCard {
                Root = card,
                OutletNameLabel = outletNameLabel,
                OutletStyleLabel = outletStyleLabel,
                ScoreLabel = scoreLabel,
                RatingLabel = ratingLabel,
                TopLabel = topLabel,
                WeakLabel = weakLabel
            });
        }

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
        _vm = viewModel as ProductReviewViewModel;
        if (_vm == null) return;

        _titleLabel.text = "Product Review: " + _vm.ProductName;
        _aggregateScoreLabel.text = _vm.AggregateScoreLabel;
        _aggregateRatingLabel.text = _vm.AggregateScoreRating;

        var dims = _vm.DimensionScores;
        int dimCount = dims.Count;
        int rowCount = _dimensionRows.Count;
        for (int i = 0; i < rowCount; i++) {
            var row = _dimensionRows[i];
            if (i < dimCount) {
                var data = dims[i];
                row.NameLabel.text = data.DimensionName;
                row.Bar.style.width = new Length(data.ScoreNormalized * 100f, LengthUnit.Percent);
                row.ScoreLabel.text = data.ScoreLabel;
                row.Container.style.display = DisplayStyle.Flex;
            } else {
                row.Container.style.display = DisplayStyle.None;
            }
        }

        var outlets = _vm.OutletScores;
        int outletCount = outlets.Count;
        int cardCount = _outletCards.Count;
        for (int i = 0; i < cardCount; i++) {
            var card = _outletCards[i];
            if (i < outletCount) {
                var data = outlets[i];
                card.OutletNameLabel.text = data.OutletName;
                card.OutletStyleLabel.text = data.OutletStyle;
                card.ScoreLabel.text = data.ScoreLabel;
                card.RatingLabel.text = data.ScoreRating;
                card.TopLabel.text = "Strong: " + data.TopDimension;
                card.WeakLabel.text = "Weak: " + data.WeakDimension;

                int score = 0;
                int.TryParse(data.ScoreLabel, out score);
                card.RatingLabel.RemoveFromClassList("text-success");
                card.RatingLabel.RemoveFromClassList("text-danger");
                card.RatingLabel.RemoveFromClassList("text-muted");
                if (score > 65)
                    card.RatingLabel.AddToClassList("text-success");
                else if (score <= 50)
                    card.RatingLabel.AddToClassList("text-danger");
                else
                    card.RatingLabel.AddToClassList("text-muted");

                card.Root.style.display = DisplayStyle.Flex;
            } else {
                card.Root.style.display = DisplayStyle.None;
            }
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
