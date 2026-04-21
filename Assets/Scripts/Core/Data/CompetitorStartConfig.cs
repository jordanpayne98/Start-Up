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
}
