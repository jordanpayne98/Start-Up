using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class GameDebugCommands : MonoBehaviour, IDebugCommandHandler
{
    private GameController _gameController;
    private DebugConsole _console;
    private bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("__GameDebugCommands__");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<GameDebugCommands>();
    }

    private void Update()
    {
        if (_registered) return;

        if (_gameController == null)
        {
            _gameController = FindAnyObjectByType<GameController>();
            if (_gameController == null) return;
        }

        _console = FindAnyObjectByType<DebugConsole>();
        if (_console == null) return;

        _console.RegisterHandler(this);
        _registered = true;
    }

    public Dictionary<string, string> GetCommands()
    {
        return new Dictionary<string, string>
        {
            { "money [amt]", "Add money (negative to subtract). No args = print balance" },
            { "set-money [amt]", "Set money to exact value" },
            { "skip [days]", "Skip forward N days (default 1)" },
            { "skip-ticks [n]", "Skip forward N ticks" },
            { "pause", "Toggle advance on/off" },
            { "state", "Print full game state summary" },
            { "contracts", "List all available + active contracts" },
            { "generate-contracts [n]", "Refresh contract pool N times (default 1)" },
            { "accept-contract [id]", "Accept contract by ID (or first available)" },
            { "assign-contract [cId] [tId]", "Assign team to contract" },
            { "complete-contract [id]", "Force-complete contract by ID (or first in-progress)" },
            { "reroll-contracts", "Reroll contract pool" },
            { "create-team [type]", "Create a new team by type (Contracts, Programming, Design, HR, etc.)" },
            { "assign-employee [eId] [tId]", "Assign employee to team" },
            { "employees", "List all active employees" },
            { "teams", "List all active teams with members" },
            { "hire [count]", "Force-hire from candidate pool" },
            { "fire [eId]", "Fire employee by ID" },
            { "set-morale [eId] [val]", "Set employee morale" },
            { "reputation [delta]", "Show or adjust reputation" },
            { "loan [amount]", "Take a loan" },
            { "repay-loan", "Repay loan early" },
        };
    }

    public bool TryExecute(string command, string[] args, DebugConsole console)
    {
        switch (command)
        {
            case "money": return CmdMoney(args, console);
            case "set-money": return CmdSetMoney(args, console);
            case "skip": return CmdSkipDays(args, console);
            case "skip-ticks": return CmdSkipTicks(args, console);
            case "pause": return CmdPause(console);
            case "state": return CmdState(console);
            case "contracts": return CmdContracts(console);
            case "generate-contracts": return CmdGenerateContracts(args, console);
            case "accept-contract": return CmdAcceptContract(args, console);
            case "assign-contract": return CmdAssignContract(args, console);
            case "complete-contract": return CmdCompleteContract(args, console);
            case "reroll-contracts": return CmdRerollContracts(console);
            case "create-team": return CmdCreateTeam(args, console);
            case "assign-employee": return CmdAssignEmployee(args, console);
            case "employees": return CmdEmployees(console);
            case "teams": return CmdTeams(console);
            case "hire": return CmdHire(args, console);
            case "fire": return CmdFire(args, console);
            case "set-morale": return CmdSetMorale(args, console);
            case "reputation": return CmdReputation(args, console);
            case "loan": return CmdLoan(args, console);
            case "repay-loan": return CmdRepayLoan(console);
            default: return false;
        }
    }

    // --- Command Implementations ---

    private bool CmdMoney(string[] args, DebugConsole console)
    {
        if (args.Length == 0)
        {
            console.Print($"Current balance: ${_gameController.FinanceSystem.Money}");
            return true;
        }

        if (!int.TryParse(args[0], out int amount))
        {
            console.PrintError("Usage: money [amount]");
            return true;
        }

        _gameController.QueueCommand(new AddMoneyCommand
        {
            Tick = _gameController.CurrentTick + 1,
            Amount = amount
        });
        console.Print($"Queued AddMoney ${amount}");
        return true;
    }

    private bool CmdSetMoney(string[] args, DebugConsole console)
    {
        if (args.Length == 0)
        {
            console.PrintError("Usage: set-money [amount]");
            return true;
        }

        if (!int.TryParse(args[0], out int target))
        {
            console.PrintError("Usage: set-money [amount]");
            return true;
        }

        int current = _gameController.FinanceSystem.Money;
        int diff = target - current;
        _gameController.QueueCommand(new AddMoneyCommand
        {
            Tick = _gameController.CurrentTick + 1,
            Amount = diff
        });
        console.Print($"Set money to ${target} (delta: {diff:+#;-#;0})");
        return true;
    }

    private bool CmdSkipDays(string[] args, DebugConsole console)
    {
        int days = 1;
        if (args.Length > 0 && !int.TryParse(args[0], out days))
        {
            console.PrintError("Usage: skip [days]");
            return true;
        }

        if (_gameController.IsAdvancing)
        {
            console.PrintError("Cannot skip while advancing. Pause first.");
            return true;
        }

        int ticks = days * TimeState.TicksPerDay;
        _gameController.SkipTicks(ticks);
        console.Print($"Skipped {days} day(s) ({ticks} ticks)");
        return true;
    }

    private bool CmdSkipTicks(string[] args, DebugConsole console)
    {
        if (args.Length == 0)
        {
            console.PrintError("Usage: skip-ticks [count]");
            return true;
        }

        if (!int.TryParse(args[0], out int ticks))
        {
            console.PrintError("Usage: skip-ticks [count]");
            return true;
        }

        if (_gameController.IsAdvancing)
        {
            console.PrintError("Cannot skip while advancing. Pause first.");
            return true;
        }

        _gameController.SkipTicks(ticks);
        console.Print($"Skipped {ticks} ticks");
        return true;
    }

    private bool CmdPause(DebugConsole console)
    {
        _gameController.TogglePause();
        string status = _gameController.IsAdvancing ? "ADVANCING" : "PAUSED";
        console.Print($"Toggled: {status}");
        return true;
    }

    private bool CmdState(DebugConsole console)
    {
        var finance = _gameController.FinanceSystem;
        var time = _gameController.TimeSystem;

        console.Print("=== GAME STATE ===");
        console.Print($"Tick: {_gameController.CurrentTick}");
        console.Print($"Date: Day {time.DayOfMonth}, Month {time.CurrentMonth}, Year {time.CurrentYear}");
        console.Print($"Time: {time.CurrentHour:D2}:{time.CurrentMinute:D2}");
        console.Print($"Advancing: {_gameController.IsAdvancing}");
        console.Print($"Money: ${finance.Money}");
        console.Print($"Monthly Expenses: ${finance.MonthlyExpenses}");
        console.Print($"Total Revenue: ${finance.TotalRevenue}");
        console.Print($"Total Expenses: ${finance.TotalExpenses}");
        console.Print($"Days In Debt: {finance.ConsecutiveDaysNegativeCash}");
        console.Print($"Bankrupt: {finance.IsBankrupt}");
        console.Print($"Employees: {_gameController.EmployeeSystem.EmployeeCount}");

        var state = _gameController.GetGameState();
        if (state?.contractState != null)
        {
            console.Print($"Available Contracts: {state.contractState.availableContracts.Count}");
            console.Print($"Active Contracts: {state.contractState.activeContracts.Count}");
        }

        if (state?.teamState != null)
        {
            int teamCount = 0;
            foreach (var kvp in state.teamState.teams)
                if (kvp.Value.isActive) teamCount++;
            console.Print($"Active Teams: {teamCount}");
        }

        if (_gameController.ReputationSystem != null)
            console.Print($"Reputation: {_gameController.ReputationSystem.GlobalReputation} (Tier: {_gameController.ReputationSystem.CurrentTier})");

        return true;
    }

    private bool CmdContracts(DebugConsole console)
    {
        var cs = _gameController.ContractSystem;
        if (cs == null)
        {
            console.PrintError("ContractSystem not initialized");
            return true;
        }

        console.Print($"=== Available Contracts ({cs.AvailableContractCount}) ===");
        foreach (var c in cs.GetAvailableContracts())
        {
            console.Print($"  [{c.Id.Value}] {c.Name} | Cat: {c.CategoryId} | Diff: {c.Difficulty} | Reward: ${c.RewardMoney} | Skill: {c.RequiredSkill}");
        }

        console.Print($"=== Active Contracts ({cs.ActiveContractCount}) ===");
        foreach (var c in cs.GetActiveContracts())
        {
            string teamInfo = c.AssignedTeamId.HasValue ? $"Team {c.AssignedTeamId.Value.Value}" : "Unassigned";
            console.Print($"  [{c.Id.Value}] {c.Name} | Status: {c.Status} | Progress: {c.ProgressPercent * 100f:F1}% | Quality: {c.QualityScore:F1}% | {teamInfo}");
        }

        return true;
    }

    private bool CmdGenerateContracts(string[] args, DebugConsole console)
    {
        int count = 1;
        if (args.Length > 0 && !int.TryParse(args[0], out count))
        {
            console.PrintError("Usage: generate-contracts [count]");
            return true;
        }

        var cs = _gameController.ContractSystem;
        if (cs == null)
        {
            console.PrintError("ContractSystem not initialized");
            return true;
        }

        for (int i = 0; i < count; i++)
            cs.RefreshContractPool(_gameController.CurrentTick);

        console.Print($"Refreshed contract pool {count} time(s)");
        return true;
    }

    private bool CmdAcceptContract(string[] args, DebugConsole console)
    {
        var cs = _gameController.ContractSystem;
        if (cs == null)
        {
            console.PrintError("ContractSystem not initialized");
            return true;
        }

        ContractId? targetId = null;
        if (args.Length > 0 && int.TryParse(args[0], out int id))
        {
            targetId = new ContractId(id);
        }
        else
        {
            foreach (var c in cs.GetAvailableContracts())
            {
                targetId = c.Id;
                break;
            }
        }

        if (!targetId.HasValue)
        {
            console.PrintError("No available contracts to accept");
            return true;
        }

        _gameController.QueueCommand(new AcceptContractCommand
        {
            Tick = _gameController.CurrentTick,
            ContractId = targetId.Value
        });
        console.Print($"Queued AcceptContract for ID:{targetId.Value.Value}");
        return true;
    }

    private bool CmdAssignContract(string[] args, DebugConsole console)
    {
        var cs = _gameController.ContractSystem;
        var ts = _gameController.TeamSystem;

        ContractId? contractId = null;
        TeamId? teamId = null;

        if (args.Length >= 2)
        {
            if (int.TryParse(args[0], out int cId))
                contractId = new ContractId(cId);
            if (int.TryParse(args[1], out int tId))
                teamId = new TeamId(tId);
        }

        // Auto-detect first unassigned contract
        if (!contractId.HasValue)
        {
            foreach (var c in cs.GetActiveContracts())
            {
                if ((c.Status == ContractStatus.Accepted || c.Status == ContractStatus.InProgress) && c.AssignedTeamId == null)
                {
                    contractId = c.Id;
                    break;
                }
            }
        }

        // Auto-detect first team
        if (!teamId.HasValue)
        {
            foreach (var t in ts.GetAllActiveTeams())
            {
                teamId = t.id;
                break;
            }
        }

        if (!contractId.HasValue || !teamId.HasValue)
        {
            console.PrintError("Need at least one unassigned contract and one team. Usage: assign-contract [contractId] [teamId]");
            return true;
        }

        _gameController.QueueCommand(new AssignTeamToContractCommand
        {
            Tick = _gameController.CurrentTick,
            ContractId = contractId.Value,
            TeamId = teamId.Value
        });
        console.Print($"Queued AssignTeamToContract: contract {contractId.Value.Value} -> team {teamId.Value.Value}");
        return true;
    }

    private bool CmdCompleteContract(string[] args, DebugConsole console)
    {
        ContractId? targetId = null;
        if (args.Length > 0 && int.TryParse(args[0], out int id))
        {
            targetId = new ContractId(id);
        }
        else
        {
            foreach (var c in _gameController.ContractSystem.GetActiveContracts())
            {
                if (c.Status == ContractStatus.InProgress)
                {
                    targetId = c.Id;
                    break;
                }
            }
        }

        if (!targetId.HasValue)
        {
            console.PrintError("No in-progress contract found");
            return true;
        }

        _gameController.QueueCommand(new CompleteContractCommand
        {
            Tick = _gameController.CurrentTick,
            ContractId = targetId.Value
        });
        console.Print($"Queued CompleteContract for ID:{targetId.Value.Value}");
        return true;
    }

    private bool CmdRerollContracts(DebugConsole console)
    {
        _gameController.QueueCommand(new RerollContractPoolCommand
        {
            Tick = _gameController.CurrentTick
        });
        console.Print("Queued RerollContractPool");
        return true;
    }

    private bool CmdCreateTeam(string[] args, DebugConsole console)
    {
        var teamType = TeamType.Development;
        if (args.Length > 0 && System.Enum.TryParse<TeamType>(args[0], true, out var parsed))
            teamType = parsed;
        _gameController.QueueCommand(new CreateTeamCommand
        {
            Tick = _gameController.CurrentTick,
            TeamType = teamType
        });
        console.Print($"Queued CreateTeam: type={teamType}");
        return true;
    }

    private bool CmdAssignEmployee(string[] args, DebugConsole console)
    {
        EmployeeId? empId = null;
        TeamId? teamId = null;

        if (args.Length >= 2)
        {
            if (int.TryParse(args[0], out int eId))
                empId = new EmployeeId(eId);
            if (int.TryParse(args[1], out int tId))
                teamId = new TeamId(tId);
        }

        // Auto-detect
        if (!empId.HasValue)
        {
            foreach (var e in _gameController.EmployeeSystem.GetAllActiveEmployees())
            {
                empId = e.id;
                break;
            }
        }

        if (!teamId.HasValue)
        {
            foreach (var t in _gameController.TeamSystem.GetAllActiveTeams())
            {
                teamId = t.id;
                break;
            }
        }

        if (!empId.HasValue || !teamId.HasValue)
        {
            console.PrintError("Need at least one employee and one team. Usage: assign-employee [empId] [teamId]");
            return true;
        }

        _gameController.QueueCommand(new AssignEmployeeToTeamCommand
        {
            Tick = _gameController.CurrentTick,
            EmployeeId = empId.Value,
            TeamId = teamId.Value
        });
        console.Print($"Queued AssignEmployee: emp {empId.Value.Value} -> team {teamId.Value.Value}");
        return true;
    }

    private bool CmdEmployees(DebugConsole console)
    {
        var employees = _gameController.EmployeeSystem.GetAllActiveEmployees();
        console.Print($"=== Active Employees ({_gameController.EmployeeSystem.EmployeeCount}) ===");

        foreach (var emp in employees)
        {
            var sb = new StringBuilder();
            sb.Append($"  [{emp.id.Value}] {emp.name} | {emp.role} | Salary: ${emp.salary} | Morale: {emp.morale}");
            sb.Append(" | Skills:");
            for (int i = 0; i < SkillTypeHelper.SkillTypeCount; i++)
            {
                int val = emp.skills != null && i < emp.skills.Length ? emp.skills[i] : 0;
                if (val > 0)
                    sb.Append($" {SkillTypeHelper.GetName((SkillType)i)}={val}");
            }
            console.Print(sb.ToString());
        }
        return true;
    }

    private bool CmdTeams(DebugConsole console)
    {
        var teams = _gameController.TeamSystem.GetAllActiveTeams();
        int count = 0;
        foreach (var _ in teams) count++;

        console.Print($"=== Active Teams ({count}) ===");
        foreach (var team in _gameController.TeamSystem.GetAllActiveTeams())
        {
            console.Print($"  [{team.id.Value}] {team.name} | Type: {team.teamType} | Members: {team.MemberCount}");
            for (int i = 0; i < team.members.Count; i++)
            {
                var emp = _gameController.EmployeeSystem.GetEmployee(team.members[i]);
                string empName = emp != null ? emp.name : "???";
                console.Print($"    - {empName} (ID:{team.members[i].Value})");
            }
        }
        return true;
    }

    private bool CmdHire(string[] args, DebugConsole console)
    {
        int count = 1;
        if (args.Length > 0 && !int.TryParse(args[0], out count))
        {
            console.PrintError("Usage: hire [count]");
            return true;
        }

        var state = _gameController.GetGameState();
        if (state?.employeeState?.availableCandidates == null || state.employeeState.availableCandidates.Count == 0)
        {
            console.PrintError("No candidates available in the pool");
            return true;
        }

        int hired = 0;
        for (int i = 0; i < count && i < state.employeeState.availableCandidates.Count; i++)
        {
            var candidate = state.employeeState.availableCandidates[i];
            _gameController.QueueCommand(new HireEmployeeCommand
            {
                Tick = _gameController.CurrentTick,
                CandidateId = candidate.CandidateId,
                Name = candidate.Name,
                Gender = candidate.Gender,
                Age = candidate.Age,
                Skills = candidate.Skills,
                HRSkill = candidate.HRSkill,
                Salary = candidate.Salary,
                Role = candidate.Role,
                PotentialAbility = candidate.PotentialAbility,
                Mode = HiringMode.Manual
            });
            hired++;
        }

        console.Print($"Queued {hired} hire command(s)");
        return true;
    }

    private bool CmdFire(string[] args, DebugConsole console)
    {
        if (args.Length == 0)
        {
            console.PrintError("Usage: fire [employeeId]");
            return true;
        }

        if (!int.TryParse(args[0], out int id))
        {
            console.PrintError("Usage: fire [employeeId]");
            return true;
        }

        _gameController.QueueCommand(new FireEmployeeCommand
        {
            Tick = _gameController.CurrentTick,
            EmployeeId = new EmployeeId(id)
        });
        console.Print($"Queued FireEmployee for ID:{id}");
        return true;
    }

    private bool CmdSetMorale(string[] args, DebugConsole console)
    {
        if (args.Length < 2)
        {
            console.PrintError("Usage: set-morale [employeeId] [value]");
            return true;
        }

        if (!int.TryParse(args[0], out int empId) || !int.TryParse(args[1], out int value))
        {
            console.PrintError("Usage: set-morale [employeeId] [value 0-100]");
            return true;
        }

        var emp = _gameController.EmployeeSystem.GetEmployee(new EmployeeId(empId));
        if (emp == null)
        {
            console.PrintError($"Employee ID:{empId} not found");
            return true;
        }

        emp.morale = value;
        console.Print($"Set morale for {emp.name} (ID:{empId}) to {value}");
        return true;
    }

    private bool CmdReputation(string[] args, DebugConsole console)
    {
        var rep = _gameController.ReputationSystem;
        if (rep == null)
        {
            console.PrintError("ReputationSystem not initialized");
            return true;
        }

        if (args.Length == 0)
        {
            console.Print($"Reputation: {rep.GlobalReputation} | Tier: {rep.CurrentTier}");
            return true;
        }

        if (!int.TryParse(args[0], out int delta))
        {
            console.PrintError("Usage: reputation [delta]");
            return true;
        }

        if (delta > 0)
            rep.AddReputation(delta);
        else if (delta < 0)
            rep.RemoveReputation(-delta);

        console.Print($"Reputation adjusted by {delta:+#;-#;0}. Now: {rep.GlobalReputation} (Tier: {rep.CurrentTier})");
        return true;
    }

    private bool CmdLoan(string[] args, DebugConsole console)
    {
        if (args.Length == 0)
        {
            console.PrintError("Usage: loan [amount]");
            return true;
        }

        if (!int.TryParse(args[0], out int amount))
        {
            console.PrintError("Usage: loan [amount]");
            return true;
        }

        _gameController.QueueCommand(new TakeLoanCommand(_gameController.CurrentTick, amount));
        console.Print($"Queued TakeLoan for ${amount}");
        return true;
    }

    private bool CmdRepayLoan(DebugConsole console)
    {
        var loanReadModel = _gameController.LoanReadModel;
        if (loanReadModel == null || !loanReadModel.HasActiveLoan)
        {
            console.PrintError("No active loan to repay");
            return true;
        }

        int remaining = loanReadModel.GetTotalRemainingDebt();
        _gameController.QueueCommand(new RepayLoanEarlyCommand(_gameController.CurrentTick, remaining));
        console.Print($"Queued RepayLoanEarly for ${remaining}");
        return true;
    }
}
