/// <summary>
/// Minimal IViewModel for unimplemented screens.
/// ScreenName is set by the factory before the view is first shown.
/// All IViewModel members are no-ops — this view carries no simulation state.
/// </summary>
public class PlaceholderViewModel : IViewModel
{
    public string ScreenName { get; set; } = "Screen";

    public bool IsDirty => false;

    public void Refresh(GameStateSnapshot snapshot) { /* no-op */ }

    public void ClearDirty() { /* no-op */ }
}
