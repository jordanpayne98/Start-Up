using System;
using System.ComponentModel;
using System.Globalization;

public class CompanyIdConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string s && int.TryParse(s, out int id))
            return new CompanyId(id);
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string) && value is CompanyId cid)
            return cid.Value.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[TypeConverter(typeof(CompanyIdConverter))]
[Serializable]
public struct CompanyId : IEquatable<CompanyId>
{
    public int Value;

    public static readonly CompanyId Player = new CompanyId(0);

    public bool IsPlayer => Value == 0;

    public CompanyId(int value)
    {
        Value = value;
    }

    public bool Equals(CompanyId other) => Value == other.Value;

    public override bool Equals(object obj)
    {
        if (obj is CompanyId other)
            return Value == other.Value;
        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CompanyId a, CompanyId b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(CompanyId a, CompanyId b)
    {
        return a.Value != b.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public static class CompanyIdExtensions
{
    public static CompanyId ToCompanyId(this CompetitorId id) => new CompanyId(id.Value);
    public static CompetitorId ToCompetitorId(this CompanyId id) => new CompetitorId(id.Value);
}
