using System;
using System.Collections.Generic;

[Serializable]
public struct NicheRecord {
    public int ProductsShipped;
    public int Successes;
    public int Failures;
    public float BestScore;
    public float AverageScore;
    public long TotalRevenue;
    public long TotalInvestment;
}

[Serializable]
public class CompetitorMemory {
    public Dictionary<ProductNiche, NicheRecord> NicheHistory;
    public Dictionary<ProductCategory, NicheRecord> CategoryHistory;
    public int ConsecutiveHits;
    public int ConsecutiveFlops;
    public int TotalProductsShipped;
    public int TotalProductsSunset;
    public float AverageReviewScore;
    public float BestReviewScore;
    public ProductNiche BestNiche;
    public ProductCategory BestCategory;

    public static CompetitorMemory CreateNew() {
        return new CompetitorMemory {
            NicheHistory = new Dictionary<ProductNiche, NicheRecord>(),
            CategoryHistory = new Dictionary<ProductCategory, NicheRecord>()
        };
    }
}
