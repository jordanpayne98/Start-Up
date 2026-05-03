// AbilitySystem Version: Wave 3A — Role-Aware CA Cache
using System;
using System.Collections.Generic;

// Lightweight ISystem that owns PA generation for employees/candidates, computes Ability on demand,
// and exposes data to the read model via IAbilityReadModel.
// CA is computed per (employee, role) pair using the weighted average formula and cached.
public class AbilitySystem : ISystem
{
    private readonly EmployeeState _employeeState;
    private readonly RoleProfileTable _profileTable;
    private readonly IRng _rng;
    private readonly ILogger _logger;
    private TuningConfig _tuning;

    // Role CA cache — keyed by packed (EmployeeId, RoleId).
    // Key = ((long)employeeId.Value << 32) | (uint)(int)roleId
    private readonly Dictionary<long, int> _roleCaCache = new Dictionary<long, int>();

    // Best Role cache — stores the max CA across all roles per employee.
    private struct BestRoleResult
    {
        public int CA;
        public RoleId Role;
    }
    private readonly Dictionary<EmployeeId, BestRoleResult> _bestRoleCache = new Dictionary<EmployeeId, BestRoleResult>();

    // Keys stored per employee for efficient invalidation.
    private readonly Dictionary<EmployeeId, List<long>> _employeeRoleKeys = new Dictionary<EmployeeId, List<long>>();

    public AbilitySystem(EmployeeState employeeState, RoleProfileTable profileTable, IRng rng, ILogger logger)
    {
        _employeeState = employeeState ?? throw new ArgumentNullException(nameof(employeeState));
        _profileTable  = profileTable  ?? throw new ArgumentNullException(nameof(profileTable));
        _rng           = rng           ?? throw new ArgumentNullException(nameof(rng));
        _logger        = logger        ?? new NullLogger();
    }

    // Exposes the RoleProfileTable so the read model can compute cross-role ability queries.
    public RoleProfileTable ProfileTable => _profileTable;

    // -------------------------------------------------------------------------
    // Cache key helpers
    // -------------------------------------------------------------------------
    private static long PackKey(EmployeeId id, RoleId role)
    {
        return ((long)id.Value << 32) | (uint)(int)role;
    }

    // -------------------------------------------------------------------------
    // Employee Role CA queries
    // -------------------------------------------------------------------------

    // Returns the CA for an employee in a specific role. Cached.
    public int GetRoleCA(EmployeeId id, RoleId role)
    {
        long key = PackKey(id, role);
        if (_roleCaCache.TryGetValue(key, out int cached))
            return cached;

        if (!_employeeState.employees.TryGetValue(id, out var employee) || !employee.isActive)
            return 0;

        int ca = ComputeRoleCARaw(employee.Stats.Skills, role);
        if (ca > employee.Stats.PotentialAbility) ca = employee.Stats.PotentialAbility;

        StoreRoleCacheEntry(id, key, ca);
        return ca;
    }

    // Returns the CA for an employee in their currently assigned role.
    public int GetCurrentRoleCA(EmployeeId id)
    {
        if (!_employeeState.employees.TryGetValue(id, out var employee) || !employee.isActive)
            return 0;
        return GetRoleCA(id, employee.role);
    }

    // Returns the highest CA across all roles for an employee, capped by PA.
    public int GetBestRoleCA(EmployeeId id)
    {
        if (_bestRoleCache.TryGetValue(id, out var cached))
            return cached.CA;

        return ComputeAndCacheBestRole(id);
    }

    // Returns the role that gives the highest CA for an employee.
    public RoleId GetBestRole(EmployeeId id)
    {
        if (_bestRoleCache.TryGetValue(id, out var cached))
            return cached.Role;

        ComputeAndCacheBestRole(id);
        if (_bestRoleCache.TryGetValue(id, out var result))
            return result.Role;
        return RoleId.SoftwareEngineer;
    }

    // Returns the stored PA for an employee.
    public int GetPotentialAbility(EmployeeId id)
    {
        if (!_employeeState.employees.TryGetValue(id, out var employee) || !employee.isActive)
            return 0;
        return employee.Stats.PotentialAbility;
    }

    // Legacy accessor — maps to GetCurrentRoleCA for backward compat with existing callers.
    // Param 'role' is respected: returns GetRoleCA(id, role).
    public int GetCA(EmployeeId id, RoleId role)
    {
        return GetRoleCA(id, role);
    }

    // -------------------------------------------------------------------------
    // Candidate CA queries
    // -------------------------------------------------------------------------

    // Returns the CA for a candidate in a specific role. Not cached (candidates are transient).
    public int ComputeCandidateRoleCA(CandidateData candidate, RoleId role)
    {
        if (candidate == null) return 0;
        int ca = ComputeRoleCARaw(candidate.Stats.Skills, role);
        if (ca > candidate.Stats.PotentialAbility) ca = candidate.Stats.PotentialAbility;
        return ca;
    }

    // Returns the highest CA across all roles for a candidate, outputs the best role.
    public int ComputeCandidateBestRoleCA(CandidateData candidate, out RoleId bestRole)
    {
        bestRole = RoleId.SoftwareEngineer;
        if (candidate == null) return 0;

        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int bestCA = 0;
        for (int i = 0; i < allRoles.Length; i++)
        {
            int ca = ComputeRoleCARaw(candidate.Stats.Skills, allRoles[i]);
            if (ca > bestCA)
            {
                bestCA = ca;
                bestRole = allRoles[i];
            }
        }
        if (bestCA > candidate.Stats.PotentialAbility) bestCA = candidate.Stats.PotentialAbility;
        return bestCA;
    }

    // Legacy accessor — maps to ComputeCandidateRoleCA(candidate.Role).
    public int ComputeCandidateCA(CandidateData candidate)
    {
        if (candidate == null) return 0;
        return ComputeCandidateRoleCA(candidate, candidate.Role);
    }

    // Returns a confidence-ranged CA/PA estimate for a candidate using the new source-based widths.
    public CandidatePotentialEstimate GetCandidateEstimate(
        CandidateData candidate,
        RoleId role,
        CandidateConfidenceInputs inputs)
    {
        if (candidate == null)
        {
            return new CandidatePotentialEstimate
            {
                AbilityMin = 1, AbilityMax = 200,
                PotentialStarsMin = 1, PotentialStarsMax = 5,
                ShowAsUnknown = true,
                ConfidenceLabel = CandidateConfidenceLevel.Low
            };
        }

        int trueCA = ComputeCandidateRoleCA(candidate, role);
        int truePA = candidate.Stats.PotentialAbility;

        // Determine range widths from source + interview state (spec section 15.2)
        int caHalf;
        int paHalf;
        CandidateConfidenceLevel confidence;

        if (inputs.InterviewComplete && inputs.InterviewKnowledge >= 0.8f)
        {
            caHalf = 3;
            paHalf = 5;
            confidence = CandidateConfidenceLevel.Confirmed;
        }
        else if (inputs.InterviewComplete || inputs.InterviewKnowledge >= 0.4f)
        {
            caHalf = 5;
            paHalf = 10;
            confidence = CandidateConfidenceLevel.High;
        }
        else if (inputs.InterviewKnowledge > 0f)
        {
            caHalf = 10;
            paHalf = 20;
            confidence = CandidateConfidenceLevel.Medium;
        }
        else if (inputs.Source == HiringMode.HR)
        {
            caHalf = 15;
            paHalf = 25;
            confidence = CandidateConfidenceLevel.Medium;
        }
        else
        {
            caHalf = 20;
            paHalf = 35;
            confidence = CandidateConfidenceLevel.Low;
        }

        int caMin = Clamp(trueCA - caHalf, 0, 200);
        int caMax = Clamp(trueCA + caHalf, 0, 200);
        int paMin = Clamp(truePA - paHalf, 0, 200);
        int paMax = Clamp(truePA + paHalf, 0, 200);

        int caStarsMin = AbilityCalculator.AbilityToStars(caMin);
        int caStarsMax = AbilityCalculator.AbilityToStars(caMax);
        int paStarsMin = AbilityCalculator.PotentialToStars(paMin);
        int paStarsMax = AbilityCalculator.PotentialToStars(paMax);

        return new CandidatePotentialEstimate
        {
            AbilityMin = caStarsMin,
            AbilityMax = caStarsMax,
            PotentialStarsMin = paStarsMin,
            PotentialStarsMax = paStarsMax,
            RoleCAMin = caMin,
            RoleCAMax = caMax,
            PAMin = paMin,
            PAMax = paMax,
            ShowAsUnknown = false,
            ConfidenceLabel = confidence
        };
    }

    // Legacy signature for backward compat with GameStateSnapshot callers.
    public CandidatePotentialEstimate GetCandidateEstimate(
        CandidateData candidate,
        int hrSkillAverage,
        HiringMode mode = HiringMode.HR,
        bool interviewComplete = false,
        InterviewSystem interviewSystem = null)
    {
        if (candidate == null)
        {
            return new CandidatePotentialEstimate
            {
                PotentialStarsMin = 1, PotentialStarsMax = 5,
                ShowAsUnknown = true,
                ConfidenceLabel = CandidateConfidenceLevel.Low
            };
        }

        float knowledge = 0f;
        if (interviewSystem != null)
            knowledge = interviewSystem.GetKnowledgeLevel(candidate.CandidateId);

        var inputs = new CandidateConfidenceInputs
        {
            Source = mode,
            InterviewComplete = interviewComplete,
            InterviewKnowledge = knowledge,
            HrSkillAverage = hrSkillAverage
        };

        return GetCandidateEstimate(candidate, candidate.Role, inputs);
    }

    // -------------------------------------------------------------------------
    // Cache management
    // -------------------------------------------------------------------------

    // Clears all cached CA entries for a specific employee. Call on skill change.
    public void InvalidateCA(EmployeeId id)
    {
        _bestRoleCache.Remove(id);
        if (_employeeRoleKeys.TryGetValue(id, out var keys))
        {
            int count = keys.Count;
            for (int i = 0; i < count; i++)
                _roleCaCache.Remove(keys[i]);
            keys.Clear();
        }
    }

    // Clears all cached CA entries. Call on save load, profile change, or debug stat edits.
    public void InvalidateAllAbilityCaches()
    {
        _roleCaCache.Clear();
        _bestRoleCache.Clear();
        _employeeRoleKeys.Clear();
    }

    // -------------------------------------------------------------------------
    // System lifecycle
    // -------------------------------------------------------------------------

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

        InvalidateCA(id);

        int ca = GetRoleCA(id, employee.role);
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

    // ISystem — no per-tick work needed.
    public void PreTick(int tick)  { }
    public void Tick(int tick)     { }
    public void PostTick(int tick) { }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        InvalidateAllAbilityCaches();
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    // Computes Role CA from raw skills using the profile's SkillBands. No caching.
    private int ComputeRoleCARaw(int[] skills, RoleId role)
    {
        var profile = _profileTable?.Get(role);
        if (profile != null && profile.SkillBands != null)
            return AbilityCalculator.ComputeRoleCA(skills, profile.SkillBands);
        // Fallback: treat all skills as Secondary
        return ComputeRoleCARawUniform(skills);
    }

    private static int ComputeRoleCARawUniform(int[] skills)
    {
        if (skills == null) return 0;
        int count = SkillIdHelper.SkillCount;
        float total = 0f;
        float w = RoleWeightBandHelper.ToWeight(RoleWeightBand.Secondary);
        for (int i = 0; i < count && i < skills.Length; i++)
        {
            int level = skills[i];
            if (level < 0) level = 0;
            if (level > 20) level = 20;
            total += level * 10f * w;
        }
        return (int)(total / (count * w));
    }

    private int ComputeAndCacheBestRole(EmployeeId id)
    {
        if (!_employeeState.employees.TryGetValue(id, out var employee) || !employee.isActive)
            return 0;

        var allRoles = RoleSuitabilityCalculator.AllRoles;
        int bestCA = 0;
        RoleId bestRole = allRoles.Length > 0 ? allRoles[0] : RoleId.SoftwareEngineer;

        for (int i = 0; i < allRoles.Length; i++)
        {
            RoleId role = allRoles[i];
            long key = PackKey(id, role);

            int ca;
            if (!_roleCaCache.TryGetValue(key, out ca))
            {
                ca = ComputeRoleCARaw(employee.Stats.Skills, role);
                if (ca > employee.Stats.PotentialAbility) ca = employee.Stats.PotentialAbility;
                StoreRoleCacheEntry(id, key, ca);
            }

            if (ca > bestCA)
            {
                bestCA = ca;
                bestRole = role;
            }
        }

        _bestRoleCache[id] = new BestRoleResult { CA = bestCA, Role = bestRole };
        return bestCA;
    }

    private void StoreRoleCacheEntry(EmployeeId id, long key, int ca)
    {
        _roleCaCache[key] = ca;
        if (!_employeeRoleKeys.TryGetValue(id, out var keys))
        {
            keys = new List<long>();
            _employeeRoleKeys[id] = keys;
        }
        keys.Add(key);
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
