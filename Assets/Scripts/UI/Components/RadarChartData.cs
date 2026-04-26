using UnityEngine;

public struct SkillDataPoint {
    public string SkillName;
    public float Level;
    public float PreviousLevel;
    public Color SkillNameColor;
}

public struct RadarChartData {
    public SkillDataPoint[] Skills;
}
