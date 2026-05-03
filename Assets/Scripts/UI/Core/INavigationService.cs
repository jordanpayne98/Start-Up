using System;

public interface INavigationService
{
    void NavigateTo(ScreenId id);
    void NavigateTo(ScreenId id, int tabHint);
    void GoBack();
    ScreenId CurrentScreen { get; }
    event Action<ScreenId, ScreenId> OnScreenChanged;
}
