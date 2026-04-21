using UnityEngine;

[CreateAssetMenu(menuName = "StartUp/RoleTierProfile")]
public class RoleTierProfile : ScriptableObject
{
    public EmployeeRole Role;

    // Length must equal SkillTypeHelper.SkillTypeCount (9).
    // Indexed by (int)SkillType.
    // Valid values: 2 = Primary (1.0x), 3 = Secondary (1.5x), 4 = Tertiary (2.0x)
    public int[] SkillTiers;

    // Returns the tier array if already the correct length, otherwise builds a
    // defensive copy padded with Secondary (3). Call once at startup and cache.
    public int[] ToIndexedTiers()
    {
        int count = SkillTypeHelper.SkillTypeCount;
        if (SkillTiers != null && SkillTiers.Length == count)
        {
            var copy = new int[count];
            for (int i = 0; i < count; i++)
                copy[i] = SkillTiers[i];
            return copy;
        }

        Debug.LogWarning($"[RoleTierProfile] '{name}' SkillTiers length is wrong " +
                         $"({(SkillTiers == null ? 0 : SkillTiers.Length)} vs {count}). " +
                         "Returning all-Secondary fallback.");
        var fallback = new int[count];
        for (int i = 0; i < count; i++)
            fallback[i] = 3;
        return fallback;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        int count = SkillTypeHelper.SkillTypeCount;
        if (SkillTiers == null || SkillTiers.Length != count)
        {
            Debug.LogWarning($"[RoleTierProfile] '{name}' must have exactly {count} entries in SkillTiers.");
            return;
        }

        int primaryCount = 0;
        int secondaryCount = 0;
        int tertiaryCount = 0;
        for (int i = 0; i < SkillTiers.Length; i++)
        {
            switch (SkillTiers[i])
            {
                case 2: primaryCount++;   break;
                case 3: secondaryCount++; break;
                case 4: tertiaryCount++;  break;
                default:
                    Debug.LogWarning($"[RoleTierProfile] '{name}' index {i} has invalid tier value {SkillTiers[i]}. Must be 2, 3, or 4.");
                    break;
            }
        }

        if (primaryCount != 1 || secondaryCount != 3 || tertiaryCount != 5)
        {
            Debug.LogWarning($"[RoleTierProfile] '{name}' tier distribution is {primaryCount}P / {secondaryCount}S / {tertiaryCount}T. " +
                             "Expected exactly 1 Primary (2), 3 Secondary (3), 5 Tertiary (4).");
        }
    }
#endif
}
