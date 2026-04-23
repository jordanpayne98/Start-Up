using System.Collections.Generic;

public struct OutletScoreData
{
    public string OutletName;
    public string OutletStyle;
    public string ScoreLabel;
    public string ScoreRating;
    public string TopDimension;
    public string WeakDimension;
}

public struct DimensionScoreData
{
    public string DimensionName;
    public string ScoreLabel;
    public float ScoreNormalized;
}

public class ProductReviewViewModel : IViewModel
{
    public string ProductName { get; private set; }
    public string AggregateScoreLabel { get; private set; }
    public string AggregateScoreRating { get; private set; }
    public List<OutletScoreData> OutletScores => _outletScores;
    public List<DimensionScoreData> DimensionScores => _dimensionScores;
    public bool HasReview { get; private set; }
    public string IdentityCommentary { get; private set; }
    public bool HasIdentityCommentary => !string.IsNullOrEmpty(IdentityCommentary);

    private readonly List<OutletScoreData> _outletScores = new List<OutletScoreData>(7);
    private readonly List<DimensionScoreData> _dimensionScores = new List<DimensionScoreData>(6);
    private ProductId _productId;

    private static readonly ReviewDimension[] _allDimensions = {
        ReviewDimension.Quality,
        ReviewDimension.Functionality,
        ReviewDimension.Innovation,
        ReviewDimension.Stability,
        ReviewDimension.Value,
        ReviewDimension.Polish
    };

    public void SetProduct(ProductId productId) {
        _productId = productId;
    }

    public void RefreshFromProduct(Product product) {
        _outletScores.Clear();
        _dimensionScores.Clear();
        ProductName = product?.ProductName ?? "Unknown";
        var result = product?.ReviewResult;
        IdentityCommentary = product != null
            ? ProductIdentityHelper.BuildReviewerCommentary(product.IdentityAtShip)
            : null;
        PopulateFromResult(result);
    }

    public void Refresh(IReadOnlyGameState state) {
        _outletScores.Clear();
        _dimensionScores.Clear();

        Product found = null;
        ProductReviewResult result = null;
        if (state.ShippedProducts != null && state.ShippedProducts.TryGetValue(_productId, out var p)) {
            ProductName = p.ProductName ?? "Unknown";
            result = p.ReviewResult;
            found = p;
        } else if (state.ArchivedProducts != null && state.ArchivedProducts.TryGetValue(_productId, out var a)) {
            ProductName = a.ProductName ?? "Unknown";
            result = a.ReviewResult;
            found = a;
        } else {
            ProductName = "Unknown";
        }

        IdentityCommentary = found != null
            ? ProductIdentityHelper.BuildReviewerCommentary(found.IdentityAtShip)
            : null;

        PopulateFromResult(result);
    }

    private void PopulateFromResult(ProductReviewResult result) {
        if (result == null) {
            HasReview = false;
            AggregateScoreLabel = "--";
            AggregateScoreRating = "--";
            return;
        }

        HasReview = true;
        int aggInt = (int)result.AggregateScore;
        AggregateScoreLabel = aggInt.ToString();
        AggregateScoreRating = GetRatingLabel(aggInt);

        int dimCount = _allDimensions.Length;
        for (int i = 0; i < dimCount; i++) {
            var dim = _allDimensions[i];
            float score = result.GetDimensionScore(dim);
            int scoreInt = (int)score;
            _dimensionScores.Add(new DimensionScoreData {
                DimensionName = dim.ToString(),
                ScoreLabel = scoreInt.ToString(),
                ScoreNormalized = score / 100f
            });
        }

        if (result.OutletReviews != null) {
            int outletCount = result.OutletReviews.Count;
            for (int i = 0; i < outletCount; i++) {
                var outlet = result.OutletReviews[i];
                int outletScore = (int)outlet.Score;

                ReviewDimension topDim = ReviewDimension.Quality;
                ReviewDimension weakDim = ReviewDimension.Quality;
                float topVal = float.MinValue;
                float weakVal = float.MaxValue;

                for (int d = 0; d < dimCount; d++) {
                    float dv = outlet.GetDimensionScore(_allDimensions[d]);
                    if (dv > topVal) { topVal = dv; topDim = _allDimensions[d]; }
                    if (dv < weakVal) { weakVal = dv; weakDim = _allDimensions[d]; }
                }

                _outletScores.Add(new OutletScoreData {
                    OutletName = outlet.OutletName ?? outlet.OutletId,
                    OutletStyle = outlet.OutletStyle ?? "",
                    ScoreLabel = outletScore.ToString(),
                    ScoreRating = GetRatingLabel(outletScore),
                    TopDimension = topDim.ToString(),
                    WeakDimension = weakDim.ToString()
                });
            }
        }
    }

    public static string GetRatingLabel(int score) {
        if (score <= 30) return "Poor";
        if (score <= 50) return "Mediocre";
        if (score <= 65) return "Decent";
        if (score <= 80) return "Good";
        if (score <= 90) return "Great";
        return "Outstanding";
    }

    public static string GetRatingUssClass(int score) {
        if (score <= 30) return "score-poor";
        if (score <= 50) return "score-mediocre";
        if (score <= 65) return "score-decent";
        if (score <= 80) return "score-good";
        if (score <= 90) return "score-great";
        return "score-outstanding";
    }
}
