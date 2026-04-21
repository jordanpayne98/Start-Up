public struct SendBetaToReviewersCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
