using System;
using System.Collections.Generic;

[Serializable]
public class ProductReviewResult {
    public float AggregateScore;
    public List<OutletReview> OutletReviews;
    public ReviewDimension[] DimensionKeys;
    public float[] DimensionValues;

    public float GetDimensionScore(ReviewDimension dimension) {
        if (DimensionKeys == null) return 0f;
        int count = DimensionKeys.Length;
        for (int i = 0; i < count; i++) {
            if (DimensionKeys[i] == dimension) return DimensionValues[i];
        }
        return 0f;
    }
}

[Serializable]
public class OutletReview {
    public string OutletId;
    public string OutletName;
    public string OutletStyle;
    public float Score;
    public ReviewDimension[] DimensionKeys;
    public float[] DimensionValues;

    public float GetDimensionScore(ReviewDimension dimension) {
        if (DimensionKeys == null) return 0f;
        int count = DimensionKeys.Length;
        for (int i = 0; i < count; i++) {
            if (DimensionKeys[i] == dimension) return DimensionValues[i];
        }
        return 0f;
    }
}
