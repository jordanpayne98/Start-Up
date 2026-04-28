using System.Collections.Generic;

public enum CompetitorDetailTab { Overview, Products, Employees, Finance }

public struct CompetitorProductRowVM {
    public ProductId Id;
    public string Name;
    public string Category;
    public string Quality;
    public string Revenue;
    public string LifecycleStage;
    public string ActiveUsers;
    public string Progress;
    public string EstRelease;
    public bool IsInDev;
}

public struct CompetitorEmployeeRowVM {
    public EmployeeId Id;
    public string Name;
    public string Role;
    public string TierLabel;
    public string SkillLevel;
    public string MoraleIndicator;
    public string Salary;
}

public struct CompetitorRevenueRowVM {
    public ProductId ProductId;
    public string ProductName;
    public string MonthlyRevenue;
}

public struct CompetitorExpenseRowVM {
    public string Category;
    public string MonthlyAmount;
}

public class CompetitorProfileViewModel : IViewModel
{
    public CompetitorId Id { get; private set; }

    // Tab state
    public CompetitorDetailTab ActiveTab { get; set; } = CompetitorDetailTab.Overview;

    // Overview
    public string CompanyName { get; private set; }
    public string FounderName { get; private set; }
    public string ArchetypeLabel { get; private set; }
    public string ReputationDescription { get; private set; }
    public string StockPrice { get; private set; }
    public string PlayerOwnership { get; private set; }
    public bool CanBuyStock { get; private set; }
    public int TotalEmployees { get; private set; }
    public int TotalProducts { get; private set; }
    public string TotalLifetimeRevenue { get; private set; }

    // Products tab
    public List<CompetitorProductRowVM> ActiveProducts { get; private set; }
    public List<CompetitorProductRowVM> InDevProducts { get; private set; }

    // Employees tab
    public List<CompetitorEmployeeRowVM> Employees { get; private set; }
    public string AvgSkillLevel { get; private set; }
    public string AvgMorale { get; private set; }
    public string AvgSalary { get; private set; }
    public string Headcount { get; private set; }

    // Finance tab
    public string Cash { get; private set; }
    public string MonthlyRevenue { get; private set; }
    public string MonthlyExpenses { get; private set; }
    public string MonthlyProfit { get; private set; }
    public string FinancialHealthLabel { get; private set; }
    public List<CompetitorRevenueRowVM> RevenueBreakdown { get; private set; }
    public List<CompetitorExpenseRowVM> ExpenseBreakdown { get; private set; }

    // Niche
    public List<string> NichePresenceKeys { get; private set; }
    public List<string> NichePresenceValues { get; private set; }

    private readonly List<CompetitorProductRowVM> _activeProducts = new List<CompetitorProductRowVM>();
    private readonly List<CompetitorProductRowVM> _inDevProducts = new List<CompetitorProductRowVM>();
    private readonly List<CompetitorEmployeeRowVM> _employees = new List<CompetitorEmployeeRowVM>();
    private readonly List<CompetitorRevenueRowVM> _revenueBreakdown = new List<CompetitorRevenueRowVM>();
    private readonly List<CompetitorExpenseRowVM> _expenseBreakdown = new List<CompetitorExpenseRowVM>();
    private readonly List<string> _nicheKeys = new List<string>();
    private readonly List<string> _nicheValues = new List<string>();

    public CompetitorProfileViewModel() {
        ActiveProducts = _activeProducts;
        InDevProducts = _inDevProducts;
        Employees = _employees;
        RevenueBreakdown = _revenueBreakdown;
        ExpenseBreakdown = _expenseBreakdown;
        NichePresenceKeys = _nicheKeys;
        NichePresenceValues = _nicheValues;
    }

    public void SetId(CompetitorId id) {
        Id = id;
    }

    public void Refresh(IReadOnlyGameState state) {
        if (state == null) return;
        var snapshot = state as GameStateSnapshot;
        if (snapshot == null) return;
        Refresh(Id, snapshot);
    }

    public void Refresh(CompetitorId id, GameStateSnapshot snapshot) {
        Id = id;
        _activeProducts.Clear();
        _inDevProducts.Clear();
        _employees.Clear();
        _revenueBreakdown.Clear();
        _expenseBreakdown.Clear();
        _nicheKeys.Clear();
        _nicheValues.Clear();

        var compState = snapshot.CompetitorState;
        if (compState?.competitors == null || !compState.competitors.TryGetValue(id, out var comp)) {
            CompanyName = "Unknown";
            FounderName = "--";
            ArchetypeLabel = "--";
            ReputationDescription = "--";
            StockPrice = "--";
            PlayerOwnership = "0%";
            CanBuyStock = false;
            TotalEmployees = 0;
            TotalProducts = 0;
            TotalLifetimeRevenue = "--";
            AvgSkillLevel = "--";
            AvgMorale = "--";
            AvgSalary = "--";
            Headcount = "0";
            Cash = "--";
            MonthlyRevenue = "--";
            MonthlyExpenses = "--";
            MonthlyProfit = "--";
            FinancialHealthLabel = "--";
            return;
        }

        CompanyName = comp.CompanyName;
        FounderName = comp.FounderName;
        ArchetypeLabel = FormatArchetype(comp.Archetype);
        ReputationDescription = UIFormatting.FormatReputationTier(ReputationSystem.CalculateTier(comp.ReputationPoints));
        TotalProducts = (comp.ActiveProductIds != null ? comp.ActiveProductIds.Count : 0)
                      + (comp.InDevelopmentProductIds != null ? comp.InDevelopmentProductIds.Count : 0);
        TotalLifetimeRevenue = UIFormatting.FormatMoney(comp.Finance.MonthlyRevenue * 12);

        float playerOwnershipPct = 0f;
        long stockPriceVal = 0L;
        var stockState = snapshot.StockState;
        if (stockState?.listings != null && stockState.listings.TryGetValue(id, out var listing)) {
            stockPriceVal = listing.StockPrice;
            if (listing.OwnershipBreakdown != null) {
                foreach (var kvp in listing.OwnershipBreakdown) {
                    if (kvp.Key.Value == 0) {
                        playerOwnershipPct = kvp.Value;
                        break;
                    }
                }
            }
            CanBuyStock = listing.UnownedPercentage > 0f;
        } else {
            CanBuyStock = false;
        }
        StockPrice = UIFormatting.FormatMoney(stockPriceVal);
        PlayerOwnership = UIFormatting.FormatPercent(playerOwnershipPct);

        var productState = snapshot.ProductStateRef;
        if (productState != null) {
            if (comp.ActiveProductIds != null) {
                int count = comp.ActiveProductIds.Count;
                for (int i = 0; i < count; i++) {
                    var pid = comp.ActiveProductIds[i];
                    Product prod = null;
                    productState.shippedProducts?.TryGetValue(pid, out prod);
                    if (prod == null) continue;
                    _activeProducts.Add(new CompetitorProductRowVM {
                        Id = pid,
                        Name = prod.ProductName,
                        Category = UIFormatting.FormatNicheOrCategory(prod),
                        Quality = ((int)prod.OverallQuality).ToString(),
                        Revenue = UIFormatting.FormatMoney(prod.MonthlyRevenue),
                        LifecycleStage = FormatLifecycle(prod.LifecycleStage),
                        ActiveUsers = prod.ActiveUserCount.ToString("N0"),
                        IsInDev = false
                    });
                    _revenueBreakdown.Add(new CompetitorRevenueRowVM {
                        ProductId = pid,
                        ProductName = prod.ProductName,
                        MonthlyRevenue = UIFormatting.FormatMoney(prod.MonthlyRevenue)
                    });
                }
            }
            if (comp.InDevelopmentProductIds != null) {
                int count = comp.InDevelopmentProductIds.Count;
                for (int i = 0; i < count; i++) {
                    var pid = comp.InDevelopmentProductIds[i];
                    Product prod = null;
                    productState.developmentProducts?.TryGetValue(pid, out prod);
                    if (prod == null) continue;
                    float progress = ComputeProductProgress(prod);
                    _inDevProducts.Add(new CompetitorProductRowVM {
                        Id = pid,
                        Name = prod.ProductName,
                        Category = UIFormatting.FormatNicheOrCategory(prod),
                        Progress = UIFormatting.FormatPercent(progress),
                        IsInDev = true
                    });
                }
            }
        }
        TotalEmployees = comp.EmployeeIds != null ? comp.EmployeeIds.Count : 0;

        var compEmployees = snapshot.GetEmployeesForCompany(comp.Id.ToCompanyId());
        int empCount = compEmployees.Count;
        long totalSkill = 0;
        long totalMorale = 0;
        long totalSalary = 0;
        for (int i = 0; i < empCount; i++) {
            var emp = compEmployees[i];
            int avgEmpSkill = ComputeAvgSkill(emp);
            totalSkill += avgEmpSkill;
            totalMorale += emp.morale;
            totalSalary += emp.salary;
            _employees.Add(new CompetitorEmployeeRowVM {
                Id = emp.id,
                Name = emp.name,
                Role = UIFormatting.FormatRole(emp.role),
                TierLabel = "L" + (avgEmpSkill / 5 + 1),
                SkillLevel = avgEmpSkill.ToString(),
                MoraleIndicator = FormatMoraleLabel(emp.morale),
                Salary = UIFormatting.FormatMoney(emp.salary)
            });
        }
        Headcount = empCount.ToString();
        AvgSkillLevel = empCount > 0 ? (totalSkill / empCount).ToString() : "--";
        AvgMorale = empCount > 0 ? FormatMoraleLabel((int)(totalMorale / empCount)) : "--";
        AvgSalary = empCount > 0 ? UIFormatting.FormatMoney(totalSalary / empCount) : "--";

        Cash = UIFormatting.FormatMoney(comp.Finance.Cash);
        MonthlyRevenue = UIFormatting.FormatMoney(comp.Finance.MonthlyRevenue);
        MonthlyExpenses = UIFormatting.FormatMoney(comp.Finance.MonthlyExpenses);
        MonthlyProfit = UIFormatting.FormatMoney(comp.Finance.MonthlyProfit);
        FinancialHealthLabel = FormatFinancialHealth(comp.Finance);

        long productBudgets = 0L;
        if (comp.ActiveProductIds != null) {
            int apCount = comp.ActiveProductIds.Count;
            for (int i = 0; i < apCount; i++) {
                Product prod = null;
                productState?.shippedProducts?.TryGetValue(comp.ActiveProductIds[i], out prod);
                if (prod != null) {
                    productBudgets += prod.MaintenanceBudgetMonthly + prod.MarketingBudgetMonthly;
                }
            }
        }

        if (comp.Finance.MonthlyExpenses > 0) {
            long unassignedSalaries = System.Math.Max(0L, totalSalary - productBudgets);
            _expenseBreakdown.Add(new CompetitorExpenseRowVM {
                Category = "Salaries",
                MonthlyAmount = UIFormatting.FormatMoney(unassignedSalaries)
            });
            if (productBudgets > 0) {
                _expenseBreakdown.Add(new CompetitorExpenseRowVM {
                    Category = "Product Budgets",
                    MonthlyAmount = UIFormatting.FormatMoney(productBudgets)
                });
            }
            long reported = unassignedSalaries + productBudgets;
            long operations = System.Math.Max(0L, comp.Finance.MonthlyExpenses - reported);
            if (operations > 0) {
                _expenseBreakdown.Add(new CompetitorExpenseRowVM {
                    Category = "Operations",
                    MonthlyAmount = UIFormatting.FormatMoney(operations)
                });
            }
        }

        if (comp.ActiveProductIds != null) {
            long totalLicensingFees = 0L;
            int prodCount = comp.ActiveProductIds.Count;
            for (int i = 0; i < prodCount; i++) {
                Product prod = null;
                productState?.shippedProducts?.TryGetValue(comp.ActiveProductIds[i], out prod);
                if (prod == null || !prod.IsOnMarket) continue;

                if (prod.RequiredToolIds != null) {
                    for (int t = 0; t < prod.RequiredToolIds.Length; t++) {
                        Product tool = null;
                        productState.shippedProducts?.TryGetValue(prod.RequiredToolIds[t], out tool);
                        if (tool == null) continue;
                        if (tool.DistributionModel != ToolDistributionModel.Licensed) continue;
                        if (tool.PlayerLicensingRate <= 0f) continue;
                        if (tool.OwnerCompanyId == comp.Id.ToCompanyId()) continue;
                        totalLicensingFees += (long)(prod.MonthlyRevenue * tool.PlayerLicensingRate);
                    }
                }
                if (prod.TargetPlatformIds != null) {
                    for (int p = 0; p < prod.TargetPlatformIds.Length; p++) {
                        Product plat = null;
                        productState.shippedProducts?.TryGetValue(prod.TargetPlatformIds[p], out plat);
                        if (plat == null) continue;
                        if (plat.DistributionModel != ToolDistributionModel.Licensed) continue;
                        if (plat.PlayerLicensingRate <= 0f) continue;
                        if (plat.OwnerCompanyId == comp.Id.ToCompanyId()) continue;
                        totalLicensingFees += (long)(prod.MonthlyRevenue * plat.PlayerLicensingRate);
                    }
                }
            }
            if (totalLicensingFees > 0) {
                _revenueBreakdown.Add(new CompetitorRevenueRowVM {
                    ProductId = default,
                    ProductName = "Licensing Fees",
                    MonthlyRevenue = UIFormatting.FormatMoney(-totalLicensingFees)
                });
            }
        }

        if (comp.NicheMarketShare != null) {
            foreach (var kvp in comp.NicheMarketShare) {
                if (kvp.Value <= 0f) continue;
                _nicheKeys.Add(kvp.Key.ToString());
                _nicheValues.Add(UIFormatting.FormatPercent(kvp.Value));
            }
        }
    }

    private static int ComputeAvgSkill(Employee emp) {
        if (emp.Stats.Skills == null || emp.Stats.Skills.Length == 0) return 0;
        int sum = 0;
        int len = emp.Stats.Skills.Length;
        for (int i = 0; i < len; i++) sum += emp.Stats.Skills[i];
        return sum / len;
    }

    private static float ComputeProductProgress(Product prod) {
        if (prod.Phases == null || prod.Phases.Length == 0) return 0f;
        float totalWork = 0f;
        float completedWork = 0f;
        int count = prod.Phases.Length;
        for (int i = 0; i < count; i++) {
            var phase = prod.Phases[i];
            totalWork += phase.totalWorkRequired;
            completedWork += phase.workCompleted;
        }
        return totalWork > 0f ? completedWork / totalWork : 0f;
    }

    private static string FormatLifecycle(ProductLifecycleStage stage) {
        switch (stage) {
            case ProductLifecycleStage.PreLaunch: return "Pre-Launch";
            case ProductLifecycleStage.Launch:    return "Launch";
            case ProductLifecycleStage.Growth:    return "Growth";
            case ProductLifecycleStage.Plateau:   return "Plateau";
            case ProductLifecycleStage.Decline:   return "Decline";
            default:                              return stage.ToString();
        }
    }

    private static string FormatArchetype(CompetitorArchetype archetype) {
        switch (archetype) {
            case CompetitorArchetype.PlatformGiant:  return "Platform Giant";
            case CompetitorArchetype.ToolMaker:      return "Tool Maker";
            case CompetitorArchetype.GameStudio:     return "Game Studio";
            case CompetitorArchetype.FullStack:      return "Full Stack";
            default:                                 return archetype.ToString();
        }
    }

    private static string FormatMoraleLabel(int morale) {
        if (morale >= 75) return "High";
        if (morale >= 40) return "OK";
        return "Low";
    }

    private static string FormatFinancialHealth(CompanyFinance finance) {
        if (finance.Cash < 0) return "Critical";
        if (finance.MonthlyExpenses <= 0) return "Stable";
        float monthsOfCash = (float)finance.Cash / finance.MonthlyExpenses;
        if (monthsOfCash >= 6f) return "Healthy";
        if (monthsOfCash >= 3f) return "Stable";
        return "Struggling";
    }
}
