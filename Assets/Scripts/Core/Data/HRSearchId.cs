using System;

[Serializable]
public struct HRSearchId
{
    public int Value;

    public HRSearchId(int value)
    {
        Value = value;
    }

    public override bool Equals(object obj)
    {
        return obj is HRSearchId id && Value == id.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(HRSearchId left, HRSearchId right)
    {
        return left.Value == right.Value;
    }

    public static bool operator !=(HRSearchId left, HRSearchId right)
    {
        return left.Value != right.Value;
    }
}
