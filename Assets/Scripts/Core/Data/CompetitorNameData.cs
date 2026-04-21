using System;
using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Competitor Name Data")]
public class CompetitorNameData : ScriptableObject
{
    [Header("Company Name Parts")]
    public string[] techPrefixes;
    public string[] techSuffixes;

    [Header("Company Name Patterns")]
    public string[] companyPatterns;

    [Header("Founder Names")]
    public string[] founderFirstNames;
    public string[] founderLastNames;

    [Header("Product Name Parts")]
    public string[] productAdjectives;
    public string[] productNouns;

    [Header("Sequel Suffixes")]
    public string[] sequelSuffixes;

    [Header("Niche-Specific Product Names")]
    public NicheNamePool[] nicheNamePools;

    [Serializable]
    public struct NicheNamePool
    {
        public ProductCategory category;
        public ProductNiche niche;
        public string[] prefixes;
        public string[] suffixes;
    }
}
