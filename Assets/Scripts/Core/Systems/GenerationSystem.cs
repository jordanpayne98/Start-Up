using System;
using System.Collections.Generic;

public class GenerationSystem : ISystem {
    private const int TicksPerDay = TimeState.TicksPerDay;
    private const int TicksPerYear = TimeState.TicksPerDay * TimeState.DaysPerYear;

    private const int ForecastTwoYears = TicksPerYear * 2;
    private const int ForecastOneYear = TicksPerYear;
    private const int ForecastSixMonths = TicksPerDay * 180;

    private const float CrossGenPenalty = 0.75f;
    private const float ParadigmWeightChangeThreshold = 0.01f;

    private enum PendingEventKind : byte { GenerationArrived, TransitionProgress, ParadigmWeightChanged }

    private struct PendingEvent {
        public PendingEventKind Kind;
        public int IntA;
        public float FloatA;
        public string StringA;
    }

    public event Action<int> OnGenerationArrived;
    public event Action<int, float> OnTransitionProgress;
    public event Action<string, float> OnParadigmWeightChanged;

    private GenerationState _state;
    private ArchitectureGenerationDefinition[] _genDefs;
    private IRng _rng;
    private InboxSystem _inboxSystem;

    private readonly List<PendingEvent> _pendingEvents;
    private readonly List<string> _paradigmKeys;
    private readonly Dictionary<string, float> _lastReportedWeights;
    private readonly HashSet<long> _sentForecasts;

    public GenerationSystem() {
        _pendingEvents = new List<PendingEvent>(16);
        _paradigmKeys = new List<string>(16);
        _lastReportedWeights = new Dictionary<string, float>();
        _sentForecasts = new HashSet<long>();
    }

    public void Initialize(GenerationState state, ArchitectureGenerationDefinition[] genDefs, IRng rng, InboxSystem inboxSystem) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _genDefs = genDefs ?? new ArchitectureGenerationDefinition[0];
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        _inboxSystem = inboxSystem;
    }

    public void PreTick(int tick) { }

    public void Tick(int tick) {
        if (_state == null) return;

        CheckGenerationArrival(tick);
        UpdateTransitionProgress(tick);
        DecayParadigmWeights(tick);
    }

    public void PostTick(int tick) {
        if (_state == null) return;

        EmitParadigmWeightEvents();
        SendInboxForecasts(tick);

        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++) {
            var e = _pendingEvents[i];
            switch (e.Kind) {
                case PendingEventKind.GenerationArrived:
                    OnGenerationArrived?.Invoke(e.IntA);
                    break;
                case PendingEventKind.TransitionProgress:
                    OnTransitionProgress?.Invoke(e.IntA, e.FloatA);
                    break;
                case PendingEventKind.ParadigmWeightChanged:
                    OnParadigmWeightChanged?.Invoke(e.StringA, e.FloatA);
                    break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose() { }

    public int GetCurrentGeneration() {
        return _state?.CurrentGeneration ?? 1;
    }

    public float GetParadigmWeight(string paradigmId) {
        if (_state == null || string.IsNullOrEmpty(paradigmId)) return 0f;
        if (_state.ParadigmWeights.TryGetValue(paradigmId, out float weight))
            return weight;
        return 0f;
    }

    public float GetFeatureAffinity(ProductFeatureDefinition feature, int productGeneration, GenerationStance stance, int? secondaryGen) {
        if (feature == null || feature.paradigmAffinities == null || feature.paradigmAffinities.Length == 0)
            return 1f;

        float baseAffinity = ComputeParadigmAffinity(feature.paradigmAffinities);

        if (stance != GenerationStance.CrossGen || !secondaryGen.HasValue)
            return baseAffinity;

        float secondaryAffinity = ComputeSecondaryAffinity(feature, secondaryGen.Value);
        float crossGenAffinity = secondaryAffinity * CrossGenPenalty;
        return UnityEngine.Mathf.Max(baseAffinity, crossGenAffinity);
    }

    public FeatureDemandStage GetFeatureDemandStage(ProductFeatureDefinition feature, float competitorCoverageRatio) {
        if (feature == null) return FeatureDemandStage.NotAvailable;
        int currentGen = GetCurrentGeneration();
        return FeatureDemandHelper.GetDemandStage(
            currentGen,
            feature.demandIntroductionGen,
            feature.demandMaturitySpeed,
            feature.isFoundational,
            competitorCoverageRatio);
    }

    public bool IsFeatureAvailable(ProductFeatureDefinition feature, int productGeneration, GenerationStance stance) {
        if (feature == null) return false;
        if (productGeneration < feature.availableFromGeneration) return false;
        if (feature.nativeOnly && stance == GenerationStance.CrossGen) return false;
        return true;
    }

    private void CheckGenerationArrival(int tick) {
        if (_state.ActualNextGenArrivalTick <= 0) return;
        if (tick < _state.ActualNextGenArrivalTick) return;
        if (_state.IsTransitioning) return;

        int nextGen = _state.CurrentGeneration + 1;
        int nextGenIndex = nextGen - 1;
        if (nextGenIndex >= _genDefs.Length) return;

        _state.IsTransitioning = true;
        _state.CurrentGenerationArrivalTick = tick;
        _state.TransitionProgress = 0f;

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.GenerationArrived, IntA = nextGen });
    }

    private void UpdateTransitionProgress(int tick) {
        if (!_state.IsTransitioning) return;

        int genIndex = _state.CurrentGeneration - 1;
        if (genIndex < 0 || genIndex >= _genDefs.Length) return;

        var currentDef = _genDefs[genIndex];
        if (currentDef == null) return;

        int transitionDuration = currentDef.transitionDurationTicks;
        if (transitionDuration <= 0) transitionDuration = TicksPerYear;

        int ticksSinceArrival = tick - _state.CurrentGenerationArrivalTick;
        float t = UnityEngine.Mathf.Clamp01((float)ticksSinceArrival / transitionDuration);
        float progress = SCurve(t);

        _state.TransitionProgress = progress;

        if (progress >= 1f) {
            CompleteTransition();
            return;
        }

        _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.TransitionProgress, IntA = _state.CurrentGeneration, FloatA = progress });

        UpdateNewGenParadigmWeights(progress);
    }

    private void CompleteTransition() {
        _state.CurrentGeneration++;
        _state.IsTransitioning = false;
        _state.TransitionProgress = 0f;

        int newGenIndex = _state.CurrentGeneration - 1;
        if (newGenIndex >= 0 && newGenIndex < _genDefs.Length) {
            var newDef = _genDefs[newGenIndex];
            if (newDef?.paradigms != null) {
                for (int i = 0; i < newDef.paradigms.Length; i++) {
                    var p = newDef.paradigms[i];
                    if (p != null && !string.IsNullOrEmpty(p.paradigmId))
                        _state.ParadigmWeights[p.paradigmId] = 1f;
                }
            }
        }

        int nextGenIndex = _state.CurrentGeneration;
        if (nextGenIndex < _genDefs.Length)
            _state.ActualNextGenArrivalTick = _state.GenerationArrivalTicks[nextGenIndex];
        else
            _state.ActualNextGenArrivalTick = 0;
    }

    private void DecayParadigmWeights(int tick) {
        if (_genDefs == null) return;

        _paradigmKeys.Clear();
        foreach (var key in _state.ParadigmWeights.Keys)
            _paradigmKeys.Add(key);

        int count = _paradigmKeys.Count;
        for (int i = 0; i < count; i++) {
            var paradigmId = _paradigmKeys[i];
            float decayRate = FindDecayRate(paradigmId);
            if (decayRate <= 0f) continue;

            float current = _state.ParadigmWeights[paradigmId];
            float decayedPerTick = decayRate / TicksPerDay;
            float newWeight = UnityEngine.Mathf.Max(0f, current - decayedPerTick);
            _state.ParadigmWeights[paradigmId] = newWeight;
        }
    }

    private void UpdateNewGenParadigmWeights(float progress) {
        int nextGenIndex = _state.CurrentGeneration;
        if (nextGenIndex >= _genDefs.Length) return;

        var nextDef = _genDefs[nextGenIndex];
        if (nextDef?.paradigms == null) return;

        for (int i = 0; i < nextDef.paradigms.Length; i++) {
            var p = nextDef.paradigms[i];
            if (p == null || string.IsNullOrEmpty(p.paradigmId)) continue;

            if (!_state.ParadigmWeights.ContainsKey(p.paradigmId))
                _state.ParadigmWeights[p.paradigmId] = 0f;

            float current = _state.ParadigmWeights[p.paradigmId];
            if (current < progress)
                _state.ParadigmWeights[p.paradigmId] = progress;
        }
    }

    private void EmitParadigmWeightEvents() {
        _paradigmKeys.Clear();
        foreach (var key in _state.ParadigmWeights.Keys)
            _paradigmKeys.Add(key);

        int count = _paradigmKeys.Count;
        for (int i = 0; i < count; i++) {
            var paradigmId = _paradigmKeys[i];
            float current = _state.ParadigmWeights[paradigmId];

            if (!_lastReportedWeights.TryGetValue(paradigmId, out float last) ||
                System.Math.Abs(current - last) >= ParadigmWeightChangeThreshold) {
                _lastReportedWeights[paradigmId] = current;
                _pendingEvents.Add(new PendingEvent { Kind = PendingEventKind.ParadigmWeightChanged, StringA = paradigmId, FloatA = current });
            }
        }
    }

    private void SendInboxForecasts(int tick) {
        if (_inboxSystem == null) return;
        if (_state.ActualNextGenArrivalTick <= 0) return;

        int ticksUntilNextGen = _state.ActualNextGenArrivalTick - tick;
        int nextGenIndex = _state.CurrentGeneration;

        if (ticksUntilNextGen <= ForecastTwoYears && ticksUntilNextGen > ForecastOneYear) {
            TrySendForecast(nextGenIndex, 0, tick,
                "Research Breakthrough",
                "Industry researchers demonstrate a breakthrough in next-generation architecture. Experts suggest the new paradigm could reshape development practices within a few years.",
                MailPriority.Info);
        } else if (ticksUntilNextGen <= ForecastOneYear && ticksUntilNextGen > ForecastSixMonths) {
            TrySendForecast(nextGenIndex, 1, tick,
                "Industry Roadmap Announced",
                "A major company has published its roadmap for transitioning to next-generation architecture. Market analysts expect broad industry adoption within the year.",
                MailPriority.Warning);
        } else if (ticksUntilNextGen <= ForecastSixMonths && ticksUntilNextGen > 0) {
            TrySendForecast(nextGenIndex, 2, tick,
                "Development Kits Shipping",
                "Development kits for the next generation are shipping to select partners. Products built on the new architecture will gain a significant quality ceiling advantage.",
                MailPriority.Warning);
        } else if (ticksUntilNextGen <= 0 && tick >= _state.ActualNextGenArrivalTick) {
            TrySendForecast(nextGenIndex, 3, tick,
                "New Technology Era Has Begun",
                "A new architecture generation has arrived. Products built on the legacy paradigm will face increasing quality ceilings and market penalties.",
                MailPriority.Critical);
        }
    }

    private void TrySendForecast(int genIndex, int stage, int tick, string title, string body, MailPriority priority) {
        long key = (long)genIndex * 10 + stage;
        if (_sentForecasts.Contains(key)) return;
        _sentForecasts.Add(key);

        _inboxSystem.AddMail(new MailItem {
            Tick = tick,
            Category = MailCategory.Technology,
            Priority = priority,
            Title = title,
            Body = body,
            Actions = new MailAction[0]
        });
    }

    private float FindDecayRate(string paradigmId) {
        for (int g = 0; g < _genDefs.Length; g++) {
            var def = _genDefs[g];
            if (def?.paradigms == null) continue;
            for (int i = 0; i < def.paradigms.Length; i++) {
                var p = def.paradigms[i];
                if (p != null && p.paradigmId == paradigmId)
                    return p.decayRate;
            }
        }
        return 0f;
    }

    private float ComputeParadigmAffinity(ParadigmAffinity[] affinities) {
        float result = 1f;
        int count = affinities.Length;
        for (int i = 0; i < count; i++) {
            float weight = GetParadigmWeight(affinities[i].paradigmId);
            result *= UnityEngine.Mathf.Lerp(1f, affinities[i].affinity, weight);
        }
        return result;
    }

    private float ComputeSecondaryAffinity(ProductFeatureDefinition feature, int secondaryGenNumber) {
        int secondaryGenIndex = secondaryGenNumber - 1;
        if (secondaryGenIndex < 0 || secondaryGenIndex >= _genDefs.Length) return 1f;

        var secondaryDef = _genDefs[secondaryGenIndex];
        if (secondaryDef?.paradigms == null) return 1f;

        float result = 1f;
        int affCount = feature.paradigmAffinities.Length;
        for (int i = 0; i < affCount; i++) {
            var aff = feature.paradigmAffinities[i];
            bool isSecondaryParadigm = false;
            for (int j = 0; j < secondaryDef.paradigms.Length; j++) {
                if (secondaryDef.paradigms[j]?.paradigmId == aff.paradigmId) {
                    isSecondaryParadigm = true;
                    break;
                }
            }

            if (isSecondaryParadigm) {
                float weight = GetParadigmWeight(aff.paradigmId);
                result *= UnityEngine.Mathf.Lerp(1f, aff.affinity, weight);
            }
        }
        return result;
    }

    private static float SCurve(float t) {
        return 3f * t * t - 2f * t * t * t;
    }
}
