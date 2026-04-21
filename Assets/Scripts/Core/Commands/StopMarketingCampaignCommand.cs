public struct StopMarketingCampaignCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
