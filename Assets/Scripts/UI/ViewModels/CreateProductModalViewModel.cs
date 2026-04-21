using System;
using System.Collections.Generic;

#if false
public class CreateProductModalViewModel : IViewModel
{
    // ── Wizard Step ─────────────────────────────────────────────────────────
    public enum WizardStep { ProductType, PlatformTarget, HardwareDesign, ToolSelection, GenreNiche, Features, PricingName, TeamAssignment }

    public WizardStep CurrentStep { get; private set; } = WizardStep.ProductType;
    public int StepIndex => (int)CurrentStep;

    private int _cachedTotalSteps;
    private bool _pricingDirty;

    public int TotalSteps
    {
        get
        {
            if (_isUpdateMode) return GetUpdateTotalSteps();
            return _cachedTotalSteps;
        }
    }

    public bool CanGoBack
    {
        get
        {
            if (_isUpdateMode) return CurrentStep != WizardStep.ProductType;
            var stepList = _isSequelMode ? BuildSequelStepList() : BuildNewProductStepList();
            return stepList.Count > 0 && CurrentStep != stepList[0];
        }
    }

    public bool CanGoNext
    {
        get
        {
            switch (CurrentStep)
            {
                case WizardStep.ProductType:
                    if (_isUpdateMode) return _updateTypeSet;
                    return !string.IsNullOrEmpty(SelectedTemplateId);
                case WizardStep.PlatformTarget:
                    return _selectedPlatformIds.Count > 0;
                case WizardStep.HardwareDesign:
                    return true;
                case WizardStep.ToolSelection:
                    if (!_templateHasRequiredTool) return true;
                    if (_selectedTemplate?.requiredToolTypes == null) return false;
                    for (int i = 0; i < _selectedTemplate.requiredToolTypes.Length; i++)
                    {
                        var cat = _selectedTemplate.requiredToolTypes[i];
                        if (!_selectedTools.ContainsKey(cat)) return false;
                        bool hasOption = false;
                        for (int j = 0; j < _availableRequiredTools.Count; j++)
                        {
                            if (_availableRequiredTools[j].Category == cat) { hasOption = true; break; }
                        }
                        if (!hasOption) return false;
                    }
                    return true;
                case WizardStep.GenreNiche:
                    return true;
                case WizardStep.Features:
                    if (_isUpdateMode && _selectedUpdateType == ProductUpdateType.AddFeatures)
                    {
                        for (int i = 0; i < _features.Count; i++)
                            if (_features[i].IsSelected && !_features[i].IsPreSelected) return true;
                        return false;
                    }
                    return SelectedFeatureCount > 0;
                case WizardStep.PricingName:
                    return !string.IsNullOrEmpty(ProductName) && Price > 0f;
                case WizardStep.TeamAssignment:
                    return AllRolesAssigned;
                default:
                    return false;
            }
        }
    }

    public event Action OnStepChanged;
    public event Action OnDismiss;

    private bool TemplateNeedsPlatformStep()
    {
        if (_selectedTemplate == null) return false;
        if (_selectedTemplate.category.IsTool()) return false;
        if (_selectedTemplate.validTargetPlatforms == null) return false;
        return _selectedTemplate.validTargetPlatforms.Length > 0;
    }

    private List<WizardStep> BuildNewProductStepList()
    {
        var steps = new List<WizardStep>();
        steps.Add(WizardStep.ProductType);
        if (TemplateNeedsPlatformStep())
            steps.Add(WizardStep.PlatformTarget);
        if (_selectedTemplate != null && _selectedTemplate.templateId == "game_console")
            steps.Add(WizardStep.HardwareDesign);
        if (_templateHasTools)
            steps.Add(WizardStep.ToolSelection);
        if (_selectedTemplate != null && _selectedTemplate.HasNiches)
            steps.Add(WizardStep.GenreNiche);
        steps.Add(WizardStep.Features);
        steps.Add(WizardStep.PricingName);
        steps.Add(WizardStep.TeamAssignment);
        return steps;
    }

    private List<WizardStep> BuildSequelStepList()
    {
        var steps = new List<WizardStep>();
        if (TemplateNeedsPlatformStep())
            steps.Add(WizardStep.PlatformTarget);
        if (_templateHasTools)
            steps.Add(WizardStep.ToolSelection);
        if (_selectedTemplate != null && _selectedTemplate.HasNiches)
            steps.Add(WizardStep.GenreNiche);
        steps.Add(WizardStep.Features);
        steps.Add(WizardStep.PricingName);
        steps.Add(WizardStep.TeamAssignment);
        return steps;
    }

    public void GoNext()
    {
        if (_isUpdateMode) { GoNextUpdateMode(); return; }
        var stepList = _isSequelMode ? BuildSequelStepList() : BuildNewProductStepList();
        int idx = stepList.IndexOf(CurrentStep);
        if (idx < 0 || idx >= stepList.Count - 1) return;
        CurrentStep = stepList[idx + 1];
        OnStepChanged?.Invoke();
    }

    public void GoBack()
    {
        if (_isUpdateMode) { GoBackUpdateMode(); return; }
        var stepList = _isSequelMode ? BuildSequelStepList() : BuildNewProductStepList();
        int idx = stepList.IndexOf(CurrentStep);
        if (idx <= 0) return;
        CurrentStep = stepList[idx - 1];
        OnStepChanged?.Invoke();
    }

    private void GoNextUpdateMode()
    {
        if (CurrentStep == WizardStep.ProductType)
        {
            CurrentStep = _selectedUpdateType == ProductUpdateType.BugFix ? WizardStep.TeamAssignment : WizardStep.Features;
            OnStepChanged?.Invoke();
            return;
        }
        if (CurrentStep == WizardStep.Features)
        {
            CurrentStep = WizardStep.TeamAssignment;
            OnStepChanged?.Invoke();
        }
    }

    private void GoBackUpdateMode()
    {
        if (CurrentStep == WizardStep.TeamAssignment)
        {
            CurrentStep = _selectedUpdateType == ProductUpdateType.BugFix ? WizardStep.ProductType : WizardStep.Features;
            OnStepChanged?.Invoke();
            return;
        }
        if (CurrentStep == WizardStep.Features)
        {
            CurrentStep = WizardStep.ProductType;
            OnStepChanged?.Invoke();
        }
    }

    private int GetUpdateTotalSteps()
    {
        if (_selectedUpdateType == ProductUpdateType.BugFix) return 2;
        return 3;
    }

    public int GetCurrentStepDisplay()
    {
        if (_isUpdateMode)
        {
            switch (CurrentStep)
            {
                case WizardStep.ProductType:    return 1;
                case WizardStep.Features:       return 2;
                case WizardStep.TeamAssignment:
                    return _selectedUpdateType == ProductUpdateType.BugFix ? 2 : 3;
                default: return 1;
            }
        }
        var stepList = _isSequelMode ? BuildSequelStepList() : BuildNewProductStepList();
        int idx = stepList.IndexOf(CurrentStep);
        return idx >= 0 ? idx + 1 : 1;
    }

    // ── Step 1 — Product Type ────────────────────────────────────────────────
    private readonly List<ProductTemplateDisplay> _templates = new List<ProductTemplateDisplay>();
    public List<ProductTemplateDisplay> Templates => _templates;
    public string SelectedTemplateId { get; private set; }

    // ── Step 2 — Platform Target ─────────────────────────────────────────────
    private readonly List<PlatformTargetDisplay> _availablePlatforms = new List<PlatformTargetDisplay>();
    public List<PlatformTargetDisplay> AvailablePlatforms => _availablePlatforms;

    private readonly List<ProductId> _selectedPlatformIds = new List<ProductId>();
    public List<ProductId> SelectedPlatformIds => _selectedPlatformIds;

    public bool IsMultiPlatformWarningVisible => _selectedPlatformIds.Count > 1;
    public string MultiPlatformWarningLabel => _selectedPlatformIds.Count > 1
        ? "1.3x dev cost per additional platform selected"
        : "";

    public void TogglePlatform(ProductId platformId, bool selected)
    {
        if (selected)
        {
            bool found = false;
            for (int i = 0; i < _selectedPlatformIds.Count; i++)
                if (_selectedPlatformIds[i] == platformId) { found = true; break; }
            if (!found) _selectedPlatformIds.Add(platformId);
        }
        else
        {
            for (int i = 0; i < _selectedPlatformIds.Count; i++)
            {
                if (_selectedPlatformIds[i] == platformId)
                {
                    _selectedPlatformIds.RemoveAt(i);
                    break;
                }
            }
        }
        RecalculateCost();
        RefreshCrossProductGates();
    }

    public bool IsPlatformSelected(ProductId platformId)
    {
        for (int i = 0; i < _selectedPlatformIds.Count; i++)
            if (_selectedPlatformIds[i] == platformId) return true;
        return false;
    }

    // ── Step 2.5 — Hardware Design (console only) ────────────────────────────
    private HardwareConfiguration _hardwareConfig = new HardwareConfiguration {
        processingTier = HardwareTier.Budget,
        graphicsTier   = HardwareTier.Budget,
        memoryTier     = HardwareTier.Budget,
        storageTier    = HardwareTier.Budget,
        formFactor     = ConsoleFormFactor.Standard
    };
    public HardwareConfiguration HardwareConfig => _hardwareConfig;

    private HardwareGenerationConfig _currentGenConfig;
    public HardwareGenerationConfig CurrentHardwareGenConfig => _currentGenConfig;

    public void SetHardwareGenConfig(HardwareGenerationConfig config) {
        _currentGenConfig = config;
    }

    public void SetProcessingTier(HardwareTier tier) {
        _hardwareConfig.processingTier = tier;
        RefreshHardwareDerivedState();
        OnStepChanged?.Invoke();
    }

    public void SetGraphicsTier(HardwareTier tier) {
        _hardwareConfig.graphicsTier = tier;
        RefreshHardwareDerivedState();
        OnStepChanged?.Invoke();
    }

    public void SetMemoryTier(HardwareTier tier) {
        _hardwareConfig.memoryTier = tier;
        RefreshHardwareDerivedState();
        OnStepChanged?.Invoke();
    }

    public void SetStorageTier(HardwareTier tier) {
        _hardwareConfig.storageTier = tier;
        RefreshHardwareDerivedState();
        OnStepChanged?.Invoke();
    }

    public void SetFormFactor(ConsoleFormFactor formFactor) {
        _hardwareConfig.formFactor = formFactor;
        RefreshHardwareDerivedState();
        OnStepChanged?.Invoke();
    }

    private void RefreshHardwareDerivedState() {
        if (_currentGenConfig != null)
            _hardwareConfig.manufactureCostPerUnit = _currentGenConfig.CalculateManufactureCost(_hardwareConfig);
        RecalculateCost();
        RebuildFilteredList();
    }

    public int ManufactureCostPerUnit => _currentGenConfig != null
        ? _currentGenConfig.CalculateManufactureCost(_hardwareConfig)
        : 0;

    public int HardwareDevCostAdd => _currentGenConfig != null
        ? _currentGenConfig.CalculateDevCostAdd(_hardwareConfig)
        : 0;

    public int AvailableFeatureCount {
        get {
            if (_selectedTemplate == null || _selectedTemplate.availableFeatures == null) return 0;
            int count = 0;
            for (int i = 0; i < _selectedTemplate.availableFeatures.Length; i++) {
                var feat = _selectedTemplate.availableFeatures[i];
                if (feat != null && IsFeatureUnlockedByHardware(feat)) count++;
            }
            return count;
        }
    }

    public int LockedFeatureCount {
        get {
            if (_selectedTemplate == null || _selectedTemplate.availableFeatures == null) return 0;
            int count = 0;
            for (int i = 0; i < _selectedTemplate.availableFeatures.Length; i++) {
                var feat = _selectedTemplate.availableFeatures[i];
                if (feat != null && !IsFeatureUnlockedByHardware(feat)) count++;
            }
            return count;
        }
    }

    public string[] LockedFeatureNames {
        get {
            if (_selectedTemplate == null || _selectedTemplate.availableFeatures == null) return new string[0];
            var result = new List<string>();
            for (int i = 0; i < _selectedTemplate.availableFeatures.Length; i++) {
                var feat = _selectedTemplate.availableFeatures[i];
                if (feat != null && !IsFeatureUnlockedByHardware(feat))
                    result.Add(feat.displayName ?? feat.featureId);
            }
            return result.ToArray();
        }
    }

    private bool IsFeatureUnlockedByHardware(ProductFeatureDefinition feat) {
        if (feat == null) return true;
        if (feat.minimumHardwareTier >= 0 && feat.constrainedByHardware >= 0) {
            var component = (HardwareComponent)feat.constrainedByHardware;
            var minTier = (HardwareTier)feat.minimumHardwareTier;
            HardwareTier selectedTier = GetHardwareTierForComponent(component);
            if (selectedTier < minTier) return false;
        }
        if (feat.formFactorRequired >= 0) {
            var required = (ConsoleFormFactor)feat.formFactorRequired;
            if (required == ConsoleFormFactor.Portable && _hardwareConfig.formFactor == ConsoleFormFactor.Hybrid) return true;
            if (_hardwareConfig.formFactor != required) return false;
        }
        return true;
    }

    private HardwareTier GetHardwareTierForComponent(HardwareComponent component) {
        switch (component) {
            case HardwareComponent.Processing: return _hardwareConfig.processingTier;
            case HardwareComponent.Graphics:   return _hardwareConfig.graphicsTier;
            case HardwareComponent.Memory:     return _hardwareConfig.memoryTier;
            case HardwareComponent.Storage:    return _hardwareConfig.storageTier;
            default:                           return _hardwareConfig.processingTier;
        }
    }

    public bool IsConsoleTemplate => _selectedTemplate != null && _selectedTemplate.templateId == "game_console";

    // ── Step 3 — Tool Selection ───────────────────────────────────────────────
    private readonly List<ToolSelectionDisplay> _availableRequiredTools = new List<ToolSelectionDisplay>();
    public List<ToolSelectionDisplay> AvailableRequiredTools => _availableRequiredTools;

    private readonly List<ProductCategory> _requiredToolCategories = new List<ProductCategory>();
    public List<ProductCategory> RequiredToolCategories => _requiredToolCategories;

    private readonly Dictionary<ProductCategory, ProductId> _selectedTools = new Dictionary<ProductCategory, ProductId>();

    private bool _templateHasTools;
    private bool _templateHasRequiredTool;

    public bool HasRequiredTools => _templateHasRequiredTool;

    public string QualityCeilingLabel
    {
        get
        {
            if (_selectedTools.Count == 0) return "No tools selected";
            if (_selectedTemplate == null) return "";

            float totalLift = 0f;
            foreach (var kvp in _selectedTools)
            {
                if (_lastState?.ShippedProducts == null) continue;
                if (!_lastState.ShippedProducts.TryGetValue(kvp.Value, out var tool)) continue;
                float toolQualityFactor = tool.OverallQuality / 100f;
                bool isOwn = !tool.IsCompetitorProduct;
                float bonus = isOwn ? _selectedTemplate.ownToolQualityBonus : _selectedTemplate.licensedToolQualityBonus;
                totalLift += bonus * toolQualityFactor;
            }

            float baseCeiling = 75f;
            float estimatedCeiling = Math.Min(100f, baseCeiling + totalLift * 100f);
            return "Est. quality ceiling: ~" + ((int)estimatedCeiling) + "%";
        }
    }

    public void SetToolSelection(ProductCategory category, ProductId toolId)
    {
        _selectedTools[category] = toolId;
        RecalculateCost();
        RefreshCrossProductGates();
    }

    public void ClearToolSelection(ProductCategory category)
    {
        _selectedTools.Remove(category);
        RecalculateCost();
        RefreshCrossProductGates();
    }

    public bool IsToolSelected(ProductCategory category, ProductId toolId)
    {
        return _selectedTools.TryGetValue(category, out var id) && id == toolId;
    }

    public ProductId? GetSelectedTool(ProductCategory category)
    {
        return _selectedTools.TryGetValue(category, out var id) ? (ProductId?)id : null;
    }

    // ── Step 4 — Genre / Niche ───────────────────────────────────────────────
    private readonly List<NicheOptionDisplay> _availableNiches = new List<NicheOptionDisplay>();
    public List<NicheOptionDisplay> AvailableNiches => _availableNiches;

    private ProductNiche? _selectedNiche;
    public ProductNiche? SelectedNiche => _selectedNiche;

    public void SelectNiche(ProductNiche niche) { _selectedNiche = niche; }

    // ── Step 6 — Pricing & Name / Generation ────────────────────────────────
    private GenerationStance _selectedStance = GenerationStance.Standard;
    public GenerationStance SelectedStance => _selectedStance;

    private ProductId? _selectedPredecessorId;
    public ProductId? SelectedPredecessorId => _selectedPredecessorId;

    public void SetGenerationStance(GenerationStance stance) { _selectedStance = stance; RecalculateCost(); }
    public void SetPredecessor(ProductId? predecessorId) { _selectedPredecessorId = predecessorId; }

    // ── Step 6 — Distribution Model (Tools & Platforms only) ─────────────────
    public bool CanSetDistribution { get; private set; }
    public ToolDistributionModel SelectedDistribution { get; private set; }
    public float SelectedLicensingRate { get; private set; }

    // ── Step 6 — Tool Subscription Pricing (Licensed tools only) ─────────────
    public bool ShowSubscriptionPricing { get; private set; }
    public bool ShowRoyaltyRate { get; private set; }
    public float SubscriptionPrice { get; private set; } = 20f;
    public string SubscriptionPriceLabel { get; private set; } = "$20/month";

    public void SetDistributionModel(ToolDistributionModel model) {
        SelectedDistribution = model;
        if (model != ToolDistributionModel.Licensed) SelectedLicensingRate = 0f;
        RefreshToolSubscriptionVisibility();
    }

    private void RefreshToolSubscriptionVisibility() {
        bool isLicensedTool = CanSetDistribution
            && _selectedTemplate != null
            && _selectedTemplate.layer == ProductLayer.Tool
            && SelectedDistribution == ToolDistributionModel.Licensed;
        ShowSubscriptionPricing = isLicensedTool;
        ShowRoyaltyRate = isLicensedTool;
    }

    public void SetSubscriptionPrice(float price) {
        price = System.Math.Max(5f, System.Math.Min(100f, price));
        float snapped = (float)System.Math.Round(price / 5f) * 5f;
        SubscriptionPrice = snapped;
        SubscriptionPriceLabel = "$" + ((int)snapped) + "/month";
    }

    public void SetLicensingRate(float rate) {
        SelectedLicensingRate = Math.Max(0.05f, Math.Min(0.30f, rate));
    }

    // ── Update Mode ──────────────────────────────────────────────────────────
    private bool _isUpdateMode;
    private ProductId _updateProductId;
    private ProductUpdateType _selectedUpdateType;
    private bool _updateTypeSet;
    public bool IsUpdateMode => _isUpdateMode;
    public ProductId UpdateProductId => _updateProductId;
    public ProductUpdateType SelectedUpdateType => _selectedUpdateType;

    // ── Sequel Mode ──────────────────────────────────────────────────────────
    private bool _isSequelMode;
    private ProductId _sequelOfId;
    private Product _originalProduct;
    public bool IsSequelMode => _isSequelMode;
    public ProductId SequelOfId => _sequelOfId;
    public string OriginalProductName => _originalProduct?.ProductName ?? "";

    public void InitAsUpdate(Product product, ProductTemplateDefinition[] definitions)
    {
        _isUpdateMode = true;
        _updateProductId = product.Id;
        _updateTypeSet = false;

        SelectedTemplateId = product.TemplateId;
        _selectedTemplate = null;
        if (definitions != null)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].templateId == product.TemplateId)
                {
                    _selectedTemplate = definitions[i];
                    break;
                }
            }
        }

        _features.Clear();
        _featureDataCache.Clear();
        _requiredRoles.Clear();
        _optionalRoles.Clear();
        _teamAssignments.Clear();

        if (_selectedTemplate != null && _selectedTemplate.availableFeatures != null)
            PopulateFeaturesForUpdate(_selectedTemplate.availableFeatures, product.SelectedFeatureIds);

        if (_selectedTemplate != null && _selectedTemplate.phases != null)
        {
            for (int ph = 0; ph < _selectedTemplate.phases.Length; ph++)
            {
                var role = _selectedTemplate.phases[ph].primaryRole;
                if (!_requiredRoles.Contains(role))
                    _requiredRoles.Add(role);
            }
        }
        if (!_requiredRoles.Contains(ProductTeamRole.Marketing))
            _optionalRoles.Add(ProductTeamRole.Marketing);

        CurrentStep = WizardStep.ProductType;
    }

    public void InitAsSequel(Product original, ProductTemplateDefinition[] definitions)
    {
        _isSequelMode = true;
        _sequelOfId = original.Id;
        _originalProduct = original;
        _isUpdateMode = false;

        SelectedTemplateId = original.TemplateId;
        _selectedTemplate = null;

        if (definitions != null)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].templateId == original.TemplateId)
                {
                    _selectedTemplate = definitions[i];
                    break;
                }
            }
        }

        _features.Clear();
        _featureDataCache.Clear();
        _requiredRoles.Clear();
        _optionalRoles.Clear();
        _teamAssignments.Clear();
        _selectedPlatformIds.Clear();
        _selectedTools.Clear();
        _availablePlatforms.Clear();
        _availableRequiredTools.Clear();
        _requiredToolCategories.Clear();
        _availableNiches.Clear();
        _selectedNiche = null;

        if (_selectedTemplate != null)
        {
            _baseTemplateCost = _selectedTemplate.baseUpfrontCost;
            _templateHasTools = _selectedTemplate.requiredToolTypes != null && _selectedTemplate.requiredToolTypes.Length > 0;
            _templateHasRequiredTool = _selectedTemplate.requiredToolTypes != null && _selectedTemplate.requiredToolTypes.Length > 0;

            if (_selectedTemplate.availableFeatures != null)
                PopulateFeatures(_selectedTemplate.availableFeatures, null);

            if (original.SelectedFeatureIds != null && _selectedTemplate.availableFeatures != null)
            {
                var originalFeatureSet = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < original.SelectedFeatureIds.Length; i++)
                    originalFeatureSet.Add(original.SelectedFeatureIds[i]);

                for (int i = 0; i < _features.Count; i++)
                {
                    if (!originalFeatureSet.Contains(_features[i].FeatureId)) continue;
                    var feat = _features[i];
                    feat.IsSelected = true;
                    _features[i] = feat;
                }
            }

            if (_selectedTemplate.phases != null)
            {
                for (int ph = 0; ph < _selectedTemplate.phases.Length; ph++)
                {
                    var role = _selectedTemplate.phases[ph].primaryRole;
                    if (!_requiredRoles.Contains(role)) _requiredRoles.Add(role);
                }
            }

            if (!_requiredRoles.Contains(ProductTeamRole.Marketing))
                _optionalRoles.Add(ProductTeamRole.Marketing);

            if (_selectedTemplate.nicheConfigs != null && _selectedTemplate.nicheConfigs.Length > 0)
                _selectedNiche = _selectedTemplate.nicheConfigs[0].niche;

            IsSubscriptionBased = original.IsSubscriptionBased;
            float seqBasePrice = IsSubscriptionBased
                ? _selectedTemplate.economyConfig?.monthlySubscriptionPrice ?? 0f
                : _selectedTemplate.economyConfig?.pricePerUnit ?? 0f;
            Price = original.PriceOverride > 0f ? original.PriceOverride : seqBasePrice;
            float seqFeatureMod = 1f + SelectedFeatureCount * 0.02f;
            SuggestedPriceMin = seqBasePrice * 0.7f * seqFeatureMod;
            SuggestedPriceMax = seqBasePrice * 1.4f * seqFeatureMod;
            MarketAveragePrice = seqBasePrice * seqFeatureMod;
            SweetSpotPrice = seqBasePrice * seqFeatureMod;
            _pricingDirty = false;
            UpdatePriceWarning();

            PopulateNicheOptions();

            CanSetDistribution = _selectedTemplate.layer == ProductLayer.Platform || _selectedTemplate.layer == ProductLayer.Tool;
            SelectedDistribution = ToolDistributionModel.Proprietary;
            SelectedLicensingRate = 0.10f;
            RefreshToolSubscriptionVisibility();
        }

        _selectedPredecessorId = original.Id;
        _selectedStance = GenerationStance.Standard;

        RecalculateCost();
        RebuildFilteredList();
        _cachedTotalSteps = BuildSequelStepList().Count;
        var sequelSteps = BuildSequelStepList();
        CurrentStep = sequelSteps.Count > 0 ? sequelSteps[0] : WizardStep.Features;
    }

    public void SelectUpdateType(ProductUpdateType updateType)
    {
        _selectedUpdateType = updateType;
        _updateTypeSet = true;

        if (updateType == ProductUpdateType.RemoveFeature)
        {
            int count = _features.Count;
            for (int i = 0; i < count; i++)
            {
                var feat = _features[i];
                feat.IsSelected = feat.IsPreSelected;
                _features[i] = feat;
            }
        }
        else if (updateType == ProductUpdateType.AddFeatures)
        {
            int count = _features.Count;
            for (int i = 0; i < count; i++)
            {
                var feat = _features[i];
                feat.IsSelected = feat.IsPreSelected;
                _features[i] = feat;
            }
        }

        RebuildFilteredList();
    }

    private void PopulateFeaturesForUpdate(ProductFeatureDefinition[] featureDefs, string[] existingFeatureIds)
    {
        _features.Clear();
        _featureDataCache.Clear();
        if (featureDefs == null) return;

        var existingSet = new System.Collections.Generic.HashSet<string>();
        if (existingFeatureIds != null)
        {
            for (int i = 0; i < existingFeatureIds.Length; i++)
                existingSet.Add(existingFeatureIds[i]);
        }

        for (int f = 0; f < featureDefs.Length; f++)
        {
            var feat = featureDefs[f];
            if (feat == null) continue;

            bool alreadyOwned = existingSet.Contains(feat.featureId);

            _featureDataCache[feat.featureId] = new FeatureData {
                FeatureId = feat.featureId,
                DisplayName = feat.displayName,
                Description = feat.description,
                AdditionalCost = 0,
                MaintenanceCostPerMonth = 0,
                FeatureCategory = feat.featureCategory,
                SynergyFeatureIds = feat.synergyFeatureIds,
                ConflictFeatureIds = feat.conflictFeatureIds,
                SynergyBonusPercent = feat.synergyBonusPercent,
                ConflictPenaltyPercent = feat.conflictPenaltyPercent
            };

            _features.Add(new FeatureToggleDisplay {
                FeatureId = feat.featureId,
                DisplayName = feat.displayName,
                Description = feat.description,
                FeatureCategory = feat.featureCategory,
                IsSelected = alreadyOwned,
                IsPreSelected = alreadyOwned,
                BaseCost = feat.baseCost,
                DevCostMultiplier = feat.devCostMultiplier
            });
        }

        RebuildFilteredList();
    }

    // ── Step 5 — Features ────────────────────────────────────────────────────
    public enum FeatureSelectedFilter { All, Selected, Unselected }
    public enum FeatureSortMode { Name, Cost, Demand }

    private readonly List<FeatureToggleDisplay> _features = new List<FeatureToggleDisplay>();
    public List<FeatureToggleDisplay> Features => _features;

    private FeatureCategory? _categoryFilter;
    public FeatureCategory? CategoryFilter => _categoryFilter;

    private MarketTrend? _trendFilter;
    public MarketTrend? TrendFilter => _trendFilter;

    private FeatureSelectedFilter _selectedFilter = FeatureSelectedFilter.All;
    public FeatureSelectedFilter SelectedFilter => _selectedFilter;

    private FeatureSortMode _sortMode = FeatureSortMode.Name;
    public FeatureSortMode SortMode => _sortMode;

    private readonly List<int> _filteredIndices = new List<int>();
    public List<int> FilteredIndices => _filteredIndices;

    public void SetCategoryFilter(FeatureCategory? filter) {
        _categoryFilter = filter;
        RebuildFilteredList();
    }

    public void SetTrendFilter(MarketTrend? filter) {
        _trendFilter = filter;
        RebuildFilteredList();
    }

    public void SetSelectedFilter(FeatureSelectedFilter filter) {
        _selectedFilter = filter;
        RebuildFilteredList();
    }

    public void SetSortMode(FeatureSortMode mode) {
        _sortMode = mode;
        RebuildFilteredList();
    }

    public void RebuildFilteredList() {
        _filteredIndices.Clear();
        int count = _features.Count;
        for (int i = 0; i < count; i++) {
            var feat = _features[i];

            if (_categoryFilter.HasValue && feat.FeatureCategory != _categoryFilter.Value)
                continue;

            if (_trendFilter.HasValue && feat.DemandTrend != _trendFilter.Value)
                continue;

            if (_selectedFilter == FeatureSelectedFilter.Selected && !feat.IsSelected)
                continue;
            if (_selectedFilter == FeatureSelectedFilter.Unselected && feat.IsSelected)
                continue;

            _filteredIndices.Add(i);
        }

        if (_sortMode != FeatureSortMode.Name) {
            _filteredIndices.Sort((int a, int b) => {
                switch (_sortMode) {
                    case FeatureSortMode.Demand:
                        return _features[b].CurrentDemand.CompareTo(_features[a].CurrentDemand);
                    default:
                        return string.Compare(_features[a].DisplayName, _features[b].DisplayName, StringComparison.Ordinal);
                }
            });
        }
    }

    // ── Feature summary bar ──────────────────────────────────────────────────
    public string MissingExpectedWarning { get; private set; } = "";

    private void RecalculateMissingExpectedWarning()
    {
        int count = _features.Count;
        int missing = 0;
        for (int i = 0; i < count; i++)
        {
            var feat = _features[i];
            if (feat.DemandStage == FeatureDemandStage.Standard && !feat.IsSelected)
                missing++;
        }
        MissingExpectedWarning = missing > 0
            ? missing + " Expected feature" + (missing > 1 ? "s" : "") + " removed. This will significantly impact review scores."
            : "";
    }

    public int SelectedFeatureCount
    {
        get
        {
            int c = 0;
            int count = _features.Count;
            for (int i = 0; i < count; i++)
                if (_features[i].IsSelected) c++;
            return c;
        }
    }

    public int TotalSelectedMaintenanceCost => 0;

    public int OptimalFeatureCount {
        get {
            if (_selectedTemplate?.availableFeatures == null) return 3;
            return Math.Max(3, _selectedTemplate.availableFeatures.Length / 3);
        }
    }

    public float ScopeEfficiency {
        get {
            int selected = SelectedFeatureCount;
            int optimal = OptimalFeatureCount;
            if (selected <= optimal) return 1f;
            float ratio = (float)selected / optimal;
            return 1f / (float)Math.Pow(ratio, 0.6);
        }
    }

    public string ScopeEfficiencyLabel {
        get {
            float eff = ScopeEfficiency;
            if (eff >= 0.95f) return "";
            int pct = (int)(eff * 100f);
            if (eff >= 0.8f) return "Scope: Manageable (" + pct + "%)";
            if (eff >= 0.6f) return "Scope: Stretched (" + pct + "%)";
            return "Scope: Overstretched (" + pct + "%)";
        }
    }

    public string ScopeEfficiencyClass {
        get {
            float eff = ScopeEfficiency;
            if (eff >= 0.95f) return "";
            if (eff >= 0.8f) return "scope-good";
            if (eff >= 0.6f) return "scope-warning";
            return "scope-critical";
        }
    }

    public int TotalSelectedUpfrontCost {
        get {
            if (_selectedTemplate?.availableFeatures == null) return 0;
            int total = 0;
            int count = _features.Count;
            for (int i = 0; i < count; i++) {
                if (!_features[i].IsSelected) continue;
                int af = _selectedTemplate.availableFeatures.Length;
                for (int j = 0; j < af; j++) {
                    if (_selectedTemplate.availableFeatures[j] != null &&
                        _selectedTemplate.availableFeatures[j].featureId == _features[i].FeatureId) {
                        total += _selectedTemplate.availableFeatures[j].baseCost;
                        break;
                    }
                }
            }
            return total;
        }
    }

    // ── Category tab filtering ───────────────────────────────────────────────
    private FeatureCategory _selectedCategory = FeatureCategory.Core;
    public FeatureCategory SelectedCategory => _selectedCategory;

    public void SelectCategory(FeatureCategory category) {
        _selectedCategory = category;
        _categoryFilter = category;
        RebuildFilteredList();
        OnStepChanged?.Invoke();
    }

    public List<FeatureCategory> GetDistinctFeatureCategories() {
        var result = new List<FeatureCategory>();
        int count = _features.Count;
        for (int i = 0; i < count; i++) {
            var cat = _features[i].FeatureCategory;
            bool found = false;
            for (int j = 0; j < result.Count; j++) { if (result[j] == cat) { found = true; break; } }
            if (!found) result.Add(cat);
        }
        return result;
    }

    public bool ShouldShowCategoryTabs
    {
        get
        {
            if (_selectedTemplate == null || _selectedTemplate.availableFeatures == null) return false;
            int poolSize = _selectedTemplate.availableFeatures.Length;
            if (poolSize < 12) return false;
            FeatureCategory first = (FeatureCategory)(-1);
            int distinct = 0;
            for (int i = 0; i < poolSize; i++)
            {
                var f = _selectedTemplate.availableFeatures[i];
                if (f == null) continue;
                if (distinct == 0) { first = f.featureCategory; distinct = 1; }
                else if (f.featureCategory != first) { distinct++; if (distinct >= 3) return true; }
            }
            return distinct >= 3;
        }
    }

    public string GetFeatureDevTimeLabel(string featureId)
    {
        if (_selectedTemplate == null || _selectedTemplate.phases == null) return "";
        if (_teamAssignments.Count == 0) return "";

        ProductFeatureDefinition featDef = null;
        if (_selectedTemplate.availableFeatures != null)
        {
            int af = _selectedTemplate.availableFeatures.Length;
            for (int fd = 0; fd < af; fd++)
            {
                if (_selectedTemplate.availableFeatures[fd] != null &&
                    _selectedTemplate.availableFeatures[fd].featureId == featureId)
                {
                    featDef = _selectedTemplate.availableFeatures[fd];
                    break;
                }
            }
        }

        if (featDef == null)
            return "";

        float baseWorkMultiplier = _lastState?.ProductBaseWorkMultiplier ?? 100f;

        float nicheDevTimeMult = 1f;
        if (_selectedTemplate.nicheConfigs != null && _selectedTemplate.nicheConfigs.Length > 0)
            nicheDevTimeMult = _lastState?.GetNicheDevTimeMultiplier(_selectedTemplate.nicheConfigs[0].niche) ?? 1f;

        float devCostMult = featDef.devCostMultiplier > 0f ? featDef.devCostMultiplier : 1f;

        int selectedFeatureCount = _features != null ? _features.Count : 0;
        int actualSelected = 0;
        for (int fi = 0; fi < selectedFeatureCount; fi++)
            if (_features[fi].IsSelected) actualSelected++;
        float difficultyScale = 1.0f + (_selectedTemplate.difficultyTier - 1) * 0.75f;
        float featureScale = 1.0f + actualSelected * 0.12f + (float)Math.Pow(actualSelected, 1.5) * 0.02f;

        float totalPhaseWork = 0f;
        int phaseCount = _selectedTemplate.phases.Length;
        for (int p = 0; p < phaseCount; p++)
            totalPhaseWork += _selectedTemplate.phases[p].baseWorkUnits;
        float avgPhaseWork = phaseCount > 0 ? totalPhaseWork / phaseCount : 0f;

        float featureWork = avgPhaseWork * baseWorkMultiplier * nicheDevTimeMult * difficultyScale * featureScale * devCostMult;

        int optimalTeamSizeFt = _selectedTemplate.optimalTeamSizePerPhase > 0 ? _selectedTemplate.optimalTeamSizePerPhase : 4;
        float totalRate = 0f;
        int assignedCount = 0;
        for (int p = 0; p < phaseCount; p++)
        {
            var phase = _selectedTemplate.phases[p];
            if (!_teamAssignments.TryGetValue(phase.primaryRole, out var teamId)) continue;
            float phaseRate = EstimateTeamWorkPerTick(teamId, phase.phaseType, optimalTeamSizeFt);
            if (phaseRate > 0f) { totalRate += phaseRate; assignedCount++; }
        }

        if (assignedCount == 0 || totalRate <= 0f) return "";
        float workRateForFeature = totalRate / assignedCount;

        float ticks = featureWork / workRateForFeature;
        int days = (int)(ticks / TimeState.TicksPerDay) + 1;
        if (days >= 30)
        {
            int months = Math.Max(1, days / 30);
            return "+" + months + " mo";
        }
        return "+" + days + "d";
    }

    public string GetFeatureCostLabel(string featureId) {
        if (_selectedTemplate?.availableFeatures == null) return "";
        int af = _selectedTemplate.availableFeatures.Length;
        for (int i = 0; i < af; i++) {
            if (_selectedTemplate.availableFeatures[i] != null &&
                _selectedTemplate.availableFeatures[i].featureId == featureId) {
                int cost = _selectedTemplate.availableFeatures[i].baseCost;
                return cost > 0 ? UIFormatting.FormatMoney(cost) : "";
            }
        }
        return "";
    }

    public TooltipData BuildFeatureTooltip(string featureId) {
        ProductFeatureDefinition featDef = null;
        if (_selectedTemplate?.availableFeatures != null) {
            int af = _selectedTemplate.availableFeatures.Length;
            for (int i = 0; i < af; i++) {
                if (_selectedTemplate.availableFeatures[i] != null &&
                    _selectedTemplate.availableFeatures[i].featureId == featureId) {
                    featDef = _selectedTemplate.availableFeatures[i];
                    break;
                }
            }
        }

        string title = featDef?.displayName ?? featureId;
        string body = featDef?.description ?? "";
        var stats = new System.Collections.Generic.List<TooltipStatRow>();

        if (featDef != null) {
            string costLabel = GetFeatureCostLabel(featureId);
            if (!string.IsNullOrEmpty(costLabel))
                stats.Add(new TooltipStatRow { Label = "Cost", Value = costLabel });

            string devTimeLabel = GetFeatureDevTimeLabel(featureId);
            if (!string.IsNullOrEmpty(devTimeLabel))
                stats.Add(new TooltipStatRow { Label = "Dev Time Impact", Value = devTimeLabel });

            string qualityImpact = featDef.qualityWeight >= 1.5f ? "High" : featDef.qualityWeight >= 0.9f ? "Medium" : "Low";
            stats.Add(new TooltipStatRow { Label = "Quality Impact", Value = qualityImpact });

            if (featDef.requiredTotalSkillPoints > 0)
                stats.Add(new TooltipStatRow { Label = "Requires", Value = featDef.requiredTotalSkillPoints + " total " + featDef.requiredSkillType.ToString() + " skill" });
        }

        FeatureToggleDisplay display = default;
        bool found = false;
        int count = _features.Count;
        for (int i = 0; i < count; i++) {
            if (_features[i].FeatureId == featureId) { display = _features[i]; found = true; break; }
        }

        if (found) {
            if (display.HasSynergyWithSelected && !string.IsNullOrEmpty(display.SynergyLabel))
                stats.Add(new TooltipStatRow { Label = "Synergy", Value = display.SynergyLabel, Style = TooltipRowStyle.Unlocked });
            if (display.HasConflictWithSelected && !string.IsNullOrEmpty(display.ConflictLabel))
                stats.Add(new TooltipStatRow { Label = "Conflict", Value = display.ConflictLabel, Style = TooltipRowStyle.Locked });
        }

        return new TooltipData { Title = title, Body = body, Stats = stats.ToArray() };
    }

    public TooltipData BuildPhaseTooltip(int phaseIndex) {
        if (_selectedTemplate?.phases == null || phaseIndex < 0 || phaseIndex >= _selectedTemplate.phases.Length)
            return new TooltipData { Title = "Phase", Body = "" };

        var phase = _selectedTemplate.phases[phaseIndex];
        string title = phase.phaseType.ToString() + " Phase";
        var stats = new System.Collections.Generic.List<TooltipStatRow>();

        stats.Add(new TooltipStatRow { Label = "Primary Role", Value = phase.primaryRole.ToString() });

        float baseWorkMultiplier = _lastState?.ProductBaseWorkMultiplier ?? 100f;
        float nicheDevTimeMult = 1f;
        if (_selectedTemplate.nicheConfigs != null && _selectedTemplate.nicheConfigs.Length > 0)
            nicheDevTimeMult = _lastState?.GetNicheDevTimeMultiplier(_selectedTemplate.nicheConfigs[0].niche) ?? 1f;

        int selectedFeatureCountPh = 0;
        if (_features != null) {
            int fCount = _features.Count;
            for (int fi = 0; fi < fCount; fi++)
                if (_features[fi].IsSelected) selectedFeatureCountPh++;
        }
        float difficultyScalePh = 1.0f + (_selectedTemplate.difficultyTier - 1) * 0.75f;
        float featureScalePh = 1.0f + selectedFeatureCountPh * 0.15f + (float)Math.Pow(selectedFeatureCountPh, 1.8) * 0.015f;

        float phaseWork = phase.baseWorkUnits * baseWorkMultiplier * nicheDevTimeMult * difficultyScalePh * featureScalePh;
        if (_teamAssignments.TryGetValue(phase.primaryRole, out var teamId)) {
            int optimalSizePh = _selectedTemplate.optimalTeamSizePerPhase > 0 ? _selectedTemplate.optimalTeamSizePerPhase : 4;
            float phaseRate = EstimateTeamWorkPerTick(teamId, phase.phaseType, optimalSizePh);
            if (phaseRate > 0f) {
                float phaseTicks = phaseWork / phaseRate;
                int phaseDays = (int)(phaseTicks / TimeState.TicksPerDay) + 1;
                string estDur = phaseDays >= 30
                    ? "~" + Math.Max(1, phaseDays / 30) + " mo"
                    : "~" + phaseDays + "d";
                stats.Add(new TooltipStatRow { Label = "Est. Duration", Value = estDur });
            }

            string teamName = "Unknown";
            var teams = _lastState?.ActiveTeams;
            if (teams != null) {
                int tc = teams.Count;
                for (int t = 0; t < tc; t++) {
                    if (teams[t].id == teamId) { teamName = teams[t].name; break; }
                }
            }
            stats.Add(new TooltipStatRow { Label = "Assigned Team", Value = teamName });
        } else {
            stats.Add(new TooltipStatRow { Label = "Assigned Team", Value = "Unassigned" });
        }

        return new TooltipData { Title = title, Body = "", Stats = stats.ToArray() };
    }

    public TooltipData BuildCostBreakdownTooltip() {
        var stats = new System.Collections.Generic.List<TooltipStatRow>();

        stats.Add(new TooltipStatRow { Label = "Base Template Cost", Value = UIFormatting.FormatMoney(_baseTemplateCost) });

        int featureCosts = TotalSelectedUpfrontCost;
        if (featureCosts > 0)
            stats.Add(new TooltipStatRow { Label = "Feature Costs", Value = UIFormatting.FormatMoney(featureCosts) });

        int extraPlatforms = _selectedPlatformIds.Count > 1 ? _selectedPlatformIds.Count - 1 : 0;
        float platformMult = 1f + extraPlatforms * 0.3f;
        if (platformMult > 1f)
            stats.Add(new TooltipStatRow { Label = "Platform Multiplier", Value = platformMult.ToString("F2") + "x" });

        if (IsConsoleTemplate) {
            int hwCost = HardwareDevCostAdd;
            if (hwCost > 0)
                stats.Add(new TooltipStatRow { Label = "Hardware Dev Cost", Value = UIFormatting.FormatMoney(hwCost) });
        }

        if (_selectedStance == GenerationStance.CrossGen)
            stats.Add(new TooltipStatRow { Label = "Stance Multiplier", Value = "1.50x (CrossGen)" });

        stats.Add(new TooltipStatRow { Label = "Estimates", Value = "", Style = TooltipRowStyle.Header });

        if (!string.IsNullOrEmpty(EstimatedCompletionLabel))
            stats.Add(new TooltipStatRow { Label = "Dev Time", Value = EstimatedCompletionLabel });

        if (EstimatedTotalSalaryCost > 0)
            stats.Add(new TooltipStatRow { Label = "Salary Cost", Value = UIFormatting.FormatMoney(EstimatedTotalSalaryCost) });

        long totalEst = CalculatedCost + EstimatedTotalSalaryCost;
        if (totalEst > 0)
            stats.Add(new TooltipStatRow { Label = "Total", Value = UIFormatting.FormatMoney(totalEst) });

        return new TooltipData { Title = "Cost Breakdown", Body = "", Stats = stats.ToArray() };
    }

    // ── Step 6 — Pricing & Name ────────────────────────────────────────────────
    public string ProductName { get; private set; } = "";
    public bool IsSubscriptionBased { get; private set; }
    public float Price { get; private set; }
    public float SuggestedPriceMin { get; private set; }
    public float SuggestedPriceMax { get; private set; }
    public float MarketAveragePrice { get; private set; }
    public bool IsPriceExtreme { get; private set; }
    public string PriceWarningMessage { get; private set; } = "";
    public float SweetSpotPrice { get; private set; }
    public string PriceRatingClass { get; private set; } = "";
    public string PriceRatingLabel { get; private set; } = "";
    public float MarginPerUnit => Price - ManufactureCostPerUnit;
    public bool IsBelowManufactureCost => IsConsoleTemplate && Price > 0f && Price < ManufactureCostPerUnit;

    // ── Release Date ─────────────────────────────────────────────────────────
    public int SelectedTargetDay { get; private set; }
    public string ReleaseDateDisplay { get; private set; } = "";
    public int MinReleaseDayFromNow { get; private set; } = 30;
    public int MaxReleaseDayFromNow { get; private set; } = 730;

    public void SetTargetDay(int absoluteDay)
    {
        if (_lastState == null) return;
        int currentDay = _lastState.CurrentDay;
        int minDay = currentDay + MinReleaseDayFromNow;
        int maxDay = currentDay + MaxReleaseDayFromNow;
        if (absoluteDay < minDay) absoluteDay = minDay;
        if (absoluteDay > maxDay) absoluteDay = maxDay;
        SelectedTargetDay = absoluteDay;
        int dom = TimeState.GetDayOfMonth(absoluteDay);
        int mon = TimeState.GetMonth(absoluteDay);
        int yr = TimeState.GetYear(absoluteDay);
        ReleaseDateDisplay = UIFormatting.FormatDate(dom, mon, yr);
    }

    public void ClearTargetDay()
    {
        SelectedTargetDay = 0;
        ReleaseDateDisplay = "";
    }

    public void RecalculateMinReleaseDay()
    {
        if (_lastState == null) { MinReleaseDayFromNow = 30; MaxReleaseDayFromNow = 730; return; }

        if (_selectedTemplate == null || _selectedTemplate.phases == null || _teamAssignments.Count == 0)
        {
            MinReleaseDayFromNow = 30;
            MaxReleaseDayFromNow = 730;
            return;
        }

        float baseWorkMultiplierRd = _lastState?.ProductBaseWorkMultiplier ?? 100f;

        float nicheDevTimeMult = 1f;
        if (_selectedTemplate.nicheConfigs != null && _selectedTemplate.nicheConfigs.Length > 0)
            nicheDevTimeMult = _lastState?.GetNicheDevTimeMultiplier(_selectedTemplate.nicheConfigs[0].niche) ?? 1f;

        int selectedFeatureCountRd = 0;
        if (_features != null) {
            int fCount = _features.Count;
            for (int fi = 0; fi < fCount; fi++)
                if (_features[fi].IsSelected) selectedFeatureCountRd++;
        }
        float difficultyScaleRd = 1.0f + (_selectedTemplate.difficultyTier - 1) * 0.75f;
        float featureScaleRd = 1.0f + selectedFeatureCountRd * 0.15f + (float)Math.Pow(selectedFeatureCountRd, 1.8) * 0.015f;

        int optimalTeamSizeRd = _selectedTemplate.optimalTeamSizePerPhase > 0 ? _selectedTemplate.optimalTeamSizePerPhase : 4;
        float totalTicks = 0f;
        int phaseCount = _selectedTemplate.phases.Length;
        for (int p = 0; p < phaseCount; p++)
        {
            var phase = _selectedTemplate.phases[p];
            float phaseWork = ComputePhaseWorkRequired(phase, baseWorkMultiplierRd, nicheDevTimeMult, difficultyScaleRd, featureScaleRd);
            if (!_teamAssignments.TryGetValue(phase.primaryRole, out var teamId)) continue;
            float phaseRate = EstimateTeamWorkPerTick(teamId, phase.phaseType, optimalTeamSizeRd);
            if (phaseRate > 0f)
                totalTicks += phaseWork / phaseRate;
        }

        if (totalTicks > 0f)
        {
            float ticksRequired = totalTicks * 1.2f;
            int daysRequired = (int)(ticksRequired / TimeState.TicksPerDay) + 1;
            MinReleaseDayFromNow = daysRequired < 7 ? 7 : daysRequired;
        }
        else
        {
            MinReleaseDayFromNow = 30;
        }
        MaxReleaseDayFromNow = 730;
    }

    public void SetProductName(string name) { ProductName = name ?? ""; }

    public void SetPricingModel(bool isSubscription)
    {
        IsSubscriptionBased = isSubscription;
        if (_selectedTemplate != null)
        {
            float basePrice = IsSubscriptionBased
                ? _selectedTemplate.economyConfig?.monthlySubscriptionPrice ?? 0f
                : _selectedTemplate.economyConfig?.pricePerUnit ?? 0f;
            float featureMod = 1f + SelectedFeatureCount * 0.02f;
            SuggestedPriceMin = basePrice * 0.7f * featureMod;
            SuggestedPriceMax = basePrice * 1.4f * featureMod;
            MarketAveragePrice = basePrice * featureMod;
            SweetSpotPrice = basePrice * featureMod;
            _pricingDirty = false;
            Price = basePrice;
        }
        UpdatePriceWarning();
    }

    public void SetPrice(float price)
    {
        Price = price;
        UpdatePriceWarning();
    }

    private void UpdatePriceWarning()
    {
        if (SuggestedPriceMin <= 0f || SuggestedPriceMax <= 0f)
        {
            IsPriceExtreme = false;
            PriceWarningMessage = "";
            PriceRatingClass = "";
            PriceRatingLabel = "";
            return;
        }
        float mid = (SuggestedPriceMin + SuggestedPriceMax) * 0.5f;
        float threshold = mid * 0.5f;
        if (Price > SuggestedPriceMax + threshold)
        {
            IsPriceExtreme = true;
            PriceWarningMessage = "Price is significantly above the suggested range";
        }
        else if (Price > 0f && Price < SuggestedPriceMin - threshold)
        {
            IsPriceExtreme = true;
            PriceWarningMessage = "Price is significantly below the suggested range";
        }
        else
        {
            IsPriceExtreme = false;
            PriceWarningMessage = "";
        }

        if (IsBelowManufactureCost && !IsPriceExtreme)
        {
            IsPriceExtreme = true;
            PriceWarningMessage = "Selling below manufacturing cost of $" + ManufactureCostPerUnit + " — loss-leader pricing";
        }

        if (SweetSpotPrice > 0f && Price > 0f)
        {
            float dist = Math.Abs(Price - SweetSpotPrice) / SweetSpotPrice;
            if (dist <= 0.15f)
            {
                PriceRatingClass = "price-rating--good";
                PriceRatingLabel = "Great price";
            }
            else if (dist <= 0.40f)
            {
                PriceRatingClass = "price-rating--okay";
                PriceRatingLabel = "Acceptable";
            }
            else
            {
                PriceRatingClass = "price-rating--bad";
                PriceRatingLabel = Price > SweetSpotPrice ? "Too expensive" : "Too cheap";
            }
        }
        else
        {
            PriceRatingClass = "";
            PriceRatingLabel = "";
        }
    }

    // ── Step 7 — Team Assignment ──────────────────────────────────────────────
    private readonly List<ProductTeamRole> _requiredRoles = new List<ProductTeamRole>();
    public List<ProductTeamRole> RequiredRoles => _requiredRoles;

    private readonly List<ProductTeamRole> _optionalRoles = new List<ProductTeamRole>();
    public List<ProductTeamRole> OptionalRoles => _optionalRoles;

    private readonly List<TeamSummaryDisplay> _availableTeams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> AvailableTeams => _availableTeams;

    private readonly List<TeamSummaryDisplay> _busyTeams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> BusyTeams => _busyTeams;

    private readonly List<TeamSummaryDisplay> _availableMarketingTeams = new List<TeamSummaryDisplay>();
    public List<TeamSummaryDisplay> AvailableMarketingTeams => _availableMarketingTeams;

    private readonly Dictionary<ProductTeamRole, TeamId> _teamAssignments = new Dictionary<ProductTeamRole, TeamId>();
    public Dictionary<ProductTeamRole, TeamId> TeamAssignments => _teamAssignments;

    public bool AllRolesAssigned
    {
        get
        {
            int count = _requiredRoles.Count;
            for (int i = 0; i < count; i++)
            {
                if (!_teamAssignments.ContainsKey(_requiredRoles[i])) return false;
            }
            return count > 0;
        }
    }

    public bool HasAnyTeams => _availableTeams.Count > 0 || _busyTeams.Count > 0;
    public string EstimatedCompletionLabel { get; private set; } = "Unknown";

    public void AssignTeam(ProductTeamRole role, TeamId teamId)
    {
        _teamAssignments[role] = teamId;
        RecalculateCompletion();
    }

    public void UnassignTeam(ProductTeamRole role)
    {
        _teamAssignments.Remove(role);
        RecalculateCompletion();
    }

    public int GetPhaseIndexForRole(ProductTeamRole role) {
        if (_selectedTemplate?.phases == null) return -1;
        int count = _selectedTemplate.phases.Length;
        for (int i = 0; i < count; i++)
            if (_selectedTemplate.phases[i].primaryRole == role) return i;
        return -1;
    }

    // ── Shared ───────────────────────────────────────────────────────────────
    public int CalculatedCost { get; private set; }
    public bool CanAfford { get; private set; }
    public int LastKnownCurrentDay { get; private set; }
    public long EstimatedTotalSalaryCost { get; private set; }
    public string EstimatedTotalCostLabel { get; private set; } = "";

    // ── Internal caches ──────────────────────────────────────────────────────
    private int _baseTemplateCost;
    private IReadOnlyGameState _lastState;
    private ProductTemplateDefinition _selectedTemplate;
    private int _estimatedDevDays = 60;
    public int EstimatedDevDays => _estimatedDevDays;
    private readonly HashSet<EmployeeId> _uniqueEmployeeIds = new HashSet<EmployeeId>();

    private readonly Dictionary<string, ProductTemplateDisplay> _templateCache =
        new Dictionary<string, ProductTemplateDisplay>();

    private readonly Dictionary<string, FeatureData> _featureDataCache =
        new Dictionary<string, FeatureData>();

    private struct FeatureData
    {
        public string FeatureId;
        public string DisplayName;
        public string Description;
        public int AdditionalCost;
        public int MaintenanceCostPerMonth;
        public FeatureCategory FeatureCategory;
        public string[] SynergyFeatureIds;
        public string[] ConflictFeatureIds;
        public float SynergyBonusPercent;
        public float ConflictPenaltyPercent;
    }

    // ── IViewModel ───────────────────────────────────────────────────────────
    public void Refresh(IReadOnlyGameState state)
    {
        if (state == null) return;
        _lastState = state;
        LastKnownCurrentDay = state.CurrentDay;
        CanAfford = state.Money >= CalculatedCost;

        _availableTeams.Clear();
        _busyTeams.Clear();
        _availableMarketingTeams.Clear();
        var activeTeams = state.ActiveTeams;
        int tc = activeTeams.Count;
        for (int i = 0; i < tc; i++)
        {
            var team = activeTeams[i];
            bool onContract = state.GetContractForTeam(team.id) != null;
            bool onProduct = state.IsTeamAssignedToProduct(team.id);
            TeamType teamType = state.GetTeamType(team.id);
            if (teamType == TeamType.HR || teamType == TeamType.Accounting) continue;
            var display = new TeamSummaryDisplay {
                Id = team.id,
                Name = team.name,
                MemberCount = team.members?.Count ?? 0,
                ContractName = "",
                TeamType = UIFormatting.FormatTeamType(teamType),
                AvgMorale = 0
            };
            if (teamType == TeamType.Marketing)
            {
                if (!onContract && !onProduct)
                    _availableMarketingTeams.Add(display);
            }
            else
            {
                if (onContract || onProduct) _busyTeams.Add(display);
                else _availableTeams.Add(display);
            }
        }

        if (_selectedTemplate != null && _pricingDirty)
        {
            float basePrice = IsSubscriptionBased
                ? _selectedTemplate.economyConfig?.monthlySubscriptionPrice ?? 0f
                : _selectedTemplate.economyConfig?.pricePerUnit ?? 0f;
            float featureMod = 1f + SelectedFeatureCount * 0.02f;
            SuggestedPriceMin = basePrice * 0.7f * featureMod;
            SuggestedPriceMax = basePrice * 1.4f * featureMod;
            MarketAveragePrice = basePrice * featureMod;
            SweetSpotPrice = basePrice * featureMod;
            _pricingDirty = false;
            UpdatePriceWarning();
        }

        RecalculateCompletion();

        if (CurrentStep == WizardStep.Features)
            UpdateFeatureMarketData(state);
    }

    // ── Platform / Tool / Niche population ────────────────────────────────────
    public void PopulatePlatformsFromState(IReadOnlyDictionary<ProductId, Product> shippedProducts,
        IReadOnlyDictionary<ProductId, Product> competitorProducts,
        ProductCategory[] validPlatformCategories,
        ProductNiche[] validPlatformNiches,
        bool playerOwns)
    {
        _availablePlatforms.Clear();

        if (shippedProducts == null) return;

        foreach (var kvp in shippedProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;
            if (!product.Category.IsPlatform()) continue;
            if (validPlatformCategories != null && !ArrayContains(validPlatformCategories, product.Category)) continue;
            if (product.Category == ProductCategory.OperatingSystem
                && validPlatformNiches != null && validPlatformNiches.Length > 0
                && !ArrayContainsNiche(validPlatformNiches, product.Niche)) continue;

            _availablePlatforms.Add(new PlatformTargetDisplay {
                PlatformId = kvp.Key,
                DisplayName = product.ProductName,
                OwnerLabel = "Your Company",
                ActiveUsers = product.ActiveUserCount,
                MarketSharePercent = 0f,
                LicensingCostLabel = "Owned",
                IsPlayerOwned = true,
                PlatformTypeLabel = ResolvePlatformTypeLabel(product)
            });
        }

        if (competitorProducts == null) return;

        foreach (var kvp in competitorProducts)
        {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;
            if (!product.Category.IsPlatform()) continue;
            if (validPlatformCategories != null && !ArrayContains(validPlatformCategories, product.Category)) continue;
            if (product.Category == ProductCategory.OperatingSystem
                && validPlatformNiches != null && validPlatformNiches.Length > 0
                && !ArrayContainsNiche(validPlatformNiches, product.Niche)) continue;

            _availablePlatforms.Add(new PlatformTargetDisplay {
                PlatformId = kvp.Key,
                DisplayName = product.ProductName,
                OwnerLabel = "Competitor",
                ActiveUsers = product.ActiveUserCount,
                MarketSharePercent = 0f,
                LicensingCostLabel = product.PlayerLicensingRate > 0f
                    ? "Licensed | " + (product.PlayerLicensingRate * 100f).ToString("F0") + "%"
                    : "Licensed (royalty)",
                IsPlayerOwned = false,
                PlatformTypeLabel = ResolvePlatformTypeLabel(product)
            });
        }

        SortAvailablePlatforms();
    }

    private static string ResolvePlatformTypeLabel(Product product) {
        if (product.Category == ProductCategory.GameConsole) return "Game Console";
        if (product.Niche == ProductNiche.DesktopOS) return "Desktop OS";
        if (product.Niche == ProductNiche.MobileOS) return "Mobile OS";
        if (product.Niche == ProductNiche.ServerOS) return "Server OS";
        return UIFormatting.FormatCategory(product.Category);
    }

    private void SortAvailablePlatforms() {
        int n = _availablePlatforms.Count;
        for (int i = 1; i < n; i++) {
            var key = _availablePlatforms[i];
            int j = i - 1;
            while (j >= 0) {
                var cmp = _availablePlatforms[j];
                int typeCmp = string.Compare(cmp.PlatformTypeLabel, key.PlatformTypeLabel, System.StringComparison.Ordinal);
                if (typeCmp > 0 || (typeCmp == 0 && cmp.ActiveUsers < key.ActiveUsers)) {
                    _availablePlatforms[j + 1] = cmp;
                    j--;
                } else {
                    break;
                }
            }
            _availablePlatforms[j + 1] = key;
        }
    }

    private static bool ArrayContainsNiche(ProductNiche[] arr, ProductNiche value) {
        int len = arr.Length;
        for (int i = 0; i < len; i++) {
            if (arr[i] == value) return true;
        }
        return false;
    }

    public void PopulateToolsFromState(IReadOnlyDictionary<ProductId, Product> shippedProducts,
        IReadOnlyDictionary<ProductId, Product> competitorProducts,
        ProductCategory[] requiredToolCategories)
    {
        _availableRequiredTools.Clear();
        _requiredToolCategories.Clear();

        _templateHasRequiredTool = requiredToolCategories != null && requiredToolCategories.Length > 0;
        _templateHasTools = _templateHasRequiredTool;

        if (requiredToolCategories != null)
        {
            for (int c = 0; c < requiredToolCategories.Length; c++)
                _requiredToolCategories.Add(requiredToolCategories[c]);
        }

        if (shippedProducts != null)
        {
            foreach (var kvp in shippedProducts)
            {
                var product = kvp.Value;
                if (!product.IsOnMarket || !product.Category.IsTool()) continue;

                if (requiredToolCategories != null && ArrayContains(requiredToolCategories, product.Category))
                {
                    float score = product.OverallQuality;
                    _availableRequiredTools.Add(new ToolSelectionDisplay {
                        ToolId = kvp.Key,
                        Category = product.Category,
                        DisplayName = product.ProductName,
                        OwnerLabel = "Your Company | Owned",
                        QualityScore = score,
                        QualitativeLabel = GetQualitativeLabel(score),
                        LicensingCostLabel = "Owned",
                        IsPlayerOwned = true
                    });
                }
            }
        }

        if (competitorProducts != null)
        {
            foreach (var kvp in competitorProducts)
            {
                var product = kvp.Value;
                if (!product.IsOnMarket || !product.Category.IsTool()) continue;
                if (product.DistributionModel == ToolDistributionModel.Proprietary) continue;

                if (requiredToolCategories != null && ArrayContains(requiredToolCategories, product.Category))
                {
                    string companyName = product.ProductName;
                    string licensingLabel = product.DistributionModel == ToolDistributionModel.OpenSource
                        ? "Open Source"
                        : "Licensed | " + (product.PlayerLicensingRate * 100f).ToString("F0") + "%";
                    float score = product.OverallQuality;
                    _availableRequiredTools.Add(new ToolSelectionDisplay {
                        ToolId = kvp.Key,
                        Category = product.Category,
                        DisplayName = product.ProductName,
                        OwnerLabel = "Competitor | " + licensingLabel,
                        QualityScore = score,
                        QualitativeLabel = GetQualitativeLabel(score),
                        LicensingCostLabel = licensingLabel,
                        IsPlayerOwned = false
                    });
                }
            }
        }
    }

    private static string GetQualitativeLabel(float quality)
    {
        if (quality >= 85f) return "Exceptional";
        if (quality >= 70f) return "High Quality";
        if (quality >= 50f) return "Adequate";
        if (quality >= 30f) return "Poor";
        return "Very Poor";
    }

    public void PopulateNicheOptions()
    {
        _availableNiches.Clear();
        if (_selectedTemplate == null || !_selectedTemplate.HasNiches)
        {
            _selectedNiche = ProductNiche.None;
            return;
        }
        for (int i = 0; i < _selectedTemplate.nicheConfigs.Length; i++)
        {
            var cfg = _selectedTemplate.nicheConfigs[i];
            _availableNiches.Add(new NicheOptionDisplay {
                Niche = cfg.niche,
                DisplayName = cfg.niche.ToString(),
                RetentionLabel = cfg.retentionMonths + " months avg retention",
                VolatilityLabel = cfg.volatility.ToString("F1") + " volatility"
            });
        }
        if (_availableNiches.Count == 1 && !_selectedNiche.HasValue)
            _selectedNiche = _availableNiches[0].Niche;
    }

    private static bool ArrayContains<T>(T[] arr, T value) where T : struct
    {
        for (int i = 0; i < arr.Length; i++)
            if (EqualityComparer<T>.Default.Equals(arr[i], value)) return true;
        return false;
    }

    // ── Cross-Product Gate tooltip support ────────────────────────────────────
    private CrossProductGateConfig _gateConfig;
    private ProductTemplateDefinition[] _allTemplates;

    public void SetGateConfig(CrossProductGateConfig config, ProductTemplateDefinition[] allTemplates) {
        _gateConfig = config;
        _allTemplates = allTemplates;
    }

    public TooltipData BuildToolCapabilityTooltip(ProductId toolId) {
        Product tool = FindShippedProduct(toolId);
        string title = tool != null ? tool.ProductName + " (Tool)" : "Unknown Tool";
        string body = tool != null ? "Quality: " + tool.OverallQuality.ToString("F0") + "/100" : "";

        var stats = new System.Collections.Generic.List<TooltipStatRow>();

        if (tool != null && tool.Features != null && tool.Features.Length > 0) {
            int fc = tool.Features.Length;
            for (int i = 0; i < fc; i++) {
                var fs = tool.Features[i];
                string label = GetFeatureDisplayName(fs.FeatureId);
                string qualLabel = fs.Quality > 0f ? fs.Quality.ToString("F0") + " (" + GetQualitativeLabel(fs.Quality) + ")" : "Not Built";
                stats.Add(new TooltipStatRow { Label = label, Value = qualLabel });
            }
        }

        AppendUnlockRows(stats, toolId, isToolContext: true, tool);

        return new TooltipData { Title = title, Body = body, Stats = stats.ToArray() };
    }

    public TooltipData BuildPlatformCapabilityTooltip(ProductId platformId) {
        Product platform = FindShippedProduct(platformId);
        string title = platform != null ? platform.ProductName + " (Platform)" : "Unknown Platform";
        string body = platform != null ? "Quality: " + platform.OverallQuality.ToString("F0") + "/100" : "";

        var stats = new System.Collections.Generic.List<TooltipStatRow>();

        if (platform != null && platform.Features != null && platform.Features.Length > 0) {
            int fc = platform.Features.Length;
            for (int i = 0; i < fc; i++) {
                var fs = platform.Features[i];
                string label = GetFeatureDisplayName(fs.FeatureId);
                string qualLabel = fs.Quality > 0f ? fs.Quality.ToString("F0") + " (" + GetQualitativeLabel(fs.Quality) + ")" : "Not Built";
                stats.Add(new TooltipStatRow { Label = label, Value = qualLabel });
            }
        }

        AppendUnlockRows(stats, platformId, isToolContext: false, platform);

        return new TooltipData { Title = title, Body = body, Stats = stats.ToArray() };
    }

    private void AppendUnlockRows(System.Collections.Generic.List<TooltipStatRow> stats, ProductId productId, bool isToolContext, Product product) {
        if (_allTemplates == null) return;

        var unlocked = new System.Collections.Generic.List<TooltipStatRow>();

        int tplCount = _allTemplates.Length;
        for (int t = 0; t < tplCount; t++) {
            var tpl = _allTemplates[t];
            if (tpl == null || tpl.availableFeatures == null) continue;
            int fc = tpl.availableFeatures.Length;
            for (int f = 0; f < fc; f++) {
                var feat = tpl.availableFeatures[f];
                if (feat == null) continue;
                string req = isToolContext ? feat.requiresToolFeature : feat.requiresPlatformFeature;
                if (string.IsNullOrEmpty(req)) continue;

                float upstreamQuality = GetUpstreamFeatureQuality(product, req);
                if (upstreamQuality <= 0f) continue;

                string featureName = feat.displayName ?? feat.featureId;
                float ceiling = _gateConfig != null ? _gateConfig.GetTierCeiling(upstreamQuality) : float.MaxValue;
                string capLabel = ceiling >= 100f ? "Unlimited" : "cap " + ceiling.ToString("F0");
                unlocked.Add(new TooltipStatRow {
                    Label = featureName,
                    Value = capLabel,
                    Style = TooltipRowStyle.Unlocked
                });
            }
        }

        if (unlocked.Count > 0) {
            stats.Add(new TooltipStatRow { Label = "Unlocked", Value = "", Style = TooltipRowStyle.Header });
            int uc = unlocked.Count;
            for (int i = 0; i < uc; i++) stats.Add(unlocked[i]);
        }
    }

    private float GetUpstreamFeatureQuality(Product product, string featureId) {
        if (product == null || product.Features == null) return 0f;
        int fc = product.Features.Length;
        for (int i = 0; i < fc; i++) {
            if (product.Features[i].FeatureId == featureId)
                return product.Features[i].Quality;
        }
        return 0f;
    }

    private string GetFeatureDisplayName(string featureId) {
        if (_allTemplates == null || string.IsNullOrEmpty(featureId)) return featureId ?? "";
        int tplCount = _allTemplates.Length;
        for (int t = 0; t < tplCount; t++) {
            var tpl = _allTemplates[t];
            if (tpl?.availableFeatures == null) continue;
            int fc = tpl.availableFeatures.Length;
            for (int f = 0; f < fc; f++) {
                var feat = tpl.availableFeatures[f];
                if (feat != null && feat.featureId == featureId)
                    return feat.displayName ?? featureId;
            }
        }
        return featureId;
    }

    private Product FindShippedProduct(ProductId id) {
        if (_lastState?.ShippedProducts == null) return null;
        _lastState.ShippedProducts.TryGetValue(id, out var product);
        return product;
    }

    // ── Feature Market Data ───────────────────────────────────────────────────
    private void UpdateFeatureMarketData(IReadOnlyGameState state)
    {
        if (state == null) return;

        // Build selected set for synergy/conflict lookup
        bool[] selectedFlags = new bool[_features.Count];
        for (int i = 0; i < _features.Count; i++)
            selectedFlags[i] = _features[i].IsSelected;

        for (int i = 0; i < _features.Count; i++)
        {
            var feat = _features[i];
            if (!_featureDataCache.TryGetValue(feat.FeatureId, out var data)) continue;

            feat.CurrentDemand = state.GetNicheDemand(_selectedNiche ?? ProductNiche.None);
            feat.DemandTrend = state.GetNicheTrend(_selectedNiche ?? ProductNiche.None);
            feat.Volatility = 0f;
            feat.ProjectedShipDemand = state.GetNicheDemand(_selectedNiche ?? ProductNiche.None);

            // Synergy/conflict against currently selected features
            feat.HasSynergyWithSelected = false;
            feat.HasConflictWithSelected = false;
            feat.SynergyLabel = "";
            feat.ConflictLabel = "";

            if (data.SynergyFeatureIds != null)
            {
                for (int s = 0; s < data.SynergyFeatureIds.Length; s++)
                {
                    string synId = data.SynergyFeatureIds[s];
                    for (int j = 0; j < _features.Count; j++)
                    {
                        if (j == i || !selectedFlags[j]) continue;
                        if (_features[j].FeatureId == synId)
                        {
                            feat.HasSynergyWithSelected = true;
                            feat.SynergyLabel = "Synergy with " + _features[j].DisplayName +
                                " (+" + (data.SynergyBonusPercent * 100f).ToString("F0") + "%)";
                            break;
                        }
                    }
                    if (feat.HasSynergyWithSelected) break;
                }
            }

            if (data.ConflictFeatureIds != null)
            {
                for (int c = 0; c < data.ConflictFeatureIds.Length; c++)
                {
                    string confId = data.ConflictFeatureIds[c];
                    for (int j = 0; j < _features.Count; j++)
                    {
                        if (j == i || !selectedFlags[j]) continue;
                        if (_features[j].FeatureId == confId)
                        {
                            feat.HasConflictWithSelected = true;
                            feat.ConflictLabel = "Conflicts with " + _features[j].DisplayName +
                                " (-" + (data.ConflictPenaltyPercent * 100f).ToString("F0") + "%)";
                            break;
                        }
                    }
                    if (feat.HasConflictWithSelected) break;
                }
            }

            _features[i] = feat;
        }
    }

    // ── Template setup ────────────────────────────────────────────────────────
    public void SetTemplates(ProductTemplateDefinition[] definitions)
    {
        _templates.Clear();
        _templateCache.Clear();
        if (definitions == null) return;

        for (int i = 0; i < definitions.Length; i++)
        {
            var def = definitions[i];
            if (def == null) continue;

            string phaseSummary = BuildPhaseSummary(def);
            string[] phasePills = BuildPhasePills(def);
            string groupLabel = DeriveGroupLabel(def);
            var display = new ProductTemplateDisplay {
                TemplateId = def.templateId,
                DisplayName = def.displayName,
                Description = def.description,
                CategoryLabel = def.category.ToString(),
                CategoryGroupLabel = groupLabel,
                BaseUpfrontCost = def.baseUpfrontCost,
                PhaseCount = def.phases != null ? def.phases.Length : 0,
                PhaseSummary = phaseSummary,
                PhasePills = phasePills
            };

            _templates.Add(display);
            _templateCache[def.templateId] = display;
        }
    }

    private static string DeriveGroupLabel(ProductTemplateDefinition def)
    {
        switch (def.category)
        {
            case ProductCategory.VideoGame:           return "Games";
            case ProductCategory.OperatingSystem:     return "Platforms";
            case ProductCategory.GameConsole:         return "Platforms";
            case ProductCategory.GameEngine:          return "Tools";
            case ProductCategory.GraphicsEditor:      return "Tools";
            case ProductCategory.AudioTool:           return "Tools";
            case ProductCategory.DevFramework:        return "Tools";
            case ProductCategory.MobileApp:           return "Apps";
            case ProductCategory.DesktopSoftware:     return "Apps";
            case ProductCategory.WebApplication:      return "Web";
            case ProductCategory.OnlineService:       return "Services";
            case ProductCategory.SecuritySoftware:    return "Services";
            case ProductCategory.CloudInfrastructure: return "Services";
            case ProductCategory.AIProduct:           return "Services";
            default:                                  return "Other";
        }
    }

    private static string[] BuildPhasePills(ProductTemplateDefinition def)
    {
        if (def.phases == null || def.phases.Length == 0) return new string[0];
        var pills = new string[def.phases.Length];
        for (int i = 0; i < def.phases.Length; i++)
            pills[i] = FormatPhaseShort(def.phases[i].phaseType);
        return pills;
    }

    public void SelectTemplate(string templateId, ProductTemplateDefinition[] definitions)
    {
        if (string.IsNullOrEmpty(templateId)) return;
        SelectedTemplateId = templateId;

        _features.Clear();
        _featureDataCache.Clear();
        _requiredRoles.Clear();
        _optionalRoles.Clear();
        _teamAssignments.Clear();
        _baseTemplateCost = 0;
        _selectedTemplate = null;
        _selectedCategory = FeatureCategory.Core;
        _categoryFilter = null;
        _selectedPlatformIds.Clear();
        _selectedTools.Clear();
        _availablePlatforms.Clear();
        _availableRequiredTools.Clear();
        _requiredToolCategories.Clear();
        _availableNiches.Clear();
        _selectedNiche = null;
        _selectedStance = GenerationStance.Standard;
        _selectedPredecessorId = null;
        _hardwareConfig = new HardwareConfiguration {
            processingTier = HardwareTier.Budget,
            graphicsTier   = HardwareTier.Budget,
            memoryTier     = HardwareTier.Budget,
            storageTier    = HardwareTier.Budget,
            formFactor     = ConsoleFormFactor.Standard
        };

        if (definitions == null) return;

        for (int i = 0; i < definitions.Length; i++)
        {
            var def = definitions[i];
            if (def == null || def.templateId != templateId) continue;

            _selectedTemplate = def;
            _baseTemplateCost = def.baseUpfrontCost;
            _templateHasTools = def.requiredToolTypes != null && def.requiredToolTypes.Length > 0;
            _templateHasRequiredTool = def.requiredToolTypes != null && def.requiredToolTypes.Length > 0;

            if (def.availableFeatures != null)
                PopulateFeatures(def.availableFeatures, null);

            if (def.phases != null)
            {
                for (int ph = 0; ph < def.phases.Length; ph++)
                {
                    var role = def.phases[ph].primaryRole;
                    if (!_requiredRoles.Contains(role)) _requiredRoles.Add(role);
                }
            }

            if (!_requiredRoles.Contains(ProductTeamRole.Marketing))
                _optionalRoles.Add(ProductTeamRole.Marketing);

            IsSubscriptionBased = def.economyConfig?.isSubscriptionBased ?? false;
            float defaultPrice = IsSubscriptionBased
                ? def.economyConfig?.monthlySubscriptionPrice ?? 0f
                : def.economyConfig?.pricePerUnit ?? 0f;
            Price = defaultPrice;
            float featureMod = 1f + SelectedFeatureCount * 0.02f;
            SuggestedPriceMin = defaultPrice * 0.7f * featureMod;
            SuggestedPriceMax = defaultPrice * 1.4f * featureMod;
            MarketAveragePrice = defaultPrice * featureMod;
            SweetSpotPrice = defaultPrice * featureMod;
            _pricingDirty = false;
            UpdatePriceWarning();

            PopulateNicheOptions();

            CanSetDistribution = def.layer == ProductLayer.Platform || def.layer == ProductLayer.Tool;
            SelectedDistribution = ToolDistributionModel.Proprietary;
            SelectedLicensingRate = 0.10f;
            RefreshToolSubscriptionVisibility();

            break;
        }

        _cachedTotalSteps = BuildNewProductStepList().Count;
        RecalculateCost();
    }

    private void PopulateFeatures(ProductFeatureDefinition[] featureDefs, object unused)
    {
        _features.Clear();
        _featureDataCache.Clear();
        if (featureDefs == null) return;

        int currentGen = _lastState != null ? _lastState.GetCurrentGeneration() : 1;
        ProductNiche niche = _selectedNiche ?? ProductNiche.None;
        string templateId = _selectedTemplate?.templateId ?? "";

        for (int f = 0; f < featureDefs.Length; f++)
        {
            var feat = featureDefs[f];
            if (feat == null) continue;

            _featureDataCache[feat.featureId] = new FeatureData {
                FeatureId = feat.featureId,
                DisplayName = feat.displayName,
                Description = feat.description,
                AdditionalCost = 0,
                MaintenanceCostPerMonth = 0,
                FeatureCategory = feat.featureCategory,
                SynergyFeatureIds = feat.synergyFeatureIds,
                ConflictFeatureIds = feat.conflictFeatureIds,
                SynergyBonusPercent = feat.synergyBonusPercent,
                ConflictPenaltyPercent = feat.conflictPenaltyPercent
            };

            bool isLocked = false;
            string lockReason = "";
            string capLabel = "";

            // Cross-product gate check
            if (_lastState != null) {
                if (!string.IsNullOrEmpty(feat.requiresToolFeature)) {
                    bool toolHasFeature = HasUpstreamFeature(feat.requiresToolFeature, isToolFeature: true);
                    if (!toolHasFeature) {
                        isLocked = true;
                        lockReason = "Requires " + GetUpstreamFeatureDisplayName(feat.requiresToolFeature) + " on your selected tool.";
                    } else if (_gateConfig != null) {
                        float upstreamQuality = GetUpstreamFeatureQualityForDisplay(feat.requiresToolFeature, isToolFeature: true);
                        float cap = _gateConfig.GetTierCeiling(upstreamQuality);
                        if (cap < 100f && cap > 0f)
                            capLabel = "Quality capped at " + cap.ToString("F0") + " (upstream quality: " + upstreamQuality.ToString("F0") + ")";
                    }
                }
                if (!isLocked && !string.IsNullOrEmpty(feat.requiresPlatformFeature)) {
                    bool platformHasFeature = HasUpstreamFeature(feat.requiresPlatformFeature, isToolFeature: false);
                    if (!platformHasFeature) {
                        isLocked = true;
                        lockReason = "Requires " + GetUpstreamFeatureDisplayName(feat.requiresPlatformFeature) + " on your target platform.";
                    } else if (_gateConfig != null && string.IsNullOrEmpty(capLabel)) {
                        float upstreamQuality = GetUpstreamFeatureQualityForDisplay(feat.requiresPlatformFeature, isToolFeature: false);
                        float cap = _gateConfig.GetTierCeiling(upstreamQuality);
                        if (cap < 100f && cap > 0f)
                            capLabel = "Quality capped at " + cap.ToString("F0") + " (upstream quality: " + upstreamQuality.ToString("F0") + ")";
                    }
                }
            }

            if (!isLocked) {
                string skillLockReason;
                if (CheckSkillGate(feat, out skillLockReason)) {
                    isLocked = true;
                    lockReason = skillLockReason;
                }
            }

            float coverageRatio = _lastState != null ? _lastState.GetFeatureAdoptionRate(feat.featureId, niche, templateId) : 0f;
            FeatureDemandStage demandStage = FeatureDemandHelper.GetDemandStage(currentGen, feat.demandIntroductionGen, feat.demandMaturitySpeed, feat.isFoundational, coverageRatio);
            string demandLabel = FeatureDemandHelper.GetDemandLabel(demandStage);
            bool preCheck = !isLocked && FeatureDemandHelper.ShouldPreCheck(demandStage);

            _features.Add(new FeatureToggleDisplay {
                FeatureId = feat.featureId,
                DisplayName = feat.displayName,
                Description = feat.description,
                FeatureCategory = feat.featureCategory,
                IsSelected = preCheck,
                IsPreSelected = preCheck,
                IsLocked = isLocked,
                LockReason = lockReason,
                CapLabel = capLabel,
                BaseCost = feat.baseCost,
                DevCostMultiplier = feat.devCostMultiplier,
                DemandStage = demandStage,
                DemandStageLabel = demandLabel
            });
        }

        RecalculateMissingExpectedWarning();
        RebuildFilteredList();
    }

    public void ToggleFeature(string featureId, bool selected)
    {
        int count = _features.Count;
        for (int i = 0; i < count; i++)
        {
            if (_features[i].FeatureId != featureId) continue;
            var item = _features[i];
            item.IsSelected = selected;
            _features[i] = item;
            break;
        }
        _pricingDirty = true;
        RecalculateMissingExpectedWarning();
        RecalculateCost();
    }

    // ── Command builders ──────────────────────────────────────────────────────
    public string[] GetSelectedFeatureIds()
    {
        var result = new List<string>();
        int count = _features.Count;
        for (int i = 0; i < count; i++)
            if (_features[i].IsSelected) result.Add(_features[i].FeatureId);
        return result.ToArray();
    }

    public ProductId[] GetSelectedPlatformIds()
    {
        int count = _selectedPlatformIds.Count;
        var result = new ProductId[count];
        for (int i = 0; i < count; i++)
            result[i] = _selectedPlatformIds[i];
        return result;
    }

    public ProductId[] GetSelectedToolIds()
    {
        if (_selectedTools.Count == 0) return null;
        var result = new ProductId[_selectedTools.Count];
        int idx = 0;
        foreach (var kvp in _selectedTools)
            result[idx++] = kvp.Value;
        return result;
    }

    public TeamAssignment[] GetTeamAssignments()
    {
        var result = new List<TeamAssignment>();
        foreach (var kvp in _teamAssignments)
            result.Add(new TeamAssignment { Role = kvp.Key, TeamId = kvp.Value });
        return result.ToArray();
    }

    public void RequestDismiss() { OnDismiss?.Invoke(); }

    // ── Private helpers ───────────────────────────────────────────────────────
    private void RecalculateCost()
    {
        float cost = _baseTemplateCost;
        int extraPlatforms = _selectedPlatformIds.Count > 1 ? _selectedPlatformIds.Count - 1 : 0;
        float platformMultiplier = 1f + extraPlatforms * 0.3f;
        float stanceMultiplier = _selectedStance == GenerationStance.CrossGen ? 1.5f : 1f;
        float hwDevCost = IsConsoleTemplate ? HardwareDevCostAdd : 0;
        CalculatedCost = (int)(cost * platformMultiplier * stanceMultiplier) + (int)hwDevCost + TotalSelectedUpfrontCost;
        CanAfford = _lastState != null && _lastState.Money >= CalculatedCost;
    }

    private void RecalculateCompletion()
    {
        if (_lastState == null) { EstimatedCompletionLabel = "Unknown"; return; }

        int unassigned = 0;
        int count = _requiredRoles.Count;
        for (int i = 0; i < count; i++)
            if (!_teamAssignments.ContainsKey(_requiredRoles[i])) unassigned++;

        if (unassigned > 0)
        {
            EstimatedCompletionLabel = "Unknown (" + unassigned + " role" + (unassigned > 1 ? "s" : "") + " unassigned)";
            EstimatedTotalSalaryCost = 0;
            EstimatedTotalCostLabel = "";
            return;
        }

        if (_selectedTemplate == null || _selectedTemplate.phases == null)
        {
            EstimatedCompletionLabel = "Unknown";
            EstimatedTotalSalaryCost = 0;
            EstimatedTotalCostLabel = "";
            return;
        }

        float baseWorkMultiplierEst = _lastState?.ProductBaseWorkMultiplier ?? 100f;

        float nicheDevTimeMult = 1f;
        if (_selectedTemplate.nicheConfigs != null && _selectedTemplate.nicheConfigs.Length > 0)
            nicheDevTimeMult = _lastState?.GetNicheDevTimeMultiplier(_selectedTemplate.nicheConfigs[0].niche) ?? 1f;

        int selectedFeatureCountEst = 0;
        if (_features != null) {
            int fCount = _features.Count;
            for (int fi = 0; fi < fCount; fi++)
                if (_features[fi].IsSelected) selectedFeatureCountEst++;
        }
        float difficultyScaleEst = 1.0f + (_selectedTemplate.difficultyTier - 1) * 0.75f;
        float featureScaleEst = 1.0f + selectedFeatureCountEst * 0.15f + (float)Math.Pow(selectedFeatureCountEst, 1.8) * 0.015f;

        int optimalTeamSizeEst = _selectedTemplate.optimalTeamSizePerPhase > 0 ? _selectedTemplate.optimalTeamSizePerPhase : 4;
        float totalTicks = 0f;
        int phaseCount = _selectedTemplate.phases.Length;
        for (int p = 0; p < phaseCount; p++)
        {
            var phase = _selectedTemplate.phases[p];
            float phaseWork = ComputePhaseWorkRequired(phase, baseWorkMultiplierEst, nicheDevTimeMult, difficultyScaleEst, featureScaleEst);
            if (!_teamAssignments.TryGetValue(phase.primaryRole, out var teamId)) continue;
            float phaseRate = EstimateTeamWorkPerTick(teamId, phase.phaseType, optimalTeamSizeEst);
            if (phaseRate > 0f)
                totalTicks += phaseWork / phaseRate;
        }

        if (totalTicks <= 0f) { EstimatedCompletionLabel = "Unknown"; EstimatedTotalSalaryCost = 0; EstimatedTotalCostLabel = ""; return; }

        int daysRequired = (int)(totalTicks / TimeState.TicksPerDay) + 1;
        _estimatedDevDays = Math.Max(1, daysRequired);
        EstimatedCompletionLabel = "~" + daysRequired + " day" + (daysRequired != 1 ? "s" : "");

        // Salary cost projection
        _uniqueEmployeeIds.Clear();
        long monthlySalarySum = 0;
        var activeTeams = _lastState.ActiveTeams;
        var activeEmployees = _lastState.ActiveEmployees;
        int teamCount = activeTeams.Count;
        int empCount = activeEmployees.Count;

        foreach (var teamId in _teamAssignments.Values)
        {
            for (int t = 0; t < teamCount; t++)
            {
                if (activeTeams[t].id != teamId) continue;
                var team = activeTeams[t];
                int memberCount = team.members != null ? team.members.Count : 0;
                for (int m = 0; m < memberCount; m++)
                {
                    EmployeeId memberId = team.members[m];
                    if (!_uniqueEmployeeIds.Add(memberId)) continue;
                    for (int e = 0; e < empCount; e++)
                    {
                        if (activeEmployees[e].id != memberId) continue;
                        monthlySalarySum += activeEmployees[e].salary;
                        break;
                    }
                }
                break;
            }
        }

        long dailySalary = monthlySalarySum / 30;
        EstimatedTotalSalaryCost = dailySalary * _estimatedDevDays;

        string upfrontStr = UIFormatting.FormatMoney(CalculatedCost);
        string salaryStr = UIFormatting.FormatMoney(EstimatedTotalSalaryCost);
        string totalStr = UIFormatting.FormatMoney(CalculatedCost + EstimatedTotalSalaryCost);
        EstimatedTotalCostLabel = upfrontStr + " upfront + ~" + salaryStr + " salaries = ~" + totalStr;
    }

    private float EstimateTeamWorkPerTick(TeamId teamId, ProductPhaseType phaseType, int optimalTeamSize)
    {
        if (_lastState == null) return 10f * 0.016f;

        SkillType skillType = TeamWorkEngine.MapPhaseToSkill(phaseType);

        var teams = _lastState.ActiveTeams;
        int tc = teams.Count;
        Team team = null;
        for (int i = 0; i < tc; i++)
        {
            if (teams[i].id == teamId) { team = teams[i]; break; }
        }
        if (team == null || team.members == null || team.members.Count == 0) return 10f * 0.016f;

        var employees = _lastState.ActiveEmployees;
        int ec = employees.Count;
        float totalSpeedSkill = 0f;
        int activeCount = 0;
        int memberCount = team.members.Count;

        for (int m = 0; m < memberCount; m++)
        {
            EmployeeId memberId = team.members[m];
            for (int e = 0; e < ec; e++)
            {
                if (employees[e].id != memberId) continue;
                var emp = employees[e];
                int sum = 0;
                int sc = emp.skills != null ? emp.skills.Length : 0;
                for (int k = 0; k < sc; k++) sum += emp.skills[k];
                float avg = sc > 0 ? (float)sum / sc : 5f;
                int ca = (int)(System.Math.Min(avg / 20f, 1f) * 200f);
                bool isRoleFit = TeamWorkEngine.IsRoleFitForSkill(emp.role, skillType);
                TeamWorkEngine.ComputeEffectiveSkills(
                    emp.GetSkill(skillType), ca, emp.morale,
                    emp.hiddenAttributes.WorkEthic,
                    emp.hiddenAttributes.Creative,
                    emp.hiddenAttributes.Adaptability,
                    isRoleFit,
                    out float speedSkill, out _);
                totalSpeedSkill += speedSkill;
                activeCount++;
                break;
            }
        }

        if (activeCount == 0) return 10f * 0.016f;

        float overhead = System.Math.Max(0.70f, 1f - 0.04f * (activeCount - 1));
        float coverageSpeedMod = TeamWorkEngine.ComputeCoverageSpeedMod(activeCount, optimalTeamSize);
        return totalSpeedSkill * 0.016f * overhead * coverageSpeedMod;
    }

    private float ComputePhaseWorkRequired(ProductPhaseDefinition phase, float baseWorkMultiplier, float nicheDevTimeMult, float difficultyScale, float featureScale)
    {
        return phase.baseWorkUnits * baseWorkMultiplier * nicheDevTimeMult * difficultyScale * featureScale;
    }

    private static string BuildPhaseSummary(ProductTemplateDefinition def)
    {
        if (def.phases == null || def.phases.Length == 0) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < def.phases.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatPhaseShort(def.phases[i].phaseType));
        }
        return sb.ToString();
    }

    private static string FormatPhaseShort(ProductPhaseType t)
    {
        switch (t)
        {
            case ProductPhaseType.Design:      return "Design";
            case ProductPhaseType.Programming: return "Prog";
            case ProductPhaseType.SFX:         return "SFX";
            case ProductPhaseType.VFX:         return "VFX";
            case ProductPhaseType.QA:          return "QA";
            default:                           return t.ToString();
        }
    }

    // ── Cross-product gate helpers ────────────────────────────────────────────

    private int ComputeTeamTotalSkill(TeamId teamId, SkillType skillType)
    {
        if (_lastState == null) return 0;
        var teams = _lastState.ActiveTeams;
        int tc = teams.Count;
        Team team = null;
        for (int i = 0; i < tc; i++)
        {
            if (teams[i].id == teamId) { team = teams[i]; break; }
        }
        if (team == null || team.members == null) return 0;

        var employees = _lastState.ActiveEmployees;
        int ec = employees.Count;
        int total = 0;
        int memberCount = team.members.Count;
        for (int m = 0; m < memberCount; m++)
        {
            EmployeeId memberId = team.members[m];
            for (int e = 0; e < ec; e++)
            {
                if (employees[e].id != memberId) continue;
                total += employees[e].GetSkill(skillType);
                break;
            }
        }
        return total;
    }

    private bool CheckSkillGate(ProductFeatureDefinition featDef, out string lockReason)
    {
        lockReason = "";
        if (featDef.requiredTotalSkillPoints <= 0) return false;

        SkillType requiredSkill = featDef.requiredSkillType;

        ProductTeamRole matchingRole;
        switch (requiredSkill)
        {
            case SkillType.Programming: matchingRole = ProductTeamRole.Programming; break;
            case SkillType.Design:      matchingRole = ProductTeamRole.Design;      break;
            case SkillType.QA:          matchingRole = ProductTeamRole.QA;          break;
            case SkillType.SFX:         matchingRole = ProductTeamRole.SFX;         break;
            case SkillType.VFX:         matchingRole = ProductTeamRole.VFX;         break;
            default:                    matchingRole = ProductTeamRole.Programming;  break;
        }

        if (!_teamAssignments.TryGetValue(matchingRole, out TeamId teamId)) {
            lockReason = "Assign a " + requiredSkill.ToString() + " team first";
            return true;
        }

        int totalSkill = ComputeTeamTotalSkill(teamId, requiredSkill);
        if (totalSkill < featDef.requiredTotalSkillPoints) {
            lockReason = "Requires " + featDef.requiredTotalSkillPoints + " total " + requiredSkill.ToString() + " skill";
            return true;
        }
        return false;
    }

    private void RefreshCrossProductGates()
    {
        if (_selectedTemplate == null || _selectedTemplate.availableFeatures == null) return;
        int count = _features.Count;
        for (int i = 0; i < count; i++) {
            var display = _features[i];
            if (display.IsPreSelected) continue;

            ProductFeatureDefinition featDef = null;
            int afc = _selectedTemplate.availableFeatures.Length;
            for (int f = 0; f < afc; f++) {
                var fd = _selectedTemplate.availableFeatures[f];
                if (fd != null && fd.featureId == display.FeatureId) { featDef = fd; break; }
            }
            if (featDef == null) continue;

            bool isLocked = false;
            string lockReason = "";
            string capLabel = "";

            if (_lastState != null) {
                if (!string.IsNullOrEmpty(featDef.requiresToolFeature)) {
                    bool toolHasFeature = HasUpstreamFeature(featDef.requiresToolFeature, isToolFeature: true);
                    if (!toolHasFeature) {
                        isLocked = true;
                        lockReason = "Requires " + GetUpstreamFeatureDisplayName(featDef.requiresToolFeature) + " on your selected tool.";
                    } else if (_gateConfig != null) {
                        float upstreamQuality = GetUpstreamFeatureQualityForDisplay(featDef.requiresToolFeature, isToolFeature: true);
                        float cap = _gateConfig.GetTierCeiling(upstreamQuality);
                        if (cap < 100f && cap > 0f)
                            capLabel = "Quality capped at " + cap.ToString("F0") + " (upstream quality: " + upstreamQuality.ToString("F0") + ")";
                    }
                }
                if (!isLocked && !string.IsNullOrEmpty(featDef.requiresPlatformFeature)) {
                    bool platformHasFeature = HasUpstreamFeature(featDef.requiresPlatformFeature, isToolFeature: false);
                    if (!platformHasFeature) {
                        isLocked = true;
                        lockReason = "Requires " + GetUpstreamFeatureDisplayName(featDef.requiresPlatformFeature) + " on your target platform.";
                    } else if (_gateConfig != null && string.IsNullOrEmpty(capLabel)) {
                        float upstreamQuality = GetUpstreamFeatureQualityForDisplay(featDef.requiresPlatformFeature, isToolFeature: false);
                        float cap = _gateConfig.GetTierCeiling(upstreamQuality);
                        if (cap < 100f && cap > 0f)
                            capLabel = "Quality capped at " + cap.ToString("F0") + " (upstream quality: " + upstreamQuality.ToString("F0") + ")";
                    }
                }
            }

            if (!isLocked) {
                string skillLockReason;
                if (CheckSkillGate(featDef, out skillLockReason)) {
                    isLocked = true;
                    lockReason = skillLockReason;
                }
            }

            display.IsLocked = isLocked;
            display.LockReason = lockReason;
            display.CapLabel = capLabel;
            if (isLocked && display.IsSelected) {
                display.IsSelected = false;
                _pricingDirty = true;
            }
            _features[i] = display;
        }
        RebuildFilteredList();
    }

    private bool HasUpstreamFeature(string requiredFeatureId, bool isToolFeature)
    {
        if (_lastState?.ShippedProducts == null) return false;
        if (isToolFeature) {
            foreach (var kvp in _selectedTools) {
                if (_lastState.ShippedProducts.TryGetValue(kvp.Value, out var tool)) {
                    if (tool.Features != null) {
                        int fc = tool.Features.Length;
                        for (int i = 0; i < fc; i++) {
                            if (tool.Features[i].FeatureId == requiredFeatureId && tool.Features[i].Quality > 0f)
                                return true;
                        }
                    }
                }
            }
        } else {
            int pc = _selectedPlatformIds.Count;
            for (int p = 0; p < pc; p++) {
                if (_lastState.ShippedProducts.TryGetValue(_selectedPlatformIds[p], out var platform)) {
                    if (platform.Features != null) {
                        int fc = platform.Features.Length;
                        for (int i = 0; i < fc; i++) {
                            if (platform.Features[i].FeatureId == requiredFeatureId && platform.Features[i].Quality > 0f)
                                return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private string GetUpstreamFeatureDisplayName(string featureId)
    {
        if (_allTemplates == null || string.IsNullOrEmpty(featureId)) return featureId ?? "";
        int tplCount = _allTemplates.Length;
        for (int t = 0; t < tplCount; t++) {
            var tpl = _allTemplates[t];
            if (tpl?.availableFeatures == null) continue;
            int fc = tpl.availableFeatures.Length;
            for (int f = 0; f < fc; f++) {
                var fd = tpl.availableFeatures[f];
                if (fd != null && fd.featureId == featureId)
                    return fd.displayName ?? featureId;
            }
        }
        return featureId;
    }

    private float GetUpstreamFeatureQualityForDisplay(string requiredFeatureId, bool isToolFeature)
    {
        if (_lastState?.ShippedProducts == null) return 0f;
        float best = 0f;
        if (isToolFeature) {
            foreach (var kvp in _selectedTools) {
                if (_lastState.ShippedProducts.TryGetValue(kvp.Value, out var tool)) {
                    if (tool.Features != null) {
                        int fc = tool.Features.Length;
                        for (int i = 0; i < fc; i++) {
                            if (tool.Features[i].FeatureId == requiredFeatureId && tool.Features[i].Quality > best)
                                best = tool.Features[i].Quality;
                        }
                    }
                }
            }
        } else {
            int pc = _selectedPlatformIds.Count;
            for (int p = 0; p < pc; p++) {
                if (_lastState.ShippedProducts.TryGetValue(_selectedPlatformIds[p], out var platform)) {
                    if (platform.Features != null) {
                        int fc = platform.Features.Length;
                        for (int i = 0; i < fc; i++) {
                            if (platform.Features[i].FeatureId == requiredFeatureId && platform.Features[i].Quality > best)
                                best = platform.Features[i].Quality;
                        }
                    }
                }
            }
        }
        return best;
    }
}
#endif
