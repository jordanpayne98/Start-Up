public struct MarkMailReadCommand : ICommand
{
    public int Tick { get; }
    public int? MailId; // null = mark all read

    public MarkMailReadCommand(int tick, int? mailId)
    {
        Tick = tick;
        MailId = mailId;
    }
}
