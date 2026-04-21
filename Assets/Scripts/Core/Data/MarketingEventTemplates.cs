public static class MarketingEventTemplates
{
    private static readonly string[][] _headlines = new string[][]
    {
        // DevelopmentLeak (0)
        new[] {
            "Insider Sources Reveal Details About {0}",
            "Leaked Screenshots of {0} Surface Online",
            "Anonymous Tip Exposes {0} Development Plans"
        },
        // InsiderPreview (1)
        new[] {
            "{0} Early Access Impressions Spark Debate",
            "Select Partners Get First Look at {0}",
            "Pre-Release Build of {0} Shown to Influencers"
        },
        // CompetitorAnnouncement (2)
        new[] {
            "Rival Announces Product That Could Rival {0}",
            "Competing {1} Steals Spotlight From {0}",
            "Market Reacts Cautiously to {0} After Competitor News"
        },
        // CommunityBuzz (3)
        new[] {
            "{0} Gains Traction on Social Media",
            "Online Communities Are Buzzing About {0}",
            "{0} Trend Picks Up Steam Across Forums"
        },
        // ViralMoment (4)
        new[] {
            "{0} Goes Viral Overnight",
            "A Single Post Sent {0} Through the Roof",
            "{0} Becomes the Talk of the Internet"
        },
        // BadReview (5)
        new[] {
            "Critics Question {0} in Scathing Write-Up",
            "Major Reviewer Slams {0}",
            "{0} Receives a Wave of Negative Press"
        },
        // IndustryAward (6)
        new[] {
            "{0} Wins Coveted Industry Award",
            "{0} Recognized as Best {1} of the Year",
            "Industry Panel Honours {0} for Excellence"
        },
        // SecurityBreach (7)
        new[] {
            "Data Breach Hits {0} Users",
            "Security Flaw Exposed in {0}",
            "{0} Under Fire After Vulnerability Discovered"
        },
        // InfluencerCoverage (8)
        new[] {
            "Popular Creator Features {0} in Viral Video",
            "{0} Picked Up by Major Online Influencer",
            "Influencer Shoutout Drives Interest in {0}"
        }
    };

    private static readonly string[][] _bodies = new string[][]
    {
        // DevelopmentLeak (0)
        new[] {
            "Details about your upcoming {1} have leaked online. This has generated buzz among potential customers. Hype +{2}.",
            "Screenshots from an internal build of {0} have appeared on social media. The community is discussing what they've seen. Hype +{2}.",
            "Anonymous sources have shared development plans for {0} across tech forums. Interest is building. Hype +{2}."
        },
        // InsiderPreview (1)
        new[] {
            "A hands-on preview of {0} was shared with select partners. Early impressions are positive. Hype +{2}.",
            "Influencers given early access to {0} have posted favourable impressions. Hype +{2}.",
            "A controlled preview of {0} generated strong word-of-mouth. Hype +{2}."
        },
        // CompetitorAnnouncement (2)
        new[] {
            "A rival company announced a competing {1}, drawing attention away from {0}. Hype {2}.",
            "News of a competing product has dampened enthusiasm for {0} among some potential customers. Hype {2}.",
            "The market shifted focus after a competitor's announcement, cooling interest in {0}. Hype {2}."
        },
        // CommunityBuzz (3)
        new[] {
            "Discussion threads about {0} are trending. Organic word-of-mouth is growing. Hype +{2}.",
            "The {0} community is growing rapidly as enthusiasts share news across platforms. Hype +{2}.",
            "Grassroots interest in {0} has spiked after community posts went viral. Hype +{2}."
        },
        // ViralMoment (4)
        new[] {
            "{0} went viral after a clip spread across social platforms. Popularity +{2}.",
            "A user-generated moment involving {0} captured massive attention online. Popularity +{2}.",
            "An unexpected post put {0} in front of millions of people overnight. Popularity +{2}."
        },
        // BadReview (5)
        new[] {
            "A prominent reviewer published a critical take on {0}. Customer confidence has dropped. Popularity {2}.",
            "A widely-shared negative review of {0} is affecting public perception. Popularity {2}.",
            "Critical press coverage of {0} has caused some customers to reconsider. Popularity {2}."
        },
        // IndustryAward (6)
        new[] {
            "{0} has been awarded recognition by a respected industry panel. Popularity +{2}, reputation improved.",
            "The industry has formally recognized {0} for outstanding quality. Popularity +{2}, reputation improved.",
            "{0} was named a standout {1} this year by industry judges. Popularity +{2}, reputation improved."
        },
        // SecurityBreach (7)
        new[] {
            "A security vulnerability in {0} has exposed user data. Customer confidence has dropped significantly. Popularity {2}, users lost: {3}.",
            "A reported breach in {0} is causing customer concern. Users are uninstalling in response. Popularity {2}, users lost: {3}.",
            "{0} users are alarmed after reports of compromised data. Popularity {2}, users lost: {3}."
        },
        // InfluencerCoverage (8)
        new[] {
            "A popular creator featured {0} in a video that reached millions of viewers. Popularity +{2}.",
            "Influencer coverage of {0} has brought a wave of new users to the platform. Popularity +{2}.",
            "Organic influencer attention on {0} is driving strong user acquisition. Popularity +{2}."
        }
    };

    private static readonly string[][] _bodiesMitigated = new string[][]
    {
        // BadReview (5) — mitigated only
        new[] {
            "A prominent reviewer published a critical take on {0}, but your marketing team issued a measured response that softened the blow. Popularity {2}.",
            "Negative press hit {0}, but your team's swift PR response limited the damage. Popularity {2}.",
            "Critics raised concerns about {0}; your marketing team's rebuttal kept the impact minimal. Popularity {2}."
        },
        // SecurityBreach (7) — mitigated only
        new[] {
            "A security vulnerability in {0} was disclosed, but your team managed communications effectively, retaining most users. Popularity {2}, users lost: {3}.",
            "Your marketing team's rapid response to the {0} breach minimized churn and stabilized sentiment. Popularity {2}, users lost: {3}.",
            "Despite a reported breach, coordinated messaging from your team kept the fallout controlled. Popularity {2}, users lost: {3}."
        }
    };

    public static (string headline, string body) GetTemplate(
        HypeEventType eventType,
        string productName,
        string categoryName,
        float primaryChange,
        int userChange,
        bool wasMitigated,
        IRng rng)
    {
        int typeIdx = (int)eventType;
        if (typeIdx < 0 || typeIdx >= _headlines.Length)
            return (productName + " News", "An event occurred.");

        string[] headlineVariants = _headlines[typeIdx];
        int hIdx = (int)(rng.NextFloat01() * headlineVariants.Length);
        if (hIdx >= headlineVariants.Length) hIdx = headlineVariants.Length - 1;
        string headline = headlineVariants[hIdx]
            .Replace("{0}", productName)
            .Replace("{1}", categoryName);

        string[] bodyVariants;
        if (wasMitigated && eventType == HypeEventType.BadReview)
            bodyVariants = _bodiesMitigated[0];
        else if (wasMitigated && eventType == HypeEventType.SecurityBreach)
            bodyVariants = _bodiesMitigated[1];
        else
            bodyVariants = _bodies[typeIdx];

        int bIdx = (int)(rng.NextFloat01() * bodyVariants.Length);
        if (bIdx >= bodyVariants.Length) bIdx = bodyVariants.Length - 1;

        string changeStr = primaryChange >= 0f
            ? "+" + primaryChange.ToString("F0")
            : primaryChange.ToString("F0");

        string userStr = userChange >= 0 ? "+" + userChange : userChange.ToString();

        string body = bodyVariants[bIdx]
            .Replace("{0}", productName)
            .Replace("{1}", categoryName)
            .Replace("{2}", changeStr)
            .Replace("{3}", userStr);

        return (headline, body);
    }
}
