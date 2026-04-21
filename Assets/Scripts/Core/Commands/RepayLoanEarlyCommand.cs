public class RepayLoanEarlyCommand : ICommand
{
    public int Tick { get; private set; }
    public int Amount { get; private set; }

    public RepayLoanEarlyCommand(int tick, int amount) {
        Tick = tick;
        Amount = amount;
    }
}
