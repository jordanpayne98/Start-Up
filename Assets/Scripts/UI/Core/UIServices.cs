/// <summary>
/// Lightweight service locator passed to IGameView.Initialize().
/// Constructed once by WindowManager and passed to every IGameView.Initialize().
/// Null-safe: any property may be null before the shell is fully initialised.
/// </summary>
public class UIServices
{
    public INavigationService Navigation { get; }
    public IModalPresenter    Modals     { get; }
    public ICommandDispatcher Commands   { get; }
    public IToastService      Toasts     { get; }
    public ITooltipProvider   Tooltips   { get; }

    public UIServices(
        INavigationService navigation,
        IModalPresenter    modals,
        ICommandDispatcher commands,
        IToastService      toasts,
        ITooltipProvider   tooltips)
    {
        Navigation = navigation;
        Modals     = modals;
        Commands   = commands;
        Toasts     = toasts;
        Tooltips   = tooltips;
    }
}
