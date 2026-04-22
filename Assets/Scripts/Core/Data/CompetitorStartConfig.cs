using System;
using UnityEngine;

[Serializable]
public struct StartingProduct {
    public ProductCategory category;
    public ProductNiche niche;
    public string productName;
    [Range(0f, 100f)] public float quality;
    [Range(0f, 100f)] public float marketSharePercent;
    public string[] featureIds;
    public float[] featureQualities;
    public int ageInMonths;
    [Range(0f, 1f)] public float licensingRate;
    public long maintenanceBudgetMonthly;
}

[Serializable]
public struct StartingDevProduct {
    public ProductCategory category;
    public ProductNiche niche;
    public string productName;
    [Range(1, 12)] public int devMonthsRemaining;
    public string[] featureIds;
}

[Serializable]
public struct ScheduledProductUpdate {
    public int productIndex;
    [Range(1, 12)] public int monthsUntilUpdate;
}

[CreateAssetMenu(menuName = "StartUp/Competitor Start Config")]
public class CompetitorStartConfig : ScriptableObject {
    [Header("Identity")]
    public string companyName;
    public string founderName;
    public CompetitorArchetype archetype;

    [Header("Personality")]
    public CompetitorPersonality personality;

    [Header("Specializations")]
    public ProductCategory[] specializations;

    [Header("Starting Products")]
    public StartingProduct[] startingProducts;

    [Header("Products In Development")]
    public StartingDevProduct[] startingDevProducts;

    [Header("Scheduled Updates")]
    public ScheduledProductUpdate[] scheduledUpdates;
}
