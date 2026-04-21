using System;
using System.Collections.Generic;

[Serializable]
public class ReputationState
{
    public Dictionary<string, int> reputationScores;
    public int totalRepEarned;
    public int totalRepLost;
    public int contractsCompletedCount;
    public int productsShippedCount;
    public int companyFans;
    public float fanSentiment;
    public Dictionary<ProductId, int> fansPerProduct;

    public static ReputationState CreateNew()
    {
        return new ReputationState
        {
            reputationScores = new Dictionary<string, int> { { "global", 0 } },
            totalRepEarned = 0,
            totalRepLost = 0,
            contractsCompletedCount = 0,
            productsShippedCount = 0,
            companyFans = 0,
            fanSentiment = 0f,
            fansPerProduct = new Dictionary<ProductId, int>()
        };
    }
}
