public enum ProductBudgetType
{
    Maintenance = 0,
    Marketing = 1
}

public struct SetProductBudgetCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public ProductBudgetType BudgetType;
    public long MonthlyAllocation;
}
