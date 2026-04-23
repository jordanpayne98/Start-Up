using System;

[Serializable]
public struct ProductIdentitySnapshot
{
    public byte Version;

    public sbyte PricePositioning;
    public sbyte InnovationRisk;
    public sbyte AudienceBreadth;
    public sbyte FeatureScope;
    public sbyte ProductionDiscipline;

    public ProductIdentityTag PrimaryTag;
    public ProductIdentityTag SecondaryTag;
    public ProductIdentityTag TertiaryTag;

    public int LastComputedTick;
    public bool IsValid;
}
