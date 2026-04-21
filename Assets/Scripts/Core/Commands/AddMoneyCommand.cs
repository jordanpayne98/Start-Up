public struct AddMoneyCommand : ICommand
{
    public int Tick { get; set; }
    public int Amount;
}
