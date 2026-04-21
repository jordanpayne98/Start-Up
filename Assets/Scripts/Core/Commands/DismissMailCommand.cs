public struct DismissMailCommand : ICommand
{
    public int Tick { get; }
    public int? MailId; // null = dismiss all

    public DismissMailCommand(int tick, int? mailId)
    {
        Tick = tick;
        MailId = mailId;
    }
}
