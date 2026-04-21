using System.Collections.Generic;

public interface IDebugCommandHandler
{
    Dictionary<string, string> GetCommands();
    bool TryExecute(string command, string[] args, DebugConsole console);
}
