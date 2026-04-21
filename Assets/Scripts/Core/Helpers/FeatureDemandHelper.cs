public static class FeatureDemandHelper {
    public static FeatureDemandStage GetDemandStage(
        int currentGen,
        int introductionGen,
        int maturitySpeed,
        bool isFoundational,
        float competitorCoverageRatio) {
        if (currentGen < introductionGen)
            return FeatureDemandStage.NotAvailable;

        int gensSinceIntro = currentGen - introductionGen;

        FeatureDemandStage stage;
        switch (maturitySpeed) {
            case 1:
                if (gensSinceIntro == 0) stage = FeatureDemandStage.Emerging;
                else if (gensSinceIntro == 1) stage = FeatureDemandStage.Growing;
                else stage = FeatureDemandStage.Standard;
                break;
            case 3:
                if (gensSinceIntro <= 1) stage = FeatureDemandStage.Emerging;
                else if (gensSinceIntro <= 3) stage = FeatureDemandStage.Growing;
                else stage = FeatureDemandStage.Standard;
                break;
            default:
                if (gensSinceIntro == 0) stage = FeatureDemandStage.Emerging;
                else if (gensSinceIntro <= 2) stage = FeatureDemandStage.Growing;
                else stage = FeatureDemandStage.Standard;
                break;
        }

        if (competitorCoverageRatio > 0.60f && stage == FeatureDemandStage.Growing)
            stage = FeatureDemandStage.Standard;

        if (isFoundational) {
            if (stage == FeatureDemandStage.Declining || stage == FeatureDemandStage.Legacy)
                stage = FeatureDemandStage.Standard;
            return stage;
        }

        if (stage == FeatureDemandStage.Standard) {
            if (gensSinceIntro >= maturitySpeed + 6) stage = FeatureDemandStage.Legacy;
            else if (gensSinceIntro >= maturitySpeed + 4) stage = FeatureDemandStage.Declining;
        }

        return stage;
    }

    public static string GetDemandLabel(FeatureDemandStage stage) {
        switch (stage) {
            case FeatureDemandStage.Emerging:  return "Cutting Edge";
            case FeatureDemandStage.Growing:   return "Trending";
            case FeatureDemandStage.Standard:  return "Expected";
            case FeatureDemandStage.Declining: return "Fading";
            case FeatureDemandStage.Legacy:    return "Outdated";
            default:                           return "";
        }
    }

    public static float GetInnovationValue(FeatureDemandStage stage) {
        switch (stage) {
            case FeatureDemandStage.Emerging:  return 15f;
            case FeatureDemandStage.Growing:   return 8f;
            case FeatureDemandStage.Standard:  return 2f;
            case FeatureDemandStage.Declining: return 0f;
            case FeatureDemandStage.Legacy:    return -3f;
            default:                           return 0f;
        }
    }

    public static float GetMissingPenalty(FeatureDemandStage stage, float competitorCoverageRatio) {
        if (stage != FeatureDemandStage.Standard) return 0f;
        return competitorCoverageRatio > 0.80f ? 12f : 8f;
    }

    public static bool ShouldPreCheck(FeatureDemandStage stage) {
        return stage == FeatureDemandStage.Standard;
    }
}
