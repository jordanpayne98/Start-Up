public interface ICommandDispatcher
{
    void Dispatch(ICommand command);
    int CurrentTick { get; }
}
