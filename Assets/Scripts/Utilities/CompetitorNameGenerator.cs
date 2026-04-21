public static class CompetitorNameGenerator
{
    public static string GenerateCompanyName(CompetitorNameData nameData, IRng rng)
    {
        if (nameData == null || nameData.techPrefixes == null || nameData.techPrefixes.Length == 0)
            return "TechCorp";

        string prefix = nameData.techPrefixes[rng.Range(0, nameData.techPrefixes.Length)];
        string suffix = (nameData.techSuffixes != null && nameData.techSuffixes.Length > 0)
            ? nameData.techSuffixes[rng.Range(0, nameData.techSuffixes.Length)]
            : "Soft";

        if (nameData.companyPatterns != null && nameData.companyPatterns.Length > 0)
        {
            string pattern = nameData.companyPatterns[rng.Range(0, nameData.companyPatterns.Length)];
            return string.Format(pattern, prefix, suffix);
        }

        return prefix + suffix;
    }

    public static string GenerateFounderName(IRng rng)
    {
        return NameGenerator.GenerateRandomName(rng);
    }

    public static string GenerateProductName(
        CompetitorNameData nameData,
        IRng rng,
        string companyName,
        ProductCategory category,
        ProductNiche niche)
    {
        string prefix = null;
        string suffix = null;

        if (nameData != null && nameData.nicheNamePools != null && nameData.nicheNamePools.Length > 0)
        {
            int poolLen = nameData.nicheNamePools.Length;
            int exactMatch = -1;
            int categoryMatch = -1;

            for (int i = 0; i < poolLen; i++)
            {
                var pool = nameData.nicheNamePools[i];
                if (pool.category == category)
                {
                    if (pool.niche == niche)
                    {
                        exactMatch = i;
                        break;
                    }
                    if (pool.niche == ProductNiche.None && categoryMatch < 0)
                        categoryMatch = i;
                }
            }

            int selectedPool = exactMatch >= 0 ? exactMatch : categoryMatch;
            if (selectedPool >= 0)
            {
                var chosen = nameData.nicheNamePools[selectedPool];
                if (chosen.prefixes != null && chosen.prefixes.Length > 0)
                    prefix = chosen.prefixes[rng.Range(0, chosen.prefixes.Length)];
                if (chosen.suffixes != null && chosen.suffixes.Length > 0)
                    suffix = chosen.suffixes[rng.Range(0, chosen.suffixes.Length)];
            }
        }

        if (prefix == null && nameData != null && nameData.productAdjectives != null && nameData.productAdjectives.Length > 0)
            prefix = nameData.productAdjectives[rng.Range(0, nameData.productAdjectives.Length)];

        if (suffix == null && nameData != null && nameData.productNouns != null && nameData.productNouns.Length > 0)
            suffix = nameData.productNouns[rng.Range(0, nameData.productNouns.Length)];

        if (prefix == null) prefix = niche != ProductNiche.None ? niche.ToString() : category.ToString();
        if (suffix == null) suffix = "Pro";

        string productName = prefix + " " + suffix;

        if (!string.IsNullOrEmpty(companyName) && rng.Chance(0.3f))
            productName = companyName + " " + productName;

        return productName;
    }
}
