using System;
using System.Collections.Generic;

[Serializable]
public class GenerationState {
    public int CurrentGeneration;
    public int CurrentGenerationArrivalTick;
    public float TransitionProgress;
    public bool IsTransitioning;
    public Dictionary<string, float> ParadigmWeights;
    public int NextGenerationForecastTick;
    public int ActualNextGenArrivalTick;
    public List<int> GenerationArrivalTicks;

    public static GenerationState CreateNew(IRng rng, ArchitectureGenerationDefinition[] genDefs) {
        var state = new GenerationState {
            CurrentGeneration = 1,
            CurrentGenerationArrivalTick = 0,
            TransitionProgress = 0f,
            IsTransitioning = false,
            ParadigmWeights = new Dictionary<string, float>(),
            NextGenerationForecastTick = 0,
            ActualNextGenArrivalTick = 0,
            GenerationArrivalTicks = new List<int>()
        };

        if (genDefs == null || genDefs.Length == 0)
            return state;

        int accumulatedTick = 0;
        for (int g = 0; g < genDefs.Length; g++) {
            var def = genDefs[g];
            if (def == null) continue;

            int range = def.arrivalTickMax - def.arrivalTickMin;
            int arrivalOffset = range > 0 ? rng.Range(def.arrivalTickMin, def.arrivalTickMax + 1) : def.arrivalTickMin;
            accumulatedTick += (g == 0) ? 0 : arrivalOffset;
            state.GenerationArrivalTicks.Add(accumulatedTick);
        }

        if (state.GenerationArrivalTicks.Count > 0)
            state.ActualNextGenArrivalTick = state.GenerationArrivalTicks.Count > 1
                ? state.GenerationArrivalTicks[1]
                : 0;

        if (genDefs[0] != null && genDefs[0].paradigms != null) {
            var paradigms = genDefs[0].paradigms;
            for (int i = 0; i < paradigms.Length; i++) {
                if (paradigms[i] != null)
                    state.ParadigmWeights[paradigms[i].paradigmId] = 1f;
            }
        }

        return state;
    }
}
