using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/Architecture Generation")]
public class ArchitectureGenerationDefinition : ScriptableObject
{
    public int generationId;
    public string displayName;
    public string description;
    public int arrivalTickMin;
    public int arrivalTickMax;
    public int transitionDurationTicks;
    public float marketGrowthMultiplier = 1f;
    public ParadigmDefinition[] paradigms;
}
