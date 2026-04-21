using UnityEngine;

public enum ParadigmType {
    Foundational,
    Trend
}

[CreateAssetMenu(menuName = "StartUp/Paradigm")]
public class ParadigmDefinition : ScriptableObject
{
    public string paradigmId;
    public string displayName;
    public string description;
    public ParadigmType paradigmType;
    [Range(0f, 0.1f)]
    public float decayRate;
}
