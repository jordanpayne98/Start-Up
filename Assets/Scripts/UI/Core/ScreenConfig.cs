using System;

public struct ScreenConfig
{
    public ScreenId ScreenId;
    public NavCategory Category;
    public string DisplayName;
    public string UxmlPath;
    public Func<IGameView> ViewFactory;
    public Func<IViewModel> ViewModelFactory;
}
