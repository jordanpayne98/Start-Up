// AbilitySystem Version: Clean v1
using System;
using System.Collections.Generic;

// Lightweight ISystem that owns PA generation for employees/candidates, computes Ability on demand,
// and exposes data to the read model via IAbilityReadModel.
// Ability is never stored — computed and cached here, invalidated on skill-change events.
public class AbilitySystem : ISystem
{
    private readonly EmployeeState _employeeState;
    private readonly RoleTierTable _tierTable;
    private readonly IRng _rng;
    private readonly ILogger _logger;
    private TuningConfig _tuning;

    // Ability cache — invalidated on skill change
    private readonly Dictionary<EmployeeId, int> _caCache = new Dictionary<EmployeeId, int>();

    public AbilitySystem(EmployeeState employeeState, RoleTierTable tierTable, IRng rng, ILogger logger)
    {
        _employeeState = employeeState ?? throw new ArgumentNullException(nameof(employeeState));
        _tierTable     = tierTable     ?? throw new ArgumentNullException(nameof(tierTable));
        _rng           = rng           ?? throw new ArgumentNullException(nameof(rng));
        _logger        = logger        ?? new NullLogger();
    }

    // Exposes the RoleTierTable so the read model can compute cross-role ability queries.
    public RoleTierTable TierTable => _tierTable;

    // Called to invalidate the Ability cache for an employee after a skill change.
    public void InvalidateCA(EmployeeId id)
    {
        _caCache.Remove(id);
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    // Called by EmployeeSystem.HireEmployee after hire — assigns HiddenAttributes to the hired employee.
    // PA transfers from CandidateData.PotentialAbility → employee.potentialAbility before this is called.
    public void OnEmployeeHired(EmployeeId id, Employee employee)
    {
        if (employee == null) return;

        if (employee.potentialAbility <= 0)
        {
            int paMin = _tuning != null ? _tuning.AbilityFallbackPAMin : 60;
            int paMax = _tuning != null ? _tuning.AbilityFallbackPAMax : 151;
            employee.potentialAbility = _rng.Range(paMin, paMax);
            _logger.LogWarning($"[AbilitySystem] Employee {id.Value} had no PA set — fallback PA:{employee.potentialAbility}");
        }

        bool hasPresetAttrs = employee.hiddenAttributes.LearningRate > 0
            && employee.hiddenAttributes.Creative > 0
            && employee.hiddenAttributes.WorkEthic > 0
            && employee.hiddenAttributes.Adaptability > 0
            && employee.hiddenAttributes.Ambition > 0;

        if (!hasPresetAttrs) {
            employee.hiddenAttributes = GenerateHiddenAttributesForPA(employee.potentialAbility);
        }
        _caCache.Remove(id);

        int[] tiers = _tierTable.GetTiers(employee.role);
        int ca = AbilityCalculator.ComputeAbility(employee.skills, tiers);
        if (ca > employee.potentialAbility) ca = employee.potentialAbility;
        _caCache[id] = ca;

        _logger.Log($"[Hired] {employee.name} ({employee.role}) | Ability:{ca} PA:{employee.potentialAbility} | LR:{employee.hiddenAttributes.LearningRate} Cr:{employee.hiddenAttributes.Creative} WE:{employee.hiddenAttributes.WorkEthic} A:{employee.hiddenAttributes.Adaptability} Amb:{employee.hiddenAttributes.Ambition}");
    }

    // Called by HRSystem.GenerateTargetedCandidate — bakes HiddenAttributes into CandidateData.
    // PA must already be set on candidate (from GenerateCandidate) before calling this.
    public void GenerateCandidateAbility(CandidateData candidate)
    {
        if (candidate == null) return;
        if (candidate.PotentialAbility == 0)
            _logger.LogWarning($"[AbilitySystem] GenerateCandidateAbility called with PA=0 on candidate {candidate.CandidateId}");
        candidate.HiddenAttributes = GenerateHiddenAttributesForPA(candidate.PotentialAbility);
    }

    // Returns the role-weighted Ability for a candidate (uses their current role's tier profile).
    public int ComputeCandidateCA(CandidateData candidate)
    {
        if (candidate == null) return 0;
        int[] tiers = _tierTable.GetTiers(candidate.Role);
        int ca = AbilityCalculator.ComputeAbility(candidate.Skills, tiers);
        if (ca > candidate.PotentialAbility) ca = candidate.PotentialAbility;
        return ca;
    }

    // Pure read — returns cached or recomputed Ability. No allocation.
    public int GetCA(EmployeeId id, EmployeeRole role)
    {
        if (_caCache.TryGetValue(id, out int cached))
            return cached;

        if (!_employeeState.employees.TryGetValue(id, out var employee) || !employee.isActive)
            return 0;

        int[] tiers = _tierTable.GetTiers(role);
        int ca = AbilityCalculator.ComputeAbility(employee.skills, tiers);
        if (ca > employee.potentialAbility) ca = employee.potentialAbility;
        _caCache[id] = ca;
        return ca;
    }

    // Returns ability/potential estimate for a candidate.
    // If interview is active or complete: delegates to InterviewSystem for noise-based estimates.
    // If no interview: returns ShowAsUnknown = true (manual mode fallback).
    public CandidatePotentialEstimate GetCandidateEstimate(CandidateData candidate, int hrSkillAverage, HiringMode mode = HiringMode.HR, bool interviewComplete = false, InterviewSystem interviewSystem = null)
    {
        if (candidate == null)
            return new CandidatePotentialEstimate { PotentialStarsMin = 1, PotentialStarsMax = 5 };

        int[] tiers = _tierTable.GetTiers(candidate.Role);
        int trueCA = AbilityCalculator.ComputeAbility(candidate.Skills, tiers);
        if (trueCA > candidate.PotentialAbility) trueCA = candidate.PotentialAbility;

        if (interviewSystem != null)
        {
            float knowledge = interviewSystem.GetKnowledgeLevel(candidate.CandidateId);
            bool hasInterview = knowledge > 0f || interviewComplete;

            if (hasInterview)
            {
                int abilityStars = interviewSystem.GetAbilityStarEstimate(candidate.CandidateId, trueCA, tiers);
                int potentialStars = interviewSystem.GetPotentialStarEstimate(candidate.CandidateId, candidate.PotentialAbility);

                if (abilityStars < 0) abilityStars = 0;
                if (potentialStars < 0) potentialStars = 0;

                bool showUnknown = abilityStars <= 0 && potentialStars <= 0;
                if (showUnknown)
                {
                    return new CandidatePotentialEstimate
                    {
                        AbilityMin = 0, AbilityMax = 200,
                        PotentialStarsMin = 1, PotentialStarsMax = 5,
                        ShowAsUnknown = false
                    };
                }

                int aMin = abilityStars > 0 ? abilityStars : 1;
                int aMax = abilityStars > 0 ? abilityStars : 5;
                int pMin = potentialStars > 0 ? potentialStars : 1;
                int pMax = potentialStars > 0 ? potentialStars : 5;

                return new CandidatePotentialEstimate
                {
                    AbilityMin = aMin,
                    AbilityMax = aMax,
                    PotentialStarsMin = pMin,
                    PotentialStarsMax = pMax,
                    ShowAsUnknown = false
                };
            }
        }

        if (mode == HiringMode.Manual && hrSkillAverage == -1)
        {
            return new CandidatePotentialEstimate
            {
                AbilityMin = 0, AbilityMax = 200,
                PotentialStarsMin = 1, PotentialStarsMax = 5,
                ShowAsUnknown = true
            };
        }

        // No interview started — return unknown
        return new CandidatePotentialEstimate
        {
            AbilityMin = 0, AbilityMax = 200,
            PotentialStarsMin = 1, PotentialStarsMax = 5,
            ShowAsUnknown = true
        };
    }

    // ISystem — PA back-fill for existing employees with default (0) PA is handled by save migration.
    public void PreTick(int tick)  { }
    public void Tick(int tick)     { }
    public void PostTick(int tick) { }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        _caCache.Clear();
    }

    // PA-linked generation. Higher PA → higher floor and ceiling for all hidden attributes.
    // PA 0–200 → floor/ceiling mapped to 0–20 attribute range.
    // Formula: floor = PA/20 (capped 1-10), spread = (20-floor)/2 + 1.
    private HiddenAttributes GenerateHiddenAttributesForPA(int pa)
    {
        int floor = pa / 20;
        if (floor < 1) floor = 1;
        if (floor > 10) floor = 10;

        int spread = (20 - floor) / 2 + 1;

        int Attr() => Clamp(_rng.Range(floor, floor + spread + 1) + _rng.Range(-1, 2), 1, 20);

        return new HiddenAttributes
        {
            LearningRate = Attr(),
            WorkEthic    = Attr(),
            Adaptability = Attr(),
            Ambition     = Attr(),
            Creative     = Attr()
        };
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
}
