public struct StartMarketingCampaignCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
}
