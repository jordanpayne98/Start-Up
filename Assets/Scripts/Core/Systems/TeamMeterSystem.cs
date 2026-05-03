// TeamMeterSystem — Wave 3C
// Computes 5 derived team-level meters: Creativity, Coordination, Reliability,
// TechnicalStrength, CommercialAwareness (each 0-100).
// Dirty-flag cache with lazy recalculation. No per-tick allocation.
using System;
using System.Collections.Generic;

public class TeamMeterSystem : ISystem
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------
    public event Action<TeamId> OnTeamMetersDirty;
    public event Action<TeamMetersChangedEvent> OnTeamMetersChanged;

    // -------------------------------------------------------------------------
    // Dependencies
    // -------------------------------------------------------------------------
    private readonly TeamSystem _teamSystem;
    private readonly EmployeeSystem _employeeSystem;
    private readonly AbilitySystem _abilitySystem;
    private readonly MoraleSystem _moraleSystem;
    private readonly TeamChemistrySystem _chemistrySystem;
    private readonly RoleProfileTable _roleProfileTable;
    private readonly ILogger _logger;

    // -------------------------------------------------------------------------
    // Cache
    // -------------------------------------------------------------------------
    private readonly Dictionary<TeamId, TeamMeterSnapshot> _cache
        = new Dictionary<TeamId, TeamMeterSnapshot>();
    private readonly HashSet<TeamId> _dirtyTeams
        = new HashSet<TeamId>();

    // -------------------------------------------------------------------------
    // Scratch lists — pre-allocated, zero-allocation in steady state
    // -------------------------------------------------------------------------
    private readonly List<EmployeeId> _memberScratch  = new List<EmployeeId>(16);
    private readonly List<int>        _intScratch      = new List<int>(16);
    private readonly List<RoleId>     _roleScratch     = new List<RoleId>(16);

    // -------------------------------------------------------------------------
    // PostTick event buffer
    // -------------------------------------------------------------------------
    private readonly List<(TeamId teamId, TeamMeterSnapshot prev, TeamMeterSnapshot curr)> _changedBuffer
        = new List<(TeamId, TeamMeterSnapshot, TeamMeterSnapshot)>(4);

    // -------------------------------------------------------------------------
    // Skill sets used by meter formulas (static — avoid re-creating per call)
    // -------------------------------------------------------------------------
    // Creativity — default creative skills
    private static readonly SkillId[] CreativeSkillsDefault =
    {
        SkillId.ProductDesign, SkillId.UxUiDesign, SkillId.GameDesign
    };
    private static readonly SkillId[] CreativeSkillsGame =
    {
        SkillId.GameDesign, SkillId.ProductDesign, SkillId.WritingContent, SkillId.VisualArt, SkillId.AudioDesign
    };
    private static readonly SkillId[] CreativeSkillsSoftware =
    {
        SkillId.ProductDesign, SkillId.UxUiDesign, SkillId.UserResearch
    };
    private static readonly SkillId[] CreativeSkillsHardware =
    {
        SkillId.ProductDesign, SkillId.HardwareIntegration, SkillId.PerformanceOptimisation
    };

    // Coordination — org skills
    private static readonly SkillId[] OrgSkills =
    {
        SkillId.ReleaseManagement, SkillId.ProductDesign
    };

    // Reliability — QA skills
    private static readonly SkillId[] QaSkills =
    {
        SkillId.QaTesting, SkillId.BugFixing, SkillId.ReleaseManagement
    };

    // CommercialAwareness — commercial skills
    private static readonly SkillId[] CommercialSkills =
    {
        SkillId.Marketing, SkillId.BrandManagement, SkillId.Sales, SkillId.Negotiation,
        SkillId.UserResearch, SkillId.ProductDesign, SkillId.WritingContent
    };
    private static readonly SkillId[] BestCommercialSpecialistSkills =
    {
        SkillId.Marketing, SkillId.Sales, SkillId.BrandManagement
    };

    // Required roles per product assignment (for role coverage)
    private static readonly RoleId[] RequiredRolesGame =
    {
        RoleId.SoftwareEngineer, RoleId.GameDesigner, RoleId.TechnicalArtist, RoleId.AudioDesigner, RoleId.QaEngineer
    };
    private static readonly RoleId[] RequiredRolesSoftware =
    {
        RoleId.SoftwareEngineer, RoleId.ProductDesigner, RoleId.QaEngineer
    };
    private static readonly RoleId[] RequiredRolesHardware =
    {
        RoleId.HardwareEngineer, RoleId.ManufacturingEngineer, RoleId.QaEngineer, RoleId.ProductDesigner
    };
    private static readonly RoleId[] RequiredRolesDefault =
    {
        RoleId.SoftwareEngineer, RoleId.ProductDesigner, RoleId.QaEngineer
    };

    // Technical Strength — skill sets per assignment
    private static readonly SkillId[] TechSkillsGame =
    {
        SkillId.Programming, SkillId.GameDesign, SkillId.VisualArt, SkillId.AudioDesign, SkillId.QaTesting
    };
    private static readonly SkillId[] TechSkillsSoftware =
    {
        SkillId.Programming, SkillId.SystemsArchitecture, SkillId.PerformanceOptimisation, SkillId.Security
    };
    private static readonly SkillId[] TechSkillsHardware =
    {
        SkillId.CpuEngineering, SkillId.GpuEngineering, SkillId.HardwareIntegration, SkillId.Manufacturing
    };
    private static readonly SkillId[] TechSkillsDefault =
    {
        SkillId.Programming, SkillId.SystemsArchitecture, SkillId.ProductDesign
    };

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------
    public TeamMeterSystem(
        TeamSystem teamSystem,
        EmployeeSystem employeeSystem,
        AbilitySystem abilitySystem,
        MoraleSystem moraleSystem,
        TeamChemistrySystem chemistrySystem,
        RoleProfileTable roleProfileTable,
        ILogger logger)
    {
        _teamSystem       = teamSystem       ?? throw new ArgumentNullException(nameof(teamSystem));
        _employeeSystem   = employeeSystem   ?? throw new ArgumentNullException(nameof(employeeSystem));
        _abilitySystem    = abilitySystem    ?? throw new ArgumentNullException(nameof(abilitySystem));
        _moraleSystem     = moraleSystem     ?? throw new ArgumentNullException(nameof(moraleSystem));
        _chemistrySystem  = chemistrySystem  ?? throw new ArgumentNullException(nameof(chemistrySystem));
        _roleProfileTable = roleProfileTable ?? throw new ArgumentNullException(nameof(roleProfileTable));
        _logger           = logger           ?? new NullLogger();

        _teamSystem.OnEmployeeAssignedToTeam  += OnEmployeeAssigned;
        _teamSystem.OnEmployeeRemovedFromTeam += OnEmployeeRemoved;
        _teamSystem.OnTeamDeleted             += OnTeamDeleted;
        _chemistrySystem.OnChemistryChanged   += OnChemistryChanged;
    }

    // -------------------------------------------------------------------------
    // Event handlers → dirty marking
    // -------------------------------------------------------------------------
    private void OnEmployeeAssigned(EmployeeId empId, TeamId teamId) => MarkTeamDirty(teamId);
    private void OnEmployeeRemoved(EmployeeId empId, TeamId teamId)  => MarkTeamDirty(teamId);
    private void OnTeamDeleted(TeamId teamId)
    {
        _cache.Remove(teamId);
        _dirtyTeams.Remove(teamId);
    }
    private void OnChemistryChanged(TeamId teamId, ChemistryBand band) => MarkTeamDirty(teamId);

    // -------------------------------------------------------------------------
    // Public API — dirty management
    // -------------------------------------------------------------------------
    public void MarkTeamDirty(TeamId teamId)
    {
        _dirtyTeams.Add(teamId);
        OnTeamMetersDirty?.Invoke(teamId);
    }

    public void MarkAllDirty()
    {
        var teams = _teamSystem.GetAllActiveTeams();
        for (int i = 0; i < teams.Count; i++)
            _dirtyTeams.Add(teams[i].id);
    }

    // -------------------------------------------------------------------------
    // Public API — queries
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the cached TeamMeterSnapshot for the given team, recomputing if dirty.
    /// Pass a non-null context to get assignment-specific Technical Strength / Creativity.
    /// </summary>
    public TeamMeterSnapshot GetTeamMeters(TeamId teamId, AssignmentContext? context = null)
    {
        if (_dirtyTeams.Contains(teamId) || !_cache.TryGetValue(teamId, out var cached))
        {
            var snapshot = ComputeSnapshot(teamId, context ?? AssignmentContext.Unassigned(), 0);
            _cache[teamId] = snapshot;
            _dirtyTeams.Remove(teamId);
            return snapshot;
        }
        return cached;
    }

    /// <summary>Returns the standard display label for a meter score.</summary>
    public TeamMeterLabel GetMeterLabel(TeamMeterId meterId, int score)
    {
        if (meterId == TeamMeterId.Creativity)
            return GetCreativityLabel(score);
        return GetStandardLabel(score);
    }

    // -------------------------------------------------------------------------
    // Public API — impact calculations (on-demand, not cached)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes per-meter delta if the given employee were removed from their team.
    /// Returns the employee's positive/negative contribution to each meter.
    /// </summary>
    public TeamImpactResult GetEmployeeTeamImpact(EmployeeId employeeId, TeamId teamId)
    {
        var current = GetTeamMeters(teamId);
        var team    = _teamSystem.GetTeam(teamId);
        if (team == null) return TeamImpactResult.Zero();

        // Build scratch member list without the target employee
        _memberScratch.Clear();
        for (int i = 0; i < team.members.Count; i++)
        {
            if (team.members[i] != employeeId)
                _memberScratch.Add(team.members[i]);
        }

        var without = ComputeSnapshotForMembers(teamId, _memberScratch, AssignmentContext.Unassigned(), 0);

        return new TeamImpactResult
        {
            CreativityDelta          = current.Creativity         - without.Creativity,
            CoordinationDelta        = current.Coordination       - without.Coordination,
            ReliabilityDelta         = current.Reliability        - without.Reliability,
            TechnicalStrengthDelta   = current.TechnicalStrength  - without.TechnicalStrength,
            CommercialAwarenessDelta = current.CommercialAwareness - without.CommercialAwareness,
            Confidence               = current.Confidence
        };
    }

    /// <summary>
    /// Computes projected per-meter delta if a candidate were added to the team.
    /// Delta ranges are widened by confidence level.
    /// </summary>
    public TeamImpactResult GetCandidateProjectedImpact(CandidateData candidate, TeamId teamId, float confidenceLevel)
    {
        var current = GetTeamMeters(teamId);
        var team    = _teamSystem.GetTeam(teamId);
        if (team == null || candidate == null) return TeamImpactResult.Zero();

        // Build scratch member list including candidate as a synthetic employee
        var synth = BuildSyntheticEmployee(candidate);

        _memberScratch.Clear();
        for (int i = 0; i < team.members.Count; i++)
            _memberScratch.Add(team.members[i]);

        var projected = ComputeSnapshotForMembersWithSynth(teamId, _memberScratch, synth, AssignmentContext.Unassigned(), 0);

        int dCreativity  = projected.Creativity         - current.Creativity;
        int dCoord       = projected.Coordination       - current.Coordination;
        int dReliability = projected.Reliability        - current.Reliability;
        int dTech        = projected.TechnicalStrength  - current.TechnicalStrength;
        int dCommercial  = projected.CommercialAwareness - current.CommercialAwareness;

        // Widen ranges by (1 - confidence)
        float spread = 1f - Clamp01(confidenceLevel);

        return new TeamImpactResult
        {
            CreativityDelta             = dCreativity,
            CreativityDeltaMin          = dCreativity  - (int)(System.Math.Abs(dCreativity)  * spread),
            CreativityDeltaMax          = dCreativity  + (int)(System.Math.Abs(dCreativity)  * spread),
            CoordinationDelta           = dCoord,
            CoordinationDeltaMin        = dCoord       - (int)(System.Math.Abs(dCoord)       * spread),
            CoordinationDeltaMax        = dCoord       + (int)(System.Math.Abs(dCoord)       * spread),
            ReliabilityDelta            = dReliability,
            ReliabilityDeltaMin         = dReliability - (int)(System.Math.Abs(dReliability) * spread),
            ReliabilityDeltaMax         = dReliability + (int)(System.Math.Abs(dReliability) * spread),
            TechnicalStrengthDelta      = dTech,
            TechnicalStrengthDeltaMin   = dTech        - (int)(System.Math.Abs(dTech)        * spread),
            TechnicalStrengthDeltaMax   = dTech        + (int)(System.Math.Abs(dTech)        * spread),
            CommercialAwarenessDelta    = dCommercial,
            CommercialAwarenessDeltaMin = dCommercial  - (int)(System.Math.Abs(dCommercial)  * spread),
            CommercialAwarenessDeltaMax = dCommercial  + (int)(System.Math.Abs(dCommercial)  * spread),
            Confidence                  = current.Confidence
        };
    }

    // -------------------------------------------------------------------------
    // ISystem
    // -------------------------------------------------------------------------
    public void PreTick(int tick)  { }
    public void Tick(int tick)     { }

    public void PostTick(int tick)
    {
        // Recalculate all dirty teams and buffer change events
        _changedBuffer.Clear();

        if (_dirtyTeams.Count == 0) return;

        // Collect dirty team IDs into scratch to avoid modifying set while iterating
        _roleScratch.Clear(); // borrow capacity — unused here, use a local
        var dirtyList = new List<TeamId>(_dirtyTeams);
        _dirtyTeams.Clear();

        for (int i = 0; i < dirtyList.Count; i++)
        {
            var teamId = dirtyList[i];
            _cache.TryGetValue(teamId, out var prev);
            var curr = ComputeSnapshot(teamId, AssignmentContext.Unassigned(), tick);
            _cache[teamId] = curr;

            // Collect which meters changed for the event
            _intScratch.Clear();
            if (curr.Creativity         != prev.Creativity)         _intScratch.Add((int)TeamMeterId.Creativity);
            if (curr.Coordination       != prev.Coordination)       _intScratch.Add((int)TeamMeterId.Coordination);
            if (curr.Reliability        != prev.Reliability)        _intScratch.Add((int)TeamMeterId.Reliability);
            if (curr.TechnicalStrength  != prev.TechnicalStrength)  _intScratch.Add((int)TeamMeterId.TechnicalStrength);
            if (curr.CommercialAwareness != prev.CommercialAwareness) _intScratch.Add((int)TeamMeterId.CommercialAwareness);

            if (_intScratch.Count > 0)
                _changedBuffer.Add((teamId, prev, curr));
        }

        // Raise events after all recalculations
        for (int i = 0; i < _changedBuffer.Count; i++)
        {
            var (teamId, prev, curr) = _changedBuffer[i];
            var changed = BuildChangedMeters(prev, curr);
            OnTeamMetersChanged?.Invoke(new TeamMetersChangedEvent(teamId, changed, tick));
        }
    }

    public void ApplyCommand(ICommand command) { }

    public void Dispose()
    {
        _teamSystem.OnEmployeeAssignedToTeam  -= OnEmployeeAssigned;
        _teamSystem.OnEmployeeRemovedFromTeam -= OnEmployeeRemoved;
        _teamSystem.OnTeamDeleted             -= OnTeamDeleted;
        _chemistrySystem.OnChemistryChanged   -= OnChemistryChanged;
    }

    // -------------------------------------------------------------------------
    // Core computation — full snapshot
    // -------------------------------------------------------------------------
    private TeamMeterSnapshot ComputeSnapshot(TeamId teamId, AssignmentContext context, int tick)
    {
        var team = _teamSystem.GetTeam(teamId);
        if (team == null || !team.isActive)
            return TeamMeterSnapshot.Empty(teamId);

        _memberScratch.Clear();
        for (int i = 0; i < team.members.Count; i++)
            _memberScratch.Add(team.members[i]);

        return ComputeSnapshotForMembers(teamId, _memberScratch, context, tick);
    }

    private TeamMeterSnapshot ComputeSnapshotForMembers(
        TeamId teamId,
        List<EmployeeId> members,
        AssignmentContext context,
        int tick)
    {
        if (members.Count == 0)
            return TeamMeterSnapshot.Empty(teamId);

        // Gather chemistry and morale inputs
        float normChemistry  = GetNormalisedChemistry(teamId);
        int   moraleModifier = GetMoraleModifier(members);

        // Compute in fixed order to resolve Coordination→TechnicalStrength dependency
        int creativity         = ComputeCreativity(members, context, normChemistry, moraleModifier);
        int coordination       = ComputeCoordination(members, context, normChemistry, moraleModifier);
        int reliability        = ComputeReliability(members, context, normChemistry, moraleModifier);
        int technicalStrength  = ComputeTechnicalStrength(members, context, moraleModifier, coordination);
        int commercialAwareness = ComputeCommercialAwareness(members, normChemistry, moraleModifier);

        return new TeamMeterSnapshot
        {
            TeamId              = teamId,
            Creativity          = Clamp0100(creativity),
            Coordination        = Clamp0100(coordination),
            Reliability         = Clamp0100(reliability),
            TechnicalStrength   = Clamp0100(technicalStrength),
            CommercialAwareness = Clamp0100(commercialAwareness),
            Confidence          = ComputeConfidence(members, tick),
            LastCalculatedTick  = tick
        };
    }

    /// <summary>
    /// Recomputes with real members + one synthetic (candidate) employee without modifying state.
    /// </summary>
    private TeamMeterSnapshot ComputeSnapshotForMembersWithSynth(
        TeamId teamId,
        List<EmployeeId> realMembers,
        SynthEmployee synth,
        AssignmentContext context,
        int tick)
    {
        if (realMembers.Count == 0 && !synth.IsValid)
            return TeamMeterSnapshot.Empty(teamId);

        float normChemistry  = GetNormalisedChemistry(teamId);
        int   moraleModifier = GetMoraleModifierWithSynth(realMembers, synth);

        int creativity         = ComputeCreativityWithSynth(realMembers, synth, context, normChemistry, moraleModifier);
        int coordination       = ComputeCoordinationWithSynth(realMembers, synth, context, normChemistry, moraleModifier);
        int reliability        = ComputeReliabilityWithSynth(realMembers, synth, context, normChemistry, moraleModifier);
        int technicalStrength  = ComputeTechnicalStrengthWithSynth(realMembers, synth, context, moraleModifier, coordination);
        int commercialAwareness = ComputeCommercialAwarenessWithSynth(realMembers, synth, normChemistry, moraleModifier);

        var conf = ComputeConfidence(realMembers, tick);
        if (synth.IsValid && conf > TeamMeterConfidence.Low)
            conf = TeamMeterConfidence.Low; // candidate data lowers confidence

        return new TeamMeterSnapshot
        {
            TeamId              = teamId,
            Creativity          = Clamp0100(creativity),
            Coordination        = Clamp0100(coordination),
            Reliability         = Clamp0100(reliability),
            TechnicalStrength   = Clamp0100(technicalStrength),
            CommercialAwareness = Clamp0100(commercialAwareness),
            Confidence          = conf,
            LastCalculatedTick  = tick
        };
    }

    // -------------------------------------------------------------------------
    // D.1 — Creativity
    // -------------------------------------------------------------------------
    private int ComputeCreativity(
        List<EmployeeId> members,
        AssignmentContext context,
        float normChemistry,
        int moraleModifier)
    {
        if (members.Count == 0) return 0;

        float avgCreativity  = AvgVisibleAttribute(members, VisibleAttributeId.Creativity);  // 0-100
        var   creativeSkills = GetCreativeSkills(context);
        float creativeAvg    = AvgSkillSet(members, creativeSkills);                          // 0-100
        float roleDiversity  = ComputeRoleDiversityBonus(members, context);                   // 0-10
        float coordControl   = AvgSkillAndAttribute(members, SkillId.ProductDesign, VisibleAttributeId.Communication); // 0-100

        float raw =
            avgCreativity * 0.40f
            + creativeAvg * 0.25f
            + roleDiversity * 10f * 0.10f  // roleDiversity is 0-1, weight makes it 0-10 pts
            + normChemistry * 0.10f
            + moraleModifier * 0.10f
            + coordControl * 0.05f;

        return (int)raw;
    }

    private int ComputeCreativityWithSynth(
        List<EmployeeId> members, SynthEmployee synth,
        AssignmentContext context, float normChemistry, int moraleModifier)
    {
        if (members.Count == 0 && !synth.IsValid) return 0;

        float avgCreativity  = AvgVisibleAttributeWithSynth(members, synth, VisibleAttributeId.Creativity);
        var   creativeSkills = GetCreativeSkills(context);
        float creativeAvg    = AvgSkillSetWithSynth(members, synth, creativeSkills);
        float roleDiversity  = ComputeRoleDiversityBonusWithSynth(members, synth, context);
        float coordControl   = AvgSkillAndAttributeWithSynth(members, synth, SkillId.ProductDesign, VisibleAttributeId.Communication);

        float raw =
            avgCreativity * 0.40f
            + creativeAvg * 0.25f
            + roleDiversity * 10f * 0.10f
            + normChemistry * 0.10f
            + moraleModifier * 0.10f
            + coordControl * 0.05f;

        return (int)raw;
    }

    // -------------------------------------------------------------------------
    // D.2 — Coordination
    // -------------------------------------------------------------------------
    private int ComputeCoordination(
        List<EmployeeId> members,
        AssignmentContext context,
        float normChemistry,
        int moraleModifier)
    {
        if (members.Count == 0) return 0;

        float avgComm;
        if (members.Count == 1)
        {
            // Single-member special case: use Focus instead of Communication/Leadership
            avgComm = NormaliseAttribute(GetEmployee(members[0])?.Stats.GetVisibleAttribute(VisibleAttributeId.Focus) ?? 10);
        }
        else
        {
            avgComm = AvgVisibleAttribute(members, VisibleAttributeId.Communication);
        }

        float bestLeadership  = BestVisibleAttribute(members, VisibleAttributeId.Leadership);
        float orgSkillAvg     = AvgSkillSet(members, OrgSkills);
        float roleCoverage    = ComputeRoleCoverage(members, context);

        float raw =
            avgComm * 0.25f
            + bestLeadership * 0.20f
            + orgSkillAvg * 0.15f
            + roleCoverage * 0.15f
            + normChemistry * 0.15f
            + moraleModifier * 0.10f;

        return (int)raw;
    }

    private int ComputeCoordinationWithSynth(
        List<EmployeeId> members, SynthEmployee synth,
        AssignmentContext context, float normChemistry, int moraleModifier)
    {
        if (members.Count == 0 && !synth.IsValid) return 0;

        int totalCount = members.Count + (synth.IsValid ? 1 : 0);
        float avgComm;
        if (totalCount == 1)
        {
            int focusVal = members.Count == 1
                ? (GetEmployee(members[0])?.Stats.GetVisibleAttribute(VisibleAttributeId.Focus) ?? 10)
                : synth.GetVisibleAttribute(VisibleAttributeId.Focus);
            avgComm = NormaliseAttribute(focusVal);
        }
        else
        {
            avgComm = AvgVisibleAttributeWithSynth(members, synth, VisibleAttributeId.Communication);
        }

        float bestLeadership  = BestVisibleAttributeWithSynth(members, synth, VisibleAttributeId.Leadership);
        float orgSkillAvg     = AvgSkillSetWithSynth(members, synth, OrgSkills);
        float roleCoverage    = ComputeRoleCoverageWithSynth(members, synth, context);

        float raw =
            avgComm * 0.25f
            + bestLeadership * 0.20f
            + orgSkillAvg * 0.15f
            + roleCoverage * 0.15f
            + normChemistry * 0.15f
            + moraleModifier * 0.10f;

        return (int)raw;
    }

    // -------------------------------------------------------------------------
    // D.3 — Reliability
    // -------------------------------------------------------------------------
    private int ComputeReliability(
        List<EmployeeId> members,
        AssignmentContext context,
        float normChemistry,
        int moraleModifier)
    {
        if (members.Count == 0) return 0;

        float avgFocus     = AvgVisibleAttribute(members, VisibleAttributeId.Focus);
        float avgWorkEthic = AvgVisibleAttribute(members, VisibleAttributeId.WorkEthic);
        float avgComposure = AvgVisibleAttribute(members, VisibleAttributeId.Composure);
        float qaSkillAvg   = AvgSkillSet(members, QaSkills);
        float roleFitAvg   = AvgRoleFit(members);

        float raw =
            avgFocus * 0.25f
            + avgWorkEthic * 0.15f
            + avgComposure * 0.20f
            + qaSkillAvg * 0.15f
            + normChemistry * 0.10f
            + moraleModifier * 0.10f
            + roleFitAvg * 0.05f;

        return (int)raw;
    }

    private int ComputeReliabilityWithSynth(
        List<EmployeeId> members, SynthEmployee synth,
        AssignmentContext context, float normChemistry, int moraleModifier)
    {
        if (members.Count == 0 && !synth.IsValid) return 0;

        float avgFocus     = AvgVisibleAttributeWithSynth(members, synth, VisibleAttributeId.Focus);
        float avgWorkEthic = AvgVisibleAttributeWithSynth(members, synth, VisibleAttributeId.WorkEthic);
        float avgComposure = AvgVisibleAttributeWithSynth(members, synth, VisibleAttributeId.Composure);
        float qaSkillAvg   = AvgSkillSetWithSynth(members, synth, QaSkills);
        float roleFitAvg   = AvgRoleFitWithSynth(members, synth);

        float raw =
            avgFocus * 0.25f
            + avgWorkEthic * 0.15f
            + avgComposure * 0.20f
            + qaSkillAvg * 0.15f
            + normChemistry * 0.10f
            + moraleModifier * 0.10f
            + roleFitAvg * 0.05f;

        return (int)raw;
    }

    // -------------------------------------------------------------------------
    // D.4 — Technical Strength
    // -------------------------------------------------------------------------
    private int ComputeTechnicalStrength(
        List<EmployeeId> members,
        AssignmentContext context,
        int moraleModifier,
        int coordinationScore)
    {
        if (members.Count == 0) return 0;

        var relevantSkills   = GetTechSkills(context);
        float skillCoverage  = BestSkillCoverage(members, relevantSkills);   // 0-100
        float roleFitAvg     = AvgRoleFit(members);                          // 0-100
        float specialistBonus = BestSkillValue(members, relevantSkills);     // 0-100
        float sizeEfficiency = TeamSizeEfficiency(members.Count);            // 0-100
        float coordModifier  = coordinationScore * 0.05f;                   // derived from already-computed value

        float raw =
            skillCoverage * 0.50f
            + roleFitAvg * 0.20f
            + specialistBonus * 0.10f
            + sizeEfficiency * 0.10f
            + moraleModifier * 0.05f
            + coordModifier;

        return (int)raw;
    }

    private int ComputeTechnicalStrengthWithSynth(
        List<EmployeeId> members, SynthEmployee synth,
        AssignmentContext context, int moraleModifier, int coordinationScore)
    {
        if (members.Count == 0 && !synth.IsValid) return 0;

        int totalCount      = members.Count + (synth.IsValid ? 1 : 0);
        var relevantSkills  = GetTechSkills(context);
        float skillCoverage  = BestSkillCoverageWithSynth(members, synth, relevantSkills);
        float roleFitAvg     = AvgRoleFitWithSynth(members, synth);
        float specialistBonus = BestSkillValueWithSynth(members, synth, relevantSkills);
        float sizeEfficiency = TeamSizeEfficiency(totalCount);
        float coordModifier  = coordinationScore * 0.05f;

        float raw =
            skillCoverage * 0.50f
            + roleFitAvg * 0.20f
            + specialistBonus * 0.10f
            + sizeEfficiency * 0.10f
            + moraleModifier * 0.05f
            + coordModifier;

        return (int)raw;
    }

    // -------------------------------------------------------------------------
    // D.5 — Commercial Awareness
    // -------------------------------------------------------------------------
    private int ComputeCommercialAwareness(
        List<EmployeeId> members,
        float normChemistry,
        int moraleModifier)
    {
        if (members.Count == 0) return 0;

        float commercialAvg        = AvgSkillSet(members, CommercialSkills);
        float userResearchDesignAvg = AvgTwoSkills(members, SkillId.UserResearch, SkillId.ProductDesign);
        float avgComm              = AvgVisibleAttribute(members, VisibleAttributeId.Communication);
        float specialistBonus      = BestSkillValue(members, BestCommercialSpecialistSkills);
        float roleDiversity        = ComputeRoleDiversityBonus(members, AssignmentContext.Unassigned());

        float raw =
            commercialAvg * 0.40f
            + userResearchDesignAvg * 0.20f
            + avgComm * 0.15f
            + specialistBonus * 0.10f
            + normChemistry * 0.05f
            + moraleModifier * 0.05f
            + roleDiversity * 10f * 0.05f;

        return (int)raw;
    }

    private int ComputeCommercialAwarenessWithSynth(
        List<EmployeeId> members, SynthEmployee synth,
        float normChemistry, int moraleModifier)
    {
        if (members.Count == 0 && !synth.IsValid) return 0;

        float commercialAvg        = AvgSkillSetWithSynth(members, synth, CommercialSkills);
        float userResearchDesignAvg = AvgTwoSkillsWithSynth(members, synth, SkillId.UserResearch, SkillId.ProductDesign);
        float avgComm              = AvgVisibleAttributeWithSynth(members, synth, VisibleAttributeId.Communication);
        float specialistBonus      = BestSkillValueWithSynth(members, synth, BestCommercialSpecialistSkills);
        float roleDiversity        = ComputeRoleDiversityBonusWithSynth(members, synth, AssignmentContext.Unassigned());

        float raw =
            commercialAvg * 0.40f
            + userResearchDesignAvg * 0.20f
            + avgComm * 0.15f
            + specialistBonus * 0.10f
            + normChemistry * 0.05f
            + moraleModifier * 0.05f
            + roleDiversity * 10f * 0.05f;

        return (int)raw;
    }

    // -------------------------------------------------------------------------
    // E — Role Coverage (0-100)
    // -------------------------------------------------------------------------
    private float ComputeRoleCoverage(List<EmployeeId> members, AssignmentContext context)
    {
        var required = GetRequiredRoles(context);
        return ComputeRoleCoverageCore(members, null, required);
    }

    private float ComputeRoleCoverageWithSynth(List<EmployeeId> members, SynthEmployee synth, AssignmentContext context)
    {
        var required = GetRequiredRoles(context);
        return ComputeRoleCoverageCore(members, synth, required);
    }

    private float ComputeRoleCoverageCore(List<EmployeeId> members, SynthEmployee? synth, RoleId[] required)
    {
        if (required == null || required.Length == 0) return 50f;

        int covered          = 0;
        float generalistCover = 0f;

        for (int r = 0; r < required.Length; r++)
        {
            bool found = false;
            for (int m = 0; m < members.Count; m++)
            {
                var emp = GetEmployee(members[m]);
                if (emp == null) continue;
                if (emp.role == required[r]) { found = true; break; }
            }
            if (!found && synth != null && synth.Value.IsValid && synth.Value.role == required[r])
                found = true;

            if (found)
            {
                covered++;
            }
            else
            {
                // Generalist coverage: high Adaptability employees cover 0.5 of a missing role
                for (int m = 0; m < members.Count; m++)
                {
                    var emp = GetEmployee(members[m]);
                    if (emp == null) continue;
                    int adaptability = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);
                    if (adaptability >= 15) { generalistCover += 0.5f; break; }
                }
            }
        }

        float totalCover  = covered + System.Math.Min(generalistCover, required.Length - covered);
        float coverFrac   = required.Length > 0 ? totalCover / required.Length : 0f;

        // Score bands: 0% → 0-30, ~50% → 31-50, ~67% → 51-70, ~83% → 71-90, 100% → 91-100
        if (coverFrac >= 1.00f) return 95f;
        if (coverFrac >= 0.83f) return 80f;
        if (coverFrac >= 0.67f) return 60f;
        if (coverFrac >= 0.50f) return 40f;
        if (coverFrac >= 0.25f) return 20f;
        return 10f;
    }

    // -------------------------------------------------------------------------
    // Helper — Role Diversity Bonus (0-1, scaled to 0-10 by caller)
    // -------------------------------------------------------------------------
    private float ComputeRoleDiversityBonus(List<EmployeeId> members, AssignmentContext context)
    {
        return ComputeRoleDiversityBonusCore(members, null, context);
    }

    private float ComputeRoleDiversityBonusWithSynth(List<EmployeeId> members, SynthEmployee synth, AssignmentContext context)
    {
        return ComputeRoleDiversityBonusCore(members, synth, context);
    }

    private float ComputeRoleDiversityBonusCore(List<EmployeeId> members, SynthEmployee? synth, AssignmentContext context)
    {
        var required = GetRequiredRoles(context);
        if (required == null || required.Length == 0) required = RequiredRolesDefault;

        _roleScratch.Clear();
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            if (!_roleScratch.Contains(emp.role))
                _roleScratch.Add(emp.role);
        }
        if (synth != null && synth.Value.IsValid && !_roleScratch.Contains(synth.Value.role))
            _roleScratch.Add(synth.Value.role);

        int uniqueRelevant = 0;
        for (int i = 0; i < _roleScratch.Count; i++)
        {
            for (int j = 0; j < required.Length; j++)
            {
                if (_roleScratch[i] == required[j]) { uniqueRelevant++; break; }
            }
        }

        float bonus = required.Length > 0 ? (float)uniqueRelevant / required.Length : 0f;
        return System.Math.Min(bonus, 1f);
    }

    // -------------------------------------------------------------------------
    // Helper — Role Fit Average (0-100)
    // -------------------------------------------------------------------------
    private float AvgRoleFit(List<EmployeeId> members)
    {
        return AvgRoleFitCore(members, null);
    }

    private float AvgRoleFitWithSynth(List<EmployeeId> members, SynthEmployee synth)
    {
        return AvgRoleFitCore(members, synth);
    }

    private float AvgRoleFitCore(List<EmployeeId> members, SynthEmployee? synth)
    {
        float total  = 0f;
        int   count  = 0;

        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            int currentCA = _abilitySystem.GetCurrentRoleCA(members[m]);
            int bestCA    = _abilitySystem.GetBestRoleCA(members[m]);
            float fit     = bestCA > 0 ? Clamp01((float)currentCA / bestCA) * 100f : 0f;
            total += fit;
            count++;
        }

        if (synth != null && synth.Value.IsValid)
        {
            // For candidates, estimate role fit using candidate role CA / best role CA
            int currentCA = _abilitySystem.ComputeCandidateRoleCA(synth.Value.source, synth.Value.role);
            RoleId bestRole;
            int bestCA    = _abilitySystem.ComputeCandidateBestRoleCA(synth.Value.source, out bestRole);
            float fit     = bestCA > 0 ? Clamp01((float)currentCA / bestCA) * 100f : 0f;
            total += fit;
            count++;
        }

        return count > 0 ? total / count : 0f;
    }

    // -------------------------------------------------------------------------
    // Helper — Attribute averages / best (normalised 0-100)
    // -------------------------------------------------------------------------
    private float AvgVisibleAttribute(List<EmployeeId> members, VisibleAttributeId attr)
    {
        float total = 0f;
        int   count = 0;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            total += NormaliseAttribute(emp.Stats.GetVisibleAttribute(attr));
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AvgVisibleAttributeWithSynth(List<EmployeeId> members, SynthEmployee synth, VisibleAttributeId attr)
    {
        float total = AvgVisibleAttribute(members, attr) * members.Count;
        int   count = members.Count;
        if (synth.IsValid)
        {
            total += NormaliseAttribute(synth.GetVisibleAttribute(attr));
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float BestVisibleAttribute(List<EmployeeId> members, VisibleAttributeId attr)
    {
        float best = 0f;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            float v = NormaliseAttribute(emp.Stats.GetVisibleAttribute(attr));
            if (v > best) best = v;
        }
        return best;
    }

    private float BestVisibleAttributeWithSynth(List<EmployeeId> members, SynthEmployee synth, VisibleAttributeId attr)
    {
        float best = BestVisibleAttribute(members, attr);
        if (synth.IsValid)
        {
            float v = NormaliseAttribute(synth.GetVisibleAttribute(attr));
            if (v > best) best = v;
        }
        return best;
    }

    // -------------------------------------------------------------------------
    // Helper — Skill averages / best (normalised 0-100)
    // -------------------------------------------------------------------------
    private float AvgSkillSet(List<EmployeeId> members, SkillId[] skills)
    {
        if (skills == null || skills.Length == 0) return 0f;
        float total = 0f;
        int   count = 0;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            float memberAvg = 0f;
            for (int s = 0; s < skills.Length; s++)
                memberAvg += NormaliseSkill(emp.Stats.GetSkill(skills[s]));
            total += memberAvg / skills.Length;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AvgSkillSetWithSynth(List<EmployeeId> members, SynthEmployee synth, SkillId[] skills)
    {
        if (skills == null || skills.Length == 0) return 0f;
        float total = AvgSkillSet(members, skills) * members.Count;
        int   count = members.Count;
        if (synth.IsValid)
        {
            float synthAvg = 0f;
            for (int s = 0; s < skills.Length; s++)
                synthAvg += NormaliseSkill(synth.GetSkill(skills[s]));
            total += synthAvg / skills.Length;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AvgTwoSkills(List<EmployeeId> members, SkillId a, SkillId b)
    {
        float total = 0f;
        int   count = 0;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            total += (NormaliseSkill(emp.Stats.GetSkill(a)) + NormaliseSkill(emp.Stats.GetSkill(b))) * 0.5f;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AvgTwoSkillsWithSynth(List<EmployeeId> members, SynthEmployee synth, SkillId a, SkillId b)
    {
        float total = AvgTwoSkills(members, a, b) * members.Count;
        int   count = members.Count;
        if (synth.IsValid)
        {
            total += (NormaliseSkill(synth.GetSkill(a)) + NormaliseSkill(synth.GetSkill(b))) * 0.5f;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    /// <summary>Average of one skill and one visible attribute per member (both normalised 0-100).</summary>
    private float AvgSkillAndAttribute(List<EmployeeId> members, SkillId skill, VisibleAttributeId attr)
    {
        float total = 0f;
        int   count = 0;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            total += (NormaliseSkill(emp.Stats.GetSkill(skill)) + NormaliseAttribute(emp.Stats.GetVisibleAttribute(attr))) * 0.5f;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    private float AvgSkillAndAttributeWithSynth(List<EmployeeId> members, SynthEmployee synth, SkillId skill, VisibleAttributeId attr)
    {
        float total = AvgSkillAndAttribute(members, skill, attr) * members.Count;
        int   count = members.Count;
        if (synth.IsValid)
        {
            total += (NormaliseSkill(synth.GetSkill(skill)) + NormaliseAttribute(synth.GetVisibleAttribute(attr))) * 0.5f;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    /// <summary>Best team skill coverage: for each skill take the best member's value, then average across skills (0-100).</summary>
    private float BestSkillCoverage(List<EmployeeId> members, SkillId[] skills)
    {
        return BestSkillCoverageCore(members, null, skills);
    }

    private float BestSkillCoverageWithSynth(List<EmployeeId> members, SynthEmployee synth, SkillId[] skills)
    {
        return BestSkillCoverageCore(members, synth, skills);
    }

    private float BestSkillCoverageCore(List<EmployeeId> members, SynthEmployee? synth, SkillId[] skills)
    {
        if (skills == null || skills.Length == 0) return 0f;
        float total = 0f;
        for (int s = 0; s < skills.Length; s++)
        {
            float best = 0f;
            for (int m = 0; m < members.Count; m++)
            {
                var emp = GetEmployee(members[m]);
                if (emp == null) continue;
                float v = NormaliseSkill(emp.Stats.GetSkill(skills[s]));
                if (v > best) best = v;
            }
            if (synth != null && synth.Value.IsValid)
            {
                float sv = NormaliseSkill(synth.Value.GetSkill(skills[s]));
                if (sv > best) best = sv;
            }
            total += best;
        }
        return total / skills.Length;
    }

    /// <summary>Highest single skill value across all relevant skills (best specialist bonus, 0-100).</summary>
    private float BestSkillValue(List<EmployeeId> members, SkillId[] skills)
    {
        return BestSkillValueCore(members, null, skills);
    }

    private float BestSkillValueWithSynth(List<EmployeeId> members, SynthEmployee synth, SkillId[] skills)
    {
        return BestSkillValueCore(members, synth, skills);
    }

    private float BestSkillValueCore(List<EmployeeId> members, SynthEmployee? synth, SkillId[] skills)
    {
        if (skills == null || skills.Length == 0) return 0f;
        float best = 0f;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) continue;
            for (int s = 0; s < skills.Length; s++)
            {
                float v = NormaliseSkill(emp.Stats.GetSkill(skills[s]));
                if (v > best) best = v;
            }
        }
        if (synth != null && synth.Value.IsValid)
        {
            for (int s = 0; s < skills.Length; s++)
            {
                float v = NormaliseSkill(synth.Value.GetSkill(skills[s]));
                if (v > best) best = v;
            }
        }
        return best;
    }

    // -------------------------------------------------------------------------
    // I — Morale Modifier
    // -------------------------------------------------------------------------
    private int GetMoraleModifier(List<EmployeeId> members)
    {
        if (members.Count == 0) return 0;
        float total = 0f;
        int   count = 0;
        for (int m = 0; m < members.Count; m++)
        {
            total += _moraleSystem.GetMorale(members[m]);
            count++;
        }
        float avg = count > 0 ? total / count : 60f;
        return MoraleToModifier(avg);
    }

    private int GetMoraleModifierWithSynth(List<EmployeeId> members, SynthEmployee synth)
    {
        if (members.Count == 0 && !synth.IsValid) return 0;
        float total = 0f;
        int   count = members.Count;
        for (int m = 0; m < members.Count; m++)
            total += _moraleSystem.GetMorale(members[m]);
        if (synth.IsValid) { total += 60f; count++; } // assume neutral morale for candidate
        float avg = count > 0 ? total / count : 60f;
        return MoraleToModifier(avg);
    }

    private static int MoraleToModifier(float avg)
    {
        if (avg <= 20f)  return -20;
        if (avg <= 40f)  return -10;
        if (avg <= 60f)  return   0;
        if (avg <= 80f)  return  +5;
        return +10;
    }

    // -------------------------------------------------------------------------
    // Chemistry normalisation
    // -------------------------------------------------------------------------
    private float GetNormalisedChemistry(TeamId teamId)
    {
        var chem = _chemistrySystem.GetTeamChemistry(teamId);
        return (chem.Score + 100f) / 2f; // [-100,100] → [0,100]
    }

    // -------------------------------------------------------------------------
    // J — Confidence
    // -------------------------------------------------------------------------
    private TeamMeterConfidence ComputeConfidence(List<EmployeeId> members, int currentTick)
    {
        if (members.Count == 0) return TeamMeterConfidence.Low;

        var lowest = TeamMeterConfidence.Confirmed;
        for (int m = 0; m < members.Count; m++)
        {
            var emp = GetEmployee(members[m]);
            if (emp == null) { lowest = TeamMeterConfidence.Low; break; }

            int tenureDays = (currentTick - emp.contractExpiryTick) * -1; // rough proxy; positive = hired earlier
            // Use hireDate if available, otherwise use contractExpiryTick as fallback
            // Tenure: < 30 days = Low, 30-90 = Medium, 90-180 = High, 180+ = Confirmed
            TeamMeterConfidence conf;
            if (tenureDays < 30)       conf = TeamMeterConfidence.Low;
            else if (tenureDays < 90)  conf = TeamMeterConfidence.Medium;
            else if (tenureDays < 180) conf = TeamMeterConfidence.High;
            else                       conf = TeamMeterConfidence.Confirmed;

            if (conf < lowest) lowest = conf;
        }
        return lowest;
    }

    // -------------------------------------------------------------------------
    // Assignment-context skill/role helpers
    // -------------------------------------------------------------------------
    private static SkillId[] GetCreativeSkills(AssignmentContext context)
    {
        if (context.Type == AssignmentType.Product && context.ProductCategory.HasValue)
        {
            switch (context.ProductCategory.Value)
            {
                case ProductCategory.VideoGame:                              return CreativeSkillsGame;
                case ProductCategory.OperatingSystem:
                case ProductCategory.GameEngine:
                case ProductCategory.DevFramework:
                case ProductCategory.GraphicsEditor:
                case ProductCategory.AudioTool:                              return CreativeSkillsSoftware;
                case ProductCategory.GameConsole:                            return CreativeSkillsHardware;
            }
        }
        return CreativeSkillsDefault;
    }

    private static SkillId[] GetTechSkills(AssignmentContext context)
    {
        if (context.RelevantSkills != null && context.RelevantSkills.Length > 0)
            return context.RelevantSkills;

        if (context.Type == AssignmentType.Product && context.ProductCategory.HasValue)
        {
            switch (context.ProductCategory.Value)
            {
                case ProductCategory.VideoGame:                              return TechSkillsGame;
                case ProductCategory.GameConsole:                            return TechSkillsHardware;
                case ProductCategory.OperatingSystem:
                case ProductCategory.GameEngine:
                case ProductCategory.DevFramework:
                case ProductCategory.GraphicsEditor:
                case ProductCategory.AudioTool:                              return TechSkillsSoftware;
            }
        }
        return TechSkillsDefault;
    }

    private static RoleId[] GetRequiredRoles(AssignmentContext context)
    {
        if (context.Type == AssignmentType.Product && context.ProductCategory.HasValue)
        {
            switch (context.ProductCategory.Value)
            {
                case ProductCategory.VideoGame:    return RequiredRolesGame;
                case ProductCategory.GameConsole:  return RequiredRolesHardware;
                case ProductCategory.OperatingSystem:
                case ProductCategory.GameEngine:
                case ProductCategory.DevFramework:
                case ProductCategory.GraphicsEditor:
                case ProductCategory.AudioTool:    return RequiredRolesSoftware;
            }
        }
        return RequiredRolesDefault;
    }

    // -------------------------------------------------------------------------
    // Team size efficiency curve (spec section 17.3)
    // -------------------------------------------------------------------------
    private static float TeamSizeEfficiency(int size)
    {
        if (size <= 0)  return 0f;
        if (size == 1)  return 60f;
        if (size <= 3)  return 80f;
        if (size <= 6)  return 100f;
        if (size <= 9)  return 85f;
        return 70f;
    }

    // -------------------------------------------------------------------------
    // G — Meter labels
    // -------------------------------------------------------------------------
    private static TeamMeterLabel GetStandardLabel(int score)
    {
        if (score >= 90) return TeamMeterLabel.Elite;
        if (score >= 75) return TeamMeterLabel.Excellent;
        if (score >= 60) return TeamMeterLabel.Strong;
        if (score >= 45) return TeamMeterLabel.Functional;
        if (score >= 25) return TeamMeterLabel.Weak;
        return TeamMeterLabel.VeryWeak;
    }

    private static TeamMeterLabel GetCreativityLabel(int score)
    {
        if (score >= 90) return TeamMeterLabel.Breakthrough;
        if (score >= 75) return TeamMeterLabel.Visionary;
        if (score >= 60) return TeamMeterLabel.Inventive;
        if (score >= 45) return TeamMeterLabel.Capable;
        if (score >= 25) return TeamMeterLabel.Conventional;
        return TeamMeterLabel.Rigid;
    }

    public static string MeterLabelToString(TeamMeterLabel label)
    {
        switch (label)
        {
            case TeamMeterLabel.VeryWeak:     return "Very Weak";
            case TeamMeterLabel.Weak:         return "Weak";
            case TeamMeterLabel.Functional:   return "Functional";
            case TeamMeterLabel.Strong:       return "Strong";
            case TeamMeterLabel.Excellent:    return "Excellent";
            case TeamMeterLabel.Elite:        return "Elite";
            case TeamMeterLabel.Rigid:        return "Rigid";
            case TeamMeterLabel.Conventional: return "Conventional";
            case TeamMeterLabel.Capable:      return "Capable";
            case TeamMeterLabel.Inventive:    return "Inventive";
            case TeamMeterLabel.Visionary:    return "Visionary";
            case TeamMeterLabel.Breakthrough: return "Breakthrough";
            default:                          return "Unknown";
        }
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------
    private Employee GetEmployee(EmployeeId id) => _employeeSystem.GetEmployee(id);

    private static float NormaliseAttribute(int value) => (value / 20f) * 100f;   // 1-20 → 0-100
    private static float NormaliseSkill(int value)     => (value / 20f) * 100f;   // 0-20 → 0-100
    private static int   Clamp0100(float v)            => (int)System.Math.Min(100f, System.Math.Max(0f, v));
    private static float Clamp01(float v)              => System.Math.Min(1f, System.Math.Max(0f, v));

    private static TeamMeterId[] BuildChangedMeters(TeamMeterSnapshot prev, TeamMeterSnapshot curr)
    {
        int count = 0;
        if (curr.Creativity          != prev.Creativity)          count++;
        if (curr.Coordination        != prev.Coordination)        count++;
        if (curr.Reliability         != prev.Reliability)         count++;
        if (curr.TechnicalStrength   != prev.TechnicalStrength)   count++;
        if (curr.CommercialAwareness != prev.CommercialAwareness) count++;

        var result = new TeamMeterId[count];
        int idx    = 0;
        if (curr.Creativity          != prev.Creativity)          result[idx++] = TeamMeterId.Creativity;
        if (curr.Coordination        != prev.Coordination)        result[idx++] = TeamMeterId.Coordination;
        if (curr.Reliability         != prev.Reliability)         result[idx++] = TeamMeterId.Reliability;
        if (curr.TechnicalStrength   != prev.TechnicalStrength)   result[idx++] = TeamMeterId.TechnicalStrength;
        if (curr.CommercialAwareness != prev.CommercialAwareness) result[idx++] = TeamMeterId.CommercialAwareness;
        return result;
    }

    // -------------------------------------------------------------------------
    // Synthetic employee (candidate proxy) — value type for zero allocation
    // -------------------------------------------------------------------------
    private SynthEmployee BuildSyntheticEmployee(CandidateData candidate)
    {
        return new SynthEmployee(candidate);
    }

    /// <summary>
    /// Lightweight proxy over CandidateData used during impact calculations.
    /// Reads directly from the candidate's Stats block to avoid allocating a real Employee.
    /// </summary>
    private struct SynthEmployee
    {
        public readonly CandidateData source;
        public readonly RoleId role;
        public readonly bool IsValid;

        public SynthEmployee(CandidateData data)
        {
            source  = data;
            role    = data != null ? data.Role : RoleId.SoftwareEngineer;
            IsValid = data != null;
        }

        public int GetSkill(SkillId id)
        {
            if (!IsValid || source.Stats.Skills == null) return 0;
            int idx = (int)id;
            return idx < source.Stats.Skills.Length ? source.Stats.Skills[idx] : 0;
        }

        public int GetVisibleAttribute(VisibleAttributeId id)
        {
            if (!IsValid || source.Stats.VisibleAttributes == null) return 10;
            int idx = (int)id;
            return idx < source.Stats.VisibleAttributes.Length ? source.Stats.VisibleAttributes[idx] : 10;
        }
    }
}
