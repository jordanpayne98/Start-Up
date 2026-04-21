using System.Collections.Generic;
using UnityEngine;

// Runtime registry of role tier arrays loaded from RoleTierProfile ScriptableObjects.
// Populated at startup by GameController via Resources.LoadAll<RoleTierProfile>.
public class RoleTierTable
{
    private readonly Dictionary<EmployeeRole, int[]> _table = new Dictionary<EmployeeRole, int[]>();

    // Register a profile and cache its indexed tier array.
    public void Register(RoleTierProfile profile)
    {
        if (profile == null) return;
        _table[profile.Role] = profile.ToIndexedTiers();
    }

    // Returns the cached indexed tier array for the given role.
    // Tier values: 2 = Primary (1.0x), 3 = Secondary (1.5x), 4 = Tertiary (2.0x).
    // Falls back to all-Secondary (3) and logs a warning if no profile is registered.
    public int[] GetTiers(EmployeeRole role)
    {
        if (_table.TryGetValue(role, out var tiers))
            return tiers;

        Debug.LogWarning($"[RoleTierTable] No tier profile registered for role '{role}'. Falling back to all-Secondary.");
        return BuildUniformTiers();
    }

    public bool HasTiers(EmployeeRole role)
    {
        return _table.ContainsKey(role);
    }

    private static int[] BuildUniformTiers()
    {
        var tiers = new int[SkillTypeHelper.SkillTypeCount];
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
            tiers[i] = 3;
        return tiers;
    }
}
