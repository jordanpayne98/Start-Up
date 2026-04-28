using System;
using System.Collections.Generic;

/// <summary>
/// Temporary state container for the product creation wizard.
/// Holds all player selections across wizard steps until confirmed.
/// </summary>
[Serializable]
public class ProductDraftState
{
    public string ProductName = "";
    public int ProductTypeIndex = -1;
    public int CategoryIndex = -1;
    public int NicheIndex = -1;
    public int GenreIndex = -1;
    public int FormatIndex = -1;
    public List<int> SelectedPlatformIds = new List<int>();
    public List<int> SelectedFeatureIds = new List<int>();
    public HardwareConfiguration HardwareConfig;
    public TeamSlotAssignment[] TeamAssignments = new TeamSlotAssignment[4];
    public BudgetSettings Budget;

    /// <summary>
    /// Checks minimum required selections to start development.
    /// </summary>
    public bool IsValid()
    {
        return ProductTypeIndex >= 0
            && CategoryIndex >= 0
            && SelectedPlatformIds.Count > 0
            && !string.IsNullOrEmpty(ProductName);
    }

    /// <summary>
    /// Clears all selections downstream of (and including) the given step index.
    /// </summary>
    public void ClearFromStep(int stepIndex)
    {
        // Step order: 0=Type, 1=Category, 2=Market, 3=Genre, 4=Platform, 5+=later
        if (stepIndex <= 0) { ProductTypeIndex = -1; }
        if (stepIndex <= 1) { CategoryIndex = -1; }
        if (stepIndex <= 2) { NicheIndex = -1; }
        if (stepIndex <= 3) { GenreIndex = -1; FormatIndex = -1; }
        if (stepIndex <= 4) { SelectedPlatformIds.Clear(); }
        if (stepIndex <= 5) { SelectedFeatureIds.Clear(); }
        if (stepIndex <= 6) { HardwareConfig = default; }
        if (stepIndex <= 7) { TeamAssignments = new TeamSlotAssignment[4]; }
        if (stepIndex <= 8) { Budget = default; }
    }

    public ProductDraftState Clone()
    {
        var clone = new ProductDraftState
        {
            ProductName = ProductName,
            ProductTypeIndex = ProductTypeIndex,
            CategoryIndex = CategoryIndex,
            NicheIndex = NicheIndex,
            GenreIndex = GenreIndex,
            FormatIndex = FormatIndex,
            SelectedPlatformIds = new List<int>(SelectedPlatformIds),
            SelectedFeatureIds = new List<int>(SelectedFeatureIds),
            HardwareConfig = HardwareConfig,
            Budget = Budget
        };
        clone.TeamAssignments = new TeamSlotAssignment[4];
        for (int i = 0; i < 4; i++)
            clone.TeamAssignments[i] = TeamAssignments[i];
        return clone;
    }
}

/// <summary>
/// Represents a team slot assignment in the product creation wizard.
/// </summary>
[Serializable]
public struct TeamSlotAssignment
{
    public int TeamId;
    public bool IsAssigned;
}

/// <summary>
/// Budget settings for the product creation wizard.
/// </summary>
[Serializable]
public struct BudgetSettings
{
    public int MarketingBudget;
    public int QABudget;
    public float PricePerUnit;
    public bool IsSubscriptionModel;
    public float MonthlySubscriptionPrice;
}
