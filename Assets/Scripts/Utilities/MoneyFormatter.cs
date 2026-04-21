public static class MoneyFormatter
{
    public static string FormatShort(int amount)
    {
        bool isNegative = amount < 0;
        int absAmount = Abs(amount);
        
        string formatted;
        if (absAmount >= 1000000)
        {
            formatted = $"${absAmount / 1000000.0f:F1}M";
        }
        else if (absAmount >= 1000)
        {
            formatted = $"${absAmount / 1000.0f:F1}K";
        }
        else
        {
            formatted = $"${absAmount}";
        }
        
        return isNegative ? "-" + formatted : formatted;
    }
    
    public static string FormatLong(int amount)
    {
        bool isNegative = amount < 0;
        int absAmount = Abs(amount);
        
        string formatted = $"${absAmount:N0}";
        return isNegative ? "-" + formatted : formatted;
    }
    
    public static string FormatCurrency(int amount)
    {
        return FormatShort(amount);
    }
    
    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }
}
