using System;
using System.Collections.Generic;

/// <summary>
/// Central pure C# class holding all gameplay-tunable parameters as instance fields.
/// Injected into systems at construction time. Replaces scattered const fields.
/// Default values exactly match prior const values so behavior is unchanged.
/// </summary>
public class TuningConfig
{
    public event Action<string, object, object> OnParameterChanged; // name, oldVal, newVal

    // ── Contract System ─────────────────────────────────────────────────────────
    private float _workRatePerSkillPoint = 0.016f;
    public float WorkRatePerSkillPoint {
        get => _workRatePerSkillPoint;
        set { if (value != _workRatePerSkillPoint) { var o = _workRatePerSkillPoint; _workRatePerSkillPoint = value; OnParameterChanged?.Invoke("WorkRatePerSkillPoint", o, value); } }
    }

    // ── Morale System ───────────────────────────────────────────────────────────
    private float _defaultStartingMorale = 60f;
    public float DefaultStartingMorale {
        get => _defaultStartingMorale;
        set { if (value != _defaultStartingMorale) { var o = _defaultStartingMorale; _defaultStartingMorale = value; OnParameterChanged?.Invoke("DefaultStartingMorale", o, value); } }
    }

    private float _quitThreshold = 20f;
    public float QuitThreshold {
        get => _quitThreshold;
        set { if (value != _quitThreshold) { var o = _quitThreshold; _quitThreshold = value; OnParameterChanged?.Invoke("QuitThreshold", o, value); } }
    }

    private float _idleAlertMoraleThreshold = 50f;
    public float IdleAlertMoraleThreshold {
        get => _idleAlertMoraleThreshold;
        set { if (value != _idleAlertMoraleThreshold) { var o = _idleAlertMoraleThreshold; _idleAlertMoraleThreshold = value; OnParameterChanged?.Invoke("IdleAlertMoraleThreshold", o, value); } }
    }

    private float _quitChanceBase = 0.025f;
    public float QuitChanceBase {
        get => _quitChanceBase;
        set { if (value != _quitChanceBase) { var o = _quitChanceBase; _quitChanceBase = value; OnParameterChanged?.Invoke("QuitChanceBase", o, value); } }
    }

    private float _quitChanceAmbitionScale = 0.05f;
    public float QuitChanceAmbitionScale {
        get => _quitChanceAmbitionScale;
        set { if (value != _quitChanceAmbitionScale) { var o = _quitChanceAmbitionScale; _quitChanceAmbitionScale = value; OnParameterChanged?.Invoke("QuitChanceAmbitionScale", o, value); } }
    }

    private float _moraleMultiplierBase = 0.85f;
    public float MoraleMultiplierBase {
        get => _moraleMultiplierBase;
        set { if (value != _moraleMultiplierBase) { var o = _moraleMultiplierBase; _moraleMultiplierBase = value; OnParameterChanged?.Invoke("MoraleMultiplierBase", o, value); } }
    }

    private float _moraleMultiplierSpread = 0.25f;
    public float MoraleMultiplierSpread {
        get => _moraleMultiplierSpread;
        set { if (value != _moraleMultiplierSpread) { var o = _moraleMultiplierSpread; _moraleMultiplierSpread = value; OnParameterChanged?.Invoke("MoraleMultiplierSpread", o, value); } }
    }

    private int _contractCompletionBaseMoraleBonus = 5;
    public int ContractCompletionBaseMoraleBonus {
        get => _contractCompletionBaseMoraleBonus;
        set { if (value != _contractCompletionBaseMoraleBonus) { var o = _contractCompletionBaseMoraleBonus; _contractCompletionBaseMoraleBonus = value; OnParameterChanged?.Invoke("ContractCompletionBaseMoraleBonus", o, value); } }
    }

    private float _moraleWorkingBonus = 0.3f;
    public float MoraleWorkingBonus {
        get => _moraleWorkingBonus;
        set { if (value != _moraleWorkingBonus) { var o = _moraleWorkingBonus; _moraleWorkingBonus = value; OnParameterChanged?.Invoke("MoraleWorkingBonus", o, value); } }
    }

    private float _moraleDailyPenaltyFloor = 3.0f;
    public float MoraleDailyPenaltyFloor {
        get => _moraleDailyPenaltyFloor;
        set { if (value != _moraleDailyPenaltyFloor) { var o = _moraleDailyPenaltyFloor; _moraleDailyPenaltyFloor = value; OnParameterChanged?.Invoke("MoraleDailyPenaltyFloor", o, value); } }
    }

    private float _moraleEquilibriumTarget = 60f;
    public float MoraleEquilibriumTarget {
        get => _moraleEquilibriumTarget;
        set { if (value != _moraleEquilibriumTarget) { var o = _moraleEquilibriumTarget; _moraleEquilibriumTarget = value; OnParameterChanged?.Invoke("MoraleEquilibriumTarget", o, value); } }
    }

    private float _moraleEquilibriumCoefficient = 0.07f;
    public float MoraleEquilibriumCoefficient {
        get => _moraleEquilibriumCoefficient;
        set { if (value != _moraleEquilibriumCoefficient) { var o = _moraleEquilibriumCoefficient; _moraleEquilibriumCoefficient = value; OnParameterChanged?.Invoke("MoraleEquilibriumCoefficient", o, value); } }
    }

    private float _moraleOverloadSevere = 2.0f;
    public float MoraleOverloadSevere {
        get => _moraleOverloadSevere;
        set { if (value != _moraleOverloadSevere) { var o = _moraleOverloadSevere; _moraleOverloadSevere = value; OnParameterChanged?.Invoke("MoraleOverloadSevere", o, value); } }
    }

    private float _moraleOverloadModerate = 0.75f;
    public float MoraleOverloadModerate {
        get => _moraleOverloadModerate;
        set { if (value != _moraleOverloadModerate) { var o = _moraleOverloadModerate; _moraleOverloadModerate = value; OnParameterChanged?.Invoke("MoraleOverloadModerate", o, value); } }
    }

    private float _moraleOverloadMild = 0.25f;
    public float MoraleOverloadMild {
        get => _moraleOverloadMild;
        set { if (value != _moraleOverloadMild) { var o = _moraleOverloadMild; _moraleOverloadMild = value; OnParameterChanged?.Invoke("MoraleOverloadMild", o, value); } }
    }

    private float _moraleWrongFuncSevere = 1.5f;
    public float MoraleWrongFuncSevere {
        get => _moraleWrongFuncSevere;
        set { if (value != _moraleWrongFuncSevere) { var o = _moraleWrongFuncSevere; _moraleWrongFuncSevere = value; OnParameterChanged?.Invoke("MoraleWrongFuncSevere", o, value); } }
    }

    private float _moraleWrongFuncModerate = 0.75f;
    public float MoraleWrongFuncModerate {
        get => _moraleWrongFuncModerate;
        set { if (value != _moraleWrongFuncModerate) { var o = _moraleWrongFuncModerate; _moraleWrongFuncModerate = value; OnParameterChanged?.Invoke("MoraleWrongFuncModerate", o, value); } }
    }

    private float _moraleWrongFuncMild = 0.25f;
    public float MoraleWrongFuncMild {
        get => _moraleWrongFuncMild;
        set { if (value != _moraleWrongFuncMild) { var o = _moraleWrongFuncMild; _moraleWrongFuncMild = value; OnParameterChanged?.Invoke("MoraleWrongFuncMild", o, value); } }
    }

    private float _moraleSatisfactionFull = 1.5f;
    public float MoraleSatisfactionFull {
        get => _moraleSatisfactionFull;
        set { if (value != _moraleSatisfactionFull) { var o = _moraleSatisfactionFull; _moraleSatisfactionFull = value; OnParameterChanged?.Invoke("MoraleSatisfactionFull", o, value); } }
    }

    private float _moraleSatisfactionPartial = 0.75f;
    public float MoraleSatisfactionPartial {
        get => _moraleSatisfactionPartial;
        set { if (value != _moraleSatisfactionPartial) { var o = _moraleSatisfactionPartial; _moraleSatisfactionPartial = value; OnParameterChanged?.Invoke("MoraleSatisfactionPartial", o, value); } }
    }

    private float _moraleProductSatisfactionFull = 1.0f;
    public float MoraleProductSatisfactionFull {
        get => _moraleProductSatisfactionFull;
        set { if (value != _moraleProductSatisfactionFull) { var o = _moraleProductSatisfactionFull; _moraleProductSatisfactionFull = value; OnParameterChanged?.Invoke("MoraleProductSatisfactionFull", o, value); } }
    }

    private float _moraleProductSatisfactionPartial = 0.5f;
    public float MoraleProductSatisfactionPartial {
        get => _moraleProductSatisfactionPartial;
        set { if (value != _moraleProductSatisfactionPartial) { var o = _moraleProductSatisfactionPartial; _moraleProductSatisfactionPartial = value; OnParameterChanged?.Invoke("MoraleProductSatisfactionPartial", o, value); } }
    }

    private float _moraleProductOverloadSevere = 1.0f;
    public float MoraleProductOverloadSevere {
        get => _moraleProductOverloadSevere;
        set { if (value != _moraleProductOverloadSevere) { var o = _moraleProductOverloadSevere; _moraleProductOverloadSevere = value; OnParameterChanged?.Invoke("MoraleProductOverloadSevere", o, value); } }
    }

    private float _moraleProductOverloadModerate = 0.5f;
    public float MoraleProductOverloadModerate {
        get => _moraleProductOverloadModerate;
        set { if (value != _moraleProductOverloadModerate) { var o = _moraleProductOverloadModerate; _moraleProductOverloadModerate = value; OnParameterChanged?.Invoke("MoraleProductOverloadModerate", o, value); } }
    }

    private float _moraleProductOverloadMild = 0.1f;
    public float MoraleProductOverloadMild {
        get => _moraleProductOverloadMild;
        set { if (value != _moraleProductOverloadMild) { var o = _moraleProductOverloadMild; _moraleProductOverloadMild = value; OnParameterChanged?.Invoke("MoraleProductOverloadMild", o, value); } }
    }

    private int _moraleCompletionQualityHigh = 8;
    public int MoraleCompletionQualityHigh {
        get => _moraleCompletionQualityHigh;
        set { if (value != _moraleCompletionQualityHigh) { var o = _moraleCompletionQualityHigh; _moraleCompletionQualityHigh = value; OnParameterChanged?.Invoke("MoraleCompletionQualityHigh", o, value); } }
    }

    private int _moraleCompletionQualityMid = 5;
    public int MoraleCompletionQualityMid {
        get => _moraleCompletionQualityMid;
        set { if (value != _moraleCompletionQualityMid) { var o = _moraleCompletionQualityMid; _moraleCompletionQualityMid = value; OnParameterChanged?.Invoke("MoraleCompletionQualityMid", o, value); } }
    }

    private int _moraleCompletionQualityLow = 2;
    public int MoraleCompletionQualityLow {
        get => _moraleCompletionQualityLow;
        set { if (value != _moraleCompletionQualityLow) { var o = _moraleCompletionQualityLow; _moraleCompletionQualityLow = value; OnParameterChanged?.Invoke("MoraleCompletionQualityLow", o, value); } }
    }

    private float _moraleLowRecoveryThreshold = 30f;
    public float MoraleLowRecoveryThreshold {
        get => _moraleLowRecoveryThreshold;
        set { if (value != _moraleLowRecoveryThreshold) { var o = _moraleLowRecoveryThreshold; _moraleLowRecoveryThreshold = value; OnParameterChanged?.Invoke("MoraleLowRecoveryThreshold", o, value); } }
    }

    private float _moraleLowRecoveryBonus = 0.5f;
    public float MoraleLowRecoveryBonus {
        get => _moraleLowRecoveryBonus;
        set { if (value != _moraleLowRecoveryBonus) { var o = _moraleLowRecoveryBonus; _moraleLowRecoveryBonus = value; OnParameterChanged?.Invoke("MoraleLowRecoveryBonus", o, value); } }
    }

    private int _phaseCompletionBaseMoraleBonus = 3;
    public int PhaseCompletionBaseMoraleBonus {
        get => _phaseCompletionBaseMoraleBonus;
        set { if (value != _phaseCompletionBaseMoraleBonus) { var o = _phaseCompletionBaseMoraleBonus; _phaseCompletionBaseMoraleBonus = value; OnParameterChanged?.Invoke("PhaseCompletionBaseMoraleBonus", o, value); } }
    }

    private int _productShipBaseMoraleBonus = 8;
    public int ProductShipBaseMoraleBonus {
        get => _productShipBaseMoraleBonus;
        set { if (value != _productShipBaseMoraleBonus) { var o = _productShipBaseMoraleBonus; _productShipBaseMoraleBonus = value; OnParameterChanged?.Invoke("ProductShipBaseMoraleBonus", o, value); } }
    }

    private int _idleRecoveryStartDay = 3;
    public int IdleRecoveryStartDay {
        get => _idleRecoveryStartDay;
        set { if (value != _idleRecoveryStartDay) { var o = _idleRecoveryStartDay; _idleRecoveryStartDay = value; OnParameterChanged?.Invoke("IdleRecoveryStartDay", o, value); } }
    }

    private float _idleRecoveryBonus = 15f;
    public float IdleRecoveryBonus {
        get => _idleRecoveryBonus;
        set { if (value != _idleRecoveryBonus) { var o = _idleRecoveryBonus; _idleRecoveryBonus = value; OnParameterChanged?.Invoke("IdleRecoveryBonus", o, value); } }
    }

    private int _idleBoredomStartDay = 8;
    public int IdleBoredomStartDay {
        get => _idleBoredomStartDay;
        set { if (value != _idleBoredomStartDay) { var o = _idleBoredomStartDay; _idleBoredomStartDay = value; OnParameterChanged?.Invoke("IdleBoredomStartDay", o, value); } }
    }

    private float _idleBoredomDecayPerDay = 0.2f;
    public float IdleBoredomDecayPerDay {
        get => _idleBoredomDecayPerDay;
        set { if (value != _idleBoredomDecayPerDay) { var o = _idleBoredomDecayPerDay; _idleBoredomDecayPerDay = value; OnParameterChanged?.Invoke("IdleBoredomDecayPerDay", o, value); } }
    }

    private int _idleDecayStartDay = 61;
    public int IdleDecayStartDay {
        get => _idleDecayStartDay;
        set { if (value != _idleDecayStartDay) { var o = _idleDecayStartDay; _idleDecayStartDay = value; OnParameterChanged?.Invoke("IdleDecayStartDay", o, value); } }
    }

    private float _idleDecayPerDay = 0.5f;
    public float IdleDecayPerDay {
        get => _idleDecayPerDay;
        set { if (value != _idleDecayPerDay) { var o = _idleDecayPerDay; _idleDecayPerDay = value; OnParameterChanged?.Invoke("IdleDecayPerDay", o, value); } }
    }

    private float _idleDecayMax = 2.0f;
    public float IdleDecayMax {
        get => _idleDecayMax;
        set { if (value != _idleDecayMax) { var o = _idleDecayMax; _idleDecayMax = value; OnParameterChanged?.Invoke("IdleDecayMax", o, value); } }
    }

    // ── Skill Growth ────────────────────────────────────────────────────────────
    private int _maxXPPerContract = 2;
    public int MaxXPPerContract {
        get => _maxXPPerContract;
        set { if (value != _maxXPPerContract) { var o = _maxXPPerContract; _maxXPPerContract = value; OnParameterChanged?.Invoke("MaxXPPerContract", o, value); } }
    }

    private float _xpVarianceMin = 0.8f;
    public float XPVarianceMin {
        get => _xpVarianceMin;
        set { if (value != _xpVarianceMin) { var o = _xpVarianceMin; _xpVarianceMin = value; OnParameterChanged?.Invoke("XPVarianceMin", o, value); } }
    }

    private float _xpVarianceRange = 0.4f;
    public float XPVarianceRange {
        get => _xpVarianceRange;
        set { if (value != _xpVarianceRange) { var o = _xpVarianceRange; _xpVarianceRange = value; OnParameterChanged?.Invoke("XPVarianceRange", o, value); } }
    }

    // ── Loan System ─────────────────────────────────────────────────────────────
    private int _loanBaseAmount = 50000;
    public int LoanBaseAmount {
        get => _loanBaseAmount;
        set { if (value != _loanBaseAmount) { var o = _loanBaseAmount; _loanBaseAmount = value; OnParameterChanged?.Invoke("LoanBaseAmount", o, value); } }
    }

    private float _loanBaseInterestRate = 0.10f;
    public float LoanBaseInterestRate {
        get => _loanBaseInterestRate;
        set { if (value != _loanBaseInterestRate) { var o = _loanBaseInterestRate; _loanBaseInterestRate = value; OnParameterChanged?.Invoke("LoanBaseInterestRate", o, value); } }
    }

    private int _loanMinDurationMonths = 1;
    public int LoanMinDurationMonths {
        get => _loanMinDurationMonths;
        set { if (value != _loanMinDurationMonths) { var o = _loanMinDurationMonths; _loanMinDurationMonths = value; OnParameterChanged?.Invoke("LoanMinDurationMonths", o, value); } }
    }

    private int _loanMaxDurationMonths = 12;
    public int LoanMaxDurationMonths {
        get => _loanMaxDurationMonths;
        set { if (value != _loanMaxDurationMonths) { var o = _loanMaxDurationMonths; _loanMaxDurationMonths = value; OnParameterChanged?.Invoke("LoanMaxDurationMonths", o, value); } }
    }

    // ── Salary Calculator ───────────────────────────────────────────────────────
    private float _salaryHRMultiplier = 0.80f;
    public float SalaryHRMultiplier {
        get => _salaryHRMultiplier;
        set { if (value != _salaryHRMultiplier) { var o = _salaryHRMultiplier; _salaryHRMultiplier = value; OnParameterChanged?.Invoke("SalaryHRMultiplier", o, value); } }
    }

    private float _salaryInterviewMultiplier = 0.90f;
    public float SalaryInterviewMultiplier {
        get => _salaryInterviewMultiplier;
        set { if (value != _salaryInterviewMultiplier) { var o = _salaryInterviewMultiplier; _salaryInterviewMultiplier = value; OnParameterChanged?.Invoke("SalaryInterviewMultiplier", o, value); } }
    }

    private float _salaryDirectMultiplier = 1.15f;
    public float SalaryDirectMultiplier {
        get => _salaryDirectMultiplier;
        set { if (value != _salaryDirectMultiplier) { var o = _salaryDirectMultiplier; _salaryDirectMultiplier = value; OnParameterChanged?.Invoke("SalaryDirectMultiplier", o, value); } }
    }

    private float _salaryMaxNegotiationDiscount = 0.15f;
    public float SalaryMaxNegotiationDiscount {
        get => _salaryMaxNegotiationDiscount;
        set { if (value != _salaryMaxNegotiationDiscount) { var o = _salaryMaxNegotiationDiscount; _salaryMaxNegotiationDiscount = value; OnParameterChanged?.Invoke("SalaryMaxNegotiationDiscount", o, value); } }
    }

    private int _salaryMinimumWage = 500;
    public int SalaryMinimumWage {
        get => _salaryMinimumWage;
        set { if (value != _salaryMinimumWage) { var o = _salaryMinimumWage; _salaryMinimumWage = value; OnParameterChanged?.Invoke("SalaryMinimumWage", o, value); } }
    }

    // ── Ability Calculator ───────────────────────────────────────────────────────────
    private int _abilityGlobalMax = 200;
    public int AbilityGlobalMax {
        get => _abilityGlobalMax;
        set { if (value != _abilityGlobalMax) { var o = _abilityGlobalMax; _abilityGlobalMax = value; OnParameterChanged?.Invoke("AbilityGlobalMax", o, value); } }
    }

    private int _potentialGlobalMax = 200;
    public int PotentialGlobalMax {
        get => _potentialGlobalMax;
        set { if (value != _potentialGlobalMax) { var o = _potentialGlobalMax; _potentialGlobalMax = value; OnParameterChanged?.Invoke("PotentialGlobalMax", o, value); } }
    }

    // ── Reputation System ───────────────────────────────────────────────────────
    private int[] _reputationTierThresholds = { 0, 200, 1500, 5000, 15000 };
    public int[] ReputationTierThresholds {
        get => _reputationTierThresholds;
        set { _reputationTierThresholds = value; OnParameterChanged?.Invoke("ReputationTierThresholds", null, value); }
    }

    private int[] _reputationDifficultyCaps = { 1, 2, 3, 4, 5 };
    public int[] ReputationDifficultyCaps {
        get => _reputationDifficultyCaps;
        set { _reputationDifficultyCaps = value; OnParameterChanged?.Invoke("ReputationDifficultyCaps", null, value); }
    }

    private float[] _reputationCandidateQualityMultipliers = { 1.0f, 1.2f, 1.5f, 1.75f, 2.0f };
    public float[] ReputationCandidateQualityMultipliers {
        get => _reputationCandidateQualityMultipliers;
        set { _reputationCandidateQualityMultipliers = value; OnParameterChanged?.Invoke("ReputationCandidateQualityMultipliers", null, value); }
    }

    // ── Company Fans ────────────────────────────────────────────────────────────
    private float _fanConversionRate = 0.002f;
    public float FanConversionRate {
        get => _fanConversionRate;
        set { if (value != _fanConversionRate) { var o = _fanConversionRate; _fanConversionRate = value; OnParameterChanged?.Invoke("FanConversionRate", o, value); } }
    }

    private float _fanLaunchBonusDivisor = 50000f;
    public float FanLaunchBonusDivisor {
        get => _fanLaunchBonusDivisor;
        set { if (value != _fanLaunchBonusDivisor) { var o = _fanLaunchBonusDivisor; _fanLaunchBonusDivisor = value; OnParameterChanged?.Invoke("FanLaunchBonusDivisor", o, value); } }
    }

    private float _fanWomRate = 0.0001f;
    public float FanWomRate {
        get => _fanWomRate;
        set { if (value != _fanWomRate) { var o = _fanWomRate; _fanWomRate = value; OnParameterChanged?.Invoke("FanWomRate", o, value); } }
    }

    private float _fanDecayRateOnProductDeath = 0.15f;
    public float FanDecayRateOnProductDeath {
        get => _fanDecayRateOnProductDeath;
        set { if (value != _fanDecayRateOnProductDeath) { var o = _fanDecayRateOnProductDeath; _fanDecayRateOnProductDeath = value; OnParameterChanged?.Invoke("FanDecayRateOnProductDeath", o, value); } }
    }

    private float _fanIdleDecayRate = 0.05f;
    public float FanIdleDecayRate {
        get => _fanIdleDecayRate;
        set { if (value != _fanIdleDecayRate) { var o = _fanIdleDecayRate; _fanIdleDecayRate = value; OnParameterChanged?.Invoke("FanIdleDecayRate", o, value); } }
    }

    private float _fanMinQualityThreshold = 35f;
    public float FanMinQualityThreshold {
        get => _fanMinQualityThreshold;
        set { if (value != _fanMinQualityThreshold) { var o = _fanMinQualityThreshold; _fanMinQualityThreshold = value; OnParameterChanged?.Invoke("FanMinQualityThreshold", o, value); } }
    }

    // ── Customer Expectations ───────────────────────────────────────────────────
    private float _launchReputationBase = 5f;
    public float LaunchReputationBase {
        get => _launchReputationBase;
        set { if (value != _launchReputationBase) { var o = _launchReputationBase; _launchReputationBase = value; OnParameterChanged?.Invoke("LaunchReputationBase", o, value); } }
    }

    private float[] _customerExpectationPenalties = { 0f, 5f, 12f, 20f, 30f };
    public float[] CustomerExpectationPenalties {
        get => _customerExpectationPenalties;
        set { _customerExpectationPenalties = value; OnParameterChanged?.Invoke("CustomerExpectationPenalties", null, value); }
    }

    // ── Recommendation Labels ───────────────────────────────────────────────────
    private float _recommendationAbilityHighThreshold = 0.4f;
    public float RecommendationAbilityHighThreshold {
        get => _recommendationAbilityHighThreshold;
        set { if (value != _recommendationAbilityHighThreshold) { var o = _recommendationAbilityHighThreshold; _recommendationAbilityHighThreshold = value; OnParameterChanged?.Invoke("RecommendationAbilityHighThreshold", o, value); } }
    }

    private float _recommendationPotentialHighThreshold = 0.4f;
    public float RecommendationPotentialHighThreshold {
        get => _recommendationPotentialHighThreshold;
        set { if (value != _recommendationPotentialHighThreshold) { var o = _recommendationPotentialHighThreshold; _recommendationPotentialHighThreshold = value; OnParameterChanged?.Invoke("RecommendationPotentialHighThreshold", o, value); } }
    }

    // ── Contract Factory ────────────────────────────────────────────────────────
    private int _contractPreferredCategoryWeight = 70;
    public int ContractPreferredCategoryWeight {
        get => _contractPreferredCategoryWeight;
        set { if (value != _contractPreferredCategoryWeight) { var o = _contractPreferredCategoryWeight; _contractPreferredCategoryWeight = value; OnParameterChanged?.Invoke("ContractPreferredCategoryWeight", o, value); } }
    }

    private int _contractOtherCategoryTotalWeight = 30;
    public int ContractOtherCategoryTotalWeight {
        get => _contractOtherCategoryTotalWeight;
        set { if (value != _contractOtherCategoryTotalWeight) { var o = _contractOtherCategoryTotalWeight; _contractOtherCategoryTotalWeight = value; OnParameterChanged?.Invoke("ContractOtherCategoryTotalWeight", o, value); } }
    }

    // ── HR Search Config ────────────────────────────────────────────────────────
    private int _hrBaseSearchCost = 10000;
    public int HRBaseSearchCost {
        get => _hrBaseSearchCost;
        set { if (value != _hrBaseSearchCost) { var o = _hrBaseSearchCost; _hrBaseSearchCost = value; OnParameterChanged?.Invoke("HRBaseSearchCost", o, value); } }
    }

    private int _hrBaseDurationDays = 7;
    public int HRBaseDurationDays {
        get => _hrBaseDurationDays;
        set { if (value != _hrBaseDurationDays) { var o = _hrBaseDurationDays; _hrBaseDurationDays = value; OnParameterChanged?.Invoke("HRBaseDurationDays", o, value); } }
    }

    private int _hrMinDurationDays = 2;
    public int HRMinDurationDays {
        get => _hrMinDurationDays;
        set { if (value != _hrMinDurationDays) { var o = _hrMinDurationDays; _hrMinDurationDays = value; OnParameterChanged?.Invoke("HRMinDurationDays", o, value); } }
    }

    private float _hrBaseSuccessChance = 0.30f;
    public float HRBaseSuccessChance {
        get => _hrBaseSuccessChance;
        set { if (value != _hrBaseSuccessChance) { var o = _hrBaseSuccessChance; _hrBaseSuccessChance = value; OnParameterChanged?.Invoke("HRBaseSuccessChance", o, value); } }
    }

    private float _hrMaxSuccessChance = 0.95f;
    public float HRMaxSuccessChance {
        get => _hrMaxSuccessChance;
        set { if (value != _hrMaxSuccessChance) { var o = _hrMaxSuccessChance; _hrMaxSuccessChance = value; OnParameterChanged?.Invoke("HRMaxSuccessChance", o, value); } }
    }

    private float _hrSkillSuccessScaleFactor = 0.006f;
    public float HRSkillSuccessScaleFactor {
        get => _hrSkillSuccessScaleFactor;
        set { if (value != _hrSkillSuccessScaleFactor) { var o = _hrSkillSuccessScaleFactor; _hrSkillSuccessScaleFactor = value; OnParameterChanged?.Invoke("HRSkillSuccessScaleFactor", o, value); } }
    }

    private float _hrTeamSizeSpeedBonusPerMember = 0.08f;
    public float HRTeamSizeSpeedBonusPerMember {
        get => _hrTeamSizeSpeedBonusPerMember;
        set { if (value != _hrTeamSizeSpeedBonusPerMember) { var o = _hrTeamSizeSpeedBonusPerMember; _hrTeamSizeSpeedBonusPerMember = value; OnParameterChanged?.Invoke("HRTeamSizeSpeedBonusPerMember", o, value); } }
    }

    private int _hrMaxTeamSizeForSpeedBonus = 5;
    public int HRMaxTeamSizeForSpeedBonus {
        get => _hrMaxTeamSizeForSpeedBonus;
        set { if (value != _hrMaxTeamSizeForSpeedBonus) { var o = _hrMaxTeamSizeForSpeedBonus; _hrMaxTeamSizeForSpeedBonus = value; OnParameterChanged?.Invoke("HRMaxTeamSizeForSpeedBonus", o, value); } }
    }

    // ── Employee System ─────────────────────────────────────────────────────────
    private int _candidatePoolSize = 20;
    public int CandidatePoolSize {
        get => _candidatePoolSize;
        set { if (value != _candidatePoolSize) { var o = _candidatePoolSize; _candidatePoolSize = value; OnParameterChanged?.Invoke("CandidatePoolSize", o, value); } }
    }

    private int _candidateListMax = 40;
    public int CandidateListMax {
        get => _candidateListMax;
        set { if (value != _candidateListMax) { var o = _candidateListMax; _candidateListMax = value; OnParameterChanged?.Invoke("CandidateListMax", o, value); } }
    }

    private int _candidateRefreshIntervalDays = 15;
    public int CandidateRefreshIntervalDays {
        get => _candidateRefreshIntervalDays;
        set { if (value != _candidateRefreshIntervalDays) { var o = _candidateRefreshIntervalDays; _candidateRefreshIntervalDays = value; OnParameterChanged?.Invoke("CandidateRefreshIntervalDays", o, value); } }
    }

    private int _retirementAge = 65;
    public int RetirementAge {
        get => _retirementAge;
        set { if (value != _retirementAge) { var o = _retirementAge; _retirementAge = value; OnParameterChanged?.Invoke("RetirementAge", o, value); } }
    }

    private int _decayWindowStartAge = 55;
    public int DecayWindowStartAge {
        get => _decayWindowStartAge;
        set { if (value != _decayWindowStartAge) { var o = _decayWindowStartAge; _decayWindowStartAge = value; OnParameterChanged?.Invoke("DecayWindowStartAge", o, value); } }
    }

    private int _retirementCheckStartAge = 60;
    public int RetirementCheckStartAge {
        get => _retirementCheckStartAge;
        set { if (value != _retirementCheckStartAge) { var o = _retirementCheckStartAge; _retirementCheckStartAge = value; OnParameterChanged?.Invoke("RetirementCheckStartAge", o, value); } }
    }

    // ── Ability System ──────────────────────────────────────────────────────────
    private int _abilityFallbackPAMin = 60;
    public int AbilityFallbackPAMin {
        get => _abilityFallbackPAMin;
        set { if (value != _abilityFallbackPAMin) { var o = _abilityFallbackPAMin; _abilityFallbackPAMin = value; OnParameterChanged?.Invoke("AbilityFallbackPAMin", o, value); } }
    }

    private int _abilityFallbackPAMax = 151;
    public int AbilityFallbackPAMax {
        get => _abilityFallbackPAMax;
        set { if (value != _abilityFallbackPAMax) { var o = _abilityFallbackPAMax; _abilityFallbackPAMax = value; OnParameterChanged?.Invoke("AbilityFallbackPAMax", o, value); } }
    }

    private int _abilityTightHRThreshold = 14;
    public int AbilityTightHRThreshold {
        get => _abilityTightHRThreshold;
        set { if (value != _abilityTightHRThreshold) { var o = _abilityTightHRThreshold; _abilityTightHRThreshold = value; OnParameterChanged?.Invoke("AbilityTightHRThreshold", o, value); } }
    }

    private int _abilityMediumHRThreshold = 7;
    public int AbilityMediumHRThreshold {
        get => _abilityMediumHRThreshold;
        set { if (value != _abilityMediumHRThreshold) { var o = _abilityMediumHRThreshold; _abilityMediumHRThreshold = value; OnParameterChanged?.Invoke("AbilityMediumHRThreshold", o, value); } }
    }

    private int _abilityTightCABlur = 5;
    public int AbilityTightCABlur {
        get => _abilityTightCABlur;
        set { if (value != _abilityTightCABlur) { var o = _abilityTightCABlur; _abilityTightCABlur = value; OnParameterChanged?.Invoke("AbilityTightCABlur", o, value); } }
    }

    private int _abilityMediumCABlur = 12;
    public int AbilityMediumCABlur {
        get => _abilityMediumCABlur;
        set { if (value != _abilityMediumCABlur) { var o = _abilityMediumCABlur; _abilityMediumCABlur = value; OnParameterChanged?.Invoke("AbilityMediumCABlur", o, value); } }
    }

    private int _abilityWideCABlur = 25;
    public int AbilityWideCABlur {
        get => _abilityWideCABlur;
        set { if (value != _abilityWideCABlur) { var o = _abilityWideCABlur; _abilityWideCABlur = value; OnParameterChanged?.Invoke("AbilityWideCABlur", o, value); } }
    }

    // ── Finance System ──────────────────────────────────────────────────────────
    private int _financeBankruptDaysThreshold = 14;
    public int FinanceBankruptDaysThreshold {
        get => _financeBankruptDaysThreshold;
        set { if (value != _financeBankruptDaysThreshold) { var o = _financeBankruptDaysThreshold; _financeBankruptDaysThreshold = value; OnParameterChanged?.Invoke("FinanceBankruptDaysThreshold", o, value); } }
    }

    private int _financeBankruptMissedThreshold = 5;
    public int FinanceBankruptMissedThreshold {
        get => _financeBankruptMissedThreshold;
        set { if (value != _financeBankruptMissedThreshold) { var o = _financeBankruptMissedThreshold; _financeBankruptMissedThreshold = value; OnParameterChanged?.Invoke("FinanceBankruptMissedThreshold", o, value); } }
    }

    private int _financeInsolventDaysThreshold = 7;
    public int FinanceInsolventDaysThreshold {
        get => _financeInsolventDaysThreshold;
        set { if (value != _financeInsolventDaysThreshold) { var o = _financeInsolventDaysThreshold; _financeInsolventDaysThreshold = value; OnParameterChanged?.Invoke("FinanceInsolventDaysThreshold", o, value); } }
    }

    private int _financeInsolventMissedThreshold = 3;
    public int FinanceInsolventMissedThreshold {
        get => _financeInsolventMissedThreshold;
        set { if (value != _financeInsolventMissedThreshold) { var o = _financeInsolventMissedThreshold; _financeInsolventMissedThreshold = value; OnParameterChanged?.Invoke("FinanceInsolventMissedThreshold", o, value); } }
    }

    private int _financeDistressedDaysThreshold = 3;
    public int FinanceDistressedDaysThreshold {
        get => _financeDistressedDaysThreshold;
        set { if (value != _financeDistressedDaysThreshold) { var o = _financeDistressedDaysThreshold; _financeDistressedDaysThreshold = value; OnParameterChanged?.Invoke("FinanceDistressedDaysThreshold", o, value); } }
    }

    private int _financeTightRunwayThreshold = 5;
    public int FinanceTightRunwayThreshold {
        get => _financeTightRunwayThreshold;
        set { if (value != _financeTightRunwayThreshold) { var o = _financeTightRunwayThreshold; _financeTightRunwayThreshold = value; OnParameterChanged?.Invoke("FinanceTightRunwayThreshold", o, value); } }
    }

    // ── Recruitment Reputation ───────────────────────────────────────────────────
    private int _recruitRepDecayIntervalDays = 30;
    public int RecruitRepDecayIntervalDays {
        get => _recruitRepDecayIntervalDays;
        set { if (value != _recruitRepDecayIntervalDays) { var o = _recruitRepDecayIntervalDays; _recruitRepDecayIntervalDays = value; OnParameterChanged?.Invoke("RecruitRepDecayIntervalDays", o, value); } }
    }

    private int _recruitRepNeutralScore = 50;
    public int RecruitRepNeutralScore {
        get => _recruitRepNeutralScore;
        set { if (value != _recruitRepNeutralScore) { var o = _recruitRepNeutralScore; _recruitRepNeutralScore = value; OnParameterChanged?.Invoke("RecruitRepNeutralScore", o, value); } }
    }

    private int _recruitRepLoyaltyDays = 180;
    public int RecruitRepLoyaltyDays {
        get => _recruitRepLoyaltyDays;
        set { if (value != _recruitRepLoyaltyDays) { var o = _recruitRepLoyaltyDays; _recruitRepLoyaltyDays = value; OnParameterChanged?.Invoke("RecruitRepLoyaltyDays", o, value); } }
    }

    private int _recruitRepHireBonus = 2;
    public int RecruitRepHireBonus {
        get => _recruitRepHireBonus;
        set { if (value != _recruitRepHireBonus) { var o = _recruitRepHireBonus; _recruitRepHireBonus = value; OnParameterChanged?.Invoke("RecruitRepHireBonus", o, value); } }
    }

    private int _recruitRepFirePenalty = 5;
    public int RecruitRepFirePenalty {
        get => _recruitRepFirePenalty;
        set { if (value != _recruitRepFirePenalty) { var o = _recruitRepFirePenalty; _recruitRepFirePenalty = value; OnParameterChanged?.Invoke("RecruitRepFirePenalty", o, value); } }
    }

    private int _recruitRepQuitPenalty = 3;
    public int RecruitRepQuitPenalty {
        get => _recruitRepQuitPenalty;
        set { if (value != _recruitRepQuitPenalty) { var o = _recruitRepQuitPenalty; _recruitRepQuitPenalty = value; OnParameterChanged?.Invoke("RecruitRepQuitPenalty", o, value); } }
    }

    private int _recruitRepRejectPenalty = 1;
    public int RecruitRepRejectPenalty {
        get => _recruitRepRejectPenalty;
        set { if (value != _recruitRepRejectPenalty) { var o = _recruitRepRejectPenalty; _recruitRepRejectPenalty = value; OnParameterChanged?.Invoke("RecruitRepRejectPenalty", o, value); } }
    }

    private int _recruitRepLoyaltyBonus = 3;
    public int RecruitRepLoyaltyBonus {
        get => _recruitRepLoyaltyBonus;
        set { if (value != _recruitRepLoyaltyBonus) { var o = _recruitRepLoyaltyBonus; _recruitRepLoyaltyBonus = value; OnParameterChanged?.Invoke("RecruitRepLoyaltyBonus", o, value); } }
    }

    // ── Candidate Expiry ────────────────────────────────────────────────────────
    private int _candidateAutoExpiryDays = 15;
    public int CandidateAutoExpiryDays {
        get => _candidateAutoExpiryDays;
        set { if (value != _candidateAutoExpiryDays) { var o = _candidateAutoExpiryDays; _candidateAutoExpiryDays = value; OnParameterChanged?.Invoke("CandidateAutoExpiryDays", o, value); } }
    }

    private int _candidateHRExpiryDays = 20;
    public int CandidateHRExpiryDays {
        get => _candidateHRExpiryDays;
        set { if (value != _candidateHRExpiryDays) { var o = _candidateHRExpiryDays; _candidateHRExpiryDays = value; OnParameterChanged?.Invoke("CandidateHRExpiryDays", o, value); } }
    }

    private float _candidateUrgencyMediumThreshold = 0.50f;
    public float CandidateUrgencyMediumThreshold {
        get => _candidateUrgencyMediumThreshold;
        set { if (value != _candidateUrgencyMediumThreshold) { var o = _candidateUrgencyMediumThreshold; _candidateUrgencyMediumThreshold = value; OnParameterChanged?.Invoke("CandidateUrgencyMediumThreshold", o, value); } }
    }

    private float _candidateUrgencyHighThreshold = 0.25f;
    public float CandidateUrgencyHighThreshold {
        get => _candidateUrgencyHighThreshold;
        set { if (value != _candidateUrgencyHighThreshold) { var o = _candidateUrgencyHighThreshold; _candidateUrgencyHighThreshold = value; OnParameterChanged?.Invoke("CandidateUrgencyHighThreshold", o, value); } }
    }

    // ── Skill Growth (additional) ───────────────────────────────────────────────
    private float _skillSpilloverRateBase = 0.08f;
    public float SkillSpilloverRateBase {
        get => _skillSpilloverRateBase;
        set { if (value != _skillSpilloverRateBase) { var o = _skillSpilloverRateBase; _skillSpilloverRateBase = value; OnParameterChanged?.Invoke("SkillSpilloverRateBase", o, value); } }
    }

    private float _skillSpilloverRateSpread = 0.12f;
    public float SkillSpilloverRateSpread {
        get => _skillSpilloverRateSpread;
        set { if (value != _skillSpilloverRateSpread) { var o = _skillSpilloverRateSpread; _skillSpilloverRateSpread = value; OnParameterChanged?.Invoke("SkillSpilloverRateSpread", o, value); } }
    }

    private int[] _skillAgeDecayBrackets = { 24, 29, 34, 39, 44, 49 };
    public int[] SkillAgeDecayBrackets {
        get => _skillAgeDecayBrackets;
        set { _skillAgeDecayBrackets = value; OnParameterChanged?.Invoke("SkillAgeDecayBrackets", null, value); }
    }

    private float[] _skillAgeDecayMultipliers = { 1.0f, 0.9f, 0.78f, 0.65f, 0.52f, 0.40f, 0.30f };
    public float[] SkillAgeDecayMultipliers {
        get => _skillAgeDecayMultipliers;
        set { _skillAgeDecayMultipliers = value; OnParameterChanged?.Invoke("SkillAgeDecayMultipliers", null, value); }
    }

    // ── Upgrade System ──────────────────────────────────────────────────────────
    private int[] _upgradeTierResearchTicks = { 0, 14400, 28800, 72000, 144000, 288000, 480000 };
    public int[] UpgradeTierResearchTicks {
        get => _upgradeTierResearchTicks;
        set { _upgradeTierResearchTicks = value; OnParameterChanged?.Invoke("UpgradeTierResearchTicks", null, value); }
    }

    // ── Product Work Rate ───────────────────────────────────────────────────────
    private float _teamOverheadPerMember = 0.04f;
    public float TeamOverheadPerMember {
        get => _teamOverheadPerMember;
        set { if (value != _teamOverheadPerMember) { var o = _teamOverheadPerMember; _teamOverheadPerMember = value; OnParameterChanged?.Invoke("TeamOverheadPerMember", o, value); } }
    }

    [System.Obsolete("Use TeamOverheadPerMember instead.")]
    private float _productTeamOverheadFactor = 0.3f;
    [System.Obsolete("Use TeamOverheadPerMember instead.")]
    public float ProductTeamOverheadFactor {
        get => _productTeamOverheadFactor;
        set { if (value != _productTeamOverheadFactor) { var o = _productTeamOverheadFactor; _productTeamOverheadFactor = value; OnParameterChanged?.Invoke("ProductTeamOverheadFactor", o, value); } }
    }

    [System.Obsolete("Per-employee WorkEthic is applied directly in TeamWorkEngine (0.90-1.10).")]
    private float _productWorkEthicMinMult = 0.7f;
    [System.Obsolete("Per-employee WorkEthic is applied directly in TeamWorkEngine (0.90-1.10).")]
    public float ProductWorkEthicMinMult {
        get => _productWorkEthicMinMult;
        set { if (value != _productWorkEthicMinMult) { var o = _productWorkEthicMinMult; _productWorkEthicMinMult = value; OnParameterChanged?.Invoke("ProductWorkEthicMinMult", o, value); } }
    }

    [System.Obsolete("Per-employee WorkEthic is applied directly in TeamWorkEngine (0.90-1.10).")]
    private float _productWorkEthicMaxMult = 1.3f;
    [System.Obsolete("Per-employee WorkEthic is applied directly in TeamWorkEngine (0.90-1.10).")]
    public float ProductWorkEthicMaxMult {
        get => _productWorkEthicMaxMult;
        set { if (value != _productWorkEthicMaxMult) { var o = _productWorkEthicMaxMult; _productWorkEthicMaxMult = value; OnParameterChanged?.Invoke("ProductWorkEthicMaxMult", o, value); } }
    }

    private float _productBaseWorkMultiplier = 100f;
    public float ProductBaseWorkMultiplier {
        get => _productBaseWorkMultiplier;
        set { if (value != _productBaseWorkMultiplier) { var o = _productBaseWorkMultiplier; _productBaseWorkMultiplier = value; OnParameterChanged?.Invoke("ProductBaseWorkMultiplier", o, value); } }
    }

    private int _maxXPPerProductShip = 3;
    public int MaxXPPerProductShip {
        get => _maxXPPerProductShip;
        set { if (value != _maxXPPerProductShip) { var o = _maxXPPerProductShip; _maxXPPerProductShip = value; OnParameterChanged?.Invoke("MaxXPPerProductShip", o, value); } }
    }

    private int _productXPDurationCapDays = 180;
    public int ProductXPDurationCapDays {
        get => _productXPDurationCapDays;
        set { if (value != _productXPDurationCapDays) { var o = _productXPDurationCapDays; _productXPDurationCapDays = value; OnParameterChanged?.Invoke("ProductXPDurationCapDays", o, value); } }
    }

    // ── Marketing / Hype ─────────────────────────────────────────────────────────
    private float _hypePassiveGainPerDay = 0.8f;
    public float HypePassiveGainPerDay {
        get => _hypePassiveGainPerDay;
        set { if (value != _hypePassiveGainPerDay) { var o = _hypePassiveGainPerDay; _hypePassiveGainPerDay = value; OnParameterChanged?.Invoke("HypePassiveGainPerDay", o, value); } }
    }

    private float _hypeBudgetReferenceCost = 5000f;
    public float HypeBudgetReferenceCost {
        get => _hypeBudgetReferenceCost;
        set { if (value != _hypeBudgetReferenceCost) { var o = _hypeBudgetReferenceCost; _hypeBudgetReferenceCost = value; OnParameterChanged?.Invoke("HypeBudgetReferenceCost", o, value); } }
    }

    private float _scopeComplexityExponent = 0.6f;
    public float ScopeComplexityExponent {
        get => _scopeComplexityExponent;
        set { if (value != _scopeComplexityExponent) { var o = _scopeComplexityExponent; _scopeComplexityExponent = value; OnParameterChanged?.Invoke("ScopeComplexityExponent", o, value); } }
    }

    private float _scopeBugRatePerFeature = 0.08f;
    public float ScopeBugRatePerFeature {
        get => _scopeBugRatePerFeature;
        set { if (value != _scopeBugRatePerFeature) { var o = _scopeBugRatePerFeature; _scopeBugRatePerFeature = value; OnParameterChanged?.Invoke("ScopeBugRatePerFeature", o, value); } }
    }

    private int _hypeCampaignBaseCost = 500;
    public int HypeCampaignBaseCost {
        get => _hypeCampaignBaseCost;
        set { if (value != _hypeCampaignBaseCost) { var o = _hypeCampaignBaseCost; _hypeCampaignBaseCost = value; OnParameterChanged?.Invoke("HypeCampaignBaseCost", o, value); } }
    }

    private float _hypeCampaignBaseGain = 5.0f;
    public float HypeCampaignBaseGain {
        get => _hypeCampaignBaseGain;
        set { if (value != _hypeCampaignBaseGain) { var o = _hypeCampaignBaseGain; _hypeCampaignBaseGain = value; OnParameterChanged?.Invoke("HypeCampaignBaseGain", o, value); } }
    }

    private int _hypeCampaignCooldownTicks = 33600;
    public int HypeCampaignCooldownTicks {
        get => _hypeCampaignCooldownTicks;
        set { if (value != _hypeCampaignCooldownTicks) { var o = _hypeCampaignCooldownTicks; _hypeCampaignCooldownTicks = value; OnParameterChanged?.Invoke("HypeCampaignCooldownTicks", o, value); } }
    }

    private float _hypeDiminishingFactor = 0.02f;
    public float HypeDiminishingFactor {
        get => _hypeDiminishingFactor;
        set { if (value != _hypeDiminishingFactor) { var o = _hypeDiminishingFactor; _hypeDiminishingFactor = value; OnParameterChanged?.Invoke("HypeDiminishingFactor", o, value); } }
    }

    private int _hypeDecayGracePeriodDays = 30;
    public int HypeDecayGracePeriodDays {
        get => _hypeDecayGracePeriodDays;
        set { if (value != _hypeDecayGracePeriodDays) { var o = _hypeDecayGracePeriodDays; _hypeDecayGracePeriodDays = value; OnParameterChanged?.Invoke("HypeDecayGracePeriodDays", o, value); } }
    }

    private float _hypeDecayPerDay = 0.3f;
    public float HypeDecayPerDay {
        get => _hypeDecayPerDay;
        set { if (value != _hypeDecayPerDay) { var o = _hypeDecayPerDay; _hypeDecayPerDay = value; OnParameterChanged?.Invoke("HypeDecayPerDay", o, value); } }
    }

    private float _hypeDecayRampDays = 30.0f;
    public float HypeDecayRampDays {
        get => _hypeDecayRampDays;
        set { if (value != _hypeDecayRampDays) { var o = _hypeDecayRampDays; _hypeDecayRampDays = value; OnParameterChanged?.Invoke("HypeDecayRampDays", o, value); } }
    }

    private float _hypeDecayMaxPerDay = 1.5f;
    public float HypeDecayMaxPerDay {
        get => _hypeDecayMaxPerDay;
        set { if (value != _hypeDecayMaxPerDay) { var o = _hypeDecayMaxPerDay; _hypeDecayMaxPerDay = value; OnParameterChanged?.Invoke("HypeDecayMaxPerDay", o, value); } }
    }

    private float _hypeMaxBonus = 2.0f;
    public float HypeMaxBonus {
        get => _hypeMaxBonus;
        set { if (value != _hypeMaxBonus) { var o = _hypeMaxBonus; _hypeMaxBonus = value; OnParameterChanged?.Invoke("HypeMaxBonus", o, value); } }
    }

    private float _hypeExpectationScale = 0.75f;
    public float HypeExpectationScale {
        get => _hypeExpectationScale;
        set { if (value != _hypeExpectationScale) { var o = _hypeExpectationScale; _hypeExpectationScale = value; OnParameterChanged?.Invoke("HypeExpectationScale", o, value); } }
    }

    private float _hypeRepPenaltyPerPoint = 0.5f;
    public float HypeRepPenaltyPerPoint {
        get => _hypeRepPenaltyPerPoint;
        set { if (value != _hypeRepPenaltyPerPoint) { var o = _hypeRepPenaltyPerPoint; _hypeRepPenaltyPerPoint = value; OnParameterChanged?.Invoke("HypeRepPenaltyPerPoint", o, value); } }
    }

    private float _hypeSentimentPenaltyScale = 0.03f;
    public float HypeSentimentPenaltyScale {
        get => _hypeSentimentPenaltyScale;
        set { if (value != _hypeSentimentPenaltyScale) { var o = _hypeSentimentPenaltyScale; _hypeSentimentPenaltyScale = value; OnParameterChanged?.Invoke("HypeSentimentPenaltyScale", o, value); } }
    }

    private float _hypeMeetsExpectationsBonus = 3.0f;
    public float HypeMeetsExpectationsBonus {
        get => _hypeMeetsExpectationsBonus;
        set { if (value != _hypeMeetsExpectationsBonus) { var o = _hypeMeetsExpectationsBonus; _hypeMeetsExpectationsBonus = value; OnParameterChanged?.Invoke("HypeMeetsExpectationsBonus", o, value); } }
    }

    public float[] HypeTierEfficiencyMults = { 0.6f, 0.8f, 1.0f, 1.3f, 1.6f };
    public float[] HypeCategorySensitivity = { 1.5f, 1.0f, 0.5f, 0.3f, 0.8f, 0.4f, 0.2f };

    // ── Post-Launch Marketing ─────────────────────────────────────────────────────
    private int _adCooldownTicks = 144000;
    public int AdCooldownTicks {
        get => _adCooldownTicks;
        set { if (value != _adCooldownTicks) { var o = _adCooldownTicks; _adCooldownTicks = value; OnParameterChanged?.Invoke("AdCooldownTicks", o, value); } }
    }

    private int _adDurationTicks = 67200;
    public int AdDurationTicks {
        get => _adDurationTicks;
        set { if (value != _adDurationTicks) { var o = _adDurationTicks; _adDurationTicks = value; OnParameterChanged?.Invoke("AdDurationTicks", o, value); } }
    }

    private float _adPopularityGainPerDay = 1.5f;
    public float AdPopularityGainPerDay {
        get => _adPopularityGainPerDay;
        set { if (value != _adPopularityGainPerDay) { var o = _adPopularityGainPerDay; _adPopularityGainPerDay = value; OnParameterChanged?.Invoke("AdPopularityGainPerDay", o, value); } }
    }

    private float _postLaunchHypeDecayPerDay = 0.5f;
    public float PostLaunchHypeDecayPerDay {
        get => _postLaunchHypeDecayPerDay;
        set { if (value != _postLaunchHypeDecayPerDay) { var o = _postLaunchHypeDecayPerDay; _postLaunchHypeDecayPerDay = value; OnParameterChanged?.Invoke("PostLaunchHypeDecayPerDay", o, value); } }
    }

    private float _adUserAcquisitionRate = 0.01f;
    public float AdUserAcquisitionRate {
        get => _adUserAcquisitionRate;
        set { if (value != _adUserAcquisitionRate) { var o = _adUserAcquisitionRate; _adUserAcquisitionRate = value; OnParameterChanged?.Invoke("AdUserAcquisitionRate", o, value); } }
    }

    private float _updateHypePassiveGainPerDay = 0.5f;
    public float UpdateHypePassiveGainPerDay {
        get => _updateHypePassiveGainPerDay;
        set { if (value != _updateHypePassiveGainPerDay) { var o = _updateHypePassiveGainPerDay; _updateHypePassiveGainPerDay = value; OnParameterChanged?.Invoke("UpdateHypePassiveGainPerDay", o, value); } }
    }

    private float _updateHypeMaxBonus = 1.5f;
    public float UpdateHypeMaxBonus {
        get => _updateHypeMaxBonus;
        set { if (value != _updateHypeMaxBonus) { var o = _updateHypeMaxBonus; _updateHypeMaxBonus = value; OnParameterChanged?.Invoke("UpdateHypeMaxBonus", o, value); } }
    }

    private int _updateHypeDecayGraceDays = 60;
    public int UpdateHypeDecayGraceDays {
        get => _updateHypeDecayGraceDays;
        set { if (value != _updateHypeDecayGraceDays) { var o = _updateHypeDecayGraceDays; _updateHypeDecayGraceDays = value; OnParameterChanged?.Invoke("UpdateHypeDecayGraceDays", o, value); } }
    }

    private float _updateBrokenPromiseSentimentPenalty = 2.0f;
    public float UpdateBrokenPromiseSentimentPenalty {
        get => _updateBrokenPromiseSentimentPenalty;
        set { if (value != _updateBrokenPromiseSentimentPenalty) { var o = _updateBrokenPromiseSentimentPenalty; _updateBrokenPromiseSentimentPenalty = value; OnParameterChanged?.Invoke("UpdateBrokenPromiseSentimentPenalty", o, value); } }
    }

    private float _maintenanceBugFixBaseRate = 0.4f;
    public float MaintenanceBugFixBaseRate {
        get => _maintenanceBugFixBaseRate;
        set { if (value != _maintenanceBugFixBaseRate) { var o = _maintenanceBugFixBaseRate; _maintenanceBugFixBaseRate = value; OnParameterChanged?.Invoke("MaintenanceBugFixBaseRate", o, value); } }
    }

    private float _unmaintainedBugGrowthUserScale = 0.05f;
    public float UnmaintainedBugGrowthUserScale {
        get => _unmaintainedBugGrowthUserScale;
        set { if (value != _unmaintainedBugGrowthUserScale) { var o = _unmaintainedBugGrowthUserScale; _unmaintainedBugGrowthUserScale = value; OnParameterChanged?.Invoke("UnmaintainedBugGrowthUserScale", o, value); } }
    }

    private float _unmaintainedBugGrowthAgePenaltyRate = 0.01f;
    public float UnmaintainedBugGrowthAgePenaltyRate {
        get => _unmaintainedBugGrowthAgePenaltyRate;
        set { if (value != _unmaintainedBugGrowthAgePenaltyRate) { var o = _unmaintainedBugGrowthAgePenaltyRate; _unmaintainedBugGrowthAgePenaltyRate = value; OnParameterChanged?.Invoke("UnmaintainedBugGrowthAgePenaltyRate", o, value); } }
    }

    private float _contractMisfitXPRate = 0.25f;
    public float ContractMisfitXPRate {
        get => _contractMisfitXPRate;
        set { if (value != _contractMisfitXPRate) { var o = _contractMisfitXPRate; _contractMisfitXPRate = value; OnParameterChanged?.Invoke("ContractMisfitXPRate", o, value); } }
    }

    private float _contractNativeXPRate = 0.15f;
    public float ContractNativeXPRate {
        get => _contractNativeXPRate;
        set { if (value != _contractNativeXPRate) { var o = _contractNativeXPRate; _contractNativeXPRate = value; OnParameterChanged?.Invoke("ContractNativeXPRate", o, value); } }
    }

    private float _productPhaseXPPerDay = 0.15f;
    public float ProductPhaseXPPerDay {
        get => _productPhaseXPPerDay;
        set { if (value != _productPhaseXPPerDay) { var o = _productPhaseXPPerDay; _productPhaseXPPerDay = value; OnParameterChanged?.Invoke("ProductPhaseXPPerDay", o, value); } }
    }

    private float _productPhaseMisfitXPRate = 0.25f;
    public float ProductPhaseMisfitXPRate {
        get => _productPhaseMisfitXPRate;
        set { if (value != _productPhaseMisfitXPRate) { var o = _productPhaseMisfitXPRate; _productPhaseMisfitXPRate = value; OnParameterChanged?.Invoke("ProductPhaseMisfitXPRate", o, value); } }
    }

    private float _productPhaseNativeXPRate = 0.15f;
    public float ProductPhaseNativeXPRate {
        get => _productPhaseNativeXPRate;
        set { if (value != _productPhaseNativeXPRate) { var o = _productPhaseNativeXPRate; _productPhaseNativeXPRate = value; OnParameterChanged?.Invoke("ProductPhaseNativeXPRate", o, value); } }
    }

    private float _marketingXPPerMonth = 0.3f;
    public float MarketingXPPerMonth {
        get => _marketingXPPerMonth;
        set { if (value != _marketingXPPerMonth) { var o = _marketingXPPerMonth; _marketingXPPerMonth = value; OnParameterChanged?.Invoke("MarketingXPPerMonth", o, value); } }
    }

    private float _marketingXPPerBoost = 0.5f;
    public float MarketingXPPerBoost {
        get => _marketingXPPerBoost;
        set { if (value != _marketingXPPerBoost) { var o = _marketingXPPerBoost; _marketingXPPerBoost = value; OnParameterChanged?.Invoke("MarketingXPPerBoost", o, value); } }
    }

    private float _marketingXPPerAdRun = 0.5f;
    public float MarketingXPPerAdRun {
        get => _marketingXPPerAdRun;
        set { if (value != _marketingXPPerAdRun) { var o = _marketingXPPerAdRun; _marketingXPPerAdRun = value; OnParameterChanged?.Invoke("MarketingXPPerAdRun", o, value); } }
    }

    private float _marketingXPPerEventMitigation = 0.3f;
    public float MarketingXPPerEventMitigation {
        get => _marketingXPPerEventMitigation;
        set { if (value != _marketingXPPerEventMitigation) { var o = _marketingXPPerEventMitigation; _marketingXPPerEventMitigation = value; OnParameterChanged?.Invoke("MarketingXPPerEventMitigation", o, value); } }
    }

    private float _hypeEventBaseChance = 0.10f;
    public float HypeEventBaseChance {
        get => _hypeEventBaseChance;
        set { if (value != _hypeEventBaseChance) { var o = _hypeEventBaseChance; _hypeEventBaseChance = value; OnParameterChanged?.Invoke("HypeEventBaseChance", o, value); } }
    }

    private int _hypeEventCooldownTicks = 144000;
    public int HypeEventCooldownTicks {
        get => _hypeEventCooldownTicks;
        set { if (value != _hypeEventCooldownTicks) { var o = _hypeEventCooldownTicks; _hypeEventCooldownTicks = value; OnParameterChanged?.Invoke("HypeEventCooldownTicks", o, value); } }
    }

    // ── Product Crisis System ───────────────────────────────────────────────────
    private int _crisisThresholdMonths1 = 3;
    public int CrisisThresholdMonths1 {
        get => _crisisThresholdMonths1;
        set { if (value != _crisisThresholdMonths1) { var o = _crisisThresholdMonths1; _crisisThresholdMonths1 = value; OnParameterChanged?.Invoke("CrisisThresholdMonths1", o, value); } }
    }

    private int _crisisThresholdMonths2 = 6;
    public int CrisisThresholdMonths2 {
        get => _crisisThresholdMonths2;
        set { if (value != _crisisThresholdMonths2) { var o = _crisisThresholdMonths2; _crisisThresholdMonths2 = value; OnParameterChanged?.Invoke("CrisisThresholdMonths2", o, value); } }
    }

    private int _crisisThresholdMonths3 = 9;
    public int CrisisThresholdMonths3 {
        get => _crisisThresholdMonths3;
        set { if (value != _crisisThresholdMonths3) { var o = _crisisThresholdMonths3; _crisisThresholdMonths3 = value; OnParameterChanged?.Invoke("CrisisThresholdMonths3", o, value); } }
    }

    private float _crisisMinorChurnMultiplier = 1.10f;
    public float CrisisMinorChurnMultiplier {
        get => _crisisMinorChurnMultiplier;
        set { if (value != _crisisMinorChurnMultiplier) { var o = _crisisMinorChurnMultiplier; _crisisMinorChurnMultiplier = value; OnParameterChanged?.Invoke("CrisisMinorChurnMultiplier", o, value); } }
    }

    private float _crisisModerateChurnMultiplier = 1.25f;
    public float CrisisModerateChurnMultiplier {
        get => _crisisModerateChurnMultiplier;
        set { if (value != _crisisModerateChurnMultiplier) { var o = _crisisModerateChurnMultiplier; _crisisModerateChurnMultiplier = value; OnParameterChanged?.Invoke("CrisisModerateChurnMultiplier", o, value); } }
    }

    private float _crisisBaseChancePerMonth = 0.05f;
    public float CrisisBaseChancePerMonth {
        get => _crisisBaseChancePerMonth;
        set { if (value != _crisisBaseChancePerMonth) { var o = _crisisBaseChancePerMonth; _crisisBaseChancePerMonth = value; OnParameterChanged?.Invoke("CrisisBaseChancePerMonth", o, value); } }
    }

    private float _crisisChanceEscalationPerMonth = 0.04f;
    public float CrisisChanceEscalationPerMonth {
        get => _crisisChanceEscalationPerMonth;
        set { if (value != _crisisChanceEscalationPerMonth) { var o = _crisisChanceEscalationPerMonth; _crisisChanceEscalationPerMonth = value; OnParameterChanged?.Invoke("CrisisChanceEscalationPerMonth", o, value); } }
    }

    // ── Release Date System ──────────────────────────────────────────────────────
    private float _rushCostScale = 1.0f;
    public float RushCostScale {
        get => _rushCostScale;
        set { if (value != _rushCostScale) { var o = _rushCostScale; _rushCostScale = value; OnParameterChanged?.Invoke("RushCostScale", o, value); } }
    }

    private float _rushFanSentimentPenaltyBase = 5.0f;
    public float RushFanSentimentPenaltyBase {
        get => _rushFanSentimentPenaltyBase;
        set { if (value != _rushFanSentimentPenaltyBase) { var o = _rushFanSentimentPenaltyBase; _rushFanSentimentPenaltyBase = value; OnParameterChanged?.Invoke("RushFanSentimentPenaltyBase", o, value); } }
    }

    private float _delayHypeDecayPerDay = 0.1f;
    public float DelayHypeDecayPerDay {
        get => _delayHypeDecayPerDay;
        set { if (value != _delayHypeDecayPerDay) { var o = _delayHypeDecayPerDay; _delayHypeDecayPerDay = value; OnParameterChanged?.Invoke("DelayHypeDecayPerDay", o, value); } }
    }

    private float _delayRepLossBase = 3.0f;
    public float DelayRepLossBase {
        get => _delayRepLossBase;
        set { if (value != _delayRepLossBase) { var o = _delayRepLossBase; _delayRepLossBase = value; OnParameterChanged?.Invoke("DelayRepLossBase", o, value); } }
    }

    private float _delayRepLossCompounding = 1.5f;
    public float DelayRepLossCompounding {
        get => _delayRepLossCompounding;
        set { if (value != _delayRepLossCompounding) { var o = _delayRepLossCompounding; _delayRepLossCompounding = value; OnParameterChanged?.Invoke("DelayRepLossCompounding", o, value); } }
    }

    private float _delayFanTrustErosionScale = 0.5f;
    public float DelayFanTrustErosionScale {
        get => _delayFanTrustErosionScale;
        set { if (value != _delayFanTrustErosionScale) { var o = _delayFanTrustErosionScale; _delayFanTrustErosionScale = value; OnParameterChanged?.Invoke("DelayFanTrustErosionScale", o, value); } }
    }

    // ── Product Lifecycle ────────────────────────────────────────────────────────
    private float _popularityConvergenceRate = 0.12f;
    public float PopularityConvergenceRate {
        get => _popularityConvergenceRate;
        set { if (value != _popularityConvergenceRate) { var o = _popularityConvergenceRate; _popularityConvergenceRate = value; OnParameterChanged?.Invoke("PopularityConvergenceRate", o, value); } }
    }

    private int _minGrowthStageDays = 60;
    public int MinGrowthStageDays {
        get => _minGrowthStageDays;
        set { if (value != _minGrowthStageDays) { var o = _minGrowthStageDays; _minGrowthStageDays = value; OnParameterChanged?.Invoke("MinGrowthStageDays", o, value); } }
    }

    private int _minPlateauStageDays = 30;
    public int MinPlateauStageDays {
        get => _minPlateauStageDays;
        set { if (value != _minPlateauStageDays) { var o = _minPlateauStageDays; _minPlateauStageDays = value; OnParameterChanged?.Invoke("MinPlateauStageDays", o, value); } }
    }

    // ── Difficulty Multipliers ───────────────────────────────────────────────────
    private float _contractRewardMultiplier = 1.0f;
    public float ContractRewardMultiplier {
        get => _contractRewardMultiplier;
        set { if (value != _contractRewardMultiplier) { var o = _contractRewardMultiplier; _contractRewardMultiplier = value; OnParameterChanged?.Invoke("ContractRewardMultiplier", o, value); } }
    }

    private float _salaryGlobalMultiplier = 1.0f;
    public float SalaryGlobalMultiplier {
        get => _salaryGlobalMultiplier;
        set { if (value != _salaryGlobalMultiplier) { var o = _salaryGlobalMultiplier; _salaryGlobalMultiplier = value; OnParameterChanged?.Invoke("SalaryGlobalMultiplier", o, value); } }
    }

    private float _competitorAggressionMultiplier = 1.0f;
    public float CompetitorAggressionMultiplier {
        get => _competitorAggressionMultiplier;
        set { if (value != _competitorAggressionMultiplier) { var o = _competitorAggressionMultiplier; _competitorAggressionMultiplier = value; OnParameterChanged?.Invoke("CompetitorAggressionMultiplier", o, value); } }
    }

    private float _marketDifficultyMultiplier = 1.0f;
    public float MarketDifficultyMultiplier {
        get => _marketDifficultyMultiplier;
        set { if (value != _marketDifficultyMultiplier) { var o = _marketDifficultyMultiplier; _marketDifficultyMultiplier = value; OnParameterChanged?.Invoke("MarketDifficultyMultiplier", o, value); } }
    }

    private float _productRevenueMultiplier = 1.0f;
    public float ProductRevenueMultiplier {
        get => _productRevenueMultiplier;
        set { if (value != _productRevenueMultiplier) { var o = _productRevenueMultiplier; _productRevenueMultiplier = value; OnParameterChanged?.Invoke("ProductRevenueMultiplier", o, value); } }
    }

    private float _bugRateMultiplier = 1.0f;
    public float BugRateMultiplier {
        get => _bugRateMultiplier;
        set { if (value != _bugRateMultiplier) { var o = _bugRateMultiplier; _bugRateMultiplier = value; OnParameterChanged?.Invoke("BugRateMultiplier", o, value); } }
    }

    private float _reviewHarshnessMultiplier = 1.0f;
    public float ReviewHarshnessMultiplier {
        get => _reviewHarshnessMultiplier;
        set { if (value != _reviewHarshnessMultiplier) { var o = _reviewHarshnessMultiplier; _reviewHarshnessMultiplier = value; OnParameterChanged?.Invoke("ReviewHarshnessMultiplier", o, value); } }
    }

    // ─── API ───────────────────────────────────────────────────────────────────

    /// <summary>Sets a parameter by name and fires OnParameterChanged.</summary>
    public void SetParameter(string name, object value)
    {
        switch (name)
        {
            case "WorkRatePerSkillPoint":              WorkRatePerSkillPoint              = Convert.ToSingle(value); break;
            case "DefaultStartingMorale":              DefaultStartingMorale              = Convert.ToSingle(value); break;
            case "QuitThreshold":                      QuitThreshold                      = Convert.ToSingle(value); break;
            case "IdleAlertMoraleThreshold":           IdleAlertMoraleThreshold           = Convert.ToSingle(value); break;
            case "QuitChanceBase":                     QuitChanceBase                     = Convert.ToSingle(value); break;
            case "QuitChanceAmbitionScale":            QuitChanceAmbitionScale            = Convert.ToSingle(value); break;
            case "MoraleMultiplierBase":               MoraleMultiplierBase               = Convert.ToSingle(value); break;
            case "MoraleMultiplierSpread":             MoraleMultiplierSpread             = Convert.ToSingle(value); break;
            case "ContractCompletionBaseMoraleBonus":  ContractCompletionBaseMoraleBonus  = Convert.ToInt32(value);  break;
            case "MoraleWorkingBonus":                 MoraleWorkingBonus                 = Convert.ToSingle(value); break;
            case "MoraleDailyPenaltyFloor":            MoraleDailyPenaltyFloor            = Convert.ToSingle(value); break;
            case "MaxXPPerContract":                   MaxXPPerContract                   = Convert.ToInt32(value);  break;
            case "XPVarianceMin":                      XPVarianceMin                      = Convert.ToSingle(value); break;
            case "XPVarianceRange":                    XPVarianceRange                    = Convert.ToSingle(value); break;
            case "LoanBaseAmount":                     LoanBaseAmount                     = Convert.ToInt32(value);  break;
            case "LoanBaseInterestRate":               LoanBaseInterestRate               = Convert.ToSingle(value); break;
            case "LoanMinDurationMonths":              LoanMinDurationMonths              = Convert.ToInt32(value);  break;
            case "LoanMaxDurationMonths":              LoanMaxDurationMonths              = Convert.ToInt32(value);  break;
            case "SalaryHRMultiplier":                 SalaryHRMultiplier                 = Convert.ToSingle(value); break;
            case "SalaryInterviewMultiplier":          SalaryInterviewMultiplier          = Convert.ToSingle(value); break;
            case "SalaryDirectMultiplier":             SalaryDirectMultiplier             = Convert.ToSingle(value); break;
            case "SalaryMaxNegotiationDiscount":       SalaryMaxNegotiationDiscount       = Convert.ToSingle(value); break;
            case "SalaryMinimumWage":                  SalaryMinimumWage                  = Convert.ToInt32(value);  break;
            case "AbilityGlobalMax":                      AbilityGlobalMax                      = Convert.ToInt32(value);  break;
            case "PotentialGlobalMax":                     PotentialGlobalMax                     = Convert.ToInt32(value);  break;
            case "RecommendationAbilityHighThreshold":     RecommendationAbilityHighThreshold     = Convert.ToSingle(value); break;
            case "RecommendationPotentialHighThreshold":   RecommendationPotentialHighThreshold   = Convert.ToSingle(value); break;
            case "ContractPreferredCategoryWeight":    ContractPreferredCategoryWeight     = Convert.ToInt32(value);  break;
            case "ContractOtherCategoryTotalWeight":   ContractOtherCategoryTotalWeight   = Convert.ToInt32(value);  break;
            case "HRBaseSearchCost":                   HRBaseSearchCost                   = Convert.ToInt32(value);  break;
            case "HRBaseDurationDays":                 HRBaseDurationDays                 = Convert.ToInt32(value);  break;
            case "HRMinDurationDays":                  HRMinDurationDays                  = Convert.ToInt32(value);  break;
            case "HRBaseSuccessChance":                HRBaseSuccessChance                = Convert.ToSingle(value); break;
            case "HRMaxSuccessChance":                 HRMaxSuccessChance                 = Convert.ToSingle(value); break;
            case "HRSkillSuccessScaleFactor":          HRSkillSuccessScaleFactor          = Convert.ToSingle(value); break;
            case "HRTeamSizeSpeedBonusPerMember":      HRTeamSizeSpeedBonusPerMember      = Convert.ToSingle(value); break;
            case "HRMaxTeamSizeForSpeedBonus":         HRMaxTeamSizeForSpeedBonus         = Convert.ToInt32(value);  break;
            case "CandidatePoolSize":                  CandidatePoolSize                  = Convert.ToInt32(value);  break;
            case "CandidateListMax":                   CandidateListMax                   = Convert.ToInt32(value);  break;
            case "CandidateRefreshIntervalDays":       CandidateRefreshIntervalDays       = Convert.ToInt32(value);  break;
            case "RetirementAge":                      RetirementAge                      = Convert.ToInt32(value);  break;
            case "DecayWindowStartAge":                DecayWindowStartAge                = Convert.ToInt32(value);  break;
            case "RetirementCheckStartAge":            RetirementCheckStartAge            = Convert.ToInt32(value);  break;
            case "AbilityFallbackPAMin":               AbilityFallbackPAMin               = Convert.ToInt32(value);  break;
            case "AbilityFallbackPAMax":               AbilityFallbackPAMax               = Convert.ToInt32(value);  break;
            case "AbilityTightHRThreshold":            AbilityTightHRThreshold            = Convert.ToInt32(value);  break;
            case "AbilityMediumHRThreshold":           AbilityMediumHRThreshold           = Convert.ToInt32(value);  break;
            case "AbilityTightCABlur":                 AbilityTightCABlur                 = Convert.ToInt32(value);  break;
            case "AbilityMediumCABlur":                AbilityMediumCABlur                = Convert.ToInt32(value);  break;
            case "AbilityWideCABlur":                  AbilityWideCABlur                  = Convert.ToInt32(value);  break;
            case "FinanceBankruptDaysThreshold":       FinanceBankruptDaysThreshold       = Convert.ToInt32(value);  break;
            case "FinanceBankruptMissedThreshold":     FinanceBankruptMissedThreshold     = Convert.ToInt32(value);  break;
            case "FinanceInsolventDaysThreshold":      FinanceInsolventDaysThreshold      = Convert.ToInt32(value);  break;
            case "FinanceInsolventMissedThreshold":    FinanceInsolventMissedThreshold    = Convert.ToInt32(value);  break;
            case "FinanceDistressedDaysThreshold":     FinanceDistressedDaysThreshold     = Convert.ToInt32(value);  break;
            case "FinanceTightRunwayThreshold":        FinanceTightRunwayThreshold        = Convert.ToInt32(value);  break;
            case "RecruitRepDecayIntervalDays":        RecruitRepDecayIntervalDays        = Convert.ToInt32(value);  break;
            case "RecruitRepNeutralScore":             RecruitRepNeutralScore             = Convert.ToInt32(value);  break;
            case "RecruitRepLoyaltyDays":              RecruitRepLoyaltyDays              = Convert.ToInt32(value);  break;
            case "RecruitRepHireBonus":                RecruitRepHireBonus                = Convert.ToInt32(value);  break;
            case "RecruitRepFirePenalty":              RecruitRepFirePenalty              = Convert.ToInt32(value);  break;
            case "RecruitRepQuitPenalty":              RecruitRepQuitPenalty              = Convert.ToInt32(value);  break;
            case "RecruitRepRejectPenalty":            RecruitRepRejectPenalty            = Convert.ToInt32(value);  break;
            case "RecruitRepLoyaltyBonus":             RecruitRepLoyaltyBonus             = Convert.ToInt32(value);  break;
            case "CandidateAutoExpiryDays":            CandidateAutoExpiryDays            = Convert.ToInt32(value);  break;
            case "CandidateHRExpiryDays":              CandidateHRExpiryDays              = Convert.ToInt32(value);  break;
            case "CandidateUrgencyMediumThreshold":    CandidateUrgencyMediumThreshold    = Convert.ToSingle(value); break;
            case "CandidateUrgencyHighThreshold":      CandidateUrgencyHighThreshold      = Convert.ToSingle(value); break;
            case "SkillSpilloverRateBase":             SkillSpilloverRateBase             = Convert.ToSingle(value); break;
            case "SkillSpilloverRateSpread":           SkillSpilloverRateSpread           = Convert.ToSingle(value); break;
            case "TeamOverheadPerMember":          TeamOverheadPerMember          = Convert.ToSingle(value); break;
            case "ProductTeamOverheadFactor":          ProductTeamOverheadFactor          = Convert.ToSingle(value); break;
            case "ProductWorkEthicMinMult":            ProductWorkEthicMinMult            = Convert.ToSingle(value); break;
            case "ProductWorkEthicMaxMult":            ProductWorkEthicMaxMult            = Convert.ToSingle(value); break;
            case "ProductBaseWorkMultiplier":          ProductBaseWorkMultiplier          = Convert.ToSingle(value); break;
            case "MaxXPPerProductShip":                MaxXPPerProductShip                = Convert.ToInt32(value);  break;
            case "ProductXPDurationCapDays":           ProductXPDurationCapDays           = Convert.ToInt32(value);  break;
            case "HypePassiveGainPerDay":               HypePassiveGainPerDay               = Convert.ToSingle(value); break;
            case "HypeBudgetReferenceCost":             HypeBudgetReferenceCost             = Convert.ToSingle(value); break;
            case "ScopeComplexityExponent":             ScopeComplexityExponent             = Convert.ToSingle(value); break;
            case "ScopeBugRatePerFeature":              ScopeBugRatePerFeature              = Convert.ToSingle(value); break;
            case "HypeCampaignBaseCost":                HypeCampaignBaseCost                = Convert.ToInt32(value);  break;
            case "HypeCampaignBaseGain":                HypeCampaignBaseGain                = Convert.ToSingle(value); break;
            case "HypeCampaignCooldownTicks":           HypeCampaignCooldownTicks           = Convert.ToInt32(value);  break;
            case "HypeDiminishingFactor":               HypeDiminishingFactor               = Convert.ToSingle(value); break;
            case "HypeDecayGracePeriodDays":            HypeDecayGracePeriodDays            = Convert.ToInt32(value);  break;
            case "HypeDecayPerDay":                     HypeDecayPerDay                     = Convert.ToSingle(value); break;
            case "HypeDecayRampDays":                   HypeDecayRampDays                   = Convert.ToSingle(value); break;
            case "HypeDecayMaxPerDay":                  HypeDecayMaxPerDay                  = Convert.ToSingle(value); break;
            case "HypeMaxBonus":                        HypeMaxBonus                        = Convert.ToSingle(value); break;
            case "HypeExpectationScale":                HypeExpectationScale                = Convert.ToSingle(value); break;
            case "HypeRepPenaltyPerPoint":              HypeRepPenaltyPerPoint              = Convert.ToSingle(value); break;
            case "HypeSentimentPenaltyScale":           HypeSentimentPenaltyScale           = Convert.ToSingle(value); break;
            case "HypeMeetsExpectationsBonus":          HypeMeetsExpectationsBonus          = Convert.ToSingle(value); break;
            case "AdCooldownTicks":                      AdCooldownTicks                      = Convert.ToInt32(value);  break;
            case "AdDurationTicks":                      AdDurationTicks                      = Convert.ToInt32(value);  break;
            case "AdPopularityGainPerDay":               AdPopularityGainPerDay               = Convert.ToSingle(value); break;
            case "AdUserAcquisitionRate":                AdUserAcquisitionRate                = Convert.ToSingle(value); break;
            case "UpdateHypePassiveGainPerDay":          UpdateHypePassiveGainPerDay          = Convert.ToSingle(value); break;
            case "UpdateHypeMaxBonus":                   UpdateHypeMaxBonus                   = Convert.ToSingle(value); break;
            case "UpdateHypeDecayGraceDays":             UpdateHypeDecayGraceDays             = Convert.ToInt32(value);  break;
            case "UpdateBrokenPromiseSentimentPenalty":  UpdateBrokenPromiseSentimentPenalty  = Convert.ToSingle(value); break;
            case "MarketingXPPerMonth":                  MarketingXPPerMonth                  = Convert.ToSingle(value); break;
            case "MarketingXPPerBoost":                  MarketingXPPerBoost                  = Convert.ToSingle(value); break;
            case "MarketingXPPerAdRun":                  MarketingXPPerAdRun                  = Convert.ToSingle(value); break;
            case "MarketingXPPerEventMitigation":        MarketingXPPerEventMitigation        = Convert.ToSingle(value); break;
            case "HypeEventBaseChance":                  HypeEventBaseChance                  = Convert.ToSingle(value); break;
            case "HypeEventCooldownTicks":               HypeEventCooldownTicks               = Convert.ToInt32(value);  break;
            case "CrisisThresholdMonths1":               CrisisThresholdMonths1               = Convert.ToInt32(value);  break;
            case "CrisisThresholdMonths2":               CrisisThresholdMonths2               = Convert.ToInt32(value);  break;
            case "CrisisThresholdMonths3":               CrisisThresholdMonths3               = Convert.ToInt32(value);  break;
            case "CrisisMinorChurnMultiplier":           CrisisMinorChurnMultiplier           = Convert.ToSingle(value); break;
            case "CrisisModerateChurnMultiplier":        CrisisModerateChurnMultiplier        = Convert.ToSingle(value); break;
            case "RushCostScale":                        RushCostScale                        = Convert.ToSingle(value); break;
            case "RushFanSentimentPenaltyBase":          RushFanSentimentPenaltyBase          = Convert.ToSingle(value); break;
            case "DelayHypeDecayPerDay":                 DelayHypeDecayPerDay                 = Convert.ToSingle(value); break;
            case "DelayRepLossBase":                     DelayRepLossBase                     = Convert.ToSingle(value); break;
            case "DelayRepLossCompounding":              DelayRepLossCompounding              = Convert.ToSingle(value); break;
            case "DelayFanTrustErosionScale":            DelayFanTrustErosionScale            = Convert.ToSingle(value); break;
            case "MaintenanceBugFixBaseRate":            MaintenanceBugFixBaseRate            = Convert.ToSingle(value); break;
            case "UnmaintainedBugGrowthUserScale":       UnmaintainedBugGrowthUserScale       = Convert.ToSingle(value); break;
            case "UnmaintainedBugGrowthAgePenaltyRate":  UnmaintainedBugGrowthAgePenaltyRate  = Convert.ToSingle(value); break;
            case "ContractMisfitXPRate":                 ContractMisfitXPRate                 = Convert.ToSingle(value); break;
            case "ContractNativeXPRate":                 ContractNativeXPRate                 = Convert.ToSingle(value); break;
            case "ProductPhaseXPPerDay":                 ProductPhaseXPPerDay                 = Convert.ToSingle(value); break;
            case "ProductPhaseMisfitXPRate":             ProductPhaseMisfitXPRate             = Convert.ToSingle(value); break;
            case "ProductPhaseNativeXPRate":             ProductPhaseNativeXPRate             = Convert.ToSingle(value); break;
            case "PopularityConvergenceRate":            PopularityConvergenceRate            = Convert.ToSingle(value); break;
            case "MinGrowthStageDays":                   MinGrowthStageDays                   = Convert.ToInt32(value);  break;
            case "MinPlateauStageDays":                  MinPlateauStageDays                  = Convert.ToInt32(value);  break;
            case "MoraleLowRecoveryThreshold":           MoraleLowRecoveryThreshold           = Convert.ToSingle(value); break;
            case "MoraleLowRecoveryBonus":               MoraleLowRecoveryBonus               = Convert.ToSingle(value); break;
            case "PhaseCompletionBaseMoraleBonus":       PhaseCompletionBaseMoraleBonus       = Convert.ToInt32(value);  break;
            case "ProductShipBaseMoraleBonus":           ProductShipBaseMoraleBonus           = Convert.ToInt32(value);  break;
            case "IdleRecoveryStartDay":                 IdleRecoveryStartDay                 = Convert.ToInt32(value);  break;
            case "IdleRecoveryBonus":                    IdleRecoveryBonus                    = Convert.ToSingle(value); break;
            case "IdleBoredomStartDay":                  IdleBoredomStartDay                  = Convert.ToInt32(value);  break;
            case "IdleBoredomDecayPerDay":               IdleBoredomDecayPerDay               = Convert.ToSingle(value); break;
            case "IdleDecayStartDay":                    IdleDecayStartDay                    = Convert.ToInt32(value);  break;
            case "IdleDecayPerDay":                      IdleDecayPerDay                      = Convert.ToSingle(value); break;
            case "IdleDecayMax":                         IdleDecayMax                         = Convert.ToSingle(value); break;
            case "ContractRewardMultiplier":             ContractRewardMultiplier             = Convert.ToSingle(value); break;
            case "SalaryGlobalMultiplier":               SalaryGlobalMultiplier               = Convert.ToSingle(value); break;
            case "CompetitorAggressionMultiplier":       CompetitorAggressionMultiplier       = Convert.ToSingle(value); break;
            case "MarketDifficultyMultiplier":           MarketDifficultyMultiplier           = Convert.ToSingle(value); break;
            case "ProductRevenueMultiplier":             ProductRevenueMultiplier             = Convert.ToSingle(value); break;
            case "BugRateMultiplier":                    BugRateMultiplier                    = Convert.ToSingle(value); break;
            case "ReviewHarshnessMultiplier":            ReviewHarshnessMultiplier            = Convert.ToSingle(value); break;
        }
    }

    /// <summary>Returns a snapshot of all scalar parameters as Dictionary for the inspector.</summary>
    public Dictionary<string, object> GetAllParameters()
    {
        return new Dictionary<string, object>
        {
            { "WorkRatePerSkillPoint",              WorkRatePerSkillPoint },
            { "DefaultStartingMorale",              DefaultStartingMorale },
            { "QuitThreshold",                      QuitThreshold },
            { "IdleAlertMoraleThreshold",           IdleAlertMoraleThreshold },
            { "QuitChanceBase",                     QuitChanceBase },
            { "QuitChanceAmbitionScale",            QuitChanceAmbitionScale },
            { "MoraleMultiplierBase",               MoraleMultiplierBase },
            { "MoraleMultiplierSpread",             MoraleMultiplierSpread },
            { "ContractCompletionBaseMoraleBonus",  ContractCompletionBaseMoraleBonus },
            { "MoraleWorkingBonus",                 MoraleWorkingBonus },
            { "MoraleDailyPenaltyFloor",            MoraleDailyPenaltyFloor },
            { "MaxXPPerContract",                   MaxXPPerContract },
            { "XPVarianceMin",                      XPVarianceMin },
            { "XPVarianceRange",                    XPVarianceRange },
            { "LoanBaseAmount",                     LoanBaseAmount },
            { "LoanBaseInterestRate",               LoanBaseInterestRate },
            { "LoanMinDurationMonths",              LoanMinDurationMonths },
            { "LoanMaxDurationMonths",              LoanMaxDurationMonths },
            { "SalaryHRMultiplier",                 SalaryHRMultiplier },
            { "SalaryInterviewMultiplier",          SalaryInterviewMultiplier },
            { "SalaryDirectMultiplier",             SalaryDirectMultiplier },
            { "SalaryMaxNegotiationDiscount",       SalaryMaxNegotiationDiscount },
            { "SalaryMinimumWage",                  SalaryMinimumWage },
            { "AbilityGlobalMax",                      AbilityGlobalMax },
            { "PotentialGlobalMax",                     PotentialGlobalMax },
            { "RecommendationAbilityHighThreshold",     RecommendationAbilityHighThreshold },
            { "RecommendationPotentialHighThreshold",   RecommendationPotentialHighThreshold },
            { "ContractPreferredCategoryWeight",    ContractPreferredCategoryWeight },
            { "ContractOtherCategoryTotalWeight",   ContractOtherCategoryTotalWeight },
            { "HRBaseSearchCost",                   HRBaseSearchCost },
            { "HRBaseDurationDays",                 HRBaseDurationDays },
            { "HRMinDurationDays",                  HRMinDurationDays },
            { "HRBaseSuccessChance",                HRBaseSuccessChance },
            { "HRMaxSuccessChance",                 HRMaxSuccessChance },
            { "HRSkillSuccessScaleFactor",          HRSkillSuccessScaleFactor },
            { "HRTeamSizeSpeedBonusPerMember",      HRTeamSizeSpeedBonusPerMember },
            { "HRMaxTeamSizeForSpeedBonus",         HRMaxTeamSizeForSpeedBonus },
            { "CandidatePoolSize",                  CandidatePoolSize },
            { "CandidateListMax",                   CandidateListMax },
            { "CandidateRefreshIntervalDays",       CandidateRefreshIntervalDays },
            { "RetirementAge",                      RetirementAge },
            { "DecayWindowStartAge",                DecayWindowStartAge },
            { "RetirementCheckStartAge",            RetirementCheckStartAge },
            { "AbilityFallbackPAMin",               AbilityFallbackPAMin },
            { "AbilityFallbackPAMax",               AbilityFallbackPAMax },
            { "AbilityTightHRThreshold",            AbilityTightHRThreshold },
            { "AbilityMediumHRThreshold",           AbilityMediumHRThreshold },
            { "AbilityTightCABlur",                 AbilityTightCABlur },
            { "AbilityMediumCABlur",                AbilityMediumCABlur },
            { "AbilityWideCABlur",                  AbilityWideCABlur },
            { "FinanceBankruptDaysThreshold",       FinanceBankruptDaysThreshold },
            { "FinanceBankruptMissedThreshold",     FinanceBankruptMissedThreshold },
            { "FinanceInsolventDaysThreshold",      FinanceInsolventDaysThreshold },
            { "FinanceInsolventMissedThreshold",    FinanceInsolventMissedThreshold },
            { "FinanceDistressedDaysThreshold",     FinanceDistressedDaysThreshold },
            { "FinanceTightRunwayThreshold",        FinanceTightRunwayThreshold },
            { "RecruitRepDecayIntervalDays",        RecruitRepDecayIntervalDays },
            { "RecruitRepNeutralScore",             RecruitRepNeutralScore },
            { "RecruitRepLoyaltyDays",              RecruitRepLoyaltyDays },
            { "RecruitRepHireBonus",                RecruitRepHireBonus },
            { "RecruitRepFirePenalty",              RecruitRepFirePenalty },
            { "RecruitRepQuitPenalty",              RecruitRepQuitPenalty },
            { "RecruitRepRejectPenalty",            RecruitRepRejectPenalty },
            { "RecruitRepLoyaltyBonus",             RecruitRepLoyaltyBonus },
            { "CandidateAutoExpiryDays",            CandidateAutoExpiryDays },
            { "CandidateHRExpiryDays",              CandidateHRExpiryDays },
            { "CandidateUrgencyMediumThreshold",    CandidateUrgencyMediumThreshold },
            { "CandidateUrgencyHighThreshold",      CandidateUrgencyHighThreshold },
            { "SkillSpilloverRateBase",             SkillSpilloverRateBase },
            { "SkillSpilloverRateSpread",           SkillSpilloverRateSpread },
            { "TeamOverheadPerMember",              TeamOverheadPerMember },
            { "ProductTeamOverheadFactor",          ProductTeamOverheadFactor },
            { "ProductWorkEthicMinMult",            ProductWorkEthicMinMult },
            { "ProductWorkEthicMaxMult",            ProductWorkEthicMaxMult },
            { "ProductBaseWorkMultiplier",          ProductBaseWorkMultiplier },
            { "MaxXPPerProductShip",                MaxXPPerProductShip },
            { "ProductXPDurationCapDays",           ProductXPDurationCapDays },
            { "HypePassiveGainPerDay",               HypePassiveGainPerDay },
            { "HypeBudgetReferenceCost",             HypeBudgetReferenceCost },
            { "ScopeComplexityExponent",             ScopeComplexityExponent },
            { "ScopeBugRatePerFeature",              ScopeBugRatePerFeature },
            { "HypeCampaignBaseCost",                HypeCampaignBaseCost },
            { "HypeCampaignBaseGain",                HypeCampaignBaseGain },
            { "HypeCampaignCooldownTicks",           HypeCampaignCooldownTicks },
            { "HypeDiminishingFactor",               HypeDiminishingFactor },
            { "HypeDecayGracePeriodDays",            HypeDecayGracePeriodDays },
            { "HypeDecayPerDay",                     HypeDecayPerDay },
            { "HypeDecayRampDays",                   HypeDecayRampDays },
            { "HypeDecayMaxPerDay",                  HypeDecayMaxPerDay },
            { "HypeMaxBonus",                        HypeMaxBonus },
            { "HypeExpectationScale",                HypeExpectationScale },
            { "HypeRepPenaltyPerPoint",              HypeRepPenaltyPerPoint },
            { "HypeSentimentPenaltyScale",           HypeSentimentPenaltyScale },
            { "HypeMeetsExpectationsBonus",          HypeMeetsExpectationsBonus },
            { "AdCooldownTicks",                     AdCooldownTicks },
            { "AdDurationTicks",                     AdDurationTicks },
            { "AdPopularityGainPerDay",              AdPopularityGainPerDay },
            { "AdUserAcquisitionRate",               AdUserAcquisitionRate },
            { "UpdateHypePassiveGainPerDay",         UpdateHypePassiveGainPerDay },
            { "UpdateHypeMaxBonus",                  UpdateHypeMaxBonus },
            { "UpdateHypeDecayGraceDays",            UpdateHypeDecayGraceDays },
            { "UpdateBrokenPromiseSentimentPenalty", UpdateBrokenPromiseSentimentPenalty },
            { "MarketingXPPerMonth",                 MarketingXPPerMonth },
            { "MarketingXPPerBoost",                 MarketingXPPerBoost },
            { "MarketingXPPerAdRun",                 MarketingXPPerAdRun },
            { "MarketingXPPerEventMitigation",       MarketingXPPerEventMitigation },
            { "HypeEventBaseChance",                  HypeEventBaseChance },
            { "HypeEventCooldownTicks",               HypeEventCooldownTicks },
            { "RushCostScale",                        RushCostScale },
            { "RushFanSentimentPenaltyBase",          RushFanSentimentPenaltyBase },
            { "DelayHypeDecayPerDay",                 DelayHypeDecayPerDay },
            { "DelayRepLossBase",                     DelayRepLossBase },
            { "DelayRepLossCompounding",              DelayRepLossCompounding },
            { "DelayFanTrustErosionScale",            DelayFanTrustErosionScale },
            { "MaintenanceBugFixBaseRate",            MaintenanceBugFixBaseRate },
            { "UnmaintainedBugGrowthUserScale",       UnmaintainedBugGrowthUserScale },
            { "UnmaintainedBugGrowthAgePenaltyRate",  UnmaintainedBugGrowthAgePenaltyRate },
            { "ContractMisfitXPRate",                 ContractMisfitXPRate },
            { "ContractNativeXPRate",                 ContractNativeXPRate },
            { "ProductPhaseXPPerDay",                 ProductPhaseXPPerDay },
            { "ProductPhaseMisfitXPRate",             ProductPhaseMisfitXPRate },
            { "ProductPhaseNativeXPRate",             ProductPhaseNativeXPRate },
            { "PopularityConvergenceRate",            PopularityConvergenceRate },
            { "MinGrowthStageDays",                   MinGrowthStageDays },
            { "MinPlateauStageDays",                  MinPlateauStageDays },
            { "MoraleLowRecoveryThreshold",           MoraleLowRecoveryThreshold },
            { "MoraleLowRecoveryBonus",               MoraleLowRecoveryBonus },
            { "PhaseCompletionBaseMoraleBonus",       PhaseCompletionBaseMoraleBonus },
            { "ProductShipBaseMoraleBonus",           ProductShipBaseMoraleBonus },
            { "IdleRecoveryStartDay",                 IdleRecoveryStartDay },
            { "IdleRecoveryBonus",                    IdleRecoveryBonus },
            { "IdleBoredomStartDay",                  IdleBoredomStartDay },
            { "IdleBoredomDecayPerDay",               IdleBoredomDecayPerDay },
            { "IdleDecayStartDay",                    IdleDecayStartDay },
            { "IdleDecayPerDay",                      IdleDecayPerDay },
            { "IdleDecayMax",                         IdleDecayMax },
            { "ContractRewardMultiplier",             ContractRewardMultiplier },
            { "SalaryGlobalMultiplier",               SalaryGlobalMultiplier },
            { "CompetitorAggressionMultiplier",       CompetitorAggressionMultiplier },
            { "MarketDifficultyMultiplier",           MarketDifficultyMultiplier },
            { "ProductRevenueMultiplier",             ProductRevenueMultiplier },
            { "BugRateMultiplier",                    BugRateMultiplier },
            { "ReviewHarshnessMultiplier",            ReviewHarshnessMultiplier },
        };
    }

    /// <summary>Returns the default TuningConfig for comparison during summary generation.</summary>
    public static TuningConfig Defaults() => new TuningConfig();
}
