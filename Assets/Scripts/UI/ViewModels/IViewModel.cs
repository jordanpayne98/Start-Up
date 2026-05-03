public interface IViewModel
{
    void Refresh(GameStateSnapshot snapshot);
    bool IsDirty { get; }
    void ClearDirty();
}
