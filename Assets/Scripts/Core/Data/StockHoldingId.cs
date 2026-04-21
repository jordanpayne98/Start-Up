using System;
using System.ComponentModel;
using System.Globalization;

public class StockHoldingIdConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string s && int.TryParse(s, out int id))
            return new StockHoldingId(id);
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string) && value is StockHoldingId sid)
            return sid.Value.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(StockHoldingIdConverter))]
[Serializable]
public struct StockHoldingId
{
    public int Value;

    public StockHoldingId(int value)
    {
        Value = value;
    }

    public override bool Equals(object obj)
    {
        if (obj is StockHoldingId other)
            return Value == other.Value;
        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(StockHoldingId a, StockHoldingId b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(StockHoldingId a, StockHoldingId b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
