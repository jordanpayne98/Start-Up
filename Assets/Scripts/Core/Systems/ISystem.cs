public interface ISystem
{
    void PreTick(int tick);
    void Tick(int tick);
    void PostTick(int tick);
    void ApplyCommand(ICommand command);
    void Dispose();
}
