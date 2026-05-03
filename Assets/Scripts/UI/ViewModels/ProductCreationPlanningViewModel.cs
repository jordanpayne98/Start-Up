using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ViewModel for the product creation planning wizard.
/// Manages wizard state, selection data per step, draft state, and forecast calculation.
/// </summary>
public class ProductCreationPlanningViewModel : IViewModel
{
    // ── Constants ───────────────────────────────────────────────────────────
    private const float PlatformScopeMultiplierPerExtra = 0.3f;
    private const string InsufficientData = "Insufficient data";

    // ── Wizard State ────────────────────────────────────────────────────────
    public int CurrentStep { get; private set; }
    public int TotalSteps { get; private set; } = 5;
    public string StepLabel => $"Step {CurrentStep + 1} of {TotalSteps}";
    public ProductDraftState Draft { get; private set; } = new ProductDraftState();

    // ── Step Ordering ───────────────────────────────────────────────────────
    private readonly List<WizardStepId> _stepOrder = new List<WizardStepId>();

    public enum WizardStepId
    {
        ProductType,
        Category,
        Market,
        Genre,
        Platform,
        Features,
        Hardware,
        Teams,
        Budget,
        Review
    }

    public IReadOnlyList<WizardStepId> StepOrder => _stepOrder;
    public WizardStepId CurrentStepId => _stepOrder.Count > CurrentStep ? _stepOrder[CurrentStep] : WizardStepId.ProductType;

    // ── Product Type Data ───────────────────────────────────────────────────
    public List<ProductTypeOption> TypeOptions { get; private set; } = new List<ProductTypeOption>();
    public int SelectedTypeIndex { get => Draft.ProductTypeIndex; private set => Draft.ProductTypeIndex = value; }

    // ── Category Data ───────────────────────────────────────────────────────
    public List<CategoryOption> CategoryOptions { get; private set; } = new List<CategoryOption>();
    public int SelectedCategoryIndex { get => Draft.CategoryIndex; private set => Draft.CategoryIndex = value; }

    // ── Market / Niche Data ─────────────────────────────────────────────────
    public List<NicheOption> NicheOptions { get; private set; } = new List<NicheOption>();
    public int SelectedNicheIndex { get => Draft.NicheIndex; private set => Draft.NicheIndex = value; }

    // ── Genre Data ──────────────────────────────────────────────────────────
    public List<GenreOption> GenreOptions { get; private set; } = new List<GenreOption>();
    public List<FormatOption> FormatOptions { get; private set; } = new List<FormatOption>();
    public int SelectedGenreIndex { get => Draft.GenreIndex; private set => Draft.GenreIndex = value; }
    public int SelectedFormatIndex { get => Draft.FormatIndex; private set => Draft.FormatIndex = value; }
    public bool ShowGenreStep { get; private set; }

    // ── Platform Data ───────────────────────────────────────────────────────
    public List<PlatformOption> PlatformOptions { get; private set; } = new List<PlatformOption>();
    public List<int> SelectedPlatformIndices { get; private set; } = new List<int>();
    public string MultiPlatformWarningText { get; private set; } = "";

    // ── Feature Selection Data ──────────────────────────────────────────────
    public List<FeatureOption> AllFeatures { get; private set; } = new List<FeatureOption>();
    public List<FeatureOption> FilteredFeatures { get; private set; } = new List<FeatureOption>();
    public string ActiveFeatureCategory { get; private set; } = "";
    public List<string> FeatureCategories { get; private set; } = new List<string>();
    public int FeatureSelectedCount { get; private set; }
    public int FeatureScopeTotal { get; private set; }
    public string FeatureSynergyScore { get; private set; } = "--";
    public string FeatureMissingExpected { get; private set; } = "";
    public string FeatureConflictWarning { get; private set; } = "";

    // ── Budget Data ─────────────────────────────────────────────────────────
    public bool IsNameValid { get; private set; } = true;
    public string NameError { get; private set; } = "";
    public int TargetPrice { get; private set; } = 30;
    public string MarketExpectation { get; private set; } = "--";
    public string ValueRisk { get; private set; } = "--";
    public string CompetitorPriceComparison { get; private set; } = "--";
    public ToolDistributionModel SelectedDistributionModel { get; private set; } = ToolDistributionModel.Proprietary;
    public int MarketingBudgetLevel { get; private set; } = 1; // 0=Low,1=Medium,2=High
    public string UpfrontCostPreview { get; private set; } = "--";
    public string MonthlyBurnPreview { get; private set; } = "--";
    public string TotalEstimatedCostPreview { get; private set; } = "--";
    public string RunwayAfterStartPreview { get; private set; } = "--";
    public string BreakEvenEstimatePreview { get; private set; } = "--";

    private static readonly string[] DistributionModelLabels = { "Proprietary", "Licensed", "Open Source" };

    // ── Validation Data ─────────────────────────────────────────────────────
    public bool CanConfirm { get; private set; }
    public List<string> BlockingErrors { get; private set; } = new List<string>();
    public List<string> ValidationWarnings { get; private set; } = new List<string>();

    // ── Hardware Config Data ────────────────────────────────────────────────
    public bool IsHardwareProduct { get; private set; }
    public HardwareTier HwProcessingTier { get; private set; } = HardwareTier.Budget;
    public HardwareTier HwGraphicsTier { get; private set; } = HardwareTier.Budget;
    public HardwareTier HwMemoryTier { get; private set; } = HardwareTier.Budget;
    public HardwareTier HwStorageTier { get; private set; } = HardwareTier.Budget;
    public ConsoleFormFactor HwFormFactor { get; private set; } = ConsoleFormFactor.Standard;
    public int HwPerformanceScore { get; private set; }
    public int HwManufactureCost { get; private set; }
    public int HwDevCostAdd { get; private set; }
    public string HwThermalRisk { get; private set; } = "--";
    public string HwDefectRisk { get; private set; } = "--";
    public string HwReliability { get; private set; } = "--";
    public string HwDevFriendliness { get; private set; } = "--";
    private HardwareGenerationConfig _hwGenConfig;

    // ── Team Planning Data ──────────────────────────────────────────────────
    public ProductSlotData[] TeamSlots { get; private set; } = new ProductSlotData[4];
    public List<TeamOption> AvailableTeams { get; private set; } = new List<TeamOption>();
    public string TeamOverallReadiness { get; private set; } = "--";
    public string TeamTotalSalaryCost { get; private set; } = "--";
    public string TeamMissingCoverage { get; private set; } = "";
    public List<TeamSuggestion> TeamSuggestions { get; private set; } = new List<TeamSuggestion>();

    private static readonly string[] SlotNames = { "Engineering", "Design", "Quality", "Commercial" };
    private static readonly SkillId[][] SlotRequiredSkills =
    {
        new[] { SkillId.Programming, SkillId.SystemsArchitecture, SkillId.PerformanceOptimisation },
        new[] { SkillId.ProductDesign, SkillId.UxUiDesign, SkillId.GameDesign },
        new[] { SkillId.QaTesting, SkillId.BugFixing, SkillId.ReleaseManagement },
        new[] { SkillId.Marketing, SkillId.Sales, SkillId.BrandManagement }
    };

    // ── Forecast Data ───────────────────────────────────────────────────────
    public string QualityRange { get; private set; } = InsufficientData;
    public string InnovationRange { get; private set; } = InsufficientData;
    public string MarketFitRange { get; private set; } = InsufficientData;
    public string ScopeRisk { get; private set; } = InsufficientData;
    public string BugRisk { get; private set; } = InsufficientData;
    public string TechnicalRisk { get; private set; } = InsufficientData;
    public string CommercialRisk { get; private set; } = InsufficientData;
    public string DurationRange { get; private set; } = InsufficientData;
    public string CostRange { get; private set; } = InsufficientData;
    public string Confidence { get; private set; } = "Low";
    public List<DiagnosticCard> TopDiagnostics { get; private set; } = new List<DiagnosticCard>();

    // ── Bottom Bar Data ─────────────────────────────────────────────────────
    public string ScopeLabel { get; private set; } = "--";
    public string CostLabel { get; private set; } = "--";
    public string DurationLabel { get; private set; } = "--";
    public string BugRiskLabel { get; private set; } = "--";
    public string MarketFitLabel { get; private set; } = "--";
    public string MissingCoverage { get; private set; } = "--";

    // ── Events ──────────────────────────────────────────────────────────────
    public event Action OnCancelRequested;
    public event Action OnDraftSaved;

    // ── Dependencies ────────────────────────────────────────────────────────
    private ProductTemplateDefinition[] _templates;
    private MarketNicheData[] _nicheData;
    private IReadOnlyGameState _lastState;

    // ── Setup ───────────────────────────────────────────────────────────────

    public void SetTemplates(ProductTemplateDefinition[] templates)
    {
        _templates = templates;
        RebuildTypeOptions();
    }

    public void SetNicheData(MarketNicheData[] nicheData)
    {
        _nicheData = nicheData;
    }

    public void SetHardwareGenConfig(HardwareGenerationConfig config)
    {
        _hwGenConfig = config;
        RecalculateHardwarePreview();
    }

    // ── IViewModel ──────────────────────────────────────────────────────────

    public void Refresh(GameStateSnapshot snapshot)
    {
        _lastState = snapshot;
        RebuildNicheOptions(snapshot);
        RebuildPlatformOptions(snapshot);
        RebuildAvailableTeams(snapshot);
        RecalculateForecasts();
        RecalculatePricingPreview();
        RecalculateBudgetPreview();
        RecalculateValidation();
        _isDirty = true;
    }

    private bool _isDirty;
    public bool IsDirty => _isDirty;
    public void ClearDirty() => _isDirty = false;

    // ── Navigation ──────────────────────────────────────────────────────────

    public bool CanGoBack => CurrentStep > 0;
    public bool CanContinue => IsCurrentStepComplete();

    public void GoToNextStep()
    {
        if (CurrentStep < TotalSteps - 1)
        {
            CurrentStep++;
        }
    }

    public void GoToPreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
        }
    }

    public void GoToStep(int index)
    {
        if (index >= 0 && index < TotalSteps)
        {
            CurrentStep = index;
        }
    }

    public void RequestCancel()
    {
        OnCancelRequested?.Invoke();
    }

    public void RequestSaveDraft()
    {
        OnDraftSaved?.Invoke();
    }

    // ── Selection Methods ───────────────────────────────────────────────────

    public void SelectProductType(int index)
    {
        if (index < 0 || index >= TypeOptions.Count) return;

        int previousType = SelectedTypeIndex;
        SelectedTypeIndex = index;

        if (previousType != index)
        {
            Draft.ClearFromStep(1);
            SelectedPlatformIndices.Clear();
        }

        var typeOpt = TypeOptions[index];
        ShowGenreStep = typeOpt.Category == ProductCategory.VideoGame;
        IsHardwareProduct = typeOpt.Category == ProductCategory.GameConsole;

        RebuildStepOrder();
        RebuildCategoryOptions();
        RecalculateForecasts();
    }

    public void SelectCategory(int index)
    {
        if (index < 0 || index >= CategoryOptions.Count) return;
        if (CategoryOptions[index].IsLocked) return;

        SelectedCategoryIndex = index;
        Draft.ClearFromStep(2);
        SelectedPlatformIndices.Clear();

        RebuildFeaturesForTemplate();
        RecalculateForecasts();
    }

    public void SelectNiche(int index)
    {
        if (index < 0 || index >= NicheOptions.Count) return;

        SelectedNicheIndex = index;
        RecalculateForecasts();
    }

    public void SelectGenre(int index)
    {
        if (index < 0 || index >= GenreOptions.Count) return;

        SelectedGenreIndex = index;
        RecalculateForecasts();
    }

    public void SelectFormat(int index)
    {
        if (index < 0 || index >= FormatOptions.Count) return;

        SelectedFormatIndex = index;
        RecalculateForecasts();
    }

    public void TogglePlatform(int index)
    {
        if (index < 0 || index >= PlatformOptions.Count) return;

        if (SelectedPlatformIndices.Contains(index))
        {
            SelectedPlatformIndices.Remove(index);
        }
        else
        {
            SelectedPlatformIndices.Add(index);
        }

        Draft.SelectedPlatformIds.Clear();
        for (int i = 0; i < SelectedPlatformIndices.Count; i++)
        {
            var opt = PlatformOptions[SelectedPlatformIndices[i]];
            Draft.SelectedPlatformIds.Add(opt.PlatformIdValue);
        }

        UpdateMultiPlatformWarning();
        RecalculateForecasts();
    }

    // ── Feature Step Methods ────────────────────────────────────────────────

    public void ToggleFeature(string featureId)
    {
        for (int i = 0; i < AllFeatures.Count; i++)
        {
            var feat = AllFeatures[i];
            if (feat.FeatureId != featureId) continue;
            if (feat.IsLocked) return;

            feat.IsSelected = !feat.IsSelected;
            AllFeatures[i] = feat;

            if (feat.IsSelected)
            {
                if (!Draft.SelectedFeatureIds.Contains(i))
                    Draft.SelectedFeatureIds.Add(i);
            }
            else
            {
                Draft.SelectedFeatureIds.Remove(i);
            }
            break;
        }

        RebuildFilteredFeatures();
        RecalculateFeatureSummary();
        RecalculateForecasts();
    }

    public void SetFeatureCategoryFilter(string category)
    {
        ActiveFeatureCategory = category;
        RebuildFilteredFeatures();
    }

    private void RebuildFeaturesForTemplate()
    {
        AllFeatures.Clear();
        FeatureCategories.Clear();
        Draft.SelectedFeatureIds.Clear();
        ActiveFeatureCategory = "";

        ProductTemplateDefinition template = GetSelectedTemplate();
        if (template == null || template.availableFeatures == null)
        {
            FilteredFeatures.Clear();
            RecalculateFeatureSummary();
            return;
        }

        int currentGen = _lastState?.GetCurrentGeneration() ?? 1;
        var featureDefs = template.availableFeatures;
        var categorySet = new List<string>();

        for (int i = 0; i < featureDefs.Length; i++)
        {
            var def = featureDefs[i];
            if (def == null) continue;

            float adoptionRate = _lastState != null
                ? _lastState.GetFeatureAdoptionRate(def.featureId, ProductNiche.None, template.templateId)
                : 0f;

            FeatureDemandStage stage = FeatureDemandHelper.GetDemandStage(
                currentGen,
                def.demandIntroductionGen,
                def.demandMaturitySpeed,
                def.isFoundational,
                adoptionRate);

            string demandLabel = stage == FeatureDemandStage.NotAvailable
                ? ""
                : FeatureDemandHelper.GetDemandLabel(stage);

            bool isLocked = currentGen < def.availableFromGeneration;
            string lockReason = isLocked ? $"Requires Gen {def.availableFromGeneration}" : "";

            string catName = FormatFeatureCategoryName(def.featureCategory);
            bool catFound = false;
            for (int c = 0; c < categorySet.Count; c++)
            {
                if (categorySet[c] == catName) { catFound = true; break; }
            }
            if (!catFound) categorySet.Add(catName);

            bool preSelected = def.isFoundational || stage == FeatureDemandStage.Standard;

            string reviewImpact = stage == FeatureDemandStage.Standard ? "+Review"
                : stage == FeatureDemandStage.Declining ? "–Review"
                : stage == FeatureDemandStage.Legacy ? "–Review"
                : "";

            string[] riskTags = new string[0];
            if (def.requiredTotalSkillPoints > 200)
                riskTags = new[] { "High Skill" };
            else if (def.requiredTotalSkillPoints > 100)
                riskTags = new[] { "Skill Req" };

            AllFeatures.Add(new FeatureOption
            {
                FeatureId = def.featureId,
                DisplayName = def.displayName ?? def.featureId,
                Description = def.description ?? "",
                DemandLabel = demandLabel,
                DemandStage = stage,
                ScopeCost = def.baseCost,
                RequiredSkill = def.requiredTotalSkillPoints > 0 ? def.requiredSkillType : (SkillId?)null,
                RequiredSkillPoints = def.requiredTotalSkillPoints,
                GenerationRequirement = def.availableFromGeneration,
                SynergyTags = def.synergyFeatureIds ?? new string[0],
                RiskTags = riskTags,
                ReviewImpact = reviewImpact,
                IsLocked = isLocked,
                LockReason = lockReason,
                IsSelected = preSelected,
                CategoryName = catName
            });

            if (preSelected)
                Draft.SelectedFeatureIds.Add(AllFeatures.Count - 1);
        }

        FeatureCategories.Clear();
        FeatureCategories.Add("All");
        for (int c = 0; c < categorySet.Count; c++)
            FeatureCategories.Add(categorySet[c]);

        ActiveFeatureCategory = "All";
        RebuildFilteredFeatures();
        RecalculateFeatureSummary();
    }

    private void RebuildFilteredFeatures()
    {
        FilteredFeatures.Clear();
        for (int i = 0; i < AllFeatures.Count; i++)
        {
            var feat = AllFeatures[i];
            if (ActiveFeatureCategory != "All" && feat.CategoryName != ActiveFeatureCategory)
                continue;
            FilteredFeatures.Add(feat);
        }
    }

    private void RecalculateFeatureSummary()
    {
        int selectedCount = 0;
        int scopeTotal = 0;
        float synergyScore = 0f;
        int missingExpected = 0;
        int conflictCount = 0;

        var selectedIds = new List<string>();
        for (int i = 0; i < AllFeatures.Count; i++)
        {
            if (AllFeatures[i].IsSelected)
                selectedIds.Add(AllFeatures[i].FeatureId);
        }

        for (int i = 0; i < AllFeatures.Count; i++)
        {
            var feat = AllFeatures[i];
            if (!feat.IsSelected)
            {
                if (feat.DemandStage == FeatureDemandStage.Standard || feat.DemandStage == FeatureDemandStage.Growing)
                    missingExpected++;
                continue;
            }

            selectedCount++;
            scopeTotal += feat.ScopeCost;

            var synTags = feat.SynergyTags;
            if (synTags != null)
            {
                for (int s = 0; s < synTags.Length; s++)
                {
                    for (int j = 0; j < selectedIds.Count; j++)
                    {
                        if (selectedIds[j] == synTags[s])
                        {
                            synergyScore += 1f;
                            break;
                        }
                    }
                }
            }
        }

        FeatureSelectedCount = selectedCount;
        FeatureScopeTotal = scopeTotal;
        FeatureSynergyScore = synergyScore > 0 ? $"+{synergyScore:F0}" : "None";
        FeatureMissingExpected = missingExpected > 0
            ? $"{missingExpected} expected feature{(missingExpected > 1 ? "s" : "")} not selected — may impact review scores."
            : "";
        FeatureConflictWarning = conflictCount > 0
            ? $"{conflictCount} feature conflict{(conflictCount > 1 ? "s" : "")} detected."
            : "";
    }

    private static string FormatFeatureCategoryName(FeatureCategory cat)
    {
        switch (cat)
        {
            case FeatureCategory.System: return "System";
            case FeatureCategory.Interface: return "Interface";
            case FeatureCategory.Network: return "Network";
            case FeatureCategory.Hardware: return "Hardware";
            case FeatureCategory.Platform: return "Platform";
            case FeatureCategory.Services: return "Services";
            case FeatureCategory.Rendering: return "Rendering";
            case FeatureCategory.Simulation: return "Simulation";
            case FeatureCategory.Tooling: return "Tooling";
            case FeatureCategory.Creation: return "Creation";
            case FeatureCategory.Pipeline: return "Pipeline";
            case FeatureCategory.Collaboration: return "Collaboration";
            case FeatureCategory.Production: return "Production";
            case FeatureCategory.Processing: return "Processing";
            case FeatureCategory.Distribution: return "Distribution";
            case FeatureCategory.Core: return "Core";
            case FeatureCategory.DeveloperExperience: return "Dev Experience";
            case FeatureCategory.Ecosystem: return "Ecosystem";
            case FeatureCategory.Gameplay: return "Gameplay";
            case FeatureCategory.Presentation: return "Presentation";
            case FeatureCategory.Social: return "Social";
            default: return cat.ToString();
        }
    }

    // ── Hardware Config Methods ─────────────────────────────────────────────

    public void SetProcessingTier(HardwareTier tier)
    {
        HwProcessingTier = tier;
        Draft.HardwareConfig.processingTier = tier;
        RecalculateHardwarePreview();
        RecalculateForecasts();
    }

    public void SetGraphicsTier(HardwareTier tier)
    {
        HwGraphicsTier = tier;
        Draft.HardwareConfig.graphicsTier = tier;
        RecalculateHardwarePreview();
        RecalculateForecasts();
    }

    public void SetMemoryTier(HardwareTier tier)
    {
        HwMemoryTier = tier;
        Draft.HardwareConfig.memoryTier = tier;
        RecalculateHardwarePreview();
        RecalculateForecasts();
    }

    public void SetStorageTier(HardwareTier tier)
    {
        HwStorageTier = tier;
        Draft.HardwareConfig.storageTier = tier;
        RecalculateHardwarePreview();
        RecalculateForecasts();
    }

    public void SetFormFactor(ConsoleFormFactor formFactor)
    {
        HwFormFactor = formFactor;
        Draft.HardwareConfig.formFactor = formFactor;
        RecalculateHardwarePreview();
        RecalculateForecasts();
    }

    private void RecalculateHardwarePreview()
    {
        var cfg = Draft.HardwareConfig;

        int tierScore(HardwareTier t) => (int)t + 1;
        int rawScore = (tierScore(cfg.processingTier) * 3 + tierScore(cfg.graphicsTier) * 3
            + tierScore(cfg.memoryTier) * 2 + tierScore(cfg.storageTier)) / 9;
        HwPerformanceScore = Mathf.Clamp(rawScore * 25, 25, 100);

        if (_hwGenConfig != null)
        {
            HwManufactureCost = _hwGenConfig.CalculateManufactureCost(cfg);
            HwDevCostAdd = _hwGenConfig.CalculateDevCostAdd(cfg);
        }
        else
        {
            HwManufactureCost = (tierScore(cfg.processingTier) + tierScore(cfg.graphicsTier)) * 25_000;
            HwDevCostAdd = (tierScore(cfg.processingTier) + tierScore(cfg.graphicsTier)) * 10_000;
        }

        int heatIndex = (int)cfg.processingTier + (int)cfg.graphicsTier;
        HwThermalRisk = heatIndex >= 6 ? "High" : heatIndex >= 3 ? "Medium" : "Low";
        HwDefectRisk = (int)cfg.processingTier >= (int)HardwareTier.Enthusiast ? "Medium" : "Low";
        HwReliability = (int)cfg.storageTier >= (int)HardwareTier.MidRange ? "Good" : "Acceptable";
        HwDevFriendliness = cfg.formFactor == ConsoleFormFactor.Standard ? "High"
            : cfg.formFactor == ConsoleFormFactor.Hybrid ? "Medium" : "Low";
    }

    // ── Team Planning Methods ───────────────────────────────────────────────

    public void AssignTeamToSlot(int slotIndex, TeamId teamId)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        var slot = TeamSlots[slotIndex];
        slot.AssignedTeamId = teamId;

        bool found = false;
        for (int t = 0; t < AvailableTeams.Count; t++)
        {
            if (AvailableTeams[t].Id == teamId)
            {
                slot.AssignedTeamName = AvailableTeams[t].Name;
                found = true;
                break;
            }
        }
        if (!found) slot.AssignedTeamName = "";

        Draft.TeamAssignments[slotIndex] = new TeamSlotAssignment
        {
            TeamId = teamId.Value,
            IsAssigned = true
        };

        RecalculateSlotMetrics(slotIndex, ref slot);
        TeamSlots[slotIndex] = slot;
        RecalculateTeamOverview();
        RecalculateForecasts();
    }

    public void ClearTeamSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        var slot = TeamSlots[slotIndex];
        slot.AssignedTeamId = null;
        slot.AssignedTeamName = "";
        slot.SkillMatch = 0;
        slot.Warnings = new string[0];
        slot.MissingSkillNames = new string[0];
        TeamSlots[slotIndex] = slot;

        Draft.TeamAssignments[slotIndex] = default;

        RecalculateTeamOverview();
        RecalculateForecasts();
    }

    private void RebuildAvailableTeams(IReadOnlyGameState state)
    {
        AvailableTeams.Clear();

        if (state == null) return;

        var activeTeams = state.ActiveTeams;
        int tc = activeTeams.Count;
        for (int i = 0; i < tc; i++)
        {
            var team = activeTeams[i];
            TeamType type = state.GetTeamType(team.id);
            if (type == TeamType.HR || type == TeamType.Marketing) continue;

            bool onProduct = state.IsTeamAssignedToProduct(team.id);
            bool onContract = state.GetContractForTeam(team.id) != null;

            string statusText = onProduct ? "On Product"
                : onContract ? "On Contract"
                : "Available";

            AvailableTeams.Add(new TeamOption
            {
                Id = team.id,
                Name = team.name,
                MemberCount = team.MemberCount,
                StatusText = statusText,
                IsAvailable = !onProduct && !onContract
            });
        }

        RebuildTeamSlotDefaults();
        RebuildTeamSuggestions();
    }

    private void RebuildTeamSlotDefaults()
    {
        if (TeamSlots[0].SlotName == null)
        {
            for (int i = 0; i < 4; i++)
            {
                TeamSlots[i] = new ProductSlotData
                {
                    SlotName = SlotNames[i],
                    RequiredSkills = SlotRequiredSkills[i],
                    AssignedTeamId = null,
                    AssignedTeamName = "",
                    SkillMatch = 0,
                    ContributionEstimate = "--",
                    Warnings = new string[0],
                    MissingSkillNames = new string[0]
                };
            }
        }

        for (int i = 0; i < 4; i++)
        {
            var slot = TeamSlots[i];
            if (slot.AssignedTeamId.HasValue)
            {
                RecalculateSlotMetrics(i, ref slot);
                TeamSlots[i] = slot;
            }
        }
    }

    private void RecalculateSlotMetrics(int slotIndex, ref ProductSlotData slot)
    {
        if (!slot.AssignedTeamId.HasValue || _lastState == null)
        {
            slot.SkillMatch = 0;
            slot.ContributionEstimate = "--";
            slot.Warnings = new string[0];
            slot.MissingSkillNames = new string[0];
            return;
        }

        var teamId = slot.AssignedTeamId.Value;
        var chemistry = _lastState.GetTeamChemistry(teamId);
        slot.MoraleValue = chemistry.Score;
        slot.ChemistryValue = chemistry.Score;
        float energy = _lastState.GetTeamAverageEnergy(teamId);
        slot.EnergyValue = (int)(energy * 100f);

        var requiredSkills = slot.RequiredSkills ?? SlotRequiredSkills[slotIndex];
        int matchedSkills = 0;
        int totalRequired = requiredSkills.Length;

        var members = _lastState.GetTeamMemberRoles(teamId);
        var missingSkillNames = new List<string>();
        for (int s = 0; s < requiredSkills.Length; s++)
        {
            bool skillCovered = false;
            for (int m = 0; m < members.Count; m++)
            {
                if (members[m].EmployeeRole == RoleId.SoftwareEngineer || members.Count > 0)
                {
                    skillCovered = true;
                    break;
                }
            }
            if (skillCovered)
                matchedSkills++;
            else
                missingSkillNames.Add(requiredSkills[s].ToString());
        }

        slot.SkillMatch = totalRequired > 0 ? Mathf.Clamp(matchedSkills * 100 / totalRequired, 0, 100) : 50;
        slot.MissingSkillNames = missingSkillNames.Count > 0 ? missingSkillNames.ToArray() : new string[0];

        string quality = slot.SkillMatch >= 75 ? "Good" : slot.SkillMatch >= 40 ? "Moderate" : "Weak";
        slot.ContributionEstimate = quality;

        var warnings = new List<string>();

        bool alreadyOnProduct = _lastState.IsTeamAssignedToProduct(teamId);
        if (alreadyOnProduct)
            warnings.Add("Team is already assigned to another product.");

        if (slot.MoraleValue < 30)
            warnings.Add("Low team morale may slow progress.");

        if (slot.EnergyValue < 25)
            warnings.Add("Team is fatigued. Performance reduced.");

        slot.Warnings = warnings.ToArray();
    }

    private void RecalculateTeamOverview()
    {
        int assignedSlots = 0;
        int totalSalary = 0;
        var missingSlots = new List<string>();

        for (int i = 0; i < 4; i++)
        {
            var slot = TeamSlots[i];
            if (slot.AssignedTeamId.HasValue)
            {
                assignedSlots++;
                if (_lastState != null)
                    totalSalary += _lastState.TotalSalaryCost / Mathf.Max(1, _lastState.ActiveTeams.Count);
            }
            else
            {
                missingSlots.Add(slot.SlotName ?? SlotNames[i]);
            }
        }

        int readinessPct = assignedSlots * 25;
        TeamOverallReadiness = readinessPct >= 100 ? "Ready"
            : readinessPct >= 50 ? $"{readinessPct}% — Partial"
            : $"{readinessPct}% — Incomplete";

        TeamTotalSalaryCost = totalSalary > 0 ? $"${totalSalary:N0}/mo" : "--";

        TeamMissingCoverage = missingSlots.Count > 0
            ? "Missing: " + string.Join(", ", missingSlots)
            : "";
    }

    private void RebuildTeamSuggestions()
    {
        TeamSuggestions.Clear();

        for (int slotIdx = 0; slotIdx < 4; slotIdx++)
        {
            TeamOption bestTeam = default;
            bool found = false;
            int bestScore = -1;

            for (int t = 0; t < AvailableTeams.Count; t++)
            {
                var team = AvailableTeams[t];
                if (!team.IsAvailable) continue;

                int score = team.MemberCount * 10;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTeam = team;
                    found = true;
                }
            }

            if (found)
            {
                TeamSuggestions.Add(new TeamSuggestion
                {
                    SlotName = SlotNames[slotIdx],
                    TeamId = bestTeam.Id,
                    TeamName = bestTeam.Name,
                    MatchScore = Mathf.Clamp(bestScore, 0, 100)
                });
            }
        }
    }

    // ── Step Ordering Logic ─────────────────────────────────────────────────

    private void RebuildStepOrder()
    {
        _stepOrder.Clear();
        _stepOrder.Add(WizardStepId.ProductType);
        _stepOrder.Add(WizardStepId.Category);
        _stepOrder.Add(WizardStepId.Market);

        if (ShowGenreStep)
        {
            _stepOrder.Add(WizardStepId.Genre);
        }

        _stepOrder.Add(WizardStepId.Platform);
        _stepOrder.Add(WizardStepId.Features);

        if (SelectedTypeIndex >= 0 && SelectedTypeIndex < TypeOptions.Count
            && TypeOptions[SelectedTypeIndex].Category == ProductCategory.GameConsole)
        {
            _stepOrder.Add(WizardStepId.Hardware);
        }

        _stepOrder.Add(WizardStepId.Teams);
        _stepOrder.Add(WizardStepId.Budget);
        _stepOrder.Add(WizardStepId.Review);

        TotalSteps = _stepOrder.Count;

        if (CurrentStep >= TotalSteps)
        {
            CurrentStep = TotalSteps - 1;
        }
    }

    // ── Type Options ────────────────────────────────────────────────────────

    private void RebuildTypeOptions()
    {
        TypeOptions.Clear();

        if (_templates == null) return;

        var categoryOrder = new List<ProductCategory>();
        for (int i = 0; i < _templates.Length; i++)
        {
            var t = _templates[i];
            if (t == null) continue;
            bool found = false;
            for (int c = 0; c < categoryOrder.Count; c++)
            {
                if (categoryOrder[c] == t.category) { found = true; break; }
            }
            if (!found) categoryOrder.Add(t.category);
        }

        for (int ci = 0; ci < categoryOrder.Count; ci++)
        {
            var cat = categoryOrder[ci];
            int templateCount = 0;
            int avgDifficulty = 0;
            int avgCost = 0;

            for (int i = 0; i < _templates.Length; i++)
            {
                if (_templates[i] == null || _templates[i].category != cat) continue;
                templateCount++;
                avgDifficulty += _templates[i].difficultyTier;
                avgCost += _templates[i].baseUpfrontCost;
            }

            if (templateCount > 0)
            {
                avgDifficulty /= templateCount;
                avgCost /= templateCount;
            }

            TypeOptions.Add(new ProductTypeOption
            {
                Category = cat,
                DisplayName = FormatCategoryName(cat),
                Description = GetCategoryDescription(cat),
                SkillNeeds = GetCategorySkillNeeds(cat),
                TeamNeeds = GetCategoryTeamNeeds(cat),
                MarketRisk = GetCategoryMarketRisk(cat),
                RecommendedStage = GetCategoryRecommendedStage(cat),
                Layer = cat.ToProductLayer(),
                TemplateCount = templateCount,
                AvgDifficulty = avgDifficulty,
                AvgCost = avgCost
            });
        }

        RebuildStepOrder();
    }

    // ── Category Options ────────────────────────────────────────────────────

    private void RebuildCategoryOptions()
    {
        CategoryOptions.Clear();

        if (_templates == null || SelectedTypeIndex < 0 || SelectedTypeIndex >= TypeOptions.Count) return;

        var selectedCategory = TypeOptions[SelectedTypeIndex].Category;

        for (int i = 0; i < _templates.Length; i++)
        {
            var t = _templates[i];
            if (t == null || t.category != selectedCategory) continue;

            string unlockReason = CheckTemplateUnlockStatus(t);
            bool isLocked = !string.IsNullOrEmpty(unlockReason);

            int phaseCount = t.phases != null ? t.phases.Length : 0;
            int featureCount = t.availableFeatures != null ? t.availableFeatures.Length : 0;
            int genReq = t.difficultyTier >= 4 ? 2 : 1;

            CategoryOptions.Add(new CategoryOption
            {
                TemplateId = t.templateId,
                DisplayName = t.displayName,
                Description = t.description,
                DifficultyTier = t.difficultyTier,
                BaseCost = t.baseUpfrontCost,
                PhaseCount = phaseCount,
                FeatureCount = featureCount,
                GenerationRequirement = genReq,
                IsLocked = isLocked,
                LockedReason = unlockReason
            });
        }
    }

    // ── Niche Options ───────────────────────────────────────────────────────

    private void RebuildNicheOptions(IReadOnlyGameState state)
    {
        NicheOptions.Clear();

        if (_nicheData == null) return;

        for (int i = 0; i < _nicheData.Length; i++)
        {
            var nd = _nicheData[i];
            if (nd == null) continue;

            float demand = state != null ? state.GetNicheDemand(nd.niche) : nd.baseDemand;
            MarketTrend trend = state != null ? state.GetNicheTrend(nd.niche) : MarketTrend.Stable;

            string competitionText;
            float saturation;
            string riskLabel;

            if (demand > 70f)
            {
                riskLabel = "Low";
                competitionText = "Low";
            }
            else if (demand > 40f)
            {
                riskLabel = "Medium";
                competitionText = "Moderate";
            }
            else
            {
                riskLabel = "High";
                competitionText = "High";
            }

            saturation = Mathf.Clamp01(1f - demand / 100f);

            NicheOptions.Add(new NicheOption
            {
                Niche = nd.niche,
                DisplayName = nd.displayName,
                DemandPercent = demand,
                DemandText = $"{demand:F0}%",
                TrendText = trend.ToString(),
                Trend = trend,
                CompetitionText = competitionText,
                SaturationPercent = saturation,
                RiskLabel = riskLabel,
                ProjectedGrowth = trend == MarketTrend.Rising ? "High" : trend == MarketTrend.Falling ? "Declining" : "Stable"
            });
        }

        if (state != null && SelectedTypeIndex >= 0 && SelectedTypeIndex < TypeOptions.Count)
        {
            var selectedCat = TypeOptions[SelectedTypeIndex].Category;
            if (selectedCat != ProductCategory.VideoGame)
            {
                float catDemand = state.GetCategoryDemand(selectedCat);
                MarketTrend catTrend = state.GetCategoryTrend(selectedCat);

                if (NicheOptions.Count == 0)
                {
                    NicheOptions.Add(new NicheOption
                    {
                        Niche = ProductNiche.None,
                        DisplayName = FormatCategoryName(selectedCat) + " Market",
                        DemandPercent = catDemand,
                        DemandText = $"{catDemand:F0}%",
                        TrendText = catTrend.ToString(),
                        Trend = catTrend,
                        CompetitionText = catDemand > 50f ? "Moderate" : "High",
                        SaturationPercent = Mathf.Clamp01(1f - catDemand / 100f),
                        RiskLabel = catDemand > 60f ? "Low" : catDemand > 35f ? "Medium" : "High",
                        ProjectedGrowth = catTrend == MarketTrend.Rising ? "High" : catTrend == MarketTrend.Falling ? "Declining" : "Stable"
                    });
                }
            }
        }
    }

    // ── Platform Options ────────────────────────────────────────────────────

    private void RebuildPlatformOptions(IReadOnlyGameState state)
    {
        PlatformOptions.Clear();

        if (state == null) return;

        var shippedProducts = state.ShippedProducts;
        if (shippedProducts == null) return;

        foreach (var kvp in shippedProducts)
        {
            var product = kvp.Value;
            if (product == null) continue;

            if (product.Category.IsPlatform())
            {
                string ownerText = product.IsCompetitorProduct ? "Competitor" : "You";

                PlatformOptions.Add(new PlatformOption
                {
                    PlatformIdValue = kvp.Key.Value,
                    DisplayName = product.ProductName,
                    OwnerText = ownerText,
                    IsOwnPlatform = !product.IsCompetitorProduct,
                    MarketSharePercent = 0f,
                    InstallBase = 0,
                    QualityCeiling = 65f,
                    LicensingRate = 0f
                });
            }
        }
    }

    // ── Forecast Calculation ────────────────────────────────────────────────

    private void RecalculateForecasts()
    {
        bool hasType = SelectedTypeIndex >= 0;
        bool hasCategory = SelectedCategoryIndex >= 0;
        bool hasNiche = SelectedNicheIndex >= 0;

        if (!hasType || !hasCategory)
        {
            ResetForecasts();
            return;
        }

        ProductTemplateDefinition selectedTemplate = GetSelectedTemplate();
        if (selectedTemplate == null)
        {
            ResetForecasts();
            return;
        }

        Confidence = "Low";

        int baseCost = selectedTemplate.baseUpfrontCost;
        int platformMultiplier = SelectedPlatformIndices.Count > 1
            ? (int)(baseCost * PlatformScopeMultiplierPerExtra * (SelectedPlatformIndices.Count - 1))
            : 0;
        int featureCostAdd = FeatureScopeTotal;
        int hwCostAdd = IsHardwareProduct ? HwDevCostAdd : 0;
        int estimatedCost = baseCost + platformMultiplier + featureCostAdd + hwCostAdd;

        CostRange = $"${estimatedCost:N0}";
        CostLabel = CostRange;

        float totalWorkUnits = 0f;
        if (selectedTemplate.phases != null)
        {
            for (int i = 0; i < selectedTemplate.phases.Length; i++)
            {
                if (selectedTemplate.phases[i] != null)
                    totalWorkUnits += selectedTemplate.phases[i].baseWorkUnits;
            }
        }

        float featureScale = 1f + FeatureSelectedCount * 0.1f;
        int estimatedDays = Mathf.Max(30, Mathf.RoundToInt(totalWorkUnits / 10f * featureScale));
        int estimatedMonths = Mathf.Max(1, estimatedDays / 30);
        DurationRange = estimatedMonths <= 1 ? "~1 month" : $"~{estimatedMonths} months";
        DurationLabel = DurationRange;

        // Scope risk
        int optimalFeatureCount = selectedTemplate.availableFeatures != null
            ? Mathf.Max(3, selectedTemplate.availableFeatures.Length / 3)
            : 3;
        float scopeRatio = optimalFeatureCount > 0 ? (float)FeatureSelectedCount / optimalFeatureCount : 0f;
        ScopeRisk = scopeRatio > 1.5f ? "High" : scopeRatio > 1.1f ? "Medium" : "Low";
        ScopeLabel = ScopeRisk;

        // Bug risk based on team QA assignment and scope
        int qaAssigned = 0;
        for (int i = 0; i < 4; i++)
        {
            if (TeamSlots[i].SlotName == "Quality" && TeamSlots[i].AssignedTeamId.HasValue)
                qaAssigned++;
        }
        BugRisk = (scopeRatio > 1.3f && qaAssigned == 0) ? "High"
            : scopeRatio > 1.1f ? "Medium"
            : "Low";
        BugRiskLabel = BugRisk;

        // Technical risk from hardware complexity + scope + team engineering coverage
        int engAssigned = 0;
        for (int i = 0; i < 4; i++)
        {
            if (TeamSlots[i].SlotName == "Engineering" && TeamSlots[i].AssignedTeamId.HasValue)
                engAssigned++;
        }
        bool highComplexity = IsHardwareProduct || selectedTemplate.difficultyTier >= 4;
        TechnicalRisk = (highComplexity && engAssigned == 0) ? "High"
            : highComplexity ? "Medium"
            : scopeRatio > 1.3f ? "Medium"
            : "Low";

        // Commercial risk from niche saturation, platform count, and marketing
        if (hasNiche && SelectedNicheIndex < NicheOptions.Count)
        {
            var nicheOptC = NicheOptions[SelectedNicheIndex];
            float saturation = nicheOptC.SaturationPercent;
            bool manyPlatforms = SelectedPlatformIndices.Count >= 3;
            CommercialRisk = (saturation > 0.7f || (manyPlatforms && saturation > 0.5f)) ? "High"
                : saturation > 0.4f ? "Medium"
                : "Low";
        }
        else
        {
            CommercialRisk = InsufficientData;
        }

        // Market fit
        if (hasNiche && SelectedNicheIndex < NicheOptions.Count)
        {
            var nicheOpt = NicheOptions[SelectedNicheIndex];
            float demand = nicheOpt.DemandPercent;

            if (demand >= 70f)
            {
                MarketFitRange = "Strong";
                MarketFitLabel = "Strong";
            }
            else if (demand >= 40f)
            {
                MarketFitRange = "Moderate";
                MarketFitLabel = "Moderate";
            }
            else
            {
                MarketFitRange = "Weak";
                MarketFitLabel = "Weak";
            }

            Confidence = "Medium";
        }
        else
        {
            MarketFitRange = InsufficientData;
            MarketFitLabel = "--";
        }

        // Quality from team skill match
        int assignedSlots = 0;
        int totalSkillMatch = 0;
        for (int i = 0; i < 4; i++)
        {
            if (TeamSlots[i].AssignedTeamId.HasValue)
            {
                assignedSlots++;
                totalSkillMatch += TeamSlots[i].SkillMatch;
            }
        }

        if (assignedSlots > 0)
        {
            int avgSkillMatch = totalSkillMatch / assignedSlots;
            QualityRange = avgSkillMatch >= 75 ? "High"
                : avgSkillMatch >= 40 ? "Medium"
                : "Low";
            Confidence = assignedSlots >= 3 ? "High" : "Medium";
        }
        else
        {
            QualityRange = InsufficientData;
        }

        // Innovation from feature synergy + emerging features
        float innovationVal = 0f;
        for (int i = 0; i < AllFeatures.Count; i++)
        {
            if (!AllFeatures[i].IsSelected) continue;
            innovationVal += FeatureDemandHelper.GetInnovationValue(AllFeatures[i].DemandStage);
        }
        InnovationRange = innovationVal >= 15f ? "High"
            : innovationVal >= 5f ? "Medium"
            : "Low";

        MissingCoverage = ComputeMissingCoverage();

        RebuildDiagnostics();
        RecalculateBudgetPreview();
        RecalculateValidation();
    }

    private void ResetForecasts()
    {
        QualityRange = InsufficientData;
        InnovationRange = InsufficientData;
        MarketFitRange = InsufficientData;
        ScopeRisk = InsufficientData;
        BugRisk = InsufficientData;
        TechnicalRisk = InsufficientData;
        CommercialRisk = InsufficientData;
        DurationRange = InsufficientData;
        CostRange = InsufficientData;
        Confidence = "Low";
        ScopeLabel = "--";
        CostLabel = "--";
        DurationLabel = "--";
        BugRiskLabel = "--";
        MarketFitLabel = "--";
        MissingCoverage = "--";
        TopDiagnostics.Clear();
    }

    private string ComputeMissingCoverage()
    {
        var missing = new List<string>();

        if (SelectedTypeIndex < 0) missing.Add("Type");
        if (SelectedCategoryIndex < 0) missing.Add("Category");
        if (SelectedNicheIndex < 0) missing.Add("Market");
        if (ShowGenreStep && SelectedGenreIndex < 0) missing.Add("Genre");
        if (SelectedPlatformIndices.Count == 0) missing.Add("Platform");
        if (FeatureSelectedCount == 0) missing.Add("Features");

        int assignedTeams = 0;
        for (int i = 0; i < 4; i++)
            if (TeamSlots[i].AssignedTeamId.HasValue) assignedTeams++;
        if (assignedTeams == 0) missing.Add("Teams");

        return missing.Count > 0 ? string.Join(", ", missing) : "Complete";
    }

    // ── Diagnostics ─────────────────────────────────────────────────────────

    private void RebuildDiagnostics()
    {
        TopDiagnostics.Clear();

        if (SelectedTypeIndex >= 0 && SelectedTypeIndex < TypeOptions.Count)
        {
            var typeOpt = TypeOptions[SelectedTypeIndex];
            if (typeOpt.AvgDifficulty >= 4)
            {
                TopDiagnostics.Add(new DiagnosticCard
                {
                    Title = "High Difficulty",
                    Description = $"{typeOpt.DisplayName} products are complex. Ensure your team has sufficient skills.",
                    Severity = DiagnosticSeverity.Warning
                });
            }
        }

        if (SelectedNicheIndex >= 0 && SelectedNicheIndex < NicheOptions.Count)
        {
            var nicheOpt = NicheOptions[SelectedNicheIndex];
            if (nicheOpt.Trend == MarketTrend.Falling)
            {
                TopDiagnostics.Add(new DiagnosticCard
                {
                    Title = "Declining Market",
                    Description = $"{nicheOpt.DisplayName} demand is falling. Consider timing or pivoting.",
                    Severity = DiagnosticSeverity.Warning
                });
            }
            else if (nicheOpt.Trend == MarketTrend.Rising)
            {
                TopDiagnostics.Add(new DiagnosticCard
                {
                    Title = "Rising Market",
                    Description = $"{nicheOpt.DisplayName} demand is growing. Good timing for entry.",
                    Severity = DiagnosticSeverity.Success
                });
            }
        }

        if (!string.IsNullOrEmpty(FeatureMissingExpected))
        {
            TopDiagnostics.Add(new DiagnosticCard
            {
                Title = "Missing Expected Features",
                Description = FeatureMissingExpected,
                Severity = DiagnosticSeverity.Warning
            });
        }

        if (SelectedPlatformIndices.Count > 2)
        {
            TopDiagnostics.Add(new DiagnosticCard
            {
                Title = "Wide Platform Spread",
                Description = $"Targeting {SelectedPlatformIndices.Count} platforms significantly increases scope and cost.",
                Severity = DiagnosticSeverity.Warning
            });
        }

        string teamMissing = TeamMissingCoverage;
        if (!string.IsNullOrEmpty(teamMissing))
        {
            TopDiagnostics.Add(new DiagnosticCard
            {
                Title = "Incomplete Team Coverage",
                Description = teamMissing,
                Severity = DiagnosticSeverity.Warning
            });
        }

        while (TopDiagnostics.Count > 4)
            TopDiagnostics.RemoveAt(TopDiagnostics.Count - 1);
    }

    // ── Multi-Platform Warning ──────────────────────────────────────────────

    private void UpdateMultiPlatformWarning()
    {
        if (SelectedPlatformIndices.Count > 1)
        {
            float extraScope = PlatformScopeMultiplierPerExtra * (SelectedPlatformIndices.Count - 1) * 100f;
            MultiPlatformWarningText = $"Multi-platform scope: +{extraScope:F0}% — {SelectedPlatformIndices.Count} platforms selected. Consider reducing platforms or extending the timeline.";
        }
        else
        {
            MultiPlatformWarningText = "";
        }
    }

    // ── Step Completion Check ────────────────────────────────────────────────

    private bool IsCurrentStepComplete()
    {
        if (CurrentStep >= _stepOrder.Count) return false;

        switch (_stepOrder[CurrentStep])
        {
            case WizardStepId.ProductType: return SelectedTypeIndex >= 0;
            case WizardStepId.Category: return SelectedCategoryIndex >= 0;
            case WizardStepId.Market: return SelectedNicheIndex >= 0;
            case WizardStepId.Genre: return SelectedGenreIndex >= 0;
            case WizardStepId.Platform: return SelectedPlatformIndices.Count > 0;
            case WizardStepId.Features: return FeatureSelectedCount >= 0;
            case WizardStepId.Hardware: return true;
            case WizardStepId.Teams: return true;
            case WizardStepId.Budget: return true;
            case WizardStepId.Review: return true;
            default: return false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ProductTemplateDefinition GetSelectedTemplate()
    {
        if (_templates == null || SelectedCategoryIndex < 0 || SelectedCategoryIndex >= CategoryOptions.Count)
            return null;

        string templateId = CategoryOptions[SelectedCategoryIndex].TemplateId;

        for (int i = 0; i < _templates.Length; i++)
        {
            if (_templates[i] != null && _templates[i].templateId == templateId)
                return _templates[i];
        }

        return null;
    }

    private string CheckTemplateUnlockStatus(ProductTemplateDefinition template)
    {
        if (_lastState == null) return null;

        if (template.difficultyTier >= 4)
        {
            int rep = _lastState.Reputation;
            if (rep < 30)
            {
                return $"Requires 30+ reputation (current: {rep})";
            }
        }

        if (template.difficultyTier >= 5)
        {
            int rep = _lastState.Reputation;
            if (rep < 60)
            {
                return $"Requires 60+ reputation (current: {rep})";
            }
        }

        return null;
    }

    private static string FormatCategoryName(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.OperatingSystem: return "Operating System";
            case ProductCategory.GameConsole: return "Game Console";
            case ProductCategory.GameEngine: return "Game Engine";
            case ProductCategory.GraphicsEditor: return "Graphics Editor";
            case ProductCategory.AudioTool: return "Audio Tool";
            case ProductCategory.DevFramework: return "Dev Framework";
            case ProductCategory.VideoGame: return "Video Game";
            default: return cat.ToString();
        }
    }

    private static string GetCategoryDescription(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.OperatingSystem: return "Build an operating system platform. High cost, high reward.";
            case ProductCategory.GameConsole: return "Design and manufacture a game console with custom hardware.";
            case ProductCategory.GameEngine: return "Create a game engine used by other developers.";
            case ProductCategory.GraphicsEditor: return "Build graphics and design tools for creative professionals.";
            case ProductCategory.AudioTool: return "Develop audio production and editing software.";
            case ProductCategory.DevFramework: return "Create development frameworks and libraries.";
            case ProductCategory.VideoGame: return "Develop video games for various platforms and genres.";
            default: return "";
        }
    }

    private static string GetCategorySkillNeeds(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.VideoGame: return "Game Design, Programming, Art";
            case ProductCategory.GameEngine: return "Systems Architecture, Programming, Tooling";
            case ProductCategory.GameConsole: return "Hardware Engineering, Systems Architecture, QA";
            case ProductCategory.OperatingSystem: return "Systems Architecture, Kernel Dev, QA";
            case ProductCategory.GraphicsEditor: return "Rendering, UI/UX Design, Programming";
            case ProductCategory.AudioTool: return "Audio Processing, DSP, Programming";
            case ProductCategory.DevFramework: return "Programming, API Design, Documentation";
            default: return "Programming, Design";
        }
    }

    private static string GetCategoryTeamNeeds(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.VideoGame: return "Dev, Design, QA, Marketing";
            case ProductCategory.GameEngine: return "Dev, Systems, QA";
            case ProductCategory.GameConsole: return "Hardware, Dev, QA";
            case ProductCategory.OperatingSystem: return "Dev, Systems, QA, Security";
            case ProductCategory.GraphicsEditor: return "Dev, Design, QA";
            case ProductCategory.AudioTool: return "Dev, Audio, QA";
            case ProductCategory.DevFramework: return "Dev, QA";
            default: return "Dev, QA";
        }
    }

    private static string GetCategoryMarketRisk(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.VideoGame: return "Medium";
            case ProductCategory.GameEngine: return "Medium";
            case ProductCategory.GameConsole: return "High";
            case ProductCategory.OperatingSystem: return "High";
            case ProductCategory.GraphicsEditor: return "Low";
            case ProductCategory.AudioTool: return "Low";
            case ProductCategory.DevFramework: return "Low";
            default: return "Medium";
        }
    }

    private static string GetCategoryRecommendedStage(ProductCategory cat)
    {
        switch (cat)
        {
            case ProductCategory.VideoGame: return "Early";
            case ProductCategory.GameEngine: return "Mid";
            case ProductCategory.GameConsole: return "Late";
            case ProductCategory.OperatingSystem: return "Late";
            case ProductCategory.GraphicsEditor: return "Early";
            case ProductCategory.AudioTool: return "Early";
            case ProductCategory.DevFramework: return "Mid";
            default: return "Any";
        }
    }

    // ── Genre & Format Options (Static Data) ────────────────────────────────

    public void BuildGenreOptions()
    {
        GenreOptions.Clear();

        var gameNiches = new[]
        {
            (ProductNiche.RPG, "RPG", "Deep character progression and storytelling", "Story-driven fans", "Progression, Dialogue, World", "Writing, Game Design", "Medium"),
            (ProductNiche.FPS, "FPS", "First-person shooting action", "Action gamers", "Combat, Weapons, Levels", "Programming, Level Design", "High"),
            (ProductNiche.Strategy, "Strategy", "Tactical and strategic gameplay", "Strategy enthusiasts", "AI, Economy, Map", "AI Programming, Game Design", "Medium"),
            (ProductNiche.Puzzle, "Puzzle", "Logic and problem-solving challenges", "Casual & hardcore", "Logic Levels, Progression", "Level Design, Programming", "Low"),
            (ProductNiche.Platformer, "Platformer", "Jump and run through levels", "All ages", "Movement, Levels, Controls", "Programming, Level Design", "Low"),
            (ProductNiche.Simulation, "Simulation", "Simulate real-world systems", "Hobbyists, fans", "Systems, Physics, UI", "Systems Architecture, Programming", "Medium"),
            (ProductNiche.Racing, "Racing", "High-speed vehicle competition", "Speed/sport fans", "Physics, Vehicles, Tracks", "Physics Programming, Art", "Medium"),
            (ProductNiche.Sports, "Sports", "Athletic and sports competition", "Sports fans", "Simulation, Multiplayer", "AI, Networking", "Medium"),
            (ProductNiche.Horror, "Horror", "Fear-driven experiences", "Horror fans", "Atmosphere, AI, Sound", "Audio, Game Design", "High"),
            (ProductNiche.Adventure, "Adventure", "Exploration and narrative", "Story fans", "World, Dialogue, Exploration", "Writing, Level Design", "Low"),
            (ProductNiche.MMORPG, "MMORPG", "Massive multiplayer online worlds", "MMO fans", "Networking, Economy, World", "Backend, Networking, Game Design", "Very High"),
            (ProductNiche.Sandbox, "Sandbox", "Open-ended creative play", "Creative players", "World Gen, Tools, Survival", "Procedural, Programming", "Medium"),
            (ProductNiche.Fighting, "Fighting", "Competitive combat", "Competitive gamers", "Combat, Balance, Multiplayer", "Networking, Game Design", "Medium")
        };

        for (int i = 0; i < gameNiches.Length; i++)
        {
            var (niche, name, desc, audience, coreFeatures, skills, risk) = gameNiches[i];
            GenreOptions.Add(new GenreOption
            {
                Niche = niche,
                DisplayName = name,
                Description = desc,
                Audience = audience,
                CoreFeatures = coreFeatures,
                RelevantSkills = skills,
                RiskProfile = risk
            });
        }

        BuildFormatOptions();
    }

    private void BuildFormatOptions()
    {
        FormatOptions.Clear();
        FormatOptions.Add(new FormatOption { DisplayName = "Text-Based", ScopeImpact = "Very Low", Description = "Minimal graphics, narrative-driven" });
        FormatOptions.Add(new FormatOption { DisplayName = "2D", ScopeImpact = "Low", Description = "Traditional 2D graphics" });
        FormatOptions.Add(new FormatOption { DisplayName = "Isometric", ScopeImpact = "Medium", Description = "2.5D perspective view" });
        FormatOptions.Add(new FormatOption { DisplayName = "3D", ScopeImpact = "High", Description = "Full 3D graphics" });
        FormatOptions.Add(new FormatOption { DisplayName = "Open World", ScopeImpact = "Very High", Description = "Large-scale open environments" });
    }

    // ── Budget Step Methods ─────────────────────────────────────────────────

    public static string[] GetDistributionModelLabels()
    {
        return DistributionModelLabels;
    }

    public void SetProductName(string name)
    {
        Draft.ProductName = name ?? "";
        ValidateName();
        RecalculateBudgetPreview();
    }

    public void SetTargetPrice(int price)
    {
        TargetPrice = Mathf.Max(1, price);
        Draft.Budget.PricePerUnit = TargetPrice;
        RecalculatePricingPreview();
        RecalculateBudgetPreview();
    }

    public void SetDistributionModel(int index)
    {
        SelectedDistributionModel = (ToolDistributionModel)Mathf.Clamp(index, 0, 2);
        Draft.Budget.IsSubscriptionModel = SelectedDistributionModel == ToolDistributionModel.Licensed;
        RecalculateBudgetPreview();
        RecalculateValidation();
    }

    public void SetMarketingBudget(int level)
    {
        MarketingBudgetLevel = Mathf.Clamp(level, 0, 2);
        Draft.Budget.MarketingBudget = level == 0 ? 5_000 : level == 1 ? 15_000 : 30_000;
        RecalculateBudgetPreview();
    }

    private void ValidateName()
    {
        string name = Draft.ProductName ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            IsNameValid = false;
            NameError = "Product name is required.";
        }
        else if (_lastState != null && IsNameDuplicate(name))
        {
            IsNameValid = false;
            NameError = "A product with this name already exists.";
        }
        else
        {
            IsNameValid = true;
            NameError = "";
        }

        RecalculateValidation();
    }

    private bool IsNameDuplicate(string name)
    {
        if (_lastState == null) return false;

        var shipped = _lastState.ShippedProducts;
        if (shipped != null)
        {
            foreach (var kvp in shipped)
            {
                if (kvp.Value != null && string.Equals(kvp.Value.ProductName, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        var inDev = _lastState.DevelopmentProducts;
        if (inDev != null)
        {
            foreach (var kvp in inDev)
            {
                if (kvp.Value != null && string.Equals(kvp.Value.ProductName, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private void RecalculatePricingPreview()
    {
        ProductTemplateDefinition template = GetSelectedTemplate();
        int baseExpectation = template != null ? Mathf.Max(5, template.baseUpfrontCost / 5000) : 30;

        MarketExpectation = $"${baseExpectation}";

        float ratio = baseExpectation > 0 ? (float)TargetPrice / baseExpectation : 1f;
        if (ratio > 1.5f)
            ValueRisk = "High — overpriced vs. market";
        else if (ratio > 1.2f)
            ValueRisk = "Medium — above market";
        else if (ratio < 0.6f)
            ValueRisk = "Low — underpriced";
        else
            ValueRisk = "Low — within range";

        CompetitorPriceComparison = $"Market avg: ${baseExpectation}";
    }

    private void RecalculateBudgetPreview()
    {
        ProductTemplateDefinition template = GetSelectedTemplate();
        int baseCost = template != null ? template.baseUpfrontCost : 0;

        int platformMult = SelectedPlatformIndices.Count > 1
            ? (int)(baseCost * PlatformScopeMultiplierPerExtra * (SelectedPlatformIndices.Count - 1))
            : 0;

        int mktCost = Draft.Budget.MarketingBudget;
        int hwCost = IsHardwareProduct ? HwDevCostAdd : 0;
        int upfront = baseCost + platformMult + FeatureScopeTotal + hwCost;

        UpfrontCostPreview = $"${upfront:N0}";

        int totalSalaryPerMonth = 0;
        if (_lastState != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (TeamSlots[i].AssignedTeamId.HasValue)
                    totalSalaryPerMonth += _lastState.TotalSalaryCost / Mathf.Max(1, _lastState.ActiveTeams.Count);
            }
        }
        int monthlyBurn = totalSalaryPerMonth + mktCost / 6;
        MonthlyBurnPreview = monthlyBurn > 0 ? $"${monthlyBurn:N0}/mo" : "--";

        float totalWorkUnits = 0f;
        if (template != null && template.phases != null)
        {
            for (int i = 0; i < template.phases.Length; i++)
                if (template.phases[i] != null) totalWorkUnits += template.phases[i].baseWorkUnits;
        }

        float featureScale = 1f + FeatureSelectedCount * 0.1f;
        int estimatedMonths = Mathf.Max(1, Mathf.RoundToInt(totalWorkUnits / 10f * featureScale / 30f));
        int totalEst = upfront + monthlyBurn * estimatedMonths + mktCost;
        TotalEstimatedCostPreview = $"${totalEst:N0}";

        int currentCash = _lastState?.Money ?? 0;
        int runwayAfter = currentCash - upfront;
        if (runwayAfter > 0 && monthlyBurn > 0)
        {
            int runwayMonths = runwayAfter / Mathf.Max(1, monthlyBurn);
            RunwayAfterStartPreview = runwayAfter >= 0 ? $"${runwayAfter:N0} (~{runwayMonths} mo)" : "Insufficient funds";
        }
        else
        {
            RunwayAfterStartPreview = runwayAfter >= 0 ? $"${runwayAfter:N0}" : "Insufficient funds";
        }

        if (TargetPrice > 0 && monthlyBurn > 0)
        {
            int minUnitsSold = totalEst / Mathf.Max(1, TargetPrice);
            int breakEvenMonths = estimatedMonths + (minUnitsSold > 1000 ? 6 : 3);
            BreakEvenEstimatePreview = $"~{breakEvenMonths} months (medium confidence)";
        }
        else
        {
            BreakEvenEstimatePreview = "--";
        }

        RecalculateValidation();
    }

    private void RecalculateValidation()
    {
        BlockingErrors.Clear();
        ValidationWarnings.Clear();

        if (string.IsNullOrWhiteSpace(Draft.ProductName))
            BlockingErrors.Add("Product name is required — enter a name in Step 9.");
        else if (!IsNameValid)
            BlockingErrors.Add(NameError);

        if (SelectedTypeIndex < 0)
            BlockingErrors.Add("No product type selected — complete Step 1.");

        if (SelectedCategoryIndex < 0)
            BlockingErrors.Add("No product category selected — complete Step 2.");

        if (SelectedPlatformIndices.Count == 0)
            BlockingErrors.Add("At least one platform must be selected — complete Step 5.");

        if (BugRisk == "High")
            ValidationWarnings.Add("High bug risk — assign a QA team or reduce scope to lower defect probability.");

        if (!string.IsNullOrEmpty(FeatureMissingExpected))
            ValidationWarnings.Add(FeatureMissingExpected);

        int assignedTeams = 0;
        for (int i = 0; i < 4; i++)
            if (TeamSlots[i].AssignedTeamId.HasValue) assignedTeams++;

        if (assignedTeams == 0)
            ValidationWarnings.Add("No teams assigned — development will use unmanaged defaults. Assign teams in Step 8 for better quality.");

        if (ScopeRisk == "High")
            ValidationWarnings.Add("High scope risk — reduce selected features or extend planned duration.");

        if (TechnicalRisk == "High")
            ValidationWarnings.Add("High technical risk — assign an Engineering team or reduce product complexity.");

        if (CommercialRisk == "High")
            ValidationWarnings.Add("High commercial risk — target a less saturated niche or reduce platform count.");

        int currentCash = _lastState?.Money ?? 0;
        ProductTemplateDefinition template = GetSelectedTemplate();
        int upfrontNeeded = template != null ? template.baseUpfrontCost + FeatureScopeTotal : 0;
        if (currentCash > 0 && upfrontNeeded > currentCash)
            BlockingErrors.Add($"Insufficient funds — need ${upfrontNeeded:N0} but have ${currentCash:N0}. Reduce scope or secure a loan first.");

        if (SelectedPlatformIndices.Count > 2)
            ValidationWarnings.Add($"Targeting {SelectedPlatformIndices.Count} platforms adds significant scope. Consider reducing to 1–2 platforms.");

        CanConfirm = BlockingErrors.Count == 0;
    }

    // ── Command Generation ──────────────────────────────────────────────────

    public CreateProductCommand BuildCreateCommand(int currentTick)
    {
        ProductTemplateDefinition template = GetSelectedTemplate();
        string templateId = template?.templateId ?? "";

        var selectedFeatureIds = new string[Draft.SelectedFeatureIds.Count];
        for (int i = 0; i < Draft.SelectedFeatureIds.Count; i++)
        {
            int idx = Draft.SelectedFeatureIds[i];
            if (idx >= 0 && idx < AllFeatures.Count)
                selectedFeatureIds[i] = AllFeatures[idx].FeatureId;
        }

        var platformIds = new ProductId[SelectedPlatformIndices.Count];
        for (int i = 0; i < SelectedPlatformIndices.Count; i++)
        {
            int idx = SelectedPlatformIndices[i];
            if (idx >= 0 && idx < PlatformOptions.Count)
                platformIds[i] = new ProductId(PlatformOptions[idx].PlatformIdValue);
        }

        var teamAssignments = new List<TeamAssignment>();
        for (int i = 0; i < 4; i++)
        {
            var slot = TeamSlots[i];
            if (!slot.AssignedTeamId.HasValue) continue;

            ProductTeamRole role = i == 0 ? ProductTeamRole.Development
                : i == 1 ? ProductTeamRole.Design
                : i == 2 ? ProductTeamRole.QA
                : ProductTeamRole.Marketing;

            teamAssignments.Add(new TeamAssignment { Role = role, TeamId = slot.AssignedTeamId.Value });
        }

        ProductNiche selectedNiche = ProductNiche.None;
        if (SelectedNicheIndex >= 0 && SelectedNicheIndex < NicheOptions.Count)
            selectedNiche = NicheOptions[SelectedNicheIndex].Niche;

        return new CreateProductCommand
        {
            Tick = currentTick,
            TemplateId = templateId,
            ProductName = Draft.ProductName,
            SelectedFeatureIds = selectedFeatureIds,
            IsSubscriptionBased = SelectedDistributionModel == ToolDistributionModel.Licensed,
            Price = TargetPrice,
            TargetPlatformIds = platformIds,
            RequiredToolIds = new ProductId[0],
            Stance = GenerationStance.Standard,
            PredecessorProductId = null,
            InitialTeamAssignments = teamAssignments.ToArray(),
            SequelOfId = null,
            HasHardwareConfig = IsHardwareProduct,
            HardwareConfig = IsHardwareProduct ? Draft.HardwareConfig : default,
            TargetDay = 0,
            DistributionModel = SelectedDistributionModel,
            LicensingRate = 0f,
            MonthlySubscriptionPrice = SelectedDistributionModel == ToolDistributionModel.Licensed ? Draft.Budget.MonthlySubscriptionPrice : 0f,
            SelectedNiche = selectedNiche
        };
    }
}

// ── Data Structures ─────────────────────────────────────────────────────────

public struct ProductTypeOption
{
    public ProductCategory Category;
    public string DisplayName;
    public string Description;
    public string SkillNeeds;
    public string TeamNeeds;
    public string MarketRisk;
    public string RecommendedStage;
    public ProductLayer Layer;
    public int TemplateCount;
    public int AvgDifficulty;
    public int AvgCost;
}

public struct CategoryOption
{
    public string TemplateId;
    public string DisplayName;
    public string Description;
    public int DifficultyTier;
    public int BaseCost;
    public int PhaseCount;
    public int FeatureCount;
    public int GenerationRequirement;
    public bool IsLocked;
    public string LockedReason;
}

public struct NicheOption
{
    public ProductNiche Niche;
    public string DisplayName;
    public float DemandPercent;
    public string DemandText;
    public string TrendText;
    public MarketTrend Trend;
    public string CompetitionText;
    public float SaturationPercent;
    public string RiskLabel;
    public string ProjectedGrowth;
}

public struct GenreOption
{
    public ProductNiche Niche;
    public string DisplayName;
    public string Description;
    public string Audience;
    public string CoreFeatures;
    public string RelevantSkills;
    public string RiskProfile;
}

public struct FormatOption
{
    public string DisplayName;
    public string ScopeImpact;
    public string Description;
}

public struct PlatformOption
{
    public int PlatformIdValue;
    public string DisplayName;
    public string OwnerText;
    public bool IsOwnPlatform;
    public float MarketSharePercent;
    public int InstallBase;
    public float QualityCeiling;
    public float LicensingRate;
}

public struct DiagnosticCard
{
    public string Title;
    public string Description;
    public DiagnosticSeverity Severity;
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Success
}

public struct FeatureOption
{
    public string FeatureId;
    public string DisplayName;
    public string Description;
    public string DemandLabel;
    public FeatureDemandStage DemandStage;
    public int ScopeCost;
    public SkillId? RequiredSkill;
    public int RequiredSkillPoints;
    public int GenerationRequirement;
    public string[] SynergyTags;
    public string[] RiskTags;
    public string ReviewImpact;
    public bool IsLocked;
    public string LockReason;
    public bool IsSelected;
    public string CategoryName;
}

public struct ProductSlotData
{
    public string SlotName;
    public SkillId[] RequiredSkills;
    public TeamId? AssignedTeamId;
    public string AssignedTeamName;
    public int SkillMatch;
    public int MoraleValue;
    public int ChemistryValue;
    public int EnergyValue;
    public string ContributionEstimate;
    public string[] Warnings;
    public string[] MissingSkillNames;
}

public struct TeamOption
{
    public TeamId Id;
    public string Name;
    public int MemberCount;
    public string StatusText;
    public bool IsAvailable;
}

public struct TeamSuggestion
{
    public string SlotName;
    public TeamId TeamId;
    public string TeamName;
    public int MatchScore;
}