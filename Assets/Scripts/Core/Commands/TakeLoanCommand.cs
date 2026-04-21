public class TakeLoanCommand : ICommand
{
    public int Tick { get; private set; }
    public int Amount { get; private set; }
    public int DurationMonths { get; private set; }

    public TakeLoanCommand(int tick, int amount, int durationMonths = 3) {
        Tick = tick;
        Amount = amount;
        DurationMonths = durationMonths;
    }
}
