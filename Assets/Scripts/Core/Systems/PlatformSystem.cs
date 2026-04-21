using System;
using System.Collections.Generic;

public class PlatformSystem : ISystem {
    private const int TicksPerMonth = TimeState.TicksPerDay * 30;
    private const float MomentumFactor = 0.85f;
    private const float GenericCeiling = 65f;
    private const float OwnedCeilingMultiplier = 1.1f;
    private const float CompetitorCeilingMultiplier = 0.95f;

    private enum PendingEventType : byte {
        PlatformShareChanged,
        LicensingRevenueEarned,
    }

    private struct PendingEvent {
        public PendingEventType Type;
        public ProductId ProductId;
        public float FloatA;
        public float FloatB;
        public int IntA;
    }

    public event Action<ProductId, float, float> OnPlatformShareChanged;
    public event Action<ProductId, int> OnLicensingRevenueEarned;

    private readonly PlatformState _state;
    private readonly ProductState _productState;
    private readonly CompetitorState _competitorState;
    private readonly IRng _rng;

    private readonly List<PendingEvent> _pendingEvents;
    private readonly List<ProductId> _platformKeys;
    private readonly List<ProductId> _sameCategoryPlatforms;

    public PlatformSystem(PlatformState state, ProductState productState, CompetitorState competitorState, IRng rng) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _productState = productState ?? throw new ArgumentNullException(nameof(productState));
        _competitorState = competitorState;
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _pendingEvents = new List<PendingEvent>(16);
        _platformKeys = new List<ProductId>(16);
        _sameCategoryPlatforms = new List<ProductId>(8);
    }

    public void PreTick(int tick) { }

    public void Tick(int tick) {
        if ((tick - _state.lastPlatformUpdateTick) < TicksPerMonth) return;

        _state.lastPlatformUpdateTick = tick;
        RefreshPlatformEntries(tick);
        RedistributeMarketShares();
        ProcessLicensingRevenue(tick);
    }

    public void PostTick(int tick) {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++) {
            var e = _pendingEvents[i];
            switch (e.Type) {
                case PendingEventType.PlatformShareChanged:
                    OnPlatformShareChanged?.Invoke(e.ProductId, e.FloatA, e.FloatB);
                    break;
                case PendingEventType.LicensingRevenueEarned:
                    OnLicensingRevenueEarned?.Invoke(e.ProductId, e.IntA);
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose() { }

    public float GetCeiling(ProductId productId, bool isOwned) {
        if (!_state.platformShares.TryGetValue(productId, out var entry))
            return GenericCeiling;

        return isOwned
            ? entry.QualityCeiling * OwnedCeilingMultiplier
            : entry.QualityCeiling * CompetitorCeilingMultiplier;
    }

    public float GetAddressableMarket(ProductNiche niche, ProductId[] targetPlatforms) {
        if (targetPlatforms == null || targetPlatforms.Length == 0)
            return GenericCeiling;

        float totalShare = 0f;
        for (int i = 0; i < targetPlatforms.Length; i++) {
            if (_state.platformShares.TryGetValue(targetPlatforms[i], out var entry))
                totalShare += entry.MarketSharePercent;
        }

        return UnityEngine.Mathf.Clamp01(totalShare);
    }

    public int CalculateLicensingRevenue(ProductId platformId, int tick) {
        if (!_state.platformShares.TryGetValue(platformId, out var entry))
            return 0;

        int total = 0;
        foreach (var kvp in _productState.shippedProducts) {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;
            if (product.TargetPlatformIds == null) continue;

            bool targetsThisPlatform = false;
            for (int i = 0; i < product.TargetPlatformIds.Length; i++) {
                if (product.TargetPlatformIds[i] == platformId) {
                    targetsThisPlatform = true;
                    break;
                }
            }

            if (!targetsThisPlatform) continue;
            int monthlyRevenue = product.MonthlyRevenue;
            total += (int)(monthlyRevenue * entry.LicensingRate);
        }

        return total;
    }

    public PlatformMarketEntry GetPlatformEntry(ProductId platformId) {
        if (_state.platformShares.TryGetValue(platformId, out var entry))
            return entry;
        return default;
    }

    private void RefreshPlatformEntries(int tick) {
        foreach (var kvp in _productState.shippedProducts) {
            var product = kvp.Value;
            if (!product.IsOnMarket) continue;
            if (!product.Category.IsPlatform()) continue;

            var productId = kvp.Key;
            if (!_state.platformShares.TryGetValue(productId, out var entry)) {
                entry = new PlatformMarketEntry {
                    PlatformId = productId,
                    OwnerId = null,
                    MarketSharePercent = 0.05f,
                    InstallBase = 0,
                    EcosystemProductCount = 0,
                    LicensingRate = 0.05f,
                    QualityCeiling = product.OverallQuality
                };
            }

            entry.QualityCeiling = product.OverallQuality;
            entry.EcosystemProductCount = CountEcosystemProducts(productId);
            _state.platformShares[productId] = entry;
        }

        if (_competitorState == null) return;

        foreach (var compKvp in _competitorState.competitors) {
            var comp = compKvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;
            if (comp.ActiveProductIds == null) continue;

            int count = comp.ActiveProductIds.Count;
            for (int i = 0; i < count; i++) {
                var productId = comp.ActiveProductIds[i];
                if (!_productState.shippedProducts.TryGetValue(productId, out var product)) continue;
                if (!product.IsOnMarket || !product.Category.IsPlatform()) continue;

                if (!_state.platformShares.TryGetValue(productId, out var entry)) {
                    entry = new PlatformMarketEntry {
                        PlatformId = productId,
                        OwnerId = compKvp.Key,
                        MarketSharePercent = 0.05f,
                        InstallBase = 0,
                        EcosystemProductCount = 0,
                        LicensingRate = 0.05f,
                        QualityCeiling = product.OverallQuality
                    };
                }

                entry.QualityCeiling = product.OverallQuality;
                entry.EcosystemProductCount = CountEcosystemProducts(productId);
                _state.platformShares[productId] = entry;
            }
        }
    }

    private void RedistributeMarketShares() {
        _platformKeys.Clear();
        foreach (var kvp in _state.platformShares)
            _platformKeys.Add(kvp.Key);

        int total = _platformKeys.Count;
        for (int i = 0; i < total; i++) {
            var platformId = _platformKeys[i];
            if (!_state.platformShares.TryGetValue(platformId, out var entry)) continue;
            if (!_productState.shippedProducts.TryGetValue(platformId, out var product)) continue;

            _sameCategoryPlatforms.Clear();
            CollectCompetingPlatforms(platformId, product.Category, _sameCategoryPlatforms);

            if (_sameCategoryPlatforms.Count == 0) continue;

            float totalAppeal = 0f;
            int sameCount = _sameCategoryPlatforms.Count;
            float[] appeals = new float[sameCount];

            for (int j = 0; j < sameCount; j++) {
                var pid = _sameCategoryPlatforms[j];
                if (_state.platformShares.TryGetValue(pid, out var e)) {
                    float appeal = CalculatePlatformAppeal(e);
                    appeals[j] = appeal;
                    totalAppeal += appeal;
                }
            }

            if (totalAppeal <= 0f) continue;

            for (int j = 0; j < sameCount; j++) {
                var pid = _sameCategoryPlatforms[j];
                if (!_state.platformShares.TryGetValue(pid, out var e)) continue;

                float targetShare = appeals[j] / totalAppeal;
                float oldShare = e.MarketSharePercent;
                float newShare = oldShare * MomentumFactor + targetShare * (1f - MomentumFactor);

                if (System.Math.Abs(newShare - oldShare) > 0.001f) {
                    _pendingEvents.Add(new PendingEvent {
                        Type = PendingEventType.PlatformShareChanged,
                        ProductId = pid,
                        FloatA = oldShare,
                        FloatB = newShare,
                    });
                }

                e.MarketSharePercent = newShare;
                e.InstallBase = _productState.shippedProducts.TryGetValue(pid, out var platformProduct)
                    ? platformProduct.ActiveUserCount
                    : (int)(newShare * 10_000_000);
                _state.platformShares[pid] = e;
            }
        }
    }

    private void ProcessLicensingRevenue(int tick) {
        _platformKeys.Clear();
        foreach (var kvp in _state.platformShares)
            _platformKeys.Add(kvp.Key);

        int count = _platformKeys.Count;
        for (int i = 0; i < count; i++) {
            var platformId = _platformKeys[i];
            int revenue = CalculateLicensingRevenue(platformId, tick);
            if (revenue <= 0) continue;

            if (_productState.shippedProducts.TryGetValue(platformId, out var platform))
                platform.MonthlyRevenue += revenue;

            _pendingEvents.Add(new PendingEvent {
                Type = PendingEventType.LicensingRevenueEarned,
                ProductId = platformId,
                IntA = revenue,
            });
        }
    }

    private float CalculatePlatformAppeal(PlatformMarketEntry entry) {
        float qualityFactor = entry.QualityCeiling / 100f;
        float ecosystemFactor = 1f + UnityEngine.Mathf.Log(1 + entry.EcosystemProductCount) * 0.1f;
        float licensingFactor = 1f - entry.LicensingRate * 0.5f;
        return qualityFactor * ecosystemFactor * licensingFactor;
    }

    private void CollectCompetingPlatforms(ProductId self, ProductCategory category, List<ProductId> result) {
        foreach (var kvp in _state.platformShares) {
            if (kvp.Key == self) {
                result.Add(kvp.Key);
                continue;
            }

            if (_productState.shippedProducts.TryGetValue(kvp.Key, out var p) && p.Category == category)
                result.Add(kvp.Key);
        }
    }

    private int CountEcosystemProducts(ProductId platformId) {
        int count = 0;
        foreach (var kvp in _productState.shippedProducts) {
            var product = kvp.Value;
            if (!product.IsOnMarket || product.TargetPlatformIds == null) continue;
            for (int i = 0; i < product.TargetPlatformIds.Length; i++) {
                if (product.TargetPlatformIds[i] == platformId) {
                    count++;
                    break;
                }
            }
        }
        return count;
    }
}
