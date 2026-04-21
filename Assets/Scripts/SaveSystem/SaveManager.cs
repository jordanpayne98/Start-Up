// SaveManager Version: Clean v2
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveManager
{
    public const int CurrentSaveVersion = 2;
    private const int MaxManualSaves = 5;
    private const int AutoSaveSlotCount = 3;

    private static string _saveDirectory;

    public static string SaveDirectory
    {
        get
        {
            if (_saveDirectory == null)
                _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            return _saveDirectory;
        }
    }

    private static JsonSerializerSettings BuildSettings()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new StringEnumConverter());
        return settings;
    }

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
    }

    private static string GetFilePath(string slotName)
    {
        return Path.Combine(SaveDirectory, slotName + ".json");
    }

    public static void SaveGame(GameState state, Dictionary<string, int> rngCounts, string slotName, string displayName, bool isAutoSave = false)
    {
        try
        {
            EnsureDirectoryExists();

            var metadata = new SaveMetadata
            {
                SlotName = slotName,
                DisplayName = displayName,
                CompanyName = state.companyName,
                InGameDay = state.timeState != null ? state.timeState.GetDayOfMonth() : 0,
                InGameMonth = state.timeState != null ? state.timeState.currentMonth : 0,
                InGameYear = state.timeState != null ? state.timeState.currentYear : 0,
                Money = state.financeState != null ? state.financeState.money : 0,
                EmployeeCount = CountActiveEmployees(state),
                CurrentTick = state.currentTick,
                RealWorldTimestamp = DateTime.UtcNow.ToString("o"),
                SaveVersion = CurrentSaveVersion,
                IsAutoSave = isAutoSave
            };

            var saveData = new SaveFileData
            {
                FormatVersion = CurrentSaveVersion,
                Metadata = metadata,
                State = state,
                RngInvocationCounts = rngCounts
            };

            string json = JsonConvert.SerializeObject(saveData, BuildSettings());
            File.WriteAllText(GetFilePath(slotName), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save game to slot '{slotName}': {e.ToString()}");
        }
    }

    public static SaveFileData LoadGame(string slotName)
    {
        try
        {
            string filePath = GetFilePath(slotName);
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[SaveManager] Save file not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            var saveData = JsonConvert.DeserializeObject<SaveFileData>(json, BuildSettings());

            if (saveData == null)
            {
                Debug.LogError($"[SaveManager] Failed to deserialize save file: {slotName}");
                return null;
            }

            if (saveData.FormatVersion > CurrentSaveVersion)
            {
                Debug.LogError($"[SaveManager] Save format version too new. Max supported: {CurrentSaveVersion}, got {saveData.FormatVersion}. Save: {slotName}");
                return null;
            }

            if (saveData.FormatVersion < CurrentSaveVersion)
            {
                Debug.LogWarning($"[SaveManager] Save format version {saveData.FormatVersion} < current {CurrentSaveVersion}. Migration will run in GameController.");
            }

            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load game from slot '{slotName}': {e.ToString()}");
            return null;
        }
    }

    public static List<SaveMetadata> GetAllSaveMetadata()
    {
        var result = new List<SaveMetadata>();
        try
        {
            EnsureDirectoryExists();
            string[] files = Directory.GetFiles(SaveDirectory, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string json = File.ReadAllText(files[i]);
                    var jObj = JObject.Parse(json);
                    var metaToken = jObj["Metadata"];
                    if (metaToken != null)
                        result.Add(metaToken.ToObject<SaveMetadata>());
                    else
                        Debug.LogWarning($"[SaveManager] No Metadata in {files[i]}, skipping.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] Failed to read metadata from {files[i]}: {e.ToString()}");
                }
            }

            result.Sort((a, b) => string.Compare(b.RealWorldTimestamp, a.RealWorldTimestamp, StringComparison.Ordinal));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to enumerate save files: {e.ToString()}");
        }
        return result;
    }

    public static List<SaveMetadata> GetManualSaves()
    {
        var all = GetAllSaveMetadata();
        var result = new List<SaveMetadata>();
        for (int i = 0; i < all.Count; i++)
        {
            if (!all[i].IsAutoSave)
                result.Add(all[i]);
        }
        return result;
    }

    public static List<SaveMetadata> GetAutoSaves()
    {
        var all = GetAllSaveMetadata();
        var result = new List<SaveMetadata>();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].IsAutoSave)
                result.Add(all[i]);
        }
        return result;
    }

    public static SaveMetadata? GetLatestSave()
    {
        var all = GetAllSaveMetadata();
        if (all.Count == 0)
            return null;
        return all[0];
    }

    public static void DeleteSave(string slotName)
    {
        try
        {
            string filePath = GetFilePath(slotName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete save '{slotName}': {e.ToString()}");
        }
    }

    public static bool SaveExists(string slotName)
    {
        return File.Exists(GetFilePath(slotName));
    }

    public static int GetManualSaveCount()
    {
        return GetManualSaves().Count;
    }

    public static string GetNextAutoSaveSlot()
    {
        string[] slotNames = { "autosave_1", "autosave_2", "autosave_3" };

        for (int i = 0; i < AutoSaveSlotCount; i++)
        {
            if (!SaveExists(slotNames[i]))
                return slotNames[i];
        }

        string oldestSlot = slotNames[0];
        string oldestTimestamp = null;

        for (int i = 0; i < AutoSaveSlotCount; i++)
        {
            try
            {
                string json = File.ReadAllText(GetFilePath(slotNames[i]));
                var jObj = JObject.Parse(json);
                var tsToken = jObj["Metadata"]?["RealWorldTimestamp"];
                if (tsToken != null)
                {
                    string ts = tsToken.Value<string>();
                    if (oldestTimestamp == null || string.Compare(ts, oldestTimestamp, StringComparison.Ordinal) < 0)
                    {
                        oldestTimestamp = ts;
                        oldestSlot = slotNames[i];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to read auto-save slot '{slotNames[i]}' for rotation: {e.ToString()}");
            }
        }

        return oldestSlot;
    }

    private static int CountActiveEmployees(GameState state)
    {
        if (state.employeeState == null || state.employeeState.employees == null)
            return 0;

        int count = 0;
        foreach (var kvp in state.employeeState.employees)
        {
            if (kvp.Value != null && kvp.Value.isActive)
                count++;
        }
        return count;
    }
}
