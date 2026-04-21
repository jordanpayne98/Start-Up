using UnityEngine;
using System;
using System.IO;
using System.Text;

public class SessionLogger : MonoBehaviour
{
    private GameController _gameController;
    private GameState _gameState;
    private StreamWriter _writer;
    private bool _bound;
    private int _sessionIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("__SessionLogger__");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<SessionLogger>();
    }

    private void OnEnable()
    {
        string logsDir = Path.Combine(Application.dataPath, "Logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);

        _sessionIndex = FindNextSessionIndex(logsDir);
        string filePath = Path.Combine(logsDir, $"session_{_sessionIndex:D3}.txt");

        _writer = new StreamWriter(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
            Encoding.UTF8) { AutoFlush = true };
        _writer.WriteLine("=== SESSION LOG ===");
        _writer.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine($"Unity: {Application.unityVersion}");
        _writer.WriteLine($"Session: {_sessionIndex}");
        _writer.WriteLine(new string('=', 60));
        _writer.WriteLine();

        Application.logMessageReceived += OnUnityLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnUnityLog;

        if (_writer != null)
        {
            _writer.WriteLine();
            _writer.WriteLine(new string('=', 60));
            _writer.WriteLine($"Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }
    }

    private void Update()
    {
        if (_bound) return;

        if (_gameController == null)
        {
            _gameController = FindAnyObjectByType<GameController>();
            if (_gameController == null) return;
        }

        if (_gameController.EventBus == null) return;

        _gameState = _gameController.GetGameState();
        if (_gameState == null) return;

        SubscribeToEvents();
        _bound = true;
        Write("SESSION", "Logger bound to GameController");
    }

    private void SubscribeToEvents()
    {
        var bus = _gameController.EventBus;

        // Employee events
        bus.Subscribe<EmployeeCountChangedEvent>(OnEmployeeCountChanged);
        bus.Subscribe<EmployeeRetiredEvent>(OnEmployeeRetired);
        bus.Subscribe<EmployeeDecayEvent>(OnEmployeeDecay);
        bus.Subscribe<SkillImprovedEvent>(OnSkillImproved);

        // Team events
        bus.Subscribe<TeamCreatedEvent>(OnTeamCreated);
        bus.Subscribe<EmployeeAssignedToTeamEvent>(OnEmployeeAssignedToTeam);
        bus.Subscribe<EmployeeRemovedFromTeamEvent>(OnEmployeeRemovedFromTeam);
        bus.Subscribe<TeamIdleMoraleAlertEvent>(OnTeamIdleMoraleAlert);

        // Contract events
        bus.Subscribe<ContractAcceptedEvent>(OnContractAccepted);
        bus.Subscribe<ContractAssignedEvent>(OnContractAssigned);
        bus.Subscribe<ContractCompletedEvent>(OnContractCompleted);
        bus.Subscribe<ContractFailedEvent>(OnContractFailed);
        bus.Subscribe<ContractExpiredEvent>(OnContractExpired);
        bus.Subscribe<PoolRerolledEvent>(OnPoolRerolled);

        // Finance events
        bus.Subscribe<FinancialHealthChangedEvent>(OnFinancialHealthChanged);
        bus.Subscribe<SalaryPaidEvent>(OnSalaryPaid);
        bus.Subscribe<LoanTakenEvent>(OnLoanTaken);
        bus.Subscribe<LoanEarlyRepaidEvent>(OnLoanEarlyRepaid);
        bus.Subscribe<LoanFullyRepaidEvent>(OnLoanFullyRepaid);

        // Reputation
        bus.Subscribe<ReputationChangedEvent>(OnReputationChanged);

        // Hiring pipeline
        bus.Subscribe<CandidatesGeneratedEvent>(OnCandidatesGenerated);
        bus.Subscribe<CandidateDeclinedEvent>(OnCandidateDeclined);
        bus.Subscribe<CandidateWithdrewEvent>(OnCandidateWithdrew);
        bus.Subscribe<CandidateHardRejectedEvent>(OnCandidateHardRejected);
        bus.Subscribe<CandidateFollowUpEvent>(OnCandidateFollowUp);
        bus.Subscribe<HRCandidatesReadyForReviewEvent>(OnHRCandidatesReady);

        // Interview
        bus.Subscribe<InterviewStartedEvent>(OnInterviewStarted);
        bus.Subscribe<InterviewFinalReportEvent>(OnInterviewFinalReport);

        // Time
        bus.Subscribe<DayChangedEvent>(OnDayChanged);

        // Auto-action
        bus.Subscribe<AutoActionTakenEvent>(OnAutoAction);
    }

    // --- Employee ---
    private void OnEmployeeCountChanged(EmployeeCountChangedEvent e)
    {
        string action = e.WasHired ? "HIRED" : "LEFT";
        string details = EmployeeDetails(e.EmployeeId);
        Write("EMPLOYEE", $"{action} | {details} | Total: {e.TotalEmployees}");
    }

    private void OnEmployeeRetired(EmployeeRetiredEvent e)
    {
        Write("EMPLOYEE", $"RETIRED | {EmployeeName(e.EmployeeId)}");
    }

    private void OnEmployeeDecay(EmployeeDecayEvent e)
    {
        Write("EMPLOYEE", $"SKILL DECAY | {EmployeeName(e.EmployeeId)} | CA lost: {e.CALost}");
    }

    private void OnSkillImproved(SkillImprovedEvent e)
    {
        Write("EMPLOYEE", $"SKILL UP | {EmployeeName(e.EmployeeId)} | {e.Skill} -> {e.NewSkillValue}");
    }

    // --- Team ---
    private void OnTeamCreated(TeamCreatedEvent e)
    {
        Write("TEAM", $"CREATED | {e.TeamName} (ID:{e.TeamId.Value})");
    }

    private void OnEmployeeAssignedToTeam(EmployeeAssignedToTeamEvent e)
    {
        Write("TEAM", $"MEMBER ADDED | {EmployeeName(e.EmployeeId)} -> {TeamName(e.TeamId)}");
    }

    private void OnEmployeeRemovedFromTeam(EmployeeRemovedFromTeamEvent e)
    {
        Write("TEAM", $"MEMBER REMOVED | {EmployeeName(e.EmployeeId)} <- {TeamName(e.TeamId)}");
    }

    private void OnTeamIdleMoraleAlert(TeamIdleMoraleAlertEvent e)
    {
        Write("TEAM", $"IDLE ALERT | {e.TeamName} (ID:{e.TeamId.Value})");
    }

    // --- Contract ---
    private void OnContractAccepted(ContractAcceptedEvent e)
    {
        Write("CONTRACT", $"ACCEPTED | {ContractDetails(e.ContractId)}");
    }

    private void OnContractAssigned(ContractAssignedEvent e)
    {
        Write("CONTRACT", $"ASSIGNED | {ContractName(e.ContractId)} -> {TeamName(e.TeamId)}");
    }

    private void OnContractCompleted(ContractCompletedEvent e)
    {
        Write("CONTRACT", $"COMPLETED | {ContractName(e.ContractId)} | Reward: ${e.Reward}");
    }

    private void OnContractFailed(ContractFailedEvent e)
    {
        Write("CONTRACT", $"FAILED | {e.ContractName} (ID:{e.ContractId.Value}) | Reason: {e.Reason}");
    }

    private void OnContractExpired(ContractExpiredEvent e)
    {
        Write("CONTRACT", $"EXPIRED | {ContractName(e.ContractId)}");
    }

    private void OnPoolRerolled(PoolRerolledEvent e)
    {
        Write("CONTRACT", "POOL REROLLED");
    }

    // --- Finance ---
    private void OnFinancialHealthChanged(FinancialHealthChangedEvent e)
    {
        Write("FINANCE", $"HEALTH CHANGED | {e.OldHealth} -> {e.NewHealth}");
    }

    private void OnSalaryPaid(SalaryPaidEvent e)
    {
        Write("FINANCE", $"SALARY PAID | ${e.TotalAmount}");
    }

    private void OnLoanTaken(LoanTakenEvent e)
    {
        Write("FINANCE", $"LOAN TAKEN | Principal: ${e.Principal} | Rate: {e.InterestRate:P1} | Duration: {e.DurationMonths}mo | Risk: {e.RiskBand}");
    }

    private void OnLoanEarlyRepaid(LoanEarlyRepaidEvent e)
    {
        Write("FINANCE", $"EARLY REPAID | Amount: ${e.AmountPaid} | Interest avoided: ${e.InterestAvoided}");
    }

    private void OnLoanFullyRepaid(LoanFullyRepaidEvent e)
    {
        Write("FINANCE", "FULLY REPAID");
    }

    // --- Reputation ---
    private void OnReputationChanged(ReputationChangedEvent e)
    {
        string tierChange = e.CurrentTier != e.PreviousTier ? $" | Tier: {e.PreviousTier} -> {e.CurrentTier}" : "";
        Write("REPUTATION", $"REP CHANGED | Delta: {e.Delta:+#;-#;0} | Now: {e.CurrentReputation}{tierChange}");
    }

    // --- Hiring ---
    private void OnCandidatesGenerated(CandidatesGeneratedEvent e)
    {
        Write("HIRING", $"GENERATED | Count: {e.Count}");
    }

    private void OnCandidateDeclined(CandidateDeclinedEvent e)
    {
        Write("HIRING", $"DECLINED | {e.CandidateName} (ID:{e.CandidateId}) | Reason: {e.ConditionText}");
    }

    private void OnCandidateWithdrew(CandidateWithdrewEvent e)
    {
        Write("HIRING", $"WITHDREW | {e.CandidateName} (ID:{e.CandidateId})");
    }

    private void OnCandidateHardRejected(CandidateHardRejectedEvent e)
    {
        Write("HIRING", $"HARD REJECTED | ID:{e.CandidateId}");
    }

    private void OnCandidateFollowUp(CandidateFollowUpEvent e)
    {
        Write("HIRING", $"FOLLOW UP | {e.CandidateName} (ID:{e.CandidateId}) | Withdrawal deadline: T{e.WithdrawalDeadlineTick}");
    }

    private void OnHRCandidatesReady(HRCandidatesReadyForReviewEvent e)
    {
        Write("HIRING", $"HR READY | Team: {e.TeamName} (ID:{e.TeamId.Value}) | Count: {e.CandidateCount} | Criteria: {e.CriteriaLabel}");
    }

    // --- Interview ---
    private void OnInterviewStarted(InterviewStartedEvent e)
    {
        Write("INTERVIEW", $"STARTED | Candidate ID:{e.CandidateId}");
    }

    private void OnInterviewFinalReport(InterviewFinalReportEvent e)
    {
        Write("INTERVIEW", $"FINAL REPORT | {e.CandidateName} (ID:{e.CandidateId})");
    }

    // --- Time ---
    private void OnDayChanged(DayChangedEvent e)
    {
        Write("TIME", $"--- Day {e.Day}, Month {e.Month}, Year {e.Year} ---");
    }

    // --- Auto ---
    private void OnAutoAction(AutoActionTakenEvent e)
    {
        Write("AUTO", $"ACTION | {e.ActionDescription}");
    }

    // --- Unity Console ---
    private void OnUnityLog(string message, string stackTrace, LogType type)
    {
        // Filter out known noisy UI Toolkit warnings
        if (type == LogType.Warning && message.Contains("Runtime cursors other than the default cursor"))
            return;

        int tick = _gameState != null ? _gameState.currentTick : -1;
        string category = type switch
        {
            LogType.Error => "ERROR",
            LogType.Exception => "EXCEPTION",
            LogType.Warning => "WARNING",
            _ => "LOG"
        };

        string line = $"[T:{tick}] [CONSOLE:{category}] {message}";
        if ((type == LogType.Error || type == LogType.Exception) && !string.IsNullOrEmpty(stackTrace))
        {
            line += $"\n  {stackTrace.Replace("\n", "\n  ")}";
        }

        _writer?.WriteLine(line);
    }

    // --- Helpers ---
    private void Write(string category, string message)
    {
        if (_writer == null) return;
        int tick = _gameState != null ? _gameState.currentTick : 0;

        int day = 0, month = 0, year = 0;
        if (_gameController != null && _gameController.TimeSystem != null)
        {
            day = _gameController.TimeSystem.DayOfMonth;
            month = _gameController.TimeSystem.CurrentMonth;
            year = _gameController.TimeSystem.CurrentYear;
        }

        _writer.WriteLine($"[T:{tick} | Day {day} Mo {month} Yr {year}] [{category}] {message}");
    }

    private string EmployeeName(EmployeeId id)
    {
        if (_gameState?.employeeState?.employees != null &&
            _gameState.employeeState.employees.TryGetValue(id, out var emp) && emp != null)
        {
            return $"{emp.name} (ID:{id.Value})";
        }
        return $"(ID:{id.Value})";
    }

    private string EmployeeDetails(EmployeeId id)
    {
        if (_gameState?.employeeState?.employees == null) return EmployeeName(id);
        if (!_gameState.employeeState.employees.TryGetValue(id, out var emp) || emp == null) return EmployeeName(id);

        var sb = new StringBuilder();
        sb.Append(emp.name);
        sb.Append($" (ID:{id.Value})");
        sb.Append($" | Role: {emp.role}");
        sb.Append($" | Age: {emp.age}");
        sb.Append($" | Salary: ${emp.salary}");
        sb.Append(" | Skills:");
        for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
        {
            int val = emp.skills != null && i < emp.skills.Length ? emp.skills[i] : 0;
            if (val > 0)
                sb.Append($" {SkillTypeHelper.GetName((SkillType)i)}={val}");
        }
        sb.Append($" | Morale: {emp.morale}");
        return sb.ToString();
    }

    private string TeamName(TeamId id)
    {
        if (_gameState?.teamState?.teams != null &&
            _gameState.teamState.teams.TryGetValue(id, out var team) && team != null)
        {
            return $"{team.name} (ID:{id.Value})";
        }
        return $"(ID:{id.Value})";
    }

    private string ContractName(ContractId id)
    {
        if (_gameState?.contractState != null)
        {
            if (_gameState.contractState.activeContracts.TryGetValue(id, out var c) && c != null)
                return $"{c.Name} (ID:{id.Value})";
            if (_gameState.contractState.availableContracts.TryGetValue(id, out c) && c != null)
                return $"{c.Name} (ID:{id.Value})";
        }
        return $"(ID:{id.Value})";
    }

    private string ContractDetails(ContractId id)
    {
        Contract contract = null;
        if (_gameState?.contractState != null)
        {
            if (!_gameState.contractState.activeContracts.TryGetValue(id, out contract))
                _gameState.contractState.availableContracts.TryGetValue(id, out contract);
        }

        if (contract == null) return ContractName(id);

        var sb = new StringBuilder();
        sb.Append($"{contract.Name} (ID:{id.Value})");
        sb.Append($" | Difficulty: {contract.Difficulty}");
        sb.Append($" | Category: {contract.CategoryId}");
        sb.Append($" | Reward: ${contract.RewardMoney}");
        sb.Append($" | Rep: {contract.ReputationReward}");
        sb.Append($" | Skill: {contract.RequiredSkill}");
        if (contract.AssignedTeamId.HasValue)
            sb.Append($" | Team: {TeamName(contract.AssignedTeamId.Value)}");
        return sb.ToString();
    }

    private static int FindNextSessionIndex(string logsDir)
    {
        int maxIndex = -1;
        string[] files = Directory.GetFiles(logsDir, "session_*.txt");
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(files[i]);
            if (fileName.Length > 8)
            {
                string numStr = fileName.Substring(8);
                if (int.TryParse(numStr, out int idx) && idx > maxIndex)
                    maxIndex = idx;
            }
        }
        return maxIndex + 1;
    }
}
