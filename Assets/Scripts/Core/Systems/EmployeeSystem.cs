// EmployeeSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class EmployeeSystem : ISystem
{
    public event Action<EmployeeId> OnEmployeeHired;
    public event Action<EmployeeId> OnEmployeeFired;
    public event Action<EmployeeId> OnEmployeeQuit;
    public event Action<EmployeeId> OnSalaryPaid;
    public event Action<int, int> OnCandidatesGenerated;
    public event Action<EmployeeId> OnEmployeeRetired;
    public event Action<EmployeeId> OnContractRenewalRequested;
    public event Action<EmployeeId, int, int> OnContractRenewed;
    public event Action<EmployeeId, CompanyId, CompanyId> OnEmployeeTransferred;

    // Candidate pool — fallback defaults (live values in TuningConfig)
    public const int CandidatePoolSize = 20;
    public const int CandidateListMax = 40;
    public const int CandidateRefreshIntervalDays = 15;

    // Age / Retirement — fallback defaults
    private const int DefaultRetirementAge          = 65;
    private const int DefaultDecayWindowStartAge    = 55;
    private const int DefaultRetirementCheckStartAge = 60;

    private const int TicksPerYear = TimeState.TicksPerDay * TimeState.DaysPerYear;
    private const int RenewalGraceDays = 60;
    private const int MinContractYears = 1;
    private const int MaxContractYearsExclusive = 3;

    private EmployeeState _state;
    private IRng _rng;
    private IRng _candidateRng;
    private ILogger _logger;
    private ReputationSystem _reputationSystem;
    private RecruitmentReputationSystem _recruitmentReputationSystem;
    private InterviewSystem _interviewSystem;
    private NegotiationSystem _negotiationSystem;
    private TeamState _teamState;
    private AbilitySystem _abilitySystem;
    private HRSystem _hrSystem;

    private List<EmployeeId> _quitBuffer;
    private bool _pendingCandidateGeneration;
    private List<Employee> _activeEmployeesCache;
    private bool _activeEmployeesDirty;
    private int _lastDayProcessed = -1;
    private RoleTierTable _roleTierTable;
    private GameEventBus _eventBus;
    private TuningConfig _tuning;

    // Event flag buffers — avoid lambda allocations in tick path
    private readonly List<EmployeeId> _firedBuffer = new List<EmployeeId>();
    private readonly List<EmployeeId> _quitEventBuffer = new List<EmployeeId>();
    private readonly List<EmployeeId> _hiredBuffer = new List<EmployeeId>();
    private readonly List<EmployeeId> _salaryPaidBuffer = new List<EmployeeId>();
    private readonly List<EmployeeId> _retiredBuffer = new List<EmployeeId>();
    private readonly List<EmployeeId> _renewalRequestedBuffer = new List<EmployeeId>();
    private struct RenewedEvent { public EmployeeId Id; public int NewSalary; public int OldSalary; }
    private readonly List<RenewedEvent> _renewedBuffer = new List<RenewedEvent>();
    private struct DecayEventData { public EmployeeId Id; public int CA; }
    private readonly List<DecayEventData> _decayEventBuffer = new List<DecayEventData>();
    private struct CandidateWithdrewData { public int CandidateId; public int Tick; }
    private readonly List<CandidateWithdrewData> _withdrawnBuffer = new List<CandidateWithdrewData>();
    private bool _pendingCandidateGeneratedEvent;
    private int _pendingCandidateGeneratedTick;
    private int _pendingCandidateGeneratedCount;

    // Scratch key list for dictionary iteration in tick path — avoids foreach allocations
    private readonly List<EmployeeId> _employeeKeysScratch = new List<EmployeeId>();

    // Pre-allocated scratch buffers for DecaySkills — zero heap allocation during decay
    private readonly int[] _decayScratch = new int[SkillTypeHelper.SkillTypeCount];
    private readonly float[] _decayWeightCdf = new float[SkillTypeHelper.SkillTypeCount];
    private readonly List<int> _expiredCandidateBuffer = new List<int>();

    // Company-filtered employee scratch — reused by GetActiveEmployeesForCompany; callers must not hold across ticks
    private readonly List<Employee> _companyEmployeesScratch = new List<Employee>(16);

    // Transferred event buffer
    private struct TransferredEvent { public EmployeeId Id; public CompanyId From; public CompanyId To; }
    private readonly List<TransferredEvent> _transferredBuffer = new List<TransferredEvent>();
    
    // Tuning-aware accessors — fall back to compile-time defaults
    private int PoolSize             => _tuning != null ? _tuning.CandidatePoolSize             : CandidatePoolSize;
    private int ListMax              => _tuning != null ? _tuning.CandidateListMax               : CandidateListMax;
    private int RefreshIntervalDays  => _tuning != null ? _tuning.CandidateRefreshIntervalDays   : CandidateRefreshIntervalDays;
    private int RetirementAge        => _tuning != null ? _tuning.RetirementAge                  : DefaultRetirementAge;
    private int DecayWindowStartAge  => _tuning != null ? _tuning.DecayWindowStartAge            : DefaultDecayWindowStartAge;
    private int RetirementCheckStartAge => _tuning != null ? _tuning.RetirementCheckStartAge     : DefaultRetirementCheckStartAge;

    public int EmployeeCount
    {
        get
        {
            int count = 0;
            var employees = _state.employees;
            foreach (var kvp in employees)
            {
                if (kvp.Value.isActive) count++;
            }
            return count;
        }
    }

    public int TotalDailySalaries
    {
        get
        {
            int total = 0;
            var employees = _state.employees;
            foreach (var kvp in employees)
            {
                if (kvp.Value.isActive) total += kvp.Value.salary;
            }
            return total;
        }
    }
    
    public EmployeeSystem(EmployeeState state, IRng rng, ILogger logger)
    {
        _state = state;
        _rng = rng;
        _candidateRng = new RngStream(state.candidatePoolSeed);
        _logger = logger ?? new NullLogger();
        _quitBuffer = new List<EmployeeId>();
        _activeEmployeesCache = new List<Employee>();
        _activeEmployeesDirty = true;
    }
    
    public void SetTeamState(TeamState teamState)
    {
        _teamState = teamState;
    }
    
    public void SetReputationSystem(ReputationSystem reputationSystem)
    {
        _reputationSystem = reputationSystem;
    }

    public void SetRecruitmentReputationSystem(RecruitmentReputationSystem system)
    {
        _recruitmentReputationSystem = system;
    }

    public void SetInterviewSystem(InterviewSystem system)
    {
        _interviewSystem = system;
    }

    public void SetNegotiationSystem(NegotiationSystem system)
    {
        _negotiationSystem = system;
    }

    public void SetAbilitySystem(AbilitySystem system)
    {
        _abilitySystem = system;
    }

    public void SetHRSystem(HRSystem hrSystem)
    {
        _hrSystem = hrSystem;
    }

    public void SetRoleTierTable(RoleTierTable table)
    {
        _roleTierTable = table;
    }

    public void SetEventBus(GameEventBus bus)
    {
        _eventBus = bus;
    }

    public void SetTuningConfig(TuningConfig tuning) { _tuning = tuning; }

    // ── Industry Salary Benchmarks ───────────────────────────────────────────
    private readonly Dictionary<SkillTier, int> _industrySalaryBenchmarks = new Dictionary<SkillTier, int>();

    public int GetBenchmarkSalary(SkillTier tier)
    {
        _industrySalaryBenchmarks.TryGetValue(tier, out int val);
        return val;
    }

    public float GetSalaryCompetitiveness(EmployeeId empId)
    {
        if (!_state.employees.TryGetValue(empId, out var emp)) return 1f;

        SkillTier tier = DeriveEmployeeTier(emp);
        int benchmark = GetBenchmarkSalary(tier);
        if (benchmark <= 0) return 1f;

        return (float)emp.salary / benchmark;
    }

    public void UpdateIndustryBenchmarks(CompetitorState compState)
    {
        if (compState == null) return;

        var tierSums = new Dictionary<SkillTier, long>();
        var tierCounts = new Dictionary<SkillTier, int>();

        foreach (SkillTier t in System.Enum.GetValues(typeof(SkillTier)))
        {
            tierSums[t] = 0L;
            tierCounts[t] = 0;
        }

        // Include player employees as a baseline
        foreach (var kvp in _state.employees)
        {
            var emp = kvp.Value;
            if (!emp.isActive) continue;
            SkillTier tier = DeriveEmployeeTier(emp);
            tierSums[tier] += emp.salary;
            tierCounts[tier]++;
        }

        // Include competitor average salary data
        foreach (var compKvp in compState.competitors)
        {
            var comp = compKvp.Value;
            if (comp.IsBankrupt || comp.IsAbsorbed) continue;
            // AverageSalaryByTier removed — competitor salaries now come from real employees
        }

        foreach (SkillTier t in System.Enum.GetValues(typeof(SkillTier)))
        {
            int count = tierCounts[t];
            _industrySalaryBenchmarks[t] = count > 0 ? (int)(tierSums[t] / count) : 0;
        }
    }

    private SkillTier DeriveEmployeeTier(Employee emp)
    {
        int maxSkill = 0;
        int[] skills = emp.skills;
        int skillCount = skills != null ? skills.Length : 0;
        for (int i = 0; i < skillCount; i++)
            if (skills[i] > maxSkill) maxSkill = skills[i];

        if (maxSkill >= 85) return SkillTier.Master;
        if (maxSkill >= 65) return SkillTier.Expert;
        if (maxSkill >= 40) return SkillTier.Competent;
        return SkillTier.Apprentice;
    }

    public EmployeeId HireEmployee(string name, Gender gender, int age, int programmingSkill, int designSkill, int qaSkill, int salary, int currentTick, EmployeeRole role, int hrSkill = 0)
    {
        var skills = new int[SkillTypeHelper.SkillTypeCount];
        skills[(int)SkillType.Programming] = programmingSkill;
        skills[(int)SkillType.Design] = designSkill;
        skills[(int)SkillType.QA] = qaSkill;
        return HireEmployee(name, gender, age, skills, salary, currentTick, role, hrSkill);
    }

    public EmployeeId HireEmployee(string name, Gender gender, int age, int[] skills, int salary, int currentTick, EmployeeRole role, int hrSkill = 0, int potentialAbility = 0)
    {
        var employeeId = new EmployeeId(_state.nextEmployeeId++);

        if (age <= 0) age = 25;

        int[] clampedSkills = new int[SkillTypeHelper.SkillTypeCount];
        if (skills != null)
        {
            int len = skills.Length < SkillTypeHelper.SkillTypeCount ? skills.Length : SkillTypeHelper.SkillTypeCount;
            for (int i = 0; i < len; i++)
                clampedSkills[i] = skills[i] < 1 ? 1 : skills[i];
        }

        // Keep hrSkill field and skills[HR] in sync — whichever is larger wins
        if (role == EmployeeRole.HR)
        {
            int fromArray = clampedSkills[(int)SkillType.HR];
            int resolved = fromArray > hrSkill ? fromArray : hrSkill;
            if (resolved < 1) resolved = 1;
            clampedSkills[(int)SkillType.HR] = resolved;
            hrSkill = resolved;
        }

        var employee = new Employee(
            employeeId,
            name,
            gender,
            age,
            clampedSkills,
            salary,
            currentTick,
            role,
            hrSkill
        );

        _state.employees[employeeId] = employee;
        _activeEmployeesDirty = true;

        if (potentialAbility > 0)
            employee.potentialAbility = potentialAbility;

        employee.contractExpiryTick = currentTick + _rng.Range(MinContractYears, MaxContractYearsExclusive) * TicksPerYear;

        _abilitySystem?.OnEmployeeHired(employeeId, employee);

        RemoveCandidateByName(name);

        _hiredBuffer.Add(employeeId);

        _logger.Log($"[Tick {currentTick}] Hired {name} (ID: {employeeId.Value}) [{role}] Age:{age} - Skills:[{string.Join(",", clampedSkills)}] HR:{hrSkill} Salary:${salary}/month");

        return employeeId;
    }
    
    private int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    
    public bool FireEmployee(EmployeeId id)
    {
        if (!_state.employees.TryGetValue(id, out var employee))
        {
            _logger.LogWarning($"Cannot fire employee {id.Value}: Not found");
            return false;
        }

        if (!employee.isActive)
        {
            _logger.LogWarning($"Cannot fire employee {id.Value}: Already inactive");
            return false;
        }

        employee.isActive = false;
        _activeEmployeesDirty = true;

        _firedBuffer.Add(id);

        _logger.Log($"Fired {employee.name} (ID: {id.Value})");

        return true;
    }

    public bool QuitEmployee(EmployeeId id)
    {
        if (!_state.employees.TryGetValue(id, out var employee)) return false;
        if (!employee.isActive) return false;

        employee.isActive = false;
        _activeEmployeesDirty = true;

        _quitEventBuffer.Add(id);

        _logger.Log($"{employee.name} (ID: {id.Value}) quit due to low morale.");

        return true;
    }
    
    public Employee GetEmployee(EmployeeId id)
    {
        if (_state.employees.TryGetValue(id, out var employee))
        {
            return employee;
        }
        
        return null;
    }

    public bool SetEmployeeSkill(EmployeeId id, SkillType skill, int value)
    {
        if (!_state.employees.TryGetValue(id, out var employee)) return false;
        if (!employee.isActive) return false;

        if (value < 0) value = 0;

        switch (skill) {
            case SkillType.Programming:  employee.SetSkill(SkillType.Programming, value);  break;
            case SkillType.Design:       employee.SetSkill(SkillType.Design, value);        break;
            case SkillType.QA:           employee.SetSkill(SkillType.QA, value);            break;
            case SkillType.VFX:          employee.SetSkill(SkillType.VFX, value);           break;
            case SkillType.SFX:          employee.SetSkill(SkillType.SFX, value);           break;
            case SkillType.HR:           employee.SetSkill(SkillType.HR, value);            break;
            case SkillType.Negotiation:  employee.SetSkill(SkillType.Negotiation, value);   break;
            case SkillType.Accountancy:  employee.SetSkill(SkillType.Accountancy, value);   break;
            default: return false;
        }
        return true;
    }
    
    public IReadOnlyList<Employee> GetAllActiveEmployees()
    {
        if (_activeEmployeesDirty) {
            RebuildActiveEmployeesCache();
        }
        return _activeEmployeesCache;
    }

    private void RebuildActiveEmployeesCache()
    {
        _activeEmployeesCache.Clear();
        foreach (var kvp in _state.employees) {
            if (kvp.Value.isActive) {
                _activeEmployeesCache.Add(kvp.Value);
            }
        }
        _activeEmployeesDirty = false;
    }

    // Returns scratch list of active employees for a specific company.
    // Caller must NOT hold the reference across ticks — contents are overwritten on next call.
    public List<Employee> GetActiveEmployeesForCompany(CompanyId companyId)
    {
        _companyEmployeesScratch.Clear();
        var employees = _state.employees;
        foreach (var kvp in employees)
        {
            var emp = kvp.Value;
            if (emp.isActive && emp.ownerCompanyId == companyId)
                _companyEmployeesScratch.Add(emp);
        }
        return _companyEmployeesScratch;
    }

    public int EmployeeCountForCompany(CompanyId companyId)
    {
        int count = 0;
        var employees = _state.employees;
        foreach (var kvp in employees)
        {
            var emp = kvp.Value;
            if (emp.isActive && emp.ownerCompanyId == companyId) count++;
        }
        return count;
    }

    public int TotalMonthlySalariesForCompany(CompanyId companyId)
    {
        int total = 0;
        var employees = _state.employees;
        foreach (var kvp in employees)
        {
            var emp = kvp.Value;
            if (emp.isActive && emp.ownerCompanyId == companyId) total += emp.salary;
        }
        return total;
    }

    // Creates count real Employee objects owned by companyId, using the shared candidate pipeline.
    // Skill quality is scaled by archetype tier. Returns created EmployeeId list (allocated once at spawn, not in tick path).
    public List<EmployeeId> BulkHireForCompany(CompanyId companyId, CompetitorArchetype archetype, int count, IRng rng, int hireTick)
    {
        var result = new List<EmployeeId>(count);
        float qualityMultiplier = GetArchetypeQualityMultiplier(archetype);

        for (int i = 0; i < count; i++)
        {
            EmployeeRole role = RollRoleForArchetype(archetype, rng);
            var candidate = CandidateData.GenerateCandidate(rng, qualityMultiplier, role);

            var employeeId = new EmployeeId(_state.nextEmployeeId++);
            int age = rng.Range(22, 45);

            int[] clampedSkills = new int[SkillTypeHelper.SkillTypeCount];
            int skillLen = candidate.Skills.Length < SkillTypeHelper.SkillTypeCount
                ? candidate.Skills.Length
                : SkillTypeHelper.SkillTypeCount;
            for (int s = 0; s < skillLen; s++)
                clampedSkills[s] = candidate.Skills[s] < 1 ? 1 : candidate.Skills[s];

            var employee = new Employee(employeeId, candidate.Name, candidate.Gender, age, clampedSkills, candidate.Salary, hireTick, role);
            employee.ownerCompanyId = companyId;
            employee.potentialAbility = candidate.PotentialAbility;
            employee.hiddenAttributes = candidate.HiddenAttributes;
            employee.contractExpiryTick = hireTick + rng.Range(MinContractYears, MaxContractYearsExclusive) * TicksPerYear;

            _state.employees[employeeId] = employee;
            _activeEmployeesDirty = true;

            _abilitySystem?.OnEmployeeHired(employeeId, employee);

            result.Add(employeeId);
        }

        return result;
    }

    // Changes ownerCompanyId. Caller must remove employee from their current team via TeamSystem first.
    // Fires OnEmployeeTransferred event in PostTick.
    public void TransferEmployee(EmployeeId employeeId, CompanyId fromCompany, CompanyId toCompany)
    {
        if (!_state.employees.TryGetValue(employeeId, out var employee)) return;
        employee.ownerCompanyId = toCompany;
        _activeEmployeesDirty = true;
        _transferredBuffer.Add(new TransferredEvent { Id = employeeId, From = fromCompany, To = toCompany });
    }

    private static float GetArchetypeQualityMultiplier(CompetitorArchetype archetype)
    {
        switch (archetype)
        {
            case CompetitorArchetype.PlatformGiant:  return 1.4f;
            case CompetitorArchetype.ToolMaker:      return 1.2f;
            case CompetitorArchetype.GameStudio:     return 1.1f;
            case CompetitorArchetype.FullStack:      return 1.15f;
            default:                                 return 1.0f;
        }
    }

    private static EmployeeRole RollRoleForArchetype(CompetitorArchetype archetype, IRng rng)
    {
        int wDev, wDes, wQA, wSound, wVFX, wMktg;
        switch (archetype)
        {
            case CompetitorArchetype.PlatformGiant:
                wDev=50; wDes=10; wQA=25; wSound=3; wVFX=5; wMktg=7; break;
            case CompetitorArchetype.ToolMaker:
                wDev=45; wDes=20; wQA=20; wSound=2; wVFX=5; wMktg=8; break;
            case CompetitorArchetype.GameStudio:
                wDev=30; wDes=25; wQA=15; wSound=10; wVFX=15; wMktg=5; break;
            case CompetitorArchetype.FullStack:
                wDev=30; wDes=15; wQA=15; wSound=10; wVFX=10; wMktg=20; break;
            default:
                wDev=35; wDes=20; wQA=20; wSound=8; wVFX=10; wMktg=7; break;
        }
        int total = wDev + wDes + wQA + wSound + wVFX + wMktg;
        int roll = rng.Range(0, total);
        if (roll < wDev)               return EmployeeRole.Developer;
        roll -= wDev;
        if (roll < wDes)               return EmployeeRole.Designer;
        roll -= wDes;
        if (roll < wQA)                return EmployeeRole.QAEngineer;
        roll -= wQA;
        if (roll < wSound)             return EmployeeRole.SoundEngineer;
        roll -= wSound;
        if (roll < wVFX)               return EmployeeRole.VFXArtist;
        return EmployeeRole.Marketer;
    }
    
    public void ProcessDailySalaries(int currentTick, FinanceSystem financeSystem)
    {
        int totalSalaries = 0;
        int employeeCount = 0;

        float salaryMultiplier = 1f;

        _employeeKeysScratch.Clear();
        foreach (var kvp in _state.employees)
            _employeeKeysScratch.Add(kvp.Key);

        int keyCount = _employeeKeysScratch.Count;
        for (int i = 0; i < keyCount; i++)
        {
            if (!_state.employees.TryGetValue(_employeeKeysScratch[i], out var emp)) continue;
            if (!emp.isActive) continue;
            if (!emp.ownerCompanyId.IsPlayer) continue;

            int adjustedSalary = (int)(emp.salary * salaryMultiplier);
            if (adjustedSalary < 1) adjustedSalary = 1;
            totalSalaries += adjustedSalary;
            employeeCount++;

            _salaryPaidBuffer.Add(emp.id);
        }

        if (totalSalaries > 0)
        {
            financeSystem.TrySubtractMoney(totalSalaries, out _);
            _logger.Log($"[Tick {currentTick}] Paid {employeeCount} employee salaries: ${totalSalaries}");
        }
    }
    
    public void PreTick(int tick)
    {
    }
    
    public void Tick(int tick)
    {
        CheckCandidateGenerationSchedule(tick);

        int currentDay = tick / TimeState.TicksPerDay;
        bool isDayBoundary = tick % TimeState.TicksPerDay == 0 && currentDay != _lastDayProcessed;

        if (isDayBoundary)
        {
            _employeeKeysScratch.Clear();
            foreach (var kvp in _state.employees)
                _employeeKeysScratch.Add(kvp.Key);

            int keyCount = _employeeKeysScratch.Count;
            for (int i = 0; i < keyCount; i++)
            {
                if (!_state.employees.TryGetValue(_employeeKeysScratch[i], out var employee)) continue;
                if (!employee.isActive) continue;

                bool isAssigned = _teamState != null && _teamState.employeeToTeam.ContainsKey(employee.id);
            }
        }

        if (isDayBoundary)
        {
            _lastDayProcessed = currentDay;

            ProcessContractExpiry(tick);

            _expiredCandidateBuffer.Clear();
            CandidateExpiryHelper.TickExpiryTimers(_state, _interviewSystem, _negotiationSystem, tick, _expiredCandidateBuffer);

            int expiredCount = _expiredCandidateBuffer.Count;
            for (int i = 0; i < expiredCount; i++)
            {
                int candidateId = _expiredCandidateBuffer[i];
                _logger.Log($"[Tick {tick}] Candidate ID {candidateId} expired and withdrew from pool");
                _withdrawnBuffer.Add(new CandidateWithdrewData { CandidateId = candidateId, Tick = tick });
            }
        }
    }
    
    public void PostTick(int tick)
    {
        if (_pendingCandidateGeneration)
        {
            int candidateCount = PoolSize;
            GenerateNewCandidates(candidateCount);
            _state.lastCandidateGenerationTick = tick;
            _state.candidateRerollsUsedThisCycle = 0;
            _logger.Log($"[Tick {tick}] Generated {candidateCount} new job candidates");

            _pendingCandidateGeneratedEvent = true;
            _pendingCandidateGeneratedTick = tick;
            _pendingCandidateGeneratedCount = candidateCount;
            _pendingCandidateGeneration = false;
        }

        int currentYear = tick / TicksPerYear;
        if (currentYear > _state.lastYearProcessed)
        {
            _state.lastYearProcessed = currentYear;
            ProcessAgeAdvancement(tick);
        }

        _quitBuffer.Clear();

        // Flush hired events
        int count = _hiredBuffer.Count;
        for (int i = 0; i < count; i++)
            OnEmployeeHired?.Invoke(_hiredBuffer[i]);
        _hiredBuffer.Clear();

        // Flush fired events
        count = _firedBuffer.Count;
        for (int i = 0; i < count; i++)
            OnEmployeeFired?.Invoke(_firedBuffer[i]);
        _firedBuffer.Clear();

        // Flush quit events
        count = _quitEventBuffer.Count;
        for (int i = 0; i < count; i++)
            OnEmployeeQuit?.Invoke(_quitEventBuffer[i]);
        _quitEventBuffer.Clear();

        // Flush salary paid events
        count = _salaryPaidBuffer.Count;
        for (int i = 0; i < count; i++)
            OnSalaryPaid?.Invoke(_salaryPaidBuffer[i]);
        _salaryPaidBuffer.Clear();

        // Flush candidate generated event
        if (_pendingCandidateGeneratedEvent)
        {
            OnCandidatesGenerated?.Invoke(_pendingCandidateGeneratedTick, _pendingCandidateGeneratedCount);
            _pendingCandidateGeneratedEvent = false;
        }

        // Flush retired events
        count = _retiredBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            OnEmployeeRetired?.Invoke(_retiredBuffer[i]);
            _eventBus?.Raise(new EmployeeRetiredEvent(tick, _retiredBuffer[i]));
        }
        _retiredBuffer.Clear();

        // Flush decay events
        count = _decayEventBuffer.Count;
        for (int i = 0; i < count; i++)
            _eventBus?.Raise(new EmployeeDecayEvent(tick, _decayEventBuffer[i].Id, _decayEventBuffer[i].CA));
        _decayEventBuffer.Clear();

        // Flush contract renewal requested events
        count = _renewalRequestedBuffer.Count;
        for (int i = 0; i < count; i++)
            OnContractRenewalRequested?.Invoke(_renewalRequestedBuffer[i]);
        _renewalRequestedBuffer.Clear();

        // Flush contract renewed events
        count = _renewedBuffer.Count;
        for (int i = 0; i < count; i++)
            OnContractRenewed?.Invoke(_renewedBuffer[i].Id, _renewedBuffer[i].NewSalary, _renewedBuffer[i].OldSalary);
        _renewedBuffer.Clear();

        // Flush candidate withdrew events
        count = _withdrawnBuffer.Count;
        for (int i = 0; i < count; i++)
            _eventBus?.Raise(new CandidateWithdrewEvent(_withdrawnBuffer[i].Tick, _withdrawnBuffer[i].CandidateId, string.Empty));
        _withdrawnBuffer.Clear();

        // Flush transferred events
        count = _transferredBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var t = _transferredBuffer[i];
            OnEmployeeTransferred?.Invoke(t.Id, t.From, t.To);
            _eventBus?.Raise(new EmployeeTransferredEvent(tick, t.Id, t.From, t.To));
        }
        _transferredBuffer.Clear();
    }

    private void ProcessContractExpiry(int tick)
    {
        _employeeKeysScratch.Clear();
        foreach (var kvp in _state.employees)
            _employeeKeysScratch.Add(kvp.Key);

        int keyCount = _employeeKeysScratch.Count;
        for (int i = 0; i < keyCount; i++)
        {
            if (!_state.employees.TryGetValue(_employeeKeysScratch[i], out var emp)) continue;
            if (!emp.isActive) continue;
            if (emp.isFounder) continue;

            if (emp.contractExpiryTick == 0)
            {
                emp.contractExpiryTick = tick + _rng.Range(MinContractYears, MaxContractYearsExclusive) * TicksPerYear;
                continue;
            }

            if (tick < emp.contractExpiryTick) continue;

            if (emp.contractRenewalPending)
            {
                emp.isActive = false;
                _activeEmployeesDirty = true;
                _quitEventBuffer.Add(emp.id);
                _logger.Log($"[Tick {tick}] {emp.name} quit — contract renewal ignored");
            }
            else
            {
                int demand = SalaryDemandCalculator.ComputeRenewalDemand(emp);
                emp.contractRenewalPending = true;
                emp.renewalDemand = demand;
                emp.contractExpiryTick = tick + RenewalGraceDays * TimeState.TicksPerDay;
                _renewalRequestedBuffer.Add(emp.id);
                _logger.Log($"[Tick {tick}] {emp.name} requesting contract renewal at ${demand}/mo");
            }
        }
    }

    private void ProcessAgeAdvancement(int tick)
    {
        _employeeKeysScratch.Clear();
        foreach (var kvp in _state.employees)
            _employeeKeysScratch.Add(kvp.Key);

        int keyCount = _employeeKeysScratch.Count;
        for (int i = 0; i < keyCount; i++)
        {
            if (!_state.employees.TryGetValue(_employeeKeysScratch[i], out var emp)) continue;
            if (!emp.isActive) continue;

            emp.age++;
            _logger.Log($"{emp.name} (ID: {emp.id.Value}) aged to {emp.age}");

            if (emp.isFounder) continue;

            // Assign decay onset age once when employee first reaches 55
            if (emp.age >= DecayWindowStartAge && emp.decayOnsetAge == 0)
                emp.decayOnsetAge = _rng.Range(DecayWindowStartAge, RetirementAge + 1);

            // Apply yearly skill decay if past onset age
            if (emp.decayOnsetAge > 0 && emp.age >= emp.decayOnsetAge)
                DecaySkills(emp, tick);

            // Roll retirement from age 60; mandatory at 65
            if (emp.age >= RetirementCheckStartAge)
                RollRetirement(emp, tick);
        }
    }

    // Distributes 5–15 CA of decay across all 11 skills, weighted by inverse tier multiplier.
    // Primary skills (mult=2, weight=0.5) have higher decay probability than tertiary (mult=4, weight=0.25).
    private void DecaySkills(Employee emp, int tick)
    {
        int decayCA = _rng.Range(5, 16);

        int[] roleTiers = _roleTierTable != null
            ? _roleTierTable.GetTiers(emp.role)
            : null;

        float minFloor = 0.5f / SkillTypeHelper.SkillTypeCount;
        float weightSum = 0f;
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
        {
            float invMult = (roleTiers != null && i < roleTiers.Length && roleTiers[i] > 0)
                ? 1.0f / roleTiers[i]
                : minFloor;
            _decayWeightCdf[i] = (invMult > minFloor ? invMult : minFloor);
            weightSum += _decayWeightCdf[i];
        }

        // Normalise into cumulative distribution
        float running = 0f;
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
        {
            running += _decayWeightCdf[i] / weightSum;
            _decayWeightCdf[i] = running;
        }
        _decayWeightCdf[SkillTypeHelper.SkillTypeCount - 1] = 1f;

        // Distribute each CA unit via weighted random selection
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
            _decayScratch[i] = 0;

        for (int u = 0; u < decayCA; u++)
        {
            float roll = _rng.Range(0, 10000) / 10000f;
            for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
            {
                if (roll <= _decayWeightCdf[i])
                {
                    _decayScratch[i]++;
                    break;
                }
            }
        }

        // Ensure accumulator arrays exist (migration safety for old saves)
        if (emp.skillXp == null)
            emp.skillXp = new float[SkillTypeHelper.SkillTypeCount];
        if (emp.skillDeltaDirection == null)
            emp.skillDeltaDirection = new sbyte[SkillTypeHelper.SkillTypeCount];

        // Apply skill losses via float accumulator — drains XP buffer before dropping a level
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
        {
            int caAlloc = _decayScratch[i];
            if (caAlloc == 0) continue;

            int tierMult = (roleTiers != null && i < roleTiers.Length && roleTiers[i] > 0)
                ? roleTiers[i]
                : 3;
            int currentLevel = emp.skills[i];
            int marginalCost = currentLevel > 0 ? AbilityCalculator.GetMarginalCost(currentLevel - 1, tierMult) : 1;
            if (marginalCost < 1) marginalCost = 1;

            float decayAmount = (float)(caAlloc * 2) / (float)(marginalCost * tierMult);

            emp.skillDeltaDirection[i] = -1;
            emp.skillXp[i] -= decayAmount;

            while (emp.skillXp[i] < 0f && emp.skills[i] > 0)
            {
                emp.skillXp[i] += 1.0f;
                emp.skills[i]--;
            }

            if (emp.skills[i] == 0 && emp.skillXp[i] < 0f)
                emp.skillXp[i] = 0f;
        }

        _abilitySystem?.InvalidateCA(emp.id);

        _decayEventBuffer.Add(new DecayEventData { Id = emp.id, CA = decayCA });

        _logger.Log($"{emp.name} (ID: {emp.id.Value}) skill decay at age {emp.age}: {decayCA} CA distributed across skills");
    }

    // Probabilistic retirement roll — once per year from age 60, mandatory at 65.
    private void RollRetirement(Employee emp, int tick)
    {
        float chance;
        switch (emp.age)
        {
            case 60: chance = 0.05f;  break;
            case 61: chance = 0.12f;  break;
            case 62: chance = 0.25f;  break;
            case 63: chance = 0.40f;  break;
            case 64: chance = 0.60f;  break;
            default: chance = 1.00f;  break; // age >= 65
        }

        if (!_rng.Chance(chance)) return;

        _quitBuffer.Add(emp.id);
        _retiredBuffer.Add(emp.id);
        _logger.Log($"{emp.name} (ID: {emp.id.Value}) retired at age {emp.age}");
    }
    
    public bool CanRerollCandidates => _state.candidateRerollsUsedThisCycle < 1;
    public int CandidateRerollsUsedThisCycle => _state.candidateRerollsUsedThisCycle;

    public bool ForceRerollCandidates(int tick)
    {
        if (_state.candidateRerollsUsedThisCycle >= 1) return false;
        _state.candidateRerollsUsedThisCycle++;

        // On an explicit reroll, remove candidates who have no interview progress
        // (untouched candidates). Candidates mid-interview or post-interview are kept.
        for (int i = _state.availableCandidates.Count - 1; i >= 0; i--)
        {
            var c = _state.availableCandidates[i];
            bool hasProgress = c.InterviewStage > 0;
            if (!hasProgress)
                _state.availableCandidates.RemoveAt(i);
        }

        int candidateCount = PoolSize;
        GenerateNewCandidates(candidateCount);
        _state.lastCandidateGenerationTick = tick;
        _logger.Log($"[Tick {tick}] Candidate pool rerolled ({candidateCount} total slots)");
        _pendingCandidateGeneratedEvent = true;
        _pendingCandidateGeneratedTick = tick;
        _pendingCandidateGeneratedCount = candidateCount;
        return true;
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is RerollCandidatePoolCommand)
        {
            ForceRerollCandidates(command.Tick);
        }
        else if (command is HireEmployeeCommand hire)
        {
            int finalSalary = hire.Salary;

            EmployeeId hiredId = HireEmployee(hire.Name, hire.Gender, hire.Age, hire.Skills, finalSalary, command.Tick, hire.Role, hire.HRSkill, hire.PotentialAbility);

            if (hiredId.Value >= 0 && _state.employees.TryGetValue(hiredId, out var hiredEmployee))
            {
                hiredEmployee.ownerCompanyId = hire.CompanyId;

                // Manual hiring penalties applied after hire
                if (hire.Mode == HiringMode.Manual)
                {
                    // 1. Lower morale
                    hiredEmployee.morale = hiredEmployee.morale - 5 < 0 ? 0 : hiredEmployee.morale - 5;

                    // 2. Downward bias on each hidden attribute (shave 0-3 off each)
                    var attrs = hiredEmployee.hiddenAttributes;
                    int lr = attrs.LearningRate - _rng.Range(0, 4);
                    int we = attrs.WorkEthic    - _rng.Range(0, 4);
                    int ad = attrs.Adaptability - _rng.Range(0, 4);
                    int am = attrs.Ambition     - _rng.Range(0, 4);
                    int cr = attrs.Creative     - _rng.Range(0, 4);
                    hiredEmployee.hiddenAttributes = new HiddenAttributes
                    {
                        LearningRate = lr < 1 ? 1 : lr,
                        WorkEthic    = we < 1 ? 1 : we,
                        Adaptability = ad < 1 ? 1 : ad,
                        Ambition     = am < 1 ? 1 : am,
                        Creative     = cr < 1 ? 1 : cr
                    };

                    _logger.Log($"[Tick {command.Tick}] Manual hire penalties applied to {hire.Name}: morale-5, attr downbias");
                }

                _state.employees[hiredId] = hiredEmployee;
            }
        }
        else if (command is FireEmployeeCommand fire)
        {
            FireEmployee(fire.EmployeeId);
        }
        else if (command is DismissCandidateCommand dismiss)
        {
            int count = _state.availableCandidates.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (_state.availableCandidates[i].CandidateId == dismiss.CandidateId)
                {
                    _state.availableCandidates.RemoveAt(i);
                    _logger.Log($"[Tick {command.Tick}] Candidate {dismiss.CandidateId} dismissed by player");
                    break;
                }
            }
        }
        else if (command is RenewContractCommand renew)
        {
            if (_state.employees.TryGetValue(renew.EmployeeId, out var emp) && emp.isActive && emp.contractRenewalPending)
            {
                int oldSalary = emp.salary;
                emp.salary = emp.renewalDemand;
                emp.contractRenewalPending = false;
                emp.contractExpiryTick = command.Tick + _rng.Range(MinContractYears, MaxContractYearsExclusive) * TicksPerYear;
                emp.renewalDemand = 0;
                _activeEmployeesDirty = true;
                _renewedBuffer.Add(new RenewedEvent { Id = renew.EmployeeId, NewSalary = emp.salary, OldSalary = oldSalary });
                _logger.Log($"[Tick {command.Tick}] Contract renewed for {emp.name}: ${oldSalary} → ${emp.salary}/mo");
            }
        }
        else if (command is DeclineRenewalCommand decline)
        {
            if (_state.employees.TryGetValue(decline.EmployeeId, out var emp) && emp.isActive)
            {
                emp.isActive = false;
                emp.contractRenewalPending = false;
                _activeEmployeesDirty = true;
                _quitEventBuffer.Add(decline.EmployeeId);
                _logger.Log($"[Tick {command.Tick}] {emp.name} quit — contract renewal declined by player");
            }
        }
    }
    
    private void CheckCandidateGenerationSchedule(int currentTick)
    {
        int ticksSinceLastGeneration = currentTick - _state.lastCandidateGenerationTick;
        
        float genSpeedMultiplier = 1f;
        int adjustedInterval = genSpeedMultiplier > 0f
            ? (int)(_state.candidateGenerationInterval / genSpeedMultiplier)
            : _state.candidateGenerationInterval;
        int minInterval = RefreshIntervalDays * TimeState.TicksPerDay;
        if (adjustedInterval < minInterval) adjustedInterval = minInterval;
        
        if (ticksSinceLastGeneration >= adjustedInterval)
        {
            _pendingCandidateGeneration = true;
        }
    }
    
    private void GenerateNewCandidates(int count)
    {
        // Count only auto-generated (non-targeted) candidates against the pool limit.
        // HR-sourced (IsTargeted) candidates occupy their own space up to CandidateListMax.
        int autoGenCount = 0;
        int existingTotal = _state.availableCandidates.Count;
        for (int i = 0; i < existingTotal; i++)
        {
            if (!_state.availableCandidates[i].IsTargeted)
                autoGenCount++;
        }

        // Re-seed the candidate RNG for variety each cycle
        _state.candidatePoolSeed = _state.candidatePoolSeed * 1103515245 + 12345;
        _candidateRng = new RngStream(_state.candidatePoolSeed);

        float qualityMultiplier = 1.0f;
        if (_reputationSystem != null)
            qualityMultiplier = _reputationSystem.GetCandidateQualityMultiplier();

        float upgradeQualityBonus = 0f;
        qualityMultiplier += upgradeQualityBonus;

        // Fill auto-gen slots up to PoolSize; never exceed ListMax total
        int newSlots = count - autoGenCount;
        if (newSlots <= 0) return;

        for (int i = 0; i < newSlots; i++)
        {
            if (_state.availableCandidates.Count >= ListMax) break;
            var candidate = CandidateData.GenerateCandidate(_candidateRng, qualityMultiplier);
            candidate.CandidateId = _state.nextCandidateId++;
            _abilitySystem?.GenerateCandidateAbility(candidate);
            _state.availableCandidates.Add(candidate);
        }
    }
    
    public IEnumerable<CandidateData> GetAvailableCandidates()
    {
        return _state.availableCandidates;
    }
    
    public void RemoveCandidate(CandidateData candidate)
    {
        for (int i = _state.availableCandidates.Count - 1; i >= 0; i--)
        {
            if (_state.availableCandidates[i].Name == candidate.Name)
            {
                _state.availableCandidates.RemoveAt(i);
                break;
            }
        }
    }
    
    private void RemoveCandidateByName(string name)
    {
        for (int i = _state.availableCandidates.Count - 1; i >= 0; i--)
        {
            if (_state.availableCandidates[i].Name == name)
            {
                _state.availableCandidates.RemoveAt(i);
                _logger.Log($"Removed candidate {name} from available candidates list");
                break;
            }
        }
    }
    
    public void Dispose()
    {
        _quitBuffer.Clear();
        _hiredBuffer.Clear();
        _firedBuffer.Clear();
        _quitEventBuffer.Clear();
        _salaryPaidBuffer.Clear();
        _retiredBuffer.Clear();
        _renewalRequestedBuffer.Clear();
        _renewedBuffer.Clear();
        _decayEventBuffer.Clear();
        _withdrawnBuffer.Clear();
        _transferredBuffer.Clear();
        _companyEmployeesScratch.Clear();
        _employeeKeysScratch.Clear();
        OnEmployeeHired = null;
        OnEmployeeFired = null;
        OnEmployeeQuit = null;
        OnSalaryPaid = null;
        OnCandidatesGenerated = null;
        OnEmployeeRetired = null;
        OnEmployeeTransferred = null;
    }
}
