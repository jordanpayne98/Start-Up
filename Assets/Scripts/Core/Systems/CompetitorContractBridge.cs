// CompetitorContractBridge Version: Clean v2
using System;

public class CompetitorContractBridge
{
    private CompetitorSystem _competitorSystem;
    private ContractSystem _contractSystem;
    private ProductState _productState;

    public void Initialize(CompetitorSystem compSys, ContractSystem contractSys, ProductState productState)
    {
        _competitorSystem = compSys ?? throw new ArgumentNullException(nameof(compSys));
        _contractSystem = contractSys ?? throw new ArgumentNullException(nameof(contractSys));
        _productState = productState ?? throw new ArgumentNullException(nameof(productState));

        _contractSystem.OnContractCompleted += OnContractCompleted;
        _contractSystem.OnContractExpired += OnContractExpired;
    }

    public void Dispose()
    {
        if (_contractSystem != null)
        {
            _contractSystem.OnContractCompleted -= OnContractCompleted;
            _contractSystem.OnContractExpired -= OnContractExpired;
        }
        _competitorSystem = null;
        _contractSystem = null;
        _productState = null;
    }

    private void OnContractCompleted(ContractId contractId, int tick, int reward, float quality)
    {
        var contract = _contractSystem.GetContract(contractId);
        if (contract == null) return;
        if (!contract.SourceCompetitorId.HasValue) return;
        if (!contract.SourceProductId.HasValue) return;

        ProductId productId = contract.SourceProductId.Value;
        if (!_productState.developmentProducts.TryGetValue(productId, out var product)) return;
        if (!product.IsCompetitorProduct) return;

        float playerQualityModifier = (quality / 100f) - 0.5f;
        if (product.Phases != null)
        {
            for (int i = 0; i < product.Phases.Length; i++)
            {
                float baseQuality = product.Phases[i].phaseQuality;
                float modified = baseQuality * (1f + playerQualityModifier * 0.2f);
                modified = System.Math.Max(1f, System.Math.Min(100f, modified));
                product.Phases[i].phaseQuality = modified;
            }
        }
    }

    private void OnContractExpired(ContractId contractId)
    {
        // Self-sourced work handling removed — AI products now develop via TeamWorkEngine.
    }
}
