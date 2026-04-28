// AbilitySystem Version: Clean v2
using System;
using System.Collections.Generic;

// Lightweight ISystem that owns PA generation for employees/candidates, computes Ability on demand,
// and exposes data to the read model via IAbilityReadModel.
// Ability is never stored — computed and cached here, invalidated on skill-change events.
public class AbilitySystem : ISystem
{
    private readonly EmployeeState _employeeState;
    private readonly RoleProfileTable _profileTable;
    private readonly IRng _rng;
    private readonly ILogger _logger;
    private TuningConfig _tuning;

    // Ability cache — invalidated on skill change
    private readonly Dictionary<EmployeeId, int> _caCache = new Dictionary<EmployeeId, int>();

    public AbilitySystem(EmployeeState employeeState, RoleProfileTable profileTable, IRng rng, ILogger logger)
    {
        _employeeState = employeeState ?? throw new ArgumentNullException(nameof(employeeState));
        _profileTable  = profileTable  ?? throw new ArgumentNullException(nameof(profileTable));
        _rng           = rng           ?? throw new ArgumentNullException(nameof(rng));
        _logger        = logger        ?? new NullLogger();
    }

    // Exposes the RoleProfileTable so the read model can compute cross-role ability queries.
    public RoleProfileTable ProfileTable => _profileTable;

    // Called to invalidate the Ability cache for an employee after a skill change.
    public void InvalidateCA(EmployeeId id)
    {
        _caCache.Remove(id);
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    // Called by EmployeeSystem.HireEmployee after hire.
    public void OnEmployeeHired(EmployeeId id, Employee employee)
    {
        if (employee == null) return;

        if (employee.Stats.PotentialAbility <= 0)
        {
            int paMin = _tuning != null ? _tuning.AbilityFallbackPAMin : 60;
            int paMax = _tuning != null ? _tuning.AbilityFallbackPAMax : 151;
            employee.Stats.PotentialAbility = _rng.Range(paMin, paMax);
            _logger.LogWarning($"[AbilitySystem] Employee {id.Value} had no PA set — fallback PA:{employee.Stats.PotentialAbility}");
        }

        _caCache.Remove(id);

        int[] tiers = GetTiersForRole(employee.role);
        int ca = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
        if (ca > employee.Stats.PotentialAbility) ca = employee.Stats.PotentialAbility;
        _caCache[id] = ca;

        _logger.Log($"[Hired] {employee.name} ({employee.role}) | Ability:{ca} PA:{employee.Stats.PotentialAbility}");
    }

    // Called by HRSystem — ensures candidate has generated hidden attributes if not already set.
    public void GenerateCandidateAbility(CandidateData candidate)
    {
        if (candidate == null) return;
        if (candidate.Stats.PotentialAbility == 0)
            _logger.LogWarning($"[AbilitySystem] GenerateCandidateAbility called with PA=0 on candidate {candidate.CandidateId}");
        // Hidden attributes are generated inside CandidateData.GenerateCandidate; nothing to do here.
    }

    // Returns the role-weighted Ability for a candidate (uses their current role's tier profile).
    public int ComputeCandidateCA(CandidateData candidate)
    {
        if (candidate == null) return 0;
        int[] tiers = GetTiersForRole(candidate.Role);
        int ca = AbilityCalculator.ComputeAbility(candidate.Stats.Skills, tiers);
        if (ca > candidate.Stats.PotentialAbility) ca = candidate.Stats.PotentialAbility;
        return ca;
    }

    // Pure read — returns cached or recomputed Ability. No allocation.
    public int GetCA(EmployeeId id, RoleId role)
    {
        if (_caCache.TryGetValue(id, out int cached))
            return cached;

        if (!_employeeState.employees.TryGetValue(id, out var employee) || !employee.isActive)
            return 0;

        int[] tiers = GetTiersForRole(role);
        int ca = AbilityCalculator.ComputeAbility(employee.Stats.Skills, tiers);
        if (ca > employee.Stats.PotentialAbility) ca = employee.Stats.PotentialAbility;
        _caCache[id] = ca;
        return ca;
    }

    // Returns ability/potential estimate for a candidate.
    public CandidatePotentialEstimate GetCandidateEstimate(CandidateData candidate, int hrSkillAverage, HiringMode mode = HiringMode.HR, bool interviewComplete = false, InterviewSystem interviewSystem = null)
    {
        if (candidate == null)
            return new CandidatePotentialEstimate { PotentialStarsMin = 1, PotentialStarsMax = 5 };

        int[] tiers = GetTiersForRole(candidate.Role);
        int trueCA = AbilityCalculator.ComputeAbility(candidate.Stats.Skills, tiers);
        if (trueCA > candidate.Stats.PotentialAbility) trueCA = candidate.Stats.PotentialAbility;

        if (interviewSystem != null)
        {
            float knowledge = interviewSystem.GetKnowledgeLevel(candidate.CandidateId);
            bool hasInterview = knowledge > 0f || interviewComplete;

            if (hasInterview)
            {
                int abilityStars = interviewSystem.GetAbilityStarEstimate(candidate.CandidateId, trueCA, tiers);
                int potentialStars = interviewSystem.GetPotentialStarEstimate(candidate.CandidateId, candidate.Stats.PotentialAbility);

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

        return new CandidatePotentialEstimate
        {
            AbilityMin = 0, AbilityMax = 200,
            PotentialStarsMin = 1, PotentialStarsMax = 5,
            ShowAsUnknown = true
        };
    }

    // ISystem — no per-tick work needed.
    public void PreTick(int tick)  { }
    public void Tick(int tick)     { }
    public void PostTick(int tick) { }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        _caCache.Clear();
    }

    // Returns the tier array for a role from the profile table, falling back to uniform Secondary.
    private int[] GetTiersForRole(RoleId role)
    {
        var profile = _profileTable?.Get(role);
        if (profile != null)
            return RoleSuitabilityCalculator.BuildTierArray(profile);
        return BuildUniformTiers();
    }

    private static int[] BuildUniformTiers()
    {
        var tiers = new int[SkillIdHelper.SkillCount];
        for (int i = 0; i < SkillIdHelper.SkillCount; i++) tiers[i] = 3;
        return tiers;
    }
}
