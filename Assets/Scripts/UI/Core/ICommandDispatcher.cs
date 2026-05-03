public interface ICommandDispatcher
{
    bool Dispatch(ICommand command);
    bool TryDispatch(ICommand command, out string error);
    int CurrentTick { get; }
}
