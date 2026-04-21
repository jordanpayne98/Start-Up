using System;
using System.Collections.Generic;

public class DisruptionSystem : ISystem
{
    private enum PendingDisruptionKind { MinorStarted, MajorStarted, Ended }

    private struct PendingDisruptionEvent
    {
        public PendingDisruptionKind Kind;
        public ActiveDisruption Disruption;
    }

    // Minor disruption: every 3-6 months (in ticks)
    private const int MinorIntervalMinMonths = 3;
    private const int MinorIntervalMaxMonths = 7;

    // Major disruption: every 12-24 months (in ticks)
    private const int MajorIntervalMinMonths = 12;
    private const int MajorIntervalMaxMonths = 25;

    private const int TicksPerMonth = TimeState.TicksPerDay * 30;

    private static readonly ProductNiche[] NicheValues = (ProductNiche[])Enum.GetValues(typeof(ProductNiche));

    private readonly DisruptionState _state;
    private readonly MarketState _marketState;
    private readonly CompetitorState _compState;
    private readonly IRng _rng;
    private readonly ILogger _logger;
    private TimeSystem _timeSystem;

    private int _nextMinorCheckTick;
    private int _nextMajorCheckTick;
    private bool _initialized;

    private readonly List<PendingDisruptionEvent> _pendingEvents;
    private readonly List<int> _expiredIds;
    private readonly List<CompetitorId> _competitorKeys;

    public event Action<ActiveDisruption> OnMinorDisruptionStarted;
    public event Action<ActiveDisruption> OnMajorDisruptionStarted;
    public event Action<ActiveDisruption> OnDisruptionEnded;

    public DisruptionSystem(
        DisruptionState state,
        MarketState marketState,
        CompetitorState compState,
        IRng rng,
        ILogger logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _marketState = marketState ?? throw new ArgumentNullException(nameof(marketState));
        _compState = compState ?? throw new ArgumentNullException(nameof(compState));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<PendingDisruptionEvent>();
        _expiredIds = new List<int>();
        _competitorKeys = new List<CompetitorId>();
    }

    public void SetTimeSystem(TimeSystem ts)
    {
        _timeSystem = ts;
    }

    public List<ActiveDisruption> GetActiveDisruptions()
    {
        return _state.activeDisruptions;
    }

    public bool HasActiveDisruption(DisruptionEventType type)
    {
        int count = _state.activeDisruptions.Count;
        for (int i = 0; i < count; i++)
        {
            if (_state.activeDisruptions[i].EventType == type)
                return true;
        }
        return false;
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        if (!_initialized)
        {
            _initialized = true;
            _nextMinorCheckTick = tick + RollMinorInterval();
            _nextMajorCheckTick = tick + RollMajorInterval();
            _state.lastMinorCheckTick = tick;
            _state.lastMajorCheckTick = tick;
        }

        ExpireDisruptions(tick);
        ApplyActiveEffects(tick);

        if (tick >= _nextMinorCheckTick)
        {
            _state.lastMinorCheckTick = tick;
            _nextMinorCheckTick = tick + RollMinorInterval();
            TrySpawnMinorDisruption(tick);
        }

        if (tick >= _nextMajorCheckTick)
        {
            _state.lastMajorCheckTick = tick;
            _nextMajorCheckTick = tick + RollMajorInterval();
            TrySpawnMajorDisruption(tick);
        }
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var e = _pendingEvents[i];
            switch (e.Kind)
            {
                case PendingDisruptionKind.MinorStarted:
                    OnMinorDisruptionStarted?.Invoke(e.Disruption);
                    break;
                case PendingDisruptionKind.MajorStarted:
                    OnMajorDisruptionStarted?.Invoke(e.Disruption);
                    break;
                case PendingDisruptionKind.Ended:
                    OnDisruptionEnded?.Invoke(e.Disruption);
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose() { }

    private void ExpireDisruptions(int tick)
    {
        _expiredIds.Clear();
        int count = _state.activeDisruptions.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _state.activeDisruptions[i];
            if (tick >= d.StartTick + d.DurationTicks)
                _expiredIds.Add(d.Id);
        }

        int expiredCount = _expiredIds.Count;
        for (int i = 0; i < expiredCount; i++)
        {
            int id = _expiredIds[i];
            for (int j = _state.activeDisruptions.Count - 1; j >= 0; j--)
            {
                if (_state.activeDisruptions[j].Id == id)
                {
                    var ended = _state.activeDisruptions[j];
                    _state.activeDisruptions.RemoveAt(j);
                    _pendingEvents.Add(new PendingDisruptionEvent { Kind = PendingDisruptionKind.Ended, Disruption = ended });
                    _logger.Log($"[DisruptionSystem] Disruption ended: {ended.EventType}");
                    break;
                }
            }
        }
    }

    private void ApplyActiveEffects(int tick)
    {
        int count = _state.activeDisruptions.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _state.activeDisruptions[i];
            ApplyDisruptionEffect(d, tick);
        }
    }

    private void ApplyDisruptionEffect(ActiveDisruption d, int tick)
    {
        switch (d.EventType)
        {
            case DisruptionEventType.NicheDemandShift:
                if (d.AffectedNiche.HasValue && _marketState.nicheDemand.ContainsKey(d.AffectedNiche.Value))
                {
                    var niche = d.AffectedNiche.Value;
                    float current = _marketState.nicheDemand[niche];
                    float delta = d.Magnitude * 0.001f;
                    _marketState.nicheDemand[niche] = Clamp(current + delta, 0f, 200f);
                }
                break;

            case DisruptionEventType.EconomicBoom:
                break;

            case DisruptionEventType.EconomicDip:
                break;

            case DisruptionEventType.CompetitorScandal:
                if (d.AffectedCompetitor.HasValue &&
                    _compState.competitors.TryGetValue(d.AffectedCompetitor.Value, out var scandalized))
                {
                    int repDelta = (int)(d.Magnitude * 0.01f);
                    scandalized.ReputationPoints = Math.Max(0, scandalized.ReputationPoints - repDelta);
                }
                break;

            case DisruptionEventType.CompetitorPartnership:
                if (d.AffectedCompetitor.HasValue &&
                    _compState.competitors.TryGetValue(d.AffectedCompetitor.Value, out var partner))
                {
                    int repGain = (int)(d.Magnitude * 0.01f);
                    partner.ReputationPoints = Math.Max(0, partner.ReputationPoints + repGain);
                }
                break;

            case DisruptionEventType.TechParadigmShift:
                if (d.AffectedNiche.HasValue && _marketState.nicheDemand.ContainsKey(d.AffectedNiche.Value))
                {
                    var niche = d.AffectedNiche.Value;
                    float boost = _marketState.nicheDemand[niche];
                    float delta = d.Magnitude * 0.001f;
                    _marketState.nicheDemand[niche] = Clamp(boost + delta, 0f, 200f);
                }
                break;

            case DisruptionEventType.Recession:
                break;

            case DisruptionEventType.SalarySpike:
                break;

            case DisruptionEventType.CandidateBurst:
                break;

            case DisruptionEventType.RegulatoryOverhaul:
                break;
        }
    }

    private void TrySpawnMinorDisruption(int tick)
    {
        float roll = _rng.NextFloat01();
        if (roll > 0.65f) return;

        int typeIndex = _rng.Range(0, 7);
        DisruptionEventType eventType;
        switch (typeIndex)
        {
            case 0: eventType = DisruptionEventType.NicheDemandShift;     break;
            case 1: eventType = DisruptionEventType.SalarySpike;          break;
            case 2: eventType = DisruptionEventType.CandidateBurst;       break;
            case 3: eventType = DisruptionEventType.EconomicBoom;         break;
            case 4: eventType = DisruptionEventType.EconomicDip;          break;
            case 5: eventType = DisruptionEventType.CompetitorScandal;    break;
            default: eventType = DisruptionEventType.CompetitorPartnership; break;
        }

        int durationMonths = _rng.Range(1, 4);
        float magnitude = 5f + _rng.NextFloat01() * 15f;
        ProductNiche? affectedNiche = PickRandomNiche();
        CompetitorId? affectedCompetitor = PickRandomCompetitor();

        string description = BuildDescription(eventType, false, affectedNiche, affectedCompetitor);
        var disruption = CreateDisruption(tick, eventType, false, durationMonths, magnitude, affectedNiche, affectedCompetitor, description);

        _state.activeDisruptions.Add(disruption);
        _pendingEvents.Add(new PendingDisruptionEvent { Kind = PendingDisruptionKind.MinorStarted, Disruption = disruption });
        _logger.Log($"[DisruptionSystem] Minor disruption started: {eventType} (magnitude={magnitude:F1}, duration={durationMonths}mo)");
    }

    private void TrySpawnMajorDisruption(int tick)
    {
        float roll = _rng.NextFloat01();
        if (roll > 0.50f) return;

        int typeIndex = _rng.Range(0, 3);
        DisruptionEventType eventType;
        switch (typeIndex)
        {
            case 0: eventType = DisruptionEventType.TechParadigmShift;  break;
            case 1: eventType = DisruptionEventType.Recession;           break;
            default: eventType = DisruptionEventType.RegulatoryOverhaul; break;
        }

        int durationMonths = _rng.Range(4, 13);
        float magnitude = 20f + _rng.NextFloat01() * 30f;
        ProductNiche? affectedNiche = PickRandomNiche();
        CompetitorId? affectedCompetitor = null;

        string description = BuildDescription(eventType, true, affectedNiche, affectedCompetitor);
        var disruption = CreateDisruption(tick, eventType, true, durationMonths, magnitude, affectedNiche, affectedCompetitor, description);

        _state.activeDisruptions.Add(disruption);
        _pendingEvents.Add(new PendingDisruptionEvent { Kind = PendingDisruptionKind.MajorStarted, Disruption = disruption });
        _logger.Log($"[DisruptionSystem] Major disruption started: {eventType} (magnitude={magnitude:F1}, duration={durationMonths}mo)");
    }

    private ActiveDisruption CreateDisruption(
        int tick,
        DisruptionEventType eventType,
        bool isMajor,
        int durationMonths,
        float magnitude,
        ProductNiche? affectedNiche,
        CompetitorId? affectedCompetitor,
        string description)
    {
        var disruption = new ActiveDisruption
        {
            Id = _state.nextDisruptionId++,
            EventType = eventType,
            IsMajor = isMajor,
            StartTick = tick,
            DurationTicks = durationMonths * TicksPerMonth,
            AffectedNiche = affectedNiche,
            AffectedCompetitor = affectedCompetitor,
            Magnitude = magnitude,
            Description = description
        };
        return disruption;
    }

    private ProductNiche? PickRandomNiche()
    {
        int index = _rng.Range(0, NicheValues.Length);
        return NicheValues[index];
    }

    private CompetitorId? PickRandomCompetitor()
    {
        if (_compState.competitors.Count == 0) return null;
        _competitorKeys.Clear();
        foreach (var kvp in _compState.competitors)
            _competitorKeys.Add(kvp.Key);
        int index = _rng.Range(0, _competitorKeys.Count);
        return _competitorKeys[index];
    }

    private static string BuildDescription(
        DisruptionEventType eventType,
        bool isMajor,
        ProductNiche? niche,
        CompetitorId? competitor)
    {
        string prefix = isMajor ? "Major: " : "Minor: ";
        switch (eventType)
        {
            case DisruptionEventType.NicheDemandShift:
                return prefix + $"Demand shift in {niche?.ToString() ?? "a niche"}";
            case DisruptionEventType.SalarySpike:
                return prefix + "Industry-wide salary spike";
            case DisruptionEventType.CandidateBurst:
                return prefix + "Surge of available candidates";
            case DisruptionEventType.EconomicBoom:
                return prefix + "Economic boom — revenue up across all segments";
            case DisruptionEventType.EconomicDip:
                return prefix + "Economic dip — revenue down across all segments";
            case DisruptionEventType.CompetitorScandal:
                return prefix + $"Scandal hits company #{competitor?.Value.ToString() ?? "a competitor"}";
            case DisruptionEventType.CompetitorPartnership:
                return prefix + $"Major partnership boosts company #{competitor?.Value.ToString() ?? "a competitor"}";
            case DisruptionEventType.TechParadigmShift:
                return prefix + $"Tech paradigm shift — {niche?.ToString() ?? "niches"} restructured";
            case DisruptionEventType.Recession:
                return prefix + "Recession — all revenue drops 20-30%";
            case DisruptionEventType.RegulatoryOverhaul:
                return prefix + $"Regulatory overhaul affects {niche?.ToString() ?? "a niche"}";
            default:
                return prefix + eventType.ToString();
        }
    }

    private int RollMinorInterval()
    {
        return _rng.Range(MinorIntervalMinMonths, MinorIntervalMaxMonths) * TicksPerMonth;
    }

    private int RollMajorInterval()
    {
        return _rng.Range(MajorIntervalMinMonths, MajorIntervalMaxMonths) * TicksPerMonth;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
