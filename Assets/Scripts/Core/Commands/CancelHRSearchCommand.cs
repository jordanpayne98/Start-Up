public class CancelHRSearchCommand : ICommand
{
    public int Tick { get; set; }
    public HRSearchId SearchId;
}
