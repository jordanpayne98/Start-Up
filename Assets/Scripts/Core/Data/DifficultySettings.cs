using System;

public enum DifficultyPreset {
    Sandbox,
    Easy,
    Normal,
    Hard,
    Custom
}

[Serializable]
public struct DifficultySettings {
    public DifficultyPreset Preset;
    public int StartingCash;
    public float ContractRewardMultiplier;
    public bool TaxEnabled;
    public float TaxRate;
    public bool SalariesEnabled;
    public float SalaryMultiplier;
    public bool QuittingEnabled;
    public float SkillGrowthMultiplier;
    public float MoraleDecayMultiplier;
    public bool CompetitorsEnabled;
    public float CompetitorAggressionMultiplier;
    public float MarketDifficultyMultiplier;
    public bool BankruptcyEnabled;
    public float LoanInterestMultiplier;
    public float ProductWorkRateMultiplier;
    public float BugRateMultiplier;
    public float ReviewHarshnessMultiplier;
    public float ProductRevenueMultiplier;

    public static DifficultySettings Default(DifficultyPreset preset) {
        switch (preset) {
            case DifficultyPreset.Sandbox:
                return new DifficultySettings {
                    Preset = DifficultyPreset.Sandbox,
                    StartingCash = 500000,
                    ContractRewardMultiplier = 2.0f,
                    TaxEnabled = false,
                    TaxRate = 0f,
                    SalariesEnabled = false,
                    SalaryMultiplier = 0f,
                    QuittingEnabled = false,
                    SkillGrowthMultiplier = 3.0f,
                    MoraleDecayMultiplier = 0f,
                    CompetitorsEnabled = false,
                    CompetitorAggressionMultiplier = 0f,
                    MarketDifficultyMultiplier = 0.25f,
                    BankruptcyEnabled = false,
                    LoanInterestMultiplier = 0f,
                    ProductWorkRateMultiplier = 2.0f,
                    BugRateMultiplier = 0.25f,
                    ReviewHarshnessMultiplier = 0.5f,
                    ProductRevenueMultiplier = 2.0f
                };
            case DifficultyPreset.Easy:
                return new DifficultySettings {
                    Preset = DifficultyPreset.Easy,
                    StartingCash = 150000,
                    ContractRewardMultiplier = 1.25f,
                    TaxEnabled = true,
                    TaxRate = 0.15f,
                    SalariesEnabled = true,
                    SalaryMultiplier = 0.75f,
                    QuittingEnabled = true,
                    SkillGrowthMultiplier = 1.5f,
                    MoraleDecayMultiplier = 0.5f,
                    CompetitorsEnabled = true,
                    CompetitorAggressionMultiplier = 0.5f,
                    MarketDifficultyMultiplier = 0.75f,
                    BankruptcyEnabled = true,
                    LoanInterestMultiplier = 0.75f,
                    ProductWorkRateMultiplier = 1.25f,
                    BugRateMultiplier = 0.75f,
                    ReviewHarshnessMultiplier = 0.75f,
                    ProductRevenueMultiplier = 1.25f
                };
            case DifficultyPreset.Hard:
                return new DifficultySettings {
                    Preset = DifficultyPreset.Hard,
                    StartingCash = 60000,
                    ContractRewardMultiplier = 0.8f,
                    TaxEnabled = true,
                    TaxRate = 0.40f,
                    SalariesEnabled = true,
                    SalaryMultiplier = 1.25f,
                    QuittingEnabled = true,
                    SkillGrowthMultiplier = 0.75f,
                    MoraleDecayMultiplier = 1.5f,
                    CompetitorsEnabled = true,
                    CompetitorAggressionMultiplier = 1.5f,
                    MarketDifficultyMultiplier = 1.5f,
                    BankruptcyEnabled = true,
                    LoanInterestMultiplier = 1.5f,
                    ProductWorkRateMultiplier = 0.75f,
                    BugRateMultiplier = 1.5f,
                    ReviewHarshnessMultiplier = 1.5f,
                    ProductRevenueMultiplier = 0.75f
                };
            default:
                return new DifficultySettings {
                    Preset = DifficultyPreset.Normal,
                    StartingCash = 100000,
                    ContractRewardMultiplier = 1.0f,
                    TaxEnabled = true,
                    TaxRate = 0.30f,
                    SalariesEnabled = true,
                    SalaryMultiplier = 1.0f,
                    QuittingEnabled = true,
                    SkillGrowthMultiplier = 1.0f,
                    MoraleDecayMultiplier = 1.0f,
                    CompetitorsEnabled = true,
                    CompetitorAggressionMultiplier = 1.0f,
                    MarketDifficultyMultiplier = 1.0f,
                    BankruptcyEnabled = true,
                    LoanInterestMultiplier = 1.0f,
                    ProductWorkRateMultiplier = 1.0f,
                    BugRateMultiplier = 1.0f,
                    ReviewHarshnessMultiplier = 1.0f,
                    ProductRevenueMultiplier = 1.0f
                };
        }
    }
}
