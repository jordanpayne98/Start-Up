using System.Collections.Generic;

public static class NewGameData {
    public static string CompanyName;
    public static List<FoundingEmployeeData> Founders;
    public static bool IsNewGame;
    public static string LoadSlotName;
    public static DifficultySettings Difficulty;
    public static int Seed;

    public static void Clear() {
        CompanyName = null;
        Founders = null;
        IsNewGame = false;
        LoadSlotName = null;
        Difficulty = DifficultySettings.Default(DifficultyPreset.Normal);
        Seed = 0;
    }
}
