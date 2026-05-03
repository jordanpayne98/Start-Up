using System;
using UnityEngine;

public struct ScreenConfig
{
    public ScreenId         Id;
    public string           Label;
    public string           IconClass;
    public string           UxmlPath;
    public NavCategoryId    Category;
    public Func<IGameView>  ViewFactory;
    public Func<IViewModel> ViewModelFactory;
    public KeyCode?         Hotkey;

    // Legacy — kept so existing ScreenRegistry wiring compiles
    public string           DisplayName;
    public NavCategory      LegacyCategory;
}
