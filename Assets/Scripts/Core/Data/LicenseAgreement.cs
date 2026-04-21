using System;

[Serializable]
public struct LicenseAgreement {
    public ProductId LicenseeProductId;   // product using the platform/tool
    public ProductId LicensorProductId;   // platform/tool being used
    public CompetitorId? LicensorOwnerId; // null = player owns the licensor
    public float RoyaltyRate;             // percentage cut
    public int AgreementStartTick;
}
