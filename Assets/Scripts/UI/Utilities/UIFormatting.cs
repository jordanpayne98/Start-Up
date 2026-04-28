using UnityEngine;
using UnityEngine.UIElements;

public static class UIFormatting
{
    public static string FormatUserCount(int users) {
        if (users >= 1_000_000_000) return (users / 1_000_000_000f).ToString("F1") + "B";
        if (users >= 1_000_000) return (users / 1_000_000f).ToString("F1") + "M";
        if (users >= 1_000) return (users / 1_000f).ToString("F1") + "K";
        return users.ToString("N0");
    }

    public static string FormatMoney(long amount) {
        if (amount < 0) {
            return "-$" + FormatPositiveMoney(-amount);
        }
        return "$" + FormatPositiveMoney(amount);
    }

    private static string FormatPositiveMoney(long amount) {
        if (amount >= 1000000) {
            float millions = amount / 1000000f;
            return millions.ToString("F1") + "M";
        }
        if (amount >= 100000) {
            float thousands = amount / 1000f;
            return thousands.ToString("F0") + "K";
        }
        return amount.ToString("N0");
    }

    public static string FormatDate(int day, int month, int year) {
        return day.ToString("D2") + "/" + FormatMonthName(month) + "/" + year;
    }

    public static string FormatDateTime(int day, int month, int year, int hour, int minute) {
        return day.ToString("D2") + "/" + FormatMonthName(month) + "/" + year
             + "  " + hour.ToString("D2") + ":" + minute.ToString("D2");
    }

    public static string FormatMonthName(int month) {
        switch (month) {
            case 1:  return "January";
            case 2:  return "February";
            case 3:  return "March";
            case 4:  return "April";
            case 5:  return "May";
            case 6:  return "June";
            case 7:  return "July";
            case 8:  return "August";
            case 9:  return "September";
            case 10: return "October";
            case 11: return "November";
            case 12: return "December";
            default: return "Month " + month;
        }
    }

    public static string FormatPercent(float value) {
        return ((int)(value * 100f)) + "%";
    }

    public static string FormatReputationTier(ReputationTier tier) {
        switch (tier) {
            case global::ReputationTier.Startup: return "Startup";
            case global::ReputationTier.Established: return "Established";
            case global::ReputationTier.Respected: return "Respected";
            case global::ReputationTier.IndustryLeader: return "Industry Leader";
            default: return "Unknown";
        }
    }

    public static string FormatCategory(ProductCategory cat) {
        switch (cat) {
            case ProductCategory.OperatingSystem:     return "Operating System";
            case ProductCategory.GameConsole:         return "Game Console";
            case ProductCategory.GameEngine:          return "Game Engine";
            case ProductCategory.GraphicsEditor:      return "Graphics Editor";
            case ProductCategory.AudioTool:           return "Audio Tool";
            case ProductCategory.DevFramework:        return "Dev Framework";
            case ProductCategory.VideoGame:           return "Video Game";
            default:                                  return cat.ToString();
        }
    }

    public static string FormatCategory(ProductCategory? cat) {
        if (!cat.HasValue) return "";
        return FormatCategory(cat.Value);
    }

    public static string FormatNiche(ProductNiche niche) {
        switch (niche) {
            case ProductNiche.DesktopOS:        return "Desktop OS";
            case ProductNiche.RPG:              return "RPG";
            case ProductNiche.FPS:              return "FPS";
            case ProductNiche.MMORPG:           return "MMORPG";
            case ProductNiche.Strategy:         return "Strategy";
            case ProductNiche.Puzzle:           return "Puzzle";
            case ProductNiche.Platformer:       return "Platformer";
            case ProductNiche.Simulation:       return "Simulation";
            case ProductNiche.Racing:           return "Racing";
            case ProductNiche.Sports:           return "Sports";
            case ProductNiche.Horror:           return "Horror";
            case ProductNiche.Adventure:        return "Adventure";
            case ProductNiche.Sandbox:          return "Sandbox";
            case ProductNiche.Fighting:         return "Fighting";
            default:                            return niche.ToString();
        }
    }

    public static string FormatNicheOrCategory(Product product) {
        if (product.Niche != ProductNiche.None && System.Enum.IsDefined(typeof(ProductNiche), product.Niche))
            return FormatNiche(product.Niche);
        return FormatCategory(product.Category);
    }

    public static string FormatFeatureList(string[] featureIds, ProductTemplateDefinition[] templates) {
        if (featureIds == null || featureIds.Length == 0) return "None";
        var sb = new System.Text.StringBuilder();
        int idCount = featureIds.Length;
        for (int i = 0; i < idCount; i++) {
            if (i > 0) sb.Append(", ");
            string displayName = featureIds[i];
            if (templates != null) {
                int tCount = templates.Length;
                bool found = false;
                for (int t = 0; t < tCount && !found; t++) {
                    if (templates[t] == null) continue;
                    var features = templates[t].availableFeatures;
                    if (features == null) continue;
                    int fCount = features.Length;
                    for (int f = 0; f < fCount; f++) {
                        if (features[f].featureId == featureIds[i]) {
                            displayName = features[f].displayName;
                            found = true;
                            break;
                        }
                    }
                }
            }
            sb.Append(displayName);
        }
        return sb.ToString();
    }

    public static string FormatRole(RoleId role) {
        return RoleIdHelper.GetName(role);
    }

    // Returns the USS modifier class for the role pill (without the base "role-pill" class).
    // Matches .role-pill--<modifier> in components.uss.
    public static string RolePillClass(RoleId role) {
        switch (role) {
            case RoleId.SoftwareEngineer:           return "role-pill--developer";
            case RoleId.SystemsEngineer:            return "role-pill--developer";
            case RoleId.SecurityEngineer:           return "role-pill--developer";
            case RoleId.PerformanceEngineer:        return "role-pill--developer";
            case RoleId.HardwareEngineer:           return "role-pill--developer";
            case RoleId.ManufacturingEngineer:      return "role-pill--developer";
            case RoleId.ProductDesigner:            return "role-pill--designer";
            case RoleId.GameDesigner:               return "role-pill--designer";
            case RoleId.TechnicalArtist:            return "role-pill--vfx-artist";
            case RoleId.AudioDesigner:              return "role-pill--sfx-artist";
            case RoleId.QaEngineer:                 return "role-pill--qa-engineer";
            case RoleId.TechnicalSupportSpecialist: return "role-pill--qa-engineer";
            case RoleId.Marketer:                   return "role-pill--marketing-specialist";
            case RoleId.SalesExecutive:             return "role-pill--marketing-specialist";
            case RoleId.Accountant:                 return "role-pill--accountant";
            case RoleId.HrSpecialist:               return "role-pill--hr-specialist";
            default:                                return "role-pill--unknown";
        }
    }

    // Alias for RolePillClass(RoleId) — returns the USS modifier class for the role pill.
    public static string GetRoleClass(RoleId role) => RolePillClass(role);

    public static void ClearRolePillClasses(VisualElement el) {
        el.RemoveFromClassList("role-pill--developer");
        el.RemoveFromClassList("role-pill--designer");
        el.RemoveFromClassList("role-pill--qa-engineer");
        el.RemoveFromClassList("role-pill--hr-specialist");
        el.RemoveFromClassList("role-pill--sfx-artist");
        el.RemoveFromClassList("role-pill--vfx-artist");
        el.RemoveFromClassList("role-pill--accountant");
        el.RemoveFromClassList("role-pill--marketing-specialist");
        el.RemoveFromClassList("role-pill--unknown");
    }

    public static string RolePillClass(string roleDisplay) {
        switch (roleDisplay) {
            case "Developer":             return "role-pill--developer";
            case "Designer":             return "role-pill--designer";
            case "QA Engineer":          return "role-pill--qa-engineer";
            case "HR Specialist":        return "role-pill--hr-specialist";
            case "SFX Artist":           return "role-pill--sfx-artist";
            case "VFX Artist":           return "role-pill--vfx-artist";
            case "Accountant":           return "role-pill--accountant";
            case "Marketing Specialist": return "role-pill--marketing-specialist";
            default:                     return "role-pill--unknown";
        }
    }

    public static Color GetSkillColor(SkillId skill) {
        switch (skill) {
            case SkillId.Programming:              return new Color(0.376f, 0.647f, 0.980f);
            case SkillId.SystemsArchitecture:      return new Color(0.376f, 0.700f, 0.940f);
            case SkillId.Security:                 return new Color(0.376f, 0.750f, 0.880f);
            case SkillId.PerformanceOptimisation:  return new Color(0.376f, 0.800f, 0.820f);
            case SkillId.HardwareIntegration:      return new Color(0.376f, 0.860f, 0.760f);
            case SkillId.Manufacturing:            return new Color(0.376f, 0.900f, 0.700f);
            case SkillId.ProductDesign:            return new Color(0.655f, 0.545f, 0.980f);
            case SkillId.GameDesign:               return new Color(0.720f, 0.545f, 0.960f);
            case SkillId.Vfx:                      return new Color(0.984f, 0.573f, 0.235f);
            case SkillId.AudioDesign:              return new Color(0.957f, 0.447f, 0.718f);
            case SkillId.QaTesting:                return new Color(0.204f, 0.827f, 0.600f);
            case SkillId.TechnicalSupport:         return new Color(0.204f, 0.880f, 0.550f);
            case SkillId.Marketing:                return new Color(0.961f, 0.620f, 0.043f);
            case SkillId.Sales:                    return new Color(0.961f, 0.700f, 0.043f);
            case SkillId.Accountancy:              return new Color(0.580f, 0.639f, 0.722f);
            case SkillId.HrRecruitment:            return new Color(0.984f, 0.749f, 0.141f);
            case SkillId.Negotiation:              return new Color(0.176f, 0.831f, 0.749f);
            default:                               return new Color(0.443f, 0.443f, 0.478f);
        }
    }

    public static string FormatContractStatus(ContractStatus status) {
        switch (status) {
            case ContractStatus.Available: return "Available";
            case ContractStatus.Accepted: return "Accepted";
            case ContractStatus.InProgress: return "In Progress";
            case ContractStatus.Completed: return "Completed";
            case ContractStatus.Failed: return "Failed";
            default: return "Unknown";
        }
    }

    public static string FormatTeamType(TeamType type) {
        switch (type) {
            case TeamType.Development: return "Development";
            case TeamType.Design:      return "Design";
            case TeamType.QA:          return "QA";
            case TeamType.Marketing:   return "Marketing";
            case TeamType.HR:          return "HR";
            default: return type.ToString();
        }
    }

    public static string TeamTypeBadgeClass(TeamType type) {
        switch (type) {
            case TeamType.Development: return "badge--role-development";
            case TeamType.Design:      return "badge--role-design";
            case TeamType.QA:          return "badge--role-qa";
            case TeamType.Marketing:   return "badge--role-negotiation";
            case TeamType.HR:          return "badge--role-hr";
            default:                   return "badge--neutral";
        }
    }

    public static string FormatMailCategory(MailCategory category) {
        switch (category) {
            case MailCategory.Alert:       return "Alert";
            case MailCategory.Contract:    return "Contract";
            case MailCategory.Recruitment: return "Recruit";
            case MailCategory.HR:          return "HR";
            case MailCategory.Finance:     return "Finance";
            case MailCategory.Technology:  return "Technology";
            case MailCategory.Operations:  return "Ops";
            case MailCategory.NewsArticle: return "News";
            default:                       return "General";
        }
    }

    public static string FormatMailPriority(MailPriority p) {
        switch (p) {
            case MailPriority.Critical: return "!";
            case MailPriority.Warning:  return "▲";
            default:                    return "";
        }
    }

    public static string FormatDaysRemaining(int deadlineTick, int currentTick) {
        int ticksRemaining = deadlineTick - currentTick;
        if (ticksRemaining <= 0) return "Overdue";
        int days = ticksRemaining / 4800;
        if (days == 0) return "< 1 day";
        if (days == 1) return "1 day";
        return days + " days";
    }

    public static string FormatMailAge(int mailTick, int currentTick) {
        int elapsed = currentTick - mailTick;
        if (elapsed <= 0) return "Just now";
        int days = elapsed / TimeState.TicksPerDay;
        if (days == 0) return "Today";
        if (days == 1) return "Yesterday";
        return days + "d ago";
    }

    // Duration label for unaccepted contracts — shows the window the player has once accepted
    public static string FormatTickDuration(int ticks) {
        int days = ticks / TimeState.TicksPerDay;
        if (days <= 1) return "1 day window";
        return days + " day window";
    }

    // Short label for a SkillId — used in contract cards and phase rows
    public static string FormatSkillShort(SkillId skill) {
        switch (skill) {
            case SkillId.Programming:            return "Prog";
            case SkillId.SystemsArchitecture:    return "Sys";
            case SkillId.PerformanceOptimisation:return "Perf";
            case SkillId.Security:               return "Sec";
            case SkillId.CpuEngineering:         return "CPU";
            case SkillId.GpuEngineering:         return "GPU";
            case SkillId.HardwareIntegration:    return "HW";
            case SkillId.Manufacturing:          return "Mfg";
            case SkillId.ProductDesign:          return "Design";
            case SkillId.UxUiDesign:             return "UX";
            case SkillId.GameDesign:             return "Game";
            case SkillId.UserResearch:           return "URes";
            case SkillId.VisualArt:              return "Art";
            case SkillId.Vfx:                    return "VFX";
            case SkillId.AudioDesign:            return "Audio";
            case SkillId.WritingContent:         return "Write";
            case SkillId.QaTesting:              return "QA";
            case SkillId.BugFixing:              return "Bug";
            case SkillId.ReleaseManagement:      return "Rel";
            case SkillId.TechnicalSupport:       return "Supp";
            case SkillId.Marketing:              return "Mktg";
            case SkillId.BrandManagement:        return "Brand";
            case SkillId.Sales:                  return "Sales";
            case SkillId.Negotiation:            return "Nego";
            case SkillId.HrRecruitment:          return "HR";
            case SkillId.Accountancy:            return "Acct";
            default:                             return "?";
        }
    }

    public static string FormatPersonality(Personality p) {
        switch (p) {
            case Personality.Collaborative:  return "Collaborative";
            case Personality.Professional:   return "Professional";
            case Personality.Easygoing:      return "Easygoing";
            case Personality.Independent:    return "Independent";
            case Personality.Competitive:    return "Competitive";
            case Personality.Perfectionist:  return "Perfectionist";
            case Personality.Intense:        return "Intense";
            case Personality.Abrasive:       return "Abrasive";
            case Personality.Volatile:       return "Volatile";
            default:                         return "Unknown";
        }
    }

    public static string PersonalityBadgeClass(Personality p) {
        switch (p) {
            case Personality.Collaborative:  return "personality--collaborative";
            case Personality.Professional:   return "personality--professional";
            case Personality.Easygoing:      return "personality--easygoing";
            case Personality.Independent:    return "personality--independent";
            case Personality.Competitive:    return "personality--competitive";
            case Personality.Perfectionist:  return "personality--perfectionist";
            case Personality.Intense:        return "personality--intense";
            case Personality.Abrasive:       return "personality--abrasive";
            case Personality.Volatile:       return "personality--volatile";
            default:                         return "personality--unknown";
        }
    }

    public static string FormatChemistryBand(ChemistryBand band) {
        switch (band) {
            case ChemistryBand.Excellent: return "Excellent";
            case ChemistryBand.Good:      return "Good";
            case ChemistryBand.Neutral:   return "Neutral";
            case ChemistryBand.Poor:      return "Poor";
            case ChemistryBand.Toxic:     return "Toxic";
            default:                      return "Neutral";
        }
    }

    public static string ChemistryBandClass(ChemistryBand band) {
        switch (band) {
            case ChemistryBand.Excellent: return "chemistry--excellent";
            case ChemistryBand.Good:      return "chemistry--good";
            case ChemistryBand.Neutral:   return "chemistry--neutral";
            case ChemistryBand.Poor:      return "chemistry--poor";
            case ChemistryBand.Toxic:     return "chemistry--toxic";
            default:                      return "chemistry--neutral";
        }
    }

    public static string FormatMarketTrend(MarketTrend trend) {
        switch (trend) {
            case MarketTrend.Rising:  return "\u25B2 Up";
            case MarketTrend.Stable:  return "\u2500 Flat";
            case MarketTrend.Falling: return "\u25BC Down";
            default:                  return "\u2500 Flat";
        }
    }

    // Build a slash-separated list of the top-weighted skills in a SkillRequirements.
    // Returns at most 3 entries in descending order (e.g. "Design / Programming").
    // No LINQ, no allocation — uses stack-local arrays of fixed size.
    public static string FormatContractSkills(SkillRequirements req) {
        if (req.Weights == null) return "Unknown";

        // Collect entries with weight > 0.05 — insertion sort descending into 3 slots
        const int MaxDisplay = 3;
        float[] topW    = new float[MaxDisplay];
        int[]   topS    = new int[MaxDisplay];
        int     topCount = 0;

        int skillCount = req.Weights.Length;
        for (int s = 0; s < skillCount; s++) {
            float w = req.Weights[s];
            if (w < 0.05f) continue;

            // Insertion sort: find position in topW/topS (descending)
            if (topCount < MaxDisplay) {
                // Insert at the right position
                int pos = topCount;
                while (pos > 0 && topW[pos - 1] < w) pos--;
                for (int j = topCount; j > pos; j--) { topW[j] = topW[j-1]; topS[j] = topS[j-1]; }
                topW[pos] = w;
                topS[pos] = s;
                topCount++;
            } else if (w > topW[MaxDisplay - 1]) {
                int pos = MaxDisplay - 1;
                while (pos > 0 && topW[pos - 1] < w) pos--;
                for (int j = MaxDisplay - 1; j > pos; j--) { topW[j] = topW[j-1]; topS[j] = topS[j-1]; }
                topW[pos] = w;
                topS[pos] = s;
            }
        }

        if (topCount == 0) return "Unknown";

        // Build the string without string concat inside the loop
        switch (topCount) {
            case 1: return FormatSkillShort((SkillId)topS[0]);
            case 2: return FormatSkillShort((SkillId)topS[0]) + " / " + FormatSkillShort((SkillId)topS[1]);
            default: return FormatSkillShort((SkillId)topS[0]) + " / " + FormatSkillShort((SkillId)topS[1]) + " / " + FormatSkillShort((SkillId)topS[2]);
        }
    }

    public static string FormatEnergyBand(EnergyBand band) {
        switch (band) {
            case EnergyBand.Fresh:    return "Fresh";
            case EnergyBand.Fit:      return "Fit";
            case EnergyBand.Tiring:   return "Tiring";
            case EnergyBand.Drained:  return "Drained";
            case EnergyBand.Exhausted: return "Exhausted";
            default:                  return "Unknown";
        }
    }

    public static string EnergyBandClass(EnergyBand band) {
        switch (band) {
            case EnergyBand.Fresh:    return "energy-band--fresh";
            case EnergyBand.Fit:      return "energy-band--fit";
            case EnergyBand.Tiring:   return "energy-band--tiring";
            case EnergyBand.Drained:  return "energy-band--drained";
            case EnergyBand.Exhausted: return "energy-band--exhausted";
            default:                  return "energy-band--fit";
        }
    }

    public static string SuitabilityDotClass(RoleSuitability s) {
        switch (s) {
            case RoleSuitability.Natural:      return "suitability-dot--natural";
            case RoleSuitability.Accomplished: return "suitability-dot--accomplished";
            case RoleSuitability.Competent:    return "suitability-dot--competent";
            case RoleSuitability.Awkward:      return "suitability-dot--awkward";
            case RoleSuitability.Unsuitable:   return "suitability-dot--unsuitable";
            default:                           return "suitability-dot--unsuitable";
        }
    }

    public static Color GetSuitabilityColor(RoleSuitability s) {
        switch (s) {
            case RoleSuitability.Natural:      return new Color(0.290f, 0.871f, 0.502f);
            case RoleSuitability.Accomplished: return new Color(0.639f, 0.898f, 0.208f);
            case RoleSuitability.Competent:    return new Color(0.980f, 0.800f, 0.082f);
            case RoleSuitability.Awkward:      return new Color(0.984f, 0.573f, 0.235f);
            case RoleSuitability.Unsuitable:   return new Color(0.420f, 0.447f, 0.502f);
            default:                           return new Color(0.420f, 0.447f, 0.502f);
        }
    }

    public static string FormatPatience(int current, int max) {
        if (max <= 0) return "";
        var sb = new System.Text.StringBuilder(max * 2);
        for (int i = 0; i < max; i++) {
            if (i > 0) sb.Append(' ');
            sb.Append(i < current ? '\u25CF' : '\u25CB');
        }
        return sb.ToString();
    }
}
