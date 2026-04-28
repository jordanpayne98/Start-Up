using System.Collections.Generic;
using UnityEngine;

public class RoleProfileTable
{
    private readonly Dictionary<RoleId, RoleProfileDefinition> _table = new Dictionary<RoleId, RoleProfileDefinition>();

    public void Register(RoleProfileDefinition profile)
    {
        if (profile == null) return;
        _table[profile.Role] = profile;
    }

    public RoleProfileDefinition Get(RoleId role)
    {
        if (_table.TryGetValue(role, out var profile))
            return profile;
        Debug.LogWarning($"[RoleProfileTable] No profile registered for role '{role}'.");
        return null;
    }

    public bool HasProfile(RoleId role)
    {
        return _table.ContainsKey(role);
    }

    public RoleProfileDefinition[] GetAllProfiles()
    {
        var values = _table.Values;
        var result = new RoleProfileDefinition[values.Count];
        int idx = 0;
        foreach (var profile in values)
            result[idx++] = profile;
        return result;
    }

    public RoleProfileDefinition[] GetByFamily(RoleFamily family)
    {
        int count = 0;
        foreach (var profile in _table.Values)
            if (profile.Family == family) count++;
        var result = new RoleProfileDefinition[count];
        int idx = 0;
        foreach (var profile in _table.Values)
            if (profile.Family == family) result[idx++] = profile;
        return result;
    }
}
