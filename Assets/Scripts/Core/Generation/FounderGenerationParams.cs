using System;

/// <summary>
/// Parameter struct passed to FounderStatGenerator.
/// Carries all wizard inputs needed to deterministically generate a founder stat block.
/// </summary>
[Serializable]
public struct FounderGenerationParams
{
    public FounderArchetypeDefinition Archetype;
    public FounderPersonalityStyleDefinition PersonalityStyle;
    public FounderWeaknessDefinition Weakness;
    public int Age;
    public bool IsSoloFounder;
    public RoleProfileDefinition RoleProfile;
}
