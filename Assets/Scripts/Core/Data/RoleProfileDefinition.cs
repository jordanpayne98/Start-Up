using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/RoleProfileDefinition")]
public class RoleProfileDefinition : ScriptableObject
{
    public RoleId Role;
    public string DisplayName;
    public RoleFamily Family;
    [TextArea] public string Description;

    // Length must equal SkillIdHelper.SkillCount (26). Indexed by (int)SkillId.
    public RoleWeightBand[] SkillBands;

    // Length must equal VisibleAttributeHelper.AttributeCount (8). Indexed by (int)VisibleAttributeId.
    public AttributeWeightBand[] AttributeBands;

    // 0.0–1.0 scalar. High=0.9, Medium-High=0.75, Medium=0.6, Medium-Low=0.45
    public float SalaryPressure;

    // Generation frequency weight. Common=1.0, Uncommon=0.5, Rare=0.2
    public float CandidatePoolWeight;

    public int SortOrder;

    public SkillId[] GetPrimarySkills()
    {
        int count = 0;
        for (int i = 0; i < SkillBands.Length; i++)
            if (SkillBands[i] == RoleWeightBand.Primary) count++;
        var result = new SkillId[count];
        int idx = 0;
        for (int i = 0; i < SkillBands.Length; i++)
            if (SkillBands[i] == RoleWeightBand.Primary) result[idx++] = (SkillId)i;
        return result;
    }

    public SkillId[] GetSecondarySkills()
    {
        int count = 0;
        for (int i = 0; i < SkillBands.Length; i++)
            if (SkillBands[i] == RoleWeightBand.Secondary) count++;
        var result = new SkillId[count];
        int idx = 0;
        for (int i = 0; i < SkillBands.Length; i++)
            if (SkillBands[i] == RoleWeightBand.Secondary) result[idx++] = (SkillId)i;
        return result;
    }

    public SkillId[] GetTertiarySkills()
    {
        int count = 0;
        for (int i = 0; i < SkillBands.Length; i++)
            if (SkillBands[i] == RoleWeightBand.Tertiary) count++;
        var result = new SkillId[count];
        int idx = 0;
        for (int i = 0; i < SkillBands.Length; i++)
            if (SkillBands[i] == RoleWeightBand.Tertiary) result[idx++] = (SkillId)i;
        return result;
    }

    public float GetSkillWeight(SkillId id)
    {
        int idx = (int)id;
        if (idx < 0 || idx >= SkillBands.Length) return 0f;
        return RoleWeightBandHelper.ToWeight(SkillBands[idx]);
    }

    public float GetAttributeWeight(VisibleAttributeId id)
    {
        int idx = (int)id;
        if (idx < 0 || idx >= AttributeBands.Length) return 0f;
        return AttributeWeightBandHelper.ToWeight(AttributeBands[idx]);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        int skillCount = SkillIdHelper.SkillCount;
        if (SkillBands == null || SkillBands.Length != skillCount)
        {
            Debug.LogWarning($"[RoleProfileDefinition] '{name}' SkillBands must have exactly {skillCount} entries.");
        }
        else
        {
            bool hasPrimary = false;
            for (int i = 0; i < SkillBands.Length; i++)
                if (SkillBands[i] == RoleWeightBand.Primary) { hasPrimary = true; break; }
            if (!hasPrimary)
                Debug.LogWarning($"[RoleProfileDefinition] '{name}' has no Primary skills defined.");
        }

        int attrCount = VisibleAttributeHelper.AttributeCount;
        if (AttributeBands == null || AttributeBands.Length != attrCount)
        {
            Debug.LogWarning($"[RoleProfileDefinition] '{name}' AttributeBands must have exactly {attrCount} entries.");
        }
        else
        {
            bool hasCritical = false;
            for (int i = 0; i < AttributeBands.Length; i++)
                if (AttributeBands[i] == AttributeWeightBand.Critical) { hasCritical = true; break; }
            if (!hasCritical)
                Debug.LogWarning($"[RoleProfileDefinition] '{name}' has no Critical attributes defined.");
        }
    }
#endif
}
