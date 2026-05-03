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

    // Age / Retirement — fallback defaults
    private const int DefaultRetirementAge          = 65;
    private const int DefaultDecayWindowStartAge    = 55;
    private const int DefaultRetirementCheckStartAge = 60;

    private const int TicksPerYear = TimeState.TicksPerDay * TimeState.DaysPerYear;
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
    private bool _pendingCandidateGeneration;
    private List<Employee> _activeEmployeesCache;
    private bool _activeEmployeesDirty;
    private int _lastDayProcessed = -1;
    private GameEventBus _eventBus;
    private TuningConfig _tuning;

    // Event flag buffers — avoid lambda allocations in tick path
    private readonly List<EmployeeId> _firedBuffer = new List<EmployeeId>();
    private readonly List<EmployeeId> _quitBuffer = new List<EmployeeId>();
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
    private bool _pendingShortlistEvent;

    // Scratch key list for dictionary iteration in tick path — avoids foreach allocations
    private readonly List<EmployeeId> _employeeKeysScratch = new List<EmployeeId>();

    // Pre-allocated scratch buffers for DecaySkills — zero heap allocation during decay
    private readonly int[] _decayScratch = new int[SkillIdHelper.SkillCount];
    private readonly float[] _decayWeightCdf = new float[SkillIdHelper.SkillCount];
    private readonly List<int> _expiredCandidateBuffer = new List<int>();

    // Company-filtered employee scratch — reused by GetActiveEmployeesForCompany; callers must not hold across ticks
    private readonly List<Employee> _companyEmployeesScratch = new List<Employee>(16);

    // Transferred event buffer
    private struct TransferredEvent { public EmployeeId Id; public CompanyId From; public CompanyId To; }
    private readonly List<TransferredEvent> _transferredBuffer = new List<TransferredEvent>();

    // Renewal event buffers
    private struct RenewalWindowOpenedData { public EmployeeId Id; public string Name; public EmploymentType CurrentType; public int DaysUntilExpiry; }
    private readonly List<RenewalWindowOpenedData> _renewalWindowOpenedBuffer = new List<RenewalWindowOpenedData>();
    private struct RenewalChangeRequestedData { public EmployeeId Id; public string Name; public bool RequestsTypeChange; public EmploymentType RequestedType; public bool RequestsLengthChange; public ContractLengthOption RequestedLength; }
    private readonly List<RenewalChangeRequestedData> _renewalChangeRequestedBuffer = new List<RenewalChangeRequestedData>();
    private struct RenewalEscalationData { public EmployeeId Id; public string Name; public int StrikeCount; public bool IsFinalStrike; }
    private readonly List<RenewalEscalationData> _renewalEscalationBuffer = new List<RenewalEscalationData>();
    private struct RenewalRequestRejectedData { public EmployeeId Id; public string Name; }
    private readonly List<RenewalRequestRejectedData> _renewalRequestRejectedBuffer = new List<RenewalRequestRejectedData>();
    private struct EmployeeDepartedData { public EmployeeId Id; public string Name; public string Reason; }
    private readonly List<EmployeeDepartedData> _employeeDepartedBuffer = new List<EmployeeDepartedData>();
    
    // Tuning-aware accessors — fall back to compile-time defaults
    private int PoolSize             => _tuning != null ? _tuning.CandidatePoolSize             : CandidatePoolSize;
    private int ListMax              => _tuning != null ? _tuning.CandidateListMax               : CandidateListMax;
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

    public void SetEventBus(GameEventBus bus)
    {
        _eventBus = bus;
    }

    public void SetTuningConfig(TuningConfig tuning) { _tuning = tuning; }

    private RoleProfileTable _roleProfileTable;

    public void SetRoleProfileTable(RoleProfileTable roleProfileTable)
    {
        _roleProfileTable = roleProfileTable;
    }

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
        int[] skills = emp.Stats.Skills;
        int skillCount = skills != null ? skills.Length : 0;
        for (int i = 0; i < skillCount; i++)
            if (skills[i] > maxSkill) maxSkill = skills[i];

        if (maxSkill >= 85) return SkillTier.Master;
        if (maxSkill >= 65) return SkillTier.Expert;
        if (maxSkill >= 40) return SkillTier.Competent;
        return SkillTier.Apprentice;
    }

    public EmployeeId HireEmployee(string name, Gender gender, int age, int salary, int currentTick, RoleId role)
    {
        var offer = new EmploymentOffer
        {
            Type          = EmploymentType.FullTime,
            Length        = ContractLengthOption.Standard,
            MonthlySalary = salary
        };
        return HireEmployee(name, gender, age, EmployeeStatBlock.Create(), currentTick, role, offer, default);
    }

    public EmployeeId HireEmployee(string name, Gender gender, int age, EmployeeStatBlock stats, int currentTick, RoleId role, EmploymentOffer offer, CandidatePreferences originalPreferences)
    {
        var employeeId = new EmployeeId(_state.nextEmployeeId++);

        if (age <= 0) age = 25;

        var employee = new Employee(employeeId, name, gender, age, stats, stats.PotentialAbility > 0 ? stats.PotentialAbility : 0, currentTick, role);
        employee.salary = offer.MonthlySalary;
        employee.Contract = ContractTerms.FromOffer(offer, currentTick);
        employee.OriginalPreferences = originalPreferences;

        _state.employees[employeeId] = employee;
        _activeEmployeesDirty = true;

        int contractTicks = employee.Contract.ContractMonths * TimeState.TicksPerDay * 30;
        employee.contractExpiryTick = currentTick + contractTicks;
        int strikeCarry = employee.Renewal.StrikeCount;
        employee.Renewal = new RenewalState
        {
            Phase       = RenewalPhase.Active,
            ExpiryTick  = employee.contractExpiryTick,
            StrikeCount = strikeCarry
        };

        _abilitySystem?.OnEmployeeHired(employeeId, employee);

        RemoveCandidateByName(name);

        _hiredBuffer.Add(employeeId);

        _logger.Log($"[Tick {currentTick}] Hired {name} (ID: {employeeId.Value}) [{role}] Age:{age} Salary:${offer.MonthlySalary}/month [{offer.Type}/{offer.Length}]");

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

    public bool SetEmployeeSkill(EmployeeId id, SkillId skill, int value)
    {
        if (!_state.employees.TryGetValue(id, out var employee)) return false;
        if (!employee.isActive) return false;
        if (value < 0) value = 0;
        employee.Stats.SetSkill(skill, value);
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

    public List<EmployeeId> BulkHireForCompany(CompanyId companyId, CompetitorArchetype archetype, int count, IRng rng, int hireTick, float fullTimeRatio = 0.65f, float salaryTierModifier = 1.0f)
    {
        var result = new List<EmployeeId>(count);
        float qualityMultiplier = GetArchetypeQualityMultiplier(archetype);

        for (int i = 0; i < count; i++)
        {
            RoleId role = RollRoleForArchetype(archetype, rng);
            var candidate = CandidateData.GenerateCandidate(rng, qualityMultiplier, role);

            int ftRoll = rng.Range(0, 100);
            EmploymentType empType = ftRoll < (int)(fullTimeRatio * 100f) ? EmploymentType.FullTime : EmploymentType.PartTime;
            float ptRatio = empType == EmploymentType.PartTime ? 0.60f : 1.0f;
            int baseSalary = SalaryBand.GetBase(role);
            int scaledSalary = SalaryDemandCalculator.Round50(baseSalary * salaryTierModifier * ptRatio);
            if (scaledSalary < 500) scaledSalary = 500;

            var compOffer = new EmploymentOffer
            {
                Type          = empType,
                Length        = ContractLengthOption.Standard,
                MonthlySalary = scaledSalary
            };

            var employeeId = new EmployeeId(_state.nextEmployeeId++);
            int age = rng.Range(22, 45);

            var employee = new Employee(employeeId, candidate.Name, candidate.Gender, age, candidate.Stats, scaledSalary, hireTick, role);
            employee.ownerCompanyId = companyId;
            employee.personality = candidate.personality;
            employee.Contract = ContractTerms.FromOffer(compOffer, hireTick);
            employee.OriginalPreferences = candidate.Preferences;
            int compContractTicks = employee.Contract.ContractMonths * TimeState.TicksPerDay * 30;
            employee.contractExpiryTick = hireTick + compContractTicks;
            employee.Renewal = new RenewalState
            {
                Phase      = RenewalPhase.Active,
                ExpiryTick = employee.contractExpiryTick,
                StrikeCount = 0
            };

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

    private static RoleId RollRoleForArchetype(CompetitorArchetype archetype, IRng rng)
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
        if (roll < wDev)               return RoleId.SoftwareEngineer;
        roll -= wDev;
        if (roll < wDes)               return RoleId.ProductDesigner;
        roll -= wDes;
        if (roll < wQA)                return RoleId.QaEngineer;
        roll -= wQA;
        if (roll < wSound)             return RoleId.AudioDesigner;
        roll -= wSound;
        if (roll < wVFX)               return RoleId.TechnicalArtist;
        return RoleId.Marketer;
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

            TickRenewals(tick);

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

        // Flush shortlist event — signals candidate list mutated for immediate UI refresh
        if (_pendingShortlistEvent)
        {
            _eventBus?.Raise(new CandidatesGeneratedEvent(tick, 0));
            _pendingShortlistEvent = false;
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

        // Flush renewal window opened events
        count = _renewalWindowOpenedBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _renewalWindowOpenedBuffer[i];
            _eventBus?.Raise(new RenewalWindowOpenedEvent(tick, d.Id, d.Name, d.CurrentType, d.DaysUntilExpiry));
        }
        _renewalWindowOpenedBuffer.Clear();

        // Flush renewal change requested events
        count = _renewalChangeRequestedBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _renewalChangeRequestedBuffer[i];
            _eventBus?.Raise(new RenewalChangeRequestedEvent(tick, d.Id, d.Name, d.RequestsTypeChange, d.RequestedType, d.RequestsLengthChange, d.RequestedLength));
        }
        _renewalChangeRequestedBuffer.Clear();

        // Flush renewal escalation events
        count = _renewalEscalationBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _renewalEscalationBuffer[i];
            _eventBus?.Raise(new RenewalEscalationEvent(tick, d.Id, d.Name, d.StrikeCount, d.IsFinalStrike));
        }
        _renewalEscalationBuffer.Clear();

        // Flush renewal request rejected events
        count = _renewalRequestRejectedBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _renewalRequestRejectedBuffer[i];
            _eventBus?.Raise(new RenewalRequestRejectedEvent(tick, d.Id, d.Name));
        }
        _renewalRequestRejectedBuffer.Clear();

        // Flush employee departed events
        count = _employeeDepartedBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var d = _employeeDepartedBuffer[i];
            _eventBus?.Raise(new EmployeeDepartedEvent(tick, d.Id, d.Name, d.Reason));
            OnEmployeeQuit?.Invoke(d.Id);
        }
        _employeeDepartedBuffer.Clear();
    }

    private const int RenewalWindowMonths = 2;
    private const int RenewalWindowTicks = RenewalWindowMonths * 30 * TimeState.TicksPerDay;

    private void TickRenewals(int tick)
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

            // Legacy save migration: seed RenewalState if not yet initialized
            if (emp.Renewal.Phase == RenewalPhase.Active && emp.Renewal.ExpiryTick == 0)
            {
                if (emp.contractExpiryTick > 0)
                {
                    emp.Renewal = new RenewalState
                    {
                        Phase       = RenewalPhase.Active,
                        ExpiryTick  = emp.contractExpiryTick,
                        StrikeCount = emp.Renewal.StrikeCount
                    };
                }
                else
                {
                    int legacyTicks = _rng.Range(MinContractYears, MaxContractYearsExclusive) * TicksPerYear;
                    emp.contractExpiryTick = tick + legacyTicks;
                    emp.Renewal = new RenewalState
                    {
                        Phase       = RenewalPhase.Active,
                        ExpiryTick  = emp.contractExpiryTick,
                        StrikeCount = 0
                    };
                    continue;
                }
            }

            // ── Phase: Active → check if window should open ──────────────────
            if (emp.Renewal.Phase == RenewalPhase.Active)
            {
                int windowStartTick = emp.Renewal.ExpiryTick - RenewalWindowTicks;
                if (tick < windowStartTick) continue;

                int daysUntilExpiry = (emp.Renewal.ExpiryTick - tick) / TimeState.TicksPerDay;

                // Strike 3: employee refuses renewal outright
                if (emp.Renewal.StrikeCount >= 2)
                {
                    emp.Renewal = new RenewalState
                    {
                        Phase          = RenewalPhase.WindowOpen,
                        WindowOpenTick = tick,
                        ExpiryTick     = emp.Renewal.ExpiryTick,
                        StrikeCount    = emp.Renewal.StrikeCount
                    };
                    emp.contractRenewalPending = true;
                    emp.contractExpiryTick = emp.Renewal.ExpiryTick;
                    _renewalWindowOpenedBuffer.Add(new RenewalWindowOpenedData { Id = emp.id, Name = emp.name, CurrentType = emp.Contract.Type, DaysUntilExpiry = daysUntilExpiry });
                    _renewalEscalationBuffer.Add(new RenewalEscalationData { Id = emp.id, Name = emp.name, StrikeCount = emp.Renewal.StrikeCount, IsFinalStrike = true });
                    emp.renewalDemand = 0;
                    _renewalRequestedBuffer.Add(emp.id);
                    _logger.Log($"[Tick {tick}] {emp.name} renewal window: FINAL STRIKE ({emp.Renewal.StrikeCount} strikes) — will not renew");
                    continue;
                }

                // Strike 2: escalation warning but still can renew
                bool hasEscalation = emp.Renewal.StrikeCount == 1;

                // Build change request based on preference mismatch
                bool typeChange = false;
                bool lengthChange = false;
                EmploymentType requestedType = emp.Contract.Type;
                ContractLengthOption requestedLength = ContractLengthOption.Standard;

                if (emp.OriginalPreferences.FtPtPref == FtPtPreference.PrefersFullTime && emp.Contract.Type == EmploymentType.PartTime)
                {
                    typeChange = true;
                    requestedType = EmploymentType.FullTime;
                }
                else if (emp.OriginalPreferences.FtPtPref == FtPtPreference.PrefersPartTime && emp.Contract.Type == EmploymentType.FullTime)
                {
                    typeChange = true;
                    requestedType = EmploymentType.PartTime;
                }

                ContractLengthOption currentLength = GetContractLengthFromMonths(emp.Contract.ContractMonths, emp.Contract.Type);
                if (emp.OriginalPreferences.LengthPref == LengthPreference.PrefersSecurity && currentLength == ContractLengthOption.Short)
                {
                    lengthChange = true;
                    requestedLength = ContractLengthOption.Long;
                }
                else if (emp.OriginalPreferences.LengthPref == LengthPreference.PrefersFlexibility && currentLength == ContractLengthOption.Long)
                {
                    lengthChange = true;
                    requestedLength = ContractLengthOption.Short;
                }
                else
                {
                    requestedLength = currentLength;
                }

                bool hasChangeRequest = typeChange || lengthChange;

                emp.Renewal = new RenewalState
                {
                    Phase              = RenewalPhase.WindowOpen,
                    WindowOpenTick     = tick,
                    ExpiryTick         = emp.Renewal.ExpiryTick,
                    StrikeCount        = emp.Renewal.StrikeCount,
                    HasChangeRequest   = hasChangeRequest,
                    RequestedType      = requestedType,
                    RequestedLength    = requestedLength,
                    RequestedTypeChange   = typeChange,
                    RequestedLengthChange = lengthChange
                };
                emp.contractRenewalPending = true;
                emp.contractExpiryTick = emp.Renewal.ExpiryTick;

                _renewalWindowOpenedBuffer.Add(new RenewalWindowOpenedData { Id = emp.id, Name = emp.name, CurrentType = emp.Contract.Type, DaysUntilExpiry = daysUntilExpiry });

                if (hasChangeRequest)
                    _renewalChangeRequestedBuffer.Add(new RenewalChangeRequestedData { Id = emp.id, Name = emp.name, RequestsTypeChange = typeChange, RequestedType = requestedType, RequestsLengthChange = lengthChange, RequestedLength = requestedLength });

                if (hasEscalation)
                    _renewalEscalationBuffer.Add(new RenewalEscalationData { Id = emp.id, Name = emp.name, StrikeCount = 2, IsFinalStrike = false });

                // Keep legacy event chain alive for inbox system compatibility
                SkillTier renewTier = DeriveEmployeeTier(emp);
                int renewMarket = GetBenchmarkSalary(renewTier);
                if (renewMarket <= 0) renewMarket = emp.salary;
                emp.renewalDemand = SalaryModifierCalculator.ComputeRenewalDemand(emp, renewMarket, emp.Contract.Type, GetContractLengthFromMonths(emp.Contract.ContractMonths, emp.Contract.Type), emp.Renewal.StrikeCount);
                _renewalRequestedBuffer.Add(emp.id);

                _logger.Log($"[Tick {tick}] {emp.name} renewal window opened — {daysUntilExpiry}d left, strikes:{emp.Renewal.StrikeCount}, changeReq:{hasChangeRequest}");
                continue;
            }

            // ── Phase: WindowOpen → check for expiry departure ───────────────
            if (emp.Renewal.Phase == RenewalPhase.WindowOpen)
            {
                if (tick < emp.Renewal.ExpiryTick) continue;

                // Final strike employees always depart; others depart if window expired without renewal
                emp.Renewal = new RenewalState
                {
                    Phase       = RenewalPhase.Departed,
                    ExpiryTick  = emp.Renewal.ExpiryTick,
                    StrikeCount = emp.Renewal.StrikeCount
                };
                emp.isActive = false;
                emp.contractRenewalPending = false;
                _activeEmployeesDirty = true;
                _employeeDepartedBuffer.Add(new EmployeeDepartedData { Id = emp.id, Name = emp.name, Reason = "Contract expired without renewal" });
                _logger.Log($"[Tick {tick}] {emp.name} departed — contract expired without renewal");
            }
        }
    }

    private static ContractLengthOption GetContractLengthFromMonths(int months, EmploymentType type)
    {
        if (type == EmploymentType.FullTime)
        {
            if (months <= 6)  return ContractLengthOption.Short;
            if (months <= 12) return ContractLengthOption.Standard;
            return ContractLengthOption.Long;
        }
        else
        {
            if (months <= 3) return ContractLengthOption.Short;
            if (months <= 6) return ContractLengthOption.Standard;
            return ContractLengthOption.Long;
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

    // Distributes 5–15 CA of decay across all 26 skills, weighted by inverse tier multiplier.
    // Primary skills (mult=2, weight=0.5) have higher decay probability than tertiary (mult=4, weight=0.25).
    private void DecaySkills(Employee emp, int tick)
    {
        int decayCA = _rng.Range(5, 16);

        int[] roleTiers = _abilitySystem != null
            ? (_abilitySystem.ProfileTable?.Get(emp.role) != null
                ? RoleSuitabilityCalculator.BuildTierArray(_abilitySystem.ProfileTable.Get(emp.role))
                : null)
            : null;

        float minFloor = 0.5f / SkillIdHelper.SkillCount;
        float weightSum = 0f;
        for (int i = 0; i < SkillIdHelper.SkillCount; i++)
        {
            float invMult = (roleTiers != null && i < roleTiers.Length && roleTiers[i] > 0)
                ? 1.0f / roleTiers[i]
                : minFloor;
            _decayWeightCdf[i] = (invMult > minFloor ? invMult : minFloor);
            weightSum += _decayWeightCdf[i];
        }

        // Normalise into cumulative distribution
        float running = 0f;
        for (int i = 0; i < SkillIdHelper.SkillCount; i++)
        {
            running += _decayWeightCdf[i] / weightSum;
            _decayWeightCdf[i] = running;
        }
        _decayWeightCdf[SkillIdHelper.SkillCount - 1] = 1f;

        // Distribute each CA unit via weighted random selection
        for (int i = 0; i < SkillIdHelper.SkillCount; i++)
            _decayScratch[i] = 0;

        for (int u = 0; u < decayCA; u++)
        {
            float roll = _rng.Range(0, 10000) / 10000f;
            for (int i = 0; i < SkillIdHelper.SkillCount; i++)
            {
                if (roll <= _decayWeightCdf[i])
                {
                    _decayScratch[i]++;
                    break;
                }
            }
        }

        // Apply skill losses via float accumulator — drains XP buffer before dropping a level
        for (int i = 0; i < SkillIdHelper.SkillCount; i++)
        {
            int caAlloc = _decayScratch[i];
            if (caAlloc == 0) continue;

            int tierMult = (roleTiers != null && i < roleTiers.Length && roleTiers[i] > 0)
                ? roleTiers[i]
                : 3;
            int currentLevel = emp.Stats.Skills[i];
            // Approximate marginal cost: higher-level skills cost more to decay (flat scale 1–3).
            int marginalCost = 1 + (currentLevel / 7); // 0-6→1, 7-13→2, 14-20→3
            if (marginalCost < 1) marginalCost = 1;

            float decayAmount = (float)(caAlloc * 2) / (float)(marginalCost * tierMult);

            emp.Stats.SkillDeltaDirection[i] = -1;
            emp.Stats.SkillXp[i] -= decayAmount;

            while (emp.Stats.SkillXp[i] < 0f && emp.Stats.Skills[i] > 0)
            {
                emp.Stats.SkillXp[i] += 1.0f;
                emp.Stats.Skills[i]--;
            }

            if (emp.Stats.Skills[i] == 0 && emp.Stats.SkillXp[i] < 0f)
                emp.Stats.SkillXp[i] = 0f;
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
            bool hasProgress = _interviewSystem != null
                ? _interviewSystem.GetKnowledgeLevel(c.CandidateId) > 0f
                : c.InterviewStage > 0;
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

            var hireStats = hire.Stats.Skills != null ? hire.Stats : EmployeeStatBlock.Create();
            if (hire.PotentialAbility > 0) hireStats.PotentialAbility = hire.PotentialAbility;

            var hireOffer = new EmploymentOffer
            {
                Type          = hire.EmploymentType != default ? hire.EmploymentType : EmploymentType.FullTime,
                Length        = hire.ContractLength != default ? hire.ContractLength : ContractLengthOption.Standard,
                MonthlySalary = finalSalary,
                Role          = hire.Role
            };
            EmployeeId hiredId = HireEmployee(hire.Name, hire.Gender, hire.Age, hireStats, command.Tick, hire.Role, hireOffer, default);

            if (hiredId.Value >= 0 && _state.employees.TryGetValue(hiredId, out var hiredEmployee))
            {
                hiredEmployee.ownerCompanyId = hire.CompanyId;
                hiredEmployee.personality = hire.Personality;
                hiredEmployee.preferredRole = hire.PreferredRole != default ? hire.PreferredRole : hire.Role;
                if (hire.Role != default)
                    hiredEmployee.role = hire.Role;
                var offer = new EmploymentOffer
                {
                    Type          = hire.EmploymentType,
                    Length        = hire.ContractLength,
                    MonthlySalary = hire.Salary,
                    Role          = hire.Role
                };
                hiredEmployee.Contract = ContractTerms.FromOffer(offer, command.Tick);
                int hireTicks = hiredEmployee.Contract.ContractMonths * TimeState.TicksPerDay * 30;
                hiredEmployee.contractExpiryTick = command.Tick + hireTicks;
                int strikeCarryHire = hiredEmployee.Renewal.StrikeCount;
                hiredEmployee.Renewal = new RenewalState
                {
                    Phase       = RenewalPhase.Active,
                    ExpiryTick  = hiredEmployee.contractExpiryTick,
                    StrikeCount = strikeCarryHire
                };

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
        else if (command is ShortlistCandidateCommand shortlist)
        {
            int count = _state.availableCandidates.Count;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (_state.availableCandidates[i].CandidateId == shortlist.CandidateId)
                {
                    var c = _state.availableCandidates[i];
                    c.IsTargeted = true;
                    if (shortlist.DurationDays == -1)
                        c.ExpiryTick = int.MaxValue;
                    else if (shortlist.DurationDays > 0)
                        c.ExpiryTick = command.Tick + shortlist.DurationDays * TimeState.TicksPerDay;
                    else
                        c.ExpiryTick = int.MaxValue;
                    _state.availableCandidates[i] = c;
                    _logger.Log($"[Tick {command.Tick}] Candidate {shortlist.CandidateId} shortlisted — IsTargeted=true, ExpiryTick={c.ExpiryTick}");
                    _pendingShortlistEvent = true;
                    found = true;
                    break;
                }
            }
            if (!found)
                _logger.Log($"[Tick {command.Tick}] ShortlistCandidateCommand: candidate {shortlist.CandidateId} not found");
        }
        else if (command is RenewContractCommand renew)
        {
            if (!_state.employees.TryGetValue(renew.EmployeeId, out var emp)) return;
            if (!emp.isActive || emp.Renewal.Phase != RenewalPhase.WindowOpen) return;

            // Strike 3: employee refuses renewal regardless
            if (emp.Renewal.StrikeCount >= 2)
            {
                _logger.Log($"[Tick {command.Tick}] Cannot renew {emp.name} — final strike, employee refused");
                return;
            }

            int oldSalary = emp.salary;

            // Determine effective type/length — use employee's requested if player accepts, otherwise use provided
            EmploymentType effectiveType   = renew.NewType;
            ContractLengthOption effectiveLength = renew.NewLength;

            // Strike management
            if (emp.Renewal.HasChangeRequest && !renew.AcceptsRequest)
            {
                emp.Renewal = new RenewalState
                {
                    Phase              = RenewalPhase.Active,
                    ExpiryTick         = 0,
                    StrikeCount        = emp.Renewal.StrikeCount + 1,
                    HasChangeRequest   = false,
                    RequestedType      = default,
                    RequestedLength    = default,
                    RequestedTypeChange   = false,
                    RequestedLengthChange = false
                };
                _renewalRequestRejectedBuffer.Add(new RenewalRequestRejectedData { Id = emp.id, Name = emp.name });
            }
            else
            {
                int resetStrikes = renew.AcceptsRequest ? 0 : emp.Renewal.StrikeCount;
                emp.Renewal = new RenewalState
                {
                    Phase       = RenewalPhase.Active,
                    ExpiryTick  = 0,
                    StrikeCount = resetStrikes
                };
            }

            // Compute renewal salary using market benchmark
            SkillTier tier = DeriveEmployeeTier(emp);
            int marketRate = GetBenchmarkSalary(tier);
            if (marketRate <= 0) marketRate = emp.salary;
            int newSalary = SalaryModifierCalculator.ComputeRenewalDemand(emp, marketRate, effectiveType, effectiveLength, emp.Renewal.StrikeCount);

            // Build new contract terms
            var renewalOffer = new EmploymentOffer { Type = effectiveType, Length = effectiveLength, MonthlySalary = newSalary };
            emp.Contract = ContractTerms.FromOffer(renewalOffer, command.Tick);
            emp.salary = newSalary;
            emp.contractRenewalPending = false;
            emp.renewalDemand = 0;
            _activeEmployeesDirty = true;

            int renewContractTicks = emp.Contract.ContractMonths * TimeState.TicksPerDay * 30;
            emp.contractExpiryTick = command.Tick + renewContractTicks;
            emp.Renewal = new RenewalState
            {
                Phase       = RenewalPhase.Active,
                ExpiryTick  = emp.contractExpiryTick,
                StrikeCount = emp.Renewal.StrikeCount
            };

            _renewedBuffer.Add(new RenewedEvent { Id = renew.EmployeeId, NewSalary = newSalary, OldSalary = oldSalary });
            _logger.Log($"[Tick {command.Tick}] Contract renewed for {emp.name}: ${oldSalary} → ${newSalary}/mo [{effectiveType}/{effectiveLength}]");
        }
        else if (command is DeclineRenewalCommand decline)
        {
            if (_state.employees.TryGetValue(decline.EmployeeId, out var emp) && emp.isActive)
            {
                emp.isActive = false;
                emp.contractRenewalPending = false;
                emp.Renewal = new RenewalState { Phase = RenewalPhase.Departed, ExpiryTick = emp.Renewal.ExpiryTick, StrikeCount = emp.Renewal.StrikeCount };
                _activeEmployeesDirty = true;
                _quitEventBuffer.Add(decline.EmployeeId);
                _logger.Log($"[Tick {command.Tick}] {emp.name} quit — contract renewal declined by player");
            }
        }
    }
    
    private void CheckCandidateGenerationSchedule(int currentTick)
    {
        int currentDay = currentTick / TimeState.TicksPerDay;
        int lastGenDay = _state.lastCandidateGenerationTick / TimeState.TicksPerDay;
        int currentMonth = TimeState.GetMonth(currentDay);
        int currentYear = TimeState.GetYear(currentDay);
        int lastGenMonth = TimeState.GetMonth(lastGenDay);
        int lastGenYear = TimeState.GetYear(lastGenDay);
        int dayOfMonth = TimeState.GetDayOfMonth(currentDay);
        bool isNewMonth = (currentYear > lastGenYear) || (currentYear == lastGenYear && currentMonth > lastGenMonth);
        if (isNewMonth && dayOfMonth == 1) {
            _pendingCandidateGeneration = true;
        }
    }
    
    private void GenerateNewCandidates(int count)
    {
        // Bulk-clear stale market candidates before filling new slots.
        // Candidates mid-interview or with accumulated knowledge are preserved.
        for (int i = _state.availableCandidates.Count - 1; i >= 0; i--)
        {
            var c = _state.availableCandidates[i];
            if (c.IsTargeted) continue;
            bool inInterview = _interviewSystem != null && _interviewSystem.IsInterviewInProgress(c.CandidateId);
            bool hasKnowledge = _interviewSystem != null && _interviewSystem.GetKnowledgeLevel(c.CandidateId) > 0f;
            if (!inInterview && !hasKnowledge)
                _state.availableCandidates.RemoveAt(i);
        }

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

        // Anti-frustration bias: build role weight overrides if company is missing critical functions
        float[] roleWeightOverrides = BuildAntiFrustrationWeights();

        // Pool composition tracking for diversity guarantee (Page 05 section 22.2)
        bool hasEngineeringOrProduct = false;
        bool hasQualitySupport = false;
        bool hasCommercialOrOps = false;
        bool hasJuniorRawTalent = false;

        for (int i = 0; i < newSlots; i++)
        {
            if (_state.availableCandidates.Count >= ListMax) break;

            var genParams = CandidateGenerationParams.OpenMarket(qualityMultiplier);
            genParams.RoleWeightOverrides = roleWeightOverrides;

            // Diversity guarantee: force missing pool composition roles in last few slots
            int remaining = newSlots - i;
            if (remaining <= 4)
            {
                if (!hasEngineeringOrProduct)
                    genParams.ForceFamily = _candidateRng.Range(0, 2) == 0 ? RoleFamily.Engineering : RoleFamily.Product;
                else if (!hasQualitySupport)
                    genParams.ForceFamily = RoleFamily.QualityAndSupport;
                else if (!hasCommercialOrOps)
                    genParams.ForceFamily = _candidateRng.Range(0, 2) == 0 ? RoleFamily.Commercial : RoleFamily.Operations;
                else if (!hasJuniorRawTalent)
                    genParams.ForceCareerStage = CareerStage.Junior;
            }

            var candidate = CandidateData.GenerateCandidate(_candidateRng, _roleProfileTable, genParams);
            candidate.CandidateId = _state.nextCandidateId++;
            _abilitySystem?.GenerateCandidateAbility(candidate);
            _state.availableCandidates.Add(candidate);

            // Track composition
            RoleFamily family = RoleIdHelper.GetFamily(candidate.Role);
            if (family == RoleFamily.Engineering || family == RoleFamily.Product) hasEngineeringOrProduct = true;
            if (family == RoleFamily.QualityAndSupport) hasQualitySupport = true;
            if (family == RoleFamily.Commercial || family == RoleFamily.Operations) hasCommercialOrOps = true;
            if (candidate.CareerStage == CareerStage.Junior || candidate.Archetype == CandidateArchetype.RawTalent) hasJuniorRawTalent = true;
        }
    }
    
    // Anti-frustration bias: if company has no employees in a critical function,
    // increase that role family's weight slightly (Page 05 section 22.3).
    // Returns null if no overrides needed, or a float[] of length RoleIdHelper.RoleCount.
    private float[] BuildAntiFrustrationWeights()
    {
        if (_state == null || _state.employees == null) return null;

        // Count active employees per family
        int[] familyCounts = new int[7]; // matches RoleFamily enum range
        foreach (var kvp in _state.employees)
        {
            var emp = kvp.Value;
            if (emp == null || !emp.isActive) continue;
            int familyIdx = (int)RoleIdHelper.GetFamily(emp.role);
            if (familyIdx >= 0 && familyIdx < familyCounts.Length)
                familyCounts[familyIdx]++;
        }

        bool needsOverride = false;
        for (int f = 0; f < familyCounts.Length; f++)
        {
            if (familyCounts[f] == 0) { needsOverride = true; break; }
        }
        if (!needsOverride) return null;

        int roleCount = RoleIdHelper.RoleCount;
        float[] overrides = new float[roleCount];
        for (int i = 0; i < roleCount; i++) overrides[i] = 1.0f;

        // Small weight boost for roles in missing families (+0.3)
        for (int i = 0; i < roleCount; i++)
        {
            int familyIdx = (int)RoleIdHelper.GetFamily((RoleId)i);
            if (familyIdx >= 0 && familyIdx < familyCounts.Length && familyCounts[familyIdx] == 0)
                overrides[i] += 0.3f;
        }
        return overrides;
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
