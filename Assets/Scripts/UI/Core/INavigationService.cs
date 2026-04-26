public interface INavigationService
{
    void NavigateTo(ScreenId screenId);
    void NavigateTo(ScreenId screenId, int tabHint);
}
