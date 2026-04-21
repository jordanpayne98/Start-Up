// ReputationSystem Version: Clean v1
using System.Collections.Generic;

public class ReputationSystem : ISystem
{
    private static readonly int[] TierThresholds = { 0, 200, 1500, 5000, 15000 };
    private static readonly int[] DifficultyCaps = { 1, 2, 3, 4, 5 };
    private static readonly float[] CandidateQualityMultipliers = { 1.0f, 1.2f, 1.5f, 1.75f, 2.0f };
    private static readonly float[] LaunchMultsPerTier = { 0.5f, 1.0f, 2.0f, 5.0f, 12.0f };

    public event System.Action<int, ReputationTier> OnReputationChanged;
    public event System.Action<ReputationTier, ReputationTier> OnTierChanged;
    public event System.Action<int, int, float> OnFansChanged;

    private ReputationState _state;
    private ILogger _logger;
    private TuningConfig _tuning;
    private List<(string category, int score)> _topCategoriesScratch;
    private List<string> _keysScratch;

    private bool _repChanged;
    private int _capturedRep;
    private ReputationTier _capturedTier;

    private bool _tierChanged;
    private ReputationTier _capturedOldTier;
    private ReputationTier _capturedNewTier;

    private bool _fansChanged;
    private int _capturedFanTotal;
    private int _capturedFanDelta;
    private float _capturedSentiment;

    public int GlobalReputation => GetReputation("global");
    public ReputationTier CurrentTier => GetTier("global");
    public int CompanyFans => _state.companyFans;
    public float FanSentiment => _state.fanSentiment;

    public ReputationSystem(ReputationState state, ILogger logger)
    {
        _state = state;
        _logger = logger ?? new NullLogger();
        _topCategoriesScratch = new List<(string, int)>();
        _keysScratch = new List<string>();
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    public int GetReputation(string category = "global")
    {
        if (_state.reputationScores.TryGetValue(category, out int value))
        {
            return value;
        }
        return 0;
    }

    public ReputationTier GetTier(string category = "global")
    {
        int rep = GetReputation(category);
        return CalculateTier(rep, _tuning);
    }

    public void AddReputation(int amount, string category = "global")
    {
        if (amount <= 0) return;

        ReputationTier oldTier = GetTier(category);

        if (!_state.reputationScores.ContainsKey(category))
        {
            _state.reputationScores[category] = 0;
        }
        _state.reputationScores[category] += amount;
        _state.totalRepEarned += amount;

        ReputationTier newTier = GetTier(category);
        _capturedRep = _state.reputationScores[category];
        _capturedTier = newTier;
        _repChanged = true;

        if (oldTier != newTier)
        {
            _capturedOldTier = oldTier;
            _capturedNewTier = newTier;
            _tierChanged = true;
            _logger.Log($"Reputation tier changed: {oldTier} -> {newTier} (rep: {_capturedRep})");
        }
    }

    public void RemoveReputation(int amount, string category = "global")
    {
        if (amount <= 0) return;

        ReputationTier oldTier = GetTier(category);

        if (!_state.reputationScores.ContainsKey(category))
        {
            return;
        }

        _state.reputationScores[category] -= amount;
        if (_state.reputationScores[category] < 0)
        {
            _state.reputationScores[category] = 0;
        }
        _state.totalRepLost += amount;

        ReputationTier newTier = GetTier(category);
        _capturedRep = _state.reputationScores[category];
        _capturedTier = newTier;
        _repChanged = true;

        if (oldTier != newTier)
        {
            _capturedOldTier = oldTier;
            _capturedNewTier = newTier;
            _tierChanged = true;
            _logger.Log($"Reputation tier changed: {oldTier} -> {newTier} (rep: {_capturedRep})");
        }
    }

    public void IncrementContractCount()
    {
        _state.contractsCompletedCount++;
    }

    public void IncrementProductCount()
    {
        _state.productsShippedCount++;
    }

    public int GetEffectiveContractDifficultyCap()
    {
        int tierIndex = (int)CurrentTier;
        var caps = _tuning != null ? _tuning.ReputationDifficultyCaps : DifficultyCaps;
        if (tierIndex < 0) tierIndex = 0;
        if (tierIndex >= caps.Length) tierIndex = caps.Length - 1;
        return caps[tierIndex];
    }

    public float GetCandidateQualityMultiplier()
    {
        int tierIndex = (int)CurrentTier;
        var multipliers = _tuning != null ? _tuning.ReputationCandidateQualityMultipliers : CandidateQualityMultipliers;
        if (tierIndex < 0) tierIndex = 0;
        if (tierIndex >= multipliers.Length) tierIndex = multipliers.Length - 1;
        return multipliers[tierIndex];
    }

    public float GetFanLaunchMultiplier()
    {
        float divisor = _tuning != null ? _tuning.FanLaunchBonusDivisor : 50000f;
        float effectiveFans = _state.companyFans * (_state.fanSentiment / 100f);
        return 1.0f + effectiveFans / divisor;
    }

    public int GetFanWomBonus()
    {
        float womRate = _tuning != null ? _tuning.FanWomRate : 0.0001f;
        float effectiveFans = _state.companyFans * (_state.fanSentiment / 100f);
        return (int)(effectiveFans * womRate);
    }

    public void AddFans(int amount, ProductId productId)
    {
        if (amount <= 0) return;

        _state.companyFans += amount;
        if (!_state.fansPerProduct.ContainsKey(productId))
        {
            _state.fansPerProduct[productId] = 0;
        }
        _state.fansPerProduct[productId] += amount;

        _capturedFanTotal = _state.companyFans;
        _capturedFanDelta = amount;
        _capturedSentiment = _state.fanSentiment;
        _fansChanged = true;
    }

    public void DecayFansForProduct(ProductId productId, float decayRate)
    {
        if (!_state.fansPerProduct.TryGetValue(productId, out int fans)) return;

        int loss = fans < 1 ? 1 : (int)(fans * decayRate);
        if (loss < 1) loss = 1;

        _state.fansPerProduct[productId] = fans - loss;
        if (_state.fansPerProduct[productId] < 0) _state.fansPerProduct[productId] = 0;

        _state.companyFans -= loss;
        if (_state.companyFans < 0) _state.companyFans = 0;

        if (_state.fansPerProduct[productId] <= 0)
        {
            _state.fansPerProduct.Remove(productId);
        }

        _capturedFanTotal = _state.companyFans;
        _capturedFanDelta = -loss;
        _capturedSentiment = _state.fanSentiment;
        _fansChanged = true;
    }

    public void DecayIdleFans(float rate)
    {
        int loss = (int)(_state.companyFans * rate);
        if (loss <= 0) return;

        _state.companyFans -= loss;
        if (_state.companyFans < 0) _state.companyFans = 0;

        _capturedFanTotal = _state.companyFans;
        _capturedFanDelta = -loss;
        _capturedSentiment = _state.fanSentiment;
        _fansChanged = true;
    }

    public void AdjustSentimentOnShip(float quality)
    {
        if (quality >= 70f)
        {
            _state.fanSentiment += (quality - 70f) * 0.5f;
            if (_state.fanSentiment > 100f) _state.fanSentiment = 100f;
        }
        else if (quality < 40f)
        {
            _state.fanSentiment -= (40f - quality) * 1.5f;
            if (_state.fanSentiment < 0f) _state.fanSentiment = 0f;
        }
    }

    public void AdjustSentimentDelta(float delta)
    {
        _state.fanSentiment += delta;
        if (_state.fanSentiment > 100f) _state.fanSentiment = 100f;
        if (_state.fanSentiment < 0f) _state.fanSentiment = 0f;
    }

    public void DriftSentiment()
    {
        _state.fanSentiment += (50f - _state.fanSentiment) * 0.02f;
    }

    public float GetExpectationPenalty()
    {
        var penalties = _tuning != null ? _tuning.CustomerExpectationPenalties : new float[] { 0f, 5f, 12f, 20f, 30f };
        int tierIndex = (int)CurrentTier;
        if (tierIndex < 0) tierIndex = 0;
        if (tierIndex >= penalties.Length) tierIndex = penalties.Length - 1;
        return penalties[tierIndex];
    }

    public float GetEffectiveQuality(float rawQuality)
    {
        float penalty = GetExpectationPenalty();
        float effective = rawQuality - penalty;
        return effective < 0f ? 0f : effective;
    }

    public float GetCategoryLaunchMultiplier(ProductCategory category)
    {
        string key = category.ToString();
        int catRep = GetReputation(key);
        if (catRep > 0)
        {
            ReputationTier catTier = CalculateTier(catRep, _tuning);
            return GetLaunchMultForTier(catTier);
        }
        return GetLaunchMultForTier(CurrentTier);
    }

    public List<(string category, int score)> GetTopCategories(int count)
    {
        _topCategoriesScratch.Clear();
        _keysScratch.Clear();
        foreach (var key in _state.reputationScores.Keys)
        {
            _keysScratch.Add(key);
        }
        int keyCount = _keysScratch.Count;
        for (int i = 0; i < keyCount; i++)
        {
            string key = _keysScratch[i];
            if (key == "global") continue;
            _topCategoriesScratch.Add((key, _state.reputationScores[key]));
        }
        _topCategoriesScratch.Sort((a, b) => b.score.CompareTo(a.score));
        if (count < _topCategoriesScratch.Count)
        {
            _topCategoriesScratch.RemoveRange(count, _topCategoriesScratch.Count - count);
        }
        return _topCategoriesScratch;
    }

    public float GenerateReceptionScore(float effectiveQuality, float marketDemandMult, float genreTimingBonus, IRng rng)
    {
        float baseReception = effectiveQuality * 0.8f + marketDemandMult * 10f + genreTimingBonus;
        float variance = (rng.NextFloat01() * 20f) - 10f;
        float score = baseReception + variance;
        if (score < 0f) score = 0f;
        if (score > 100f) score = 100f;
        return score;
    }

    public static float GetLaunchMultForTier(ReputationTier tier)
    {
        int index = (int)tier;
        if (index < 0) index = 0;
        if (index >= LaunchMultsPerTier.Length) index = LaunchMultsPerTier.Length - 1;
        return LaunchMultsPerTier[index];
    }

    public static ReputationTier CalculateTier(int rep, TuningConfig tuning = null)
    {
        var thresholds = tuning != null ? tuning.ReputationTierThresholds : TierThresholds;
        ReputationTier tier = ReputationTier.Unknown;
        for (int i = thresholds.Length - 1; i >= 0; i--)
        {
            if (rep >= thresholds[i])
            {
                tier = (ReputationTier)i;
                break;
            }
        }
        return tier;
    }

    public void PreTick(int tick)
    {
    }

    public void Tick(int tick)
    {
        if (tick > 0 && tick % (TimeState.TicksPerDay * 30) == 0) {
            int globalRep = GetReputation("global");
            if (globalRep > 0) {
                int decay = globalRep / 200;
                if (decay < 1) decay = 1;
                RemoveReputation(decay, "global");
            }
        }
    }

    public void PostTick(int tick)
    {
        if (_repChanged)
        {
            OnReputationChanged?.Invoke(_capturedRep, _capturedTier);
            _repChanged = false;
        }
        if (_tierChanged)
        {
            OnTierChanged?.Invoke(_capturedOldTier, _capturedNewTier);
            _tierChanged = false;
        }
        if (_fansChanged)
        {
            OnFansChanged?.Invoke(_capturedFanTotal, _capturedFanDelta, _capturedSentiment);
            _fansChanged = false;
        }
    }

    public void ApplyCommand(ICommand command)
    {
    }

    public void Dispose()
    {
        _topCategoriesScratch.Clear();
        _keysScratch.Clear();
        OnReputationChanged = null;
        OnTierChanged = null;
        OnFansChanged = null;
    }
}
