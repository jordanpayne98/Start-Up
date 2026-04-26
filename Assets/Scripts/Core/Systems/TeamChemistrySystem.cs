using System;
using System.Collections.Generic;

public class TeamChemistrySystem : ISystem
{
    public event Action<TeamId, ChemistryBand> OnChemistryChanged;
    public event Action<TeamId, EmployeeId, EmployeeId> OnConflictOccurred;

    private ChemistryState _state;
    private TeamSystem _teamSystem;
    private EmployeeSystem _employeeSystem;
    private MoraleSystem _moraleSystem;
    private FatigueSystem _fatigueSystem;
    private GameEventBus _eventBus;
    private IRng _rng;
    private TuningConfig _tuning;

    private const float ConflictChanceBase = 0.05f;
    private const float ConflictChanceLowChemMod = 0.03f;
    private const float DailyDriftBase = 0.0f;
    private const float ConflictPairAffinityHit = -0.35f;
    private const float ConflictOtherPairsHit = -0.05f;
    private const float ConflictTeamMoralePenalty = -1f;
    private const float ConflictEmployeeMoralePenalty = -3f;
    private const float ConflictSpeedPenalty = -0.03f;
    private const float ConflictQualityPenalty = -0.05f;
    private const int ConflictPenaltyDays = 2;
    private const float MaxAffinity = 2.0f;
    private const float MinAffinity = -2.0f;
    private const float SuccessShift = 0.15f;
    private const float FailureShift = -0.15f;

    private readonly HashSet<TeamId> _dirtyTeams = new HashSet<TeamId>();

    private readonly List<EmployeeId> _memberScratch = new List<EmployeeId>(16);
    private readonly List<long> _pairScratch = new List<long>(64);
    private readonly List<float> _pairEffective = new List<float>(64);
    private readonly List<long> _negativePairScratch = new List<long>(16);
    private readonly List<(TeamId teamId, ChemistryBand oldBand, ChemistryBand newBand)> _bandChangedBuffer
        = new List<(TeamId, ChemistryBand, ChemistryBand)>(4);
    private readonly List<(TeamId teamId, EmployeeId a, EmployeeId b)> _conflictBuffer
        = new List<(TeamId, EmployeeId, EmployeeId)>(4);

    public TeamChemistrySystem(
        ChemistryState state,
        TeamSystem teamSystem,
        EmployeeSystem employeeSystem,
        MoraleSystem moraleSystem,
        GameEventBus eventBus,
        IRng rng)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _teamSystem = teamSystem ?? throw new ArgumentNullException(nameof(teamSystem));
        _employeeSystem = employeeSystem ?? throw new ArgumentNullException(nameof(employeeSystem));
        _moraleSystem = moraleSystem ?? throw new ArgumentNullException(nameof(moraleSystem));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));

        _teamSystem.OnEmployeeAssignedToTeam += OnEmployeeAssignedHandler;
        _teamSystem.OnEmployeeRemovedFromTeam += OnEmployeeRemovedHandler;
        _teamSystem.OnTeamDeleted += OnTeamDeletedHandler;
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    public void SetFatigueSystem(FatigueSystem fatigueSystem)
    {
        _fatigueSystem = fatigueSystem;
    }

    private void OnEmployeeAssignedHandler(EmployeeId empId, TeamId teamId)
    {
        MarkTeamDirty(teamId);
    }

    private void OnEmployeeRemovedHandler(EmployeeId empId, TeamId teamId)
    {
        MarkTeamDirty(teamId);
    }

    private void OnTeamDeletedHandler(TeamId teamId)
    {
        _state.teamChemistry.Remove(teamId);
        _dirtyTeams.Remove(teamId);
    }

    public void MarkTeamDirty(TeamId teamId)
    {
        _dirtyTeams.Add(teamId);
    }

    public void OnEmployeeRemoved(EmployeeId employeeId)
    {
        var keysToRemove = new List<long>(8);
        foreach (var kvp in _state.relationships)
        {
            long key = kvp.Key;
            int lo = (int)(key >> 32);
            int hi = (int)(key & 0xFFFFFFFF);
            if (lo == employeeId.Value || hi == employeeId.Value)
                keysToRemove.Add(key);
        }
        int removeCount = keysToRemove.Count;
        for (int i = 0; i < removeCount; i++)
            _state.relationships.Remove(keysToRemove[i]);
    }

    public void ApplyEventShift(TeamId teamId, float delta)
    {
        var team = _teamSystem.GetTeam(teamId);
        if (team == null) return;

        _memberScratch.Clear();
        int mc = team.members.Count;
        for (int i = 0; i < mc; i++)
        {
            var emp = _employeeSystem.GetEmployee(team.members[i]);
            if (emp != null && emp.isActive) _memberScratch.Add(team.members[i]);
        }

        int active = _memberScratch.Count;
        for (int i = 0; i < active; i++)
        {
            for (int j = i + 1; j < active; j++)
            {
                long key = ChemistryState.PackPairKey(_memberScratch[i], _memberScratch[j]);
                float current = 0f;
                _state.relationships.TryGetValue(key, out current);
                float newAffinity = current + delta;
                if (newAffinity > MaxAffinity) newAffinity = MaxAffinity;
                if (newAffinity < MinAffinity) newAffinity = MinAffinity;
                _state.relationships[key] = newAffinity;
            }
        }
        MarkTeamDirty(teamId);
    }

    public int ProjectChemistryChange(TeamId teamId, EmployeeId candidate)
    {
        var team = _teamSystem.GetTeam(teamId);
        if (team == null) return 0;

        var candidateEmp = _employeeSystem.GetEmployee(candidate);
        if (candidateEmp == null) return 0;

        _memberScratch.Clear();
        int mc = team.members.Count;
        for (int i = 0; i < mc; i++)
        {
            var emp = _employeeSystem.GetEmployee(team.members[i]);
            if (emp != null && emp.isActive) _memberScratch.Add(team.members[i]);
        }

        int currentScore = ComputeSnapshotForMembers(_memberScratch).Score;

        _memberScratch.Add(candidate);
        int projectedScore = ComputeSnapshotForMembers(_memberScratch).Score;

        return projectedScore - currentScore;
    }

    public TeamChemistrySnapshot GetTeamChemistry(TeamId teamId)
    {
        if (_state.teamChemistry.TryGetValue(teamId, out var snapshot))
            return snapshot;
        return new TeamChemistrySnapshot { Score = 0, Band = ChemistryBand.Neutral };
    }

    public float GetPairAffinity(EmployeeId a, EmployeeId b)
    {
        long key = ChemistryState.PackPairKey(a, b);
        if (_state.relationships.TryGetValue(key, out float affinity))
            return affinity;
        return 0f;
    }

    public static ChemistryBand GetChemistryBand(int score)
    {
        if (score >= 35)  return ChemistryBand.Excellent;
        if (score >= 10)  return ChemistryBand.Good;
        if (score >= -9)  return ChemistryBand.Neutral;
        if (score >= -34) return ChemistryBand.Poor;
        return ChemistryBand.Toxic;
    }

    public void ProcessDailyChemistry(int tick)
    {
        var teams = _teamSystem.GetActiveTeamsForCompany(CompanyId.Player);
        int teamCount = teams.Count;

        for (int t = 0; t < teamCount; t++)
        {
            var team = teams[t];
            if (!team.isActive) continue;

            _memberScratch.Clear();
            int mc = team.members.Count;
            for (int m = 0; m < mc; m++)
            {
                var emp = _employeeSystem.GetEmployee(team.members[m]);
                if (emp != null && emp.isActive && emp.ownerCompanyId.IsPlayer)
                    _memberScratch.Add(team.members[m]);
            }

            if (_memberScratch.Count < 2)
            {
                var zeroSnap = new TeamChemistrySnapshot { Score = 0, Band = ChemistryBand.Neutral };
                _state.teamChemistry[team.id] = zeroSnap;
                continue;
            }

            bool isDirty = _dirtyTeams.Contains(team.id);

            if (isDirty)
            {
                var oldBand = ChemistryBand.Neutral;
                _state.teamChemistry.TryGetValue(team.id, out var existing);
                oldBand = existing.Band;

                var snap = ComputeSnapshotForMembers(_memberScratch);
                _state.teamChemistry[team.id] = snap;

                if (snap.Band != oldBand)
                    _bandChangedBuffer.Add((team.id, oldBand, snap.Band));
            }

            _state.teamChemistry.TryGetValue(team.id, out var currentSnap);
            float conflictChance = ConflictChanceBase;
            if (currentSnap.Score < 0) conflictChance += ConflictChanceLowChemMod;

            // Energy pressure increases conflict chance when team is fatigued
            if (_fatigueSystem != null) {
                float avgEnergy = _fatigueSystem.GetAverageTeamEnergy(team.id);
                if (avgEnergy < 40f) conflictChance += 0.02f;
                if (avgEnergy < 25f) conflictChance += 0.03f;
            }

            if (_rng.Chance(conflictChance))
            {
                TryRollConflict(team.id, _memberScratch, tick);
            }
        }

        _dirtyTeams.Clear();

        // Tick down conflict penalties
        int penaltyCount = _state.activeConflictPenalties.Count;
        for (int i = penaltyCount - 1; i >= 0; i--)
        {
            var penalty = _state.activeConflictPenalties[i];
            penalty.ticksRemaining -= TimeState.TicksPerDay;
            if (penalty.ticksRemaining <= 0)
                _state.activeConflictPenalties.RemoveAt(i);
            else
                _state.activeConflictPenalties[i] = penalty;
        }
    }

    private void TryRollConflict(TeamId teamId, List<EmployeeId> activeMembers, int tick)
    {
        int count = activeMembers.Count;
        _negativePairScratch.Clear();

        for (int i = 0; i < count; i++)
        {
            var empA = _employeeSystem.GetEmployee(activeMembers[i]);
            if (empA == null) continue;
            for (int j = i + 1; j < count; j++)
            {
                var empB = _employeeSystem.GetEmployee(activeMembers[j]);
                if (empB == null) continue;

                int compat = PersonalitySystem.GetBaseCompatibility(empA.personality, empB.personality);
                long key = ChemistryState.PackPairKey(activeMembers[i], activeMembers[j]);
                float affinity = 0f;
                _state.relationships.TryGetValue(key, out affinity);
                float effective = compat + affinity;

                if (effective < 0f)
                    _negativePairScratch.Add(key);
            }
        }

        if (_negativePairScratch.Count == 0) return;

        int chosen = _rng.Range(0, _negativePairScratch.Count);
        long conflictKey = _negativePairScratch[chosen];
        int loId = (int)(conflictKey >> 32);
        int hiId = (int)(conflictKey & 0xFFFFFFFF);
        var empIdA = new EmployeeId(loId);
        var empIdB = new EmployeeId(hiId);

        // Apply affinity hit to conflict pair
        float pairAffinity = 0f;
        _state.relationships.TryGetValue(conflictKey, out pairAffinity);
        pairAffinity += ConflictPairAffinityHit;
        if (pairAffinity < MinAffinity) pairAffinity = MinAffinity;
        _state.relationships[conflictKey] = pairAffinity;

        // Apply minor affinity hit to all other pairs
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                long pKey = ChemistryState.PackPairKey(activeMembers[i], activeMembers[j]);
                if (pKey == conflictKey) continue;
                float a = 0f;
                _state.relationships.TryGetValue(pKey, out a);
                a += ConflictOtherPairsHit;
                if (a < MinAffinity) a = MinAffinity;
                _state.relationships[pKey] = a;
            }
        }

        // Morale hit to team and conflict pair
        for (int m = 0; m < count; m++)
        {
            float hit = (activeMembers[m] == empIdA || activeMembers[m] == empIdB)
                ? ConflictEmployeeMoralePenalty
                : ConflictTeamMoralePenalty;
            _moraleSystem.ApplyDirectMoraleDelta(activeMembers[m], hit);
        }

        // Add conflict penalty
        _state.activeConflictPenalties.Add(new ConflictPenalty
        {
            teamId = teamId,
            ticksRemaining = ConflictPenaltyDays * TimeState.TicksPerDay,
            speedPenalty = ConflictSpeedPenalty,
            qualityPenalty = ConflictQualityPenalty
        });

        MarkTeamDirty(teamId);
        _conflictBuffer.Add((teamId, empIdA, empIdB));
    }

    private TeamChemistrySnapshot ComputeSnapshotForMembers(List<EmployeeId> members)
    {
        int count = members.Count;
        if (count < 2) return new TeamChemistrySnapshot { Score = 0, Band = ChemistryBand.Neutral };

        _pairScratch.Clear();
        _pairEffective.Clear();

        float pairSum = 0f;
        int pairCount = 0;
        int disruptiveCount = 0;

        for (int i = 0; i < count; i++)
        {
            var empA = _employeeSystem.GetEmployee(members[i]);
            if (empA == null) continue;
            if (PersonalitySystem.IsDisruptive(empA.personality)) disruptiveCount++;

            for (int j = i + 1; j < count; j++)
            {
                var empB = _employeeSystem.GetEmployee(members[j]);
                if (empB == null) continue;

                int compat = PersonalitySystem.GetBaseCompatibility(empA.personality, empB.personality);
                long key = ChemistryState.PackPairKey(members[i], members[j]);
                float affinity = 0f;
                _state.relationships.TryGetValue(key, out affinity);
                float effective = compat + affinity;
                pairSum += effective;
                pairCount++;
            }
        }

        if (pairCount == 0) return new TeamChemistrySnapshot { Score = 0, Band = ChemistryBand.Neutral };

        float avgPair = pairSum / pairCount;

        float compositionBonus = 0f;
        if (disruptiveCount == 0 && count >= 3) compositionBonus = 0.50f;
        else if (disruptiveCount == 1) compositionBonus = -0.50f;
        else if (disruptiveCount >= 2) compositionBonus = -1.00f;

        float rawScore = (avgPair + compositionBonus) * 20f;
        int score = (int)Math.Round(rawScore);
        if (score > 100) score = 100;
        if (score < -100) score = -100;

        var band = GetChemistryBand(score);
        return new TeamChemistrySnapshot { Score = score, Band = band };
    }

    public float GetTeamSpeedPenalty(TeamId teamId)
    {
        float total = 0f;
        int count = _state.activeConflictPenalties.Count;
        for (int i = 0; i < count; i++)
        {
            if (_state.activeConflictPenalties[i].teamId.Value == teamId.Value)
                total += _state.activeConflictPenalties[i].speedPenalty;
        }
        return total;
    }

    public float GetTeamQualityPenalty(TeamId teamId)
    {
        float total = 0f;
        int count = _state.activeConflictPenalties.Count;
        for (int i = 0; i < count; i++)
        {
            if (_state.activeConflictPenalties[i].teamId.Value == teamId.Value)
                total += _state.activeConflictPenalties[i].qualityPenalty;
        }
        return total;
    }

    public void PreTick(int tick) { }
    public void Tick(int tick) { }

    public void PostTick(int tick)
    {
        int changeCount = _bandChangedBuffer.Count;
        for (int i = 0; i < changeCount; i++)
        {
            var (teamId, _, newBand) = _bandChangedBuffer[i];
            OnChemistryChanged?.Invoke(teamId, newBand);
        }
        _bandChangedBuffer.Clear();

        int conflictCount = _conflictBuffer.Count;
        for (int i = 0; i < conflictCount; i++)
        {
            var (teamId, empA, empB) = _conflictBuffer[i];
            OnConflictOccurred?.Invoke(teamId, empA, empB);
            _eventBus.Raise(new TeamConflictEvent(tick, teamId, empA, empB));
        }
        _conflictBuffer.Clear();
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        if (_teamSystem != null)
        {
            _teamSystem.OnEmployeeAssignedToTeam -= OnEmployeeAssignedHandler;
            _teamSystem.OnEmployeeRemovedFromTeam -= OnEmployeeRemovedHandler;
            _teamSystem.OnTeamDeleted -= OnTeamDeletedHandler;
        }
    }
}
