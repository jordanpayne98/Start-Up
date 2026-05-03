using System.Collections.Generic;

public struct SaveSlotDisplay {
    public string SlotName;
    public string DisplayName;
    public string CompanyName;
    public string DateDisplay;
    public string MoneyDisplay;
    public int EmployeeCount;
    public string TimestampDisplay;
    public bool IsAutoSave;
    public bool IsEmpty;
}

public class PauseMenuViewModel : IViewModel {
    public List<SaveSlotDisplay> ManualSaves { get; private set; }
    public List<SaveSlotDisplay> AutoSaves { get; private set; }
    public int ManualSaveCount => ManualSaves.Count;
    public int MaxManualSaves => 5;
    public bool CanCreateNewSave => ManualSaveCount < MaxManualSaves;
    public string DefaultSaveName { get; private set; }

    private IReadOnlyGameState _state;

    public PauseMenuViewModel() {
        ManualSaves = new List<SaveSlotDisplay>();
        AutoSaves = new List<SaveSlotDisplay>();
        DefaultSaveName = "New Save";
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        _state = snapshot;
        DefaultSaveName = BuildDefaultSaveName(snapshot);
        IsDirty = true;
    }

    public void RefreshSaveSlots() {
        ManualSaves.Clear();
        AutoSaves.Clear();

        var manual = SaveManager.GetManualSaves();
        for (int i = 0; i < manual.Count; i++) {
            ManualSaves.Add(BuildDisplay(manual[i]));
        }

        var auto = SaveManager.GetAutoSaves();
        for (int i = 0; i < auto.Count; i++) {
            AutoSaves.Add(BuildDisplay(auto[i]));
        }
    }

    private static SaveSlotDisplay BuildDisplay(SaveMetadata meta) {
        string ts = meta.RealWorldTimestamp;
        string tsDisplay = ts;
        if (!string.IsNullOrEmpty(ts) && ts.Length >= 16) {
            tsDisplay = ts.Substring(0, 10) + " " + ts.Substring(11, 5);
        }
        return new SaveSlotDisplay {
            SlotName = meta.SlotName,
            DisplayName = meta.DisplayName,
            CompanyName = meta.CompanyName,
            DateDisplay = "Y" + meta.InGameYear + " M" + meta.InGameMonth + " D" + meta.InGameDay,
            MoneyDisplay = UIFormatting.FormatMoney(meta.Money),
            EmployeeCount = meta.EmployeeCount,
            TimestampDisplay = tsDisplay,
            IsAutoSave = meta.IsAutoSave,
            IsEmpty = false
        };
    }

    private static string BuildDefaultSaveName(IReadOnlyGameState state) {
        if (state == null) return "New Save";
        string company = "Company";
        return company + " - Y" + state.CurrentYear + " M" + state.CurrentMonth;
    }
}
