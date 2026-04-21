using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TooltipRegistry", menuName = "Game/Tooltip Registry")]
public class TooltipRegistry : ScriptableObject {
    [SerializeField] private TooltipEntry[] _entries = Array.Empty<TooltipEntry>();

    private Dictionary<string, TooltipData> _lookup;

    public bool TryGet(string key, out TooltipData data) {
        if (_lookup == null) RebuildLookup();
        return _lookup.TryGetValue(key, out data);
    }

    public void RebuildLookup() {
        _lookup = new Dictionary<string, TooltipData>(_entries.Length);
        int count = _entries.Length;
        for (int i = 0; i < count; i++) {
            string key = _entries[i].Key;
            if (string.IsNullOrEmpty(key)) continue;
#if UNITY_EDITOR
            if (_lookup.ContainsKey(key)) {
                Debug.LogWarning($"[TooltipRegistry] Duplicate key found: '{key}'. Second entry will be ignored.");
                continue;
            }
#endif
            _lookup[key] = _entries[i].Data;
        }
    }

    private void OnEnable() {
        _lookup = null;
    }
}

[Serializable]
public struct TooltipEntry {
    public string Key;
    public TooltipData Data;
}
