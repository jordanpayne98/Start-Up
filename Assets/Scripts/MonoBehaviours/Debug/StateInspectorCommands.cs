using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Debug console commands for live game state inspection and tuning parameter control.
/// Registered automatically at runtime — no scene setup required.
/// </summary>
public class StateInspectorCommands : MonoBehaviour, IDebugCommandHandler
{
    private GameController _gameController;
    private DebugConsole _console;
    private bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("__StateInspectorCommands__");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<StateInspectorCommands>();
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
            { "state.employees",       "Dump all active employees (id, name, role, Ability, Potential, morale, salary)" },
            { "state.contracts",       "Dump all contracts by lifecycle stage" },
            { "state.finance",         "Dump finance state (money, runway, health, loans)" },
            { "state.morale",          "Dump morale data per employee" },
            { "state.teams",           "Dump all teams with members and assigned contract" },
            { "state.reputation",      "Dump reputation scores and tier" },
            { "state.candidates",      "Dump current candidate pool" },
            { "state.upgrades",        "Dump unlocked upgrades and research queue" },
            { "tuning.list [filter]",  "List all tuning parameters (optional keyword filter)" },
            { "tuning.set NAME VALUE", "Set a tuning parameter by name" },
            { "tuning.summary",        "Show parameters that differ from default values" },
            { "tuning.reset",          "Reset all tuning parameters to their default values" },
        };
    }

    public bool TryExecute(string command, string[] args, DebugConsole console)
    {
        switch (command)
        {
            case "state.employees":   DumpEmployees(console);                          return true;
            case "state.contracts":   DumpContracts(console);                          return true;
            case "state.finance":     DumpFinance(console);                            return true;
            case "state.morale":      DumpMorale(console);                             return true;
            case "state.teams":       DumpTeams(console);                              return true;
            case "state.reputation":  DumpReputation(console);                         return true;
            case "state.candidates":  DumpCandidates(console);                         return true;
            case "tuning.list":       TuningList(args, console);                       return true;
            case "tuning.set":        TuningSet(args, console);                        return true;
            case "tuning.summary":    TuningSummary(console);                          return true;
            case "tuning.reset":      TuningReset(console);                            return true;
        }
        return false;
    }

    // ─── State Dump Commands ─────────────────────────────────────────────────────

    private void DumpEmployees(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.employeeState?.employees == null)
        {
            console.PrintError("[State] No employee state.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[State] Active Employees:");
        sb.AppendLine($"  {"ID",-4} {"Name",-18} {"Role",-14} {"Ability",7} {"Potential",9} {"Morale",7} {"Salary",8}");
        sb.AppendLine(new string('-', 65));

        int count = 0;
        foreach (var emp in state.employeeState.employees.Values)
        {
            if (!emp.isActive) continue;
            int ca = _gameController.AbilitySystem != null
                ? _gameController.AbilitySystem.GetCA(emp.id, emp.role)
                : 0;
            sb.AppendLine($"  {emp.id.Value,-4} {emp.name,-18} {emp.role,-14} {ca,7} {emp.Stats.PotentialAbility,9} {emp.morale,7} {emp.salary,8}");
            count++;
        }
        sb.AppendLine($"  Total: {count}");
        console.Print(sb.ToString());
    }

    private void DumpContracts(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.contractState == null)
        {
            console.PrintError("[State] No contract state.");
            return;
        }

        var cs = state.contractState;
        var sb = new StringBuilder();
        sb.AppendLine("[State] Contracts:");
        sb.AppendLine($"  Available: {cs.availableContracts?.Count ?? 0}");

        if (cs.availableContracts != null)
        {
            foreach (var c in cs.availableContracts.Values)
            {
                sb.AppendLine($"    [{c.Id.Value}] {c.Name} – D{c.Difficulty} ${c.RewardMoney} ({c.RequiredSkill})");
            }
        }

        sb.AppendLine($"  Active/In-Progress:");
        if (cs.activeContracts != null)
        {
            foreach (var c in cs.activeContracts.Values)
            {
                string teamName = GetTeamNameForContract(c.Id, state);
                sb.AppendLine($"    [{c.Id.Value}] {c.Name} – {c.Status} – Work: {c.WorkCompleted:F1}/{c.TotalWorkRequired:F1} – Team: {teamName}");
            }
        }
        console.Print(sb.ToString());
    }

    private void DumpFinance(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.financeState == null)
        {
            console.PrintError("[State] No finance state.");
            return;
        }

        var fs = state.financeState;
        var sb = new StringBuilder();
        sb.AppendLine("[State] Finance:");
        sb.AppendLine($"  Money:              ${fs.money:N0}");
        sb.AppendLine($"  Health:             {fs.financialHealth}");
        sb.AppendLine($"  Consecutive Neg. Days: {fs.consecutiveDaysNegativeCash}");
        sb.AppendLine($"  Missed Obligations: {fs.missedObligationCount}");

        // Compute monthly expenses and revenue from recurring costs
        int dailyExpenses = 0;
        int dailyRevenue = 0;
        if (fs.recurringCosts != null)
        {
            for (int i = 0; i < fs.recurringCosts.Count; i++)
            {
                var entry = fs.recurringCosts[i];
                if (!entry.isActive) continue;
                if (entry.interval != RecurringInterval.Monthly) continue;
                if (entry.amount > 0) dailyRevenue += entry.amount;
                else dailyExpenses += -entry.amount;
            }
        }
        sb.AppendLine($"  Monthly Expenses:   ${dailyExpenses:N0}");
        sb.AppendLine($"  Monthly Revenue:    ${dailyRevenue:N0}");
        sb.AppendLine($"  Net/Month:          ${(dailyRevenue - dailyExpenses):N0}");

        if (_gameController.LoanReadModel != null)
        {
            var lr = _gameController.LoanReadModel;
            sb.AppendLine($"  Active Loan:        {lr.HasActiveLoan}");
            if (lr.HasActiveLoan)
            {
                var activeLoan = lr.GetActiveLoan();
                if (activeLoan.HasValue)
                {
                    sb.AppendLine($"    Remaining:        ${activeLoan.Value.remainingOwed:N0}");
                    sb.AppendLine($"    Monthly Payment:  ${activeLoan.Value.monthlyPayment:N0}");
                    sb.AppendLine($"    Months Left:      {activeLoan.Value.remainingMonths}");
                }
            }
        }
        console.Print(sb.ToString());
    }

    private void DumpMorale(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.moraleState == null)
        {
            console.PrintError("[State] No morale state.");
            return;
        }

        var ms = state.moraleState;
        var sb = new StringBuilder();
        sb.AppendLine("[State] Morale:");
        sb.AppendLine($"  {"ID",-4} {"Name",-18} {"Morale",7} {"Multiplier",12}");
        sb.AppendLine(new string('-', 48));

        foreach (var kvp in ms.employeeMorale)
        {
            string name = "Unknown";
            if (state.employeeState?.employees != null &&
                state.employeeState.employees.TryGetValue(kvp.Key, out var foundEmp))
            {
                name = foundEmp.name;
            }
            float mult = MoraleSystem.MoraleMultiplier(kvp.Value.currentMorale);
            sb.AppendLine($"  {kvp.Key.Value,-4} {name,-18} {kvp.Value.currentMorale,7:F1} {mult,12:F3}");
        }
        console.Print(sb.ToString());
    }

    private void DumpTeams(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.teamState?.teams == null)
        {
            console.PrintError("[State] No team state.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[State] Teams:");

        foreach (var team in state.teamState.teams.Values)
        {
            string contractInfo = "No contract";
            if (state.contractState?.teamAssignments != null &&
                state.contractState.teamAssignments.TryGetValue(team.id, out var contractId))
            {
                Contract c = null;
                if (state.contractState.activeContracts?.TryGetValue(contractId, out c) == true ||
                    state.contractState.availableContracts?.TryGetValue(contractId, out c) == true)
                {
                    contractInfo = $"Contract [{c.Id.Value}] {c.Name} ({c.Status})";
                }
            }

            sb.AppendLine($"  [{team.id.Value}] {team.name} – {contractInfo}");
            sb.AppendLine($"    Members ({team.members?.Count ?? 0}):");
            if (team.members != null)
            {
                int mc = team.members.Count;
                for (int i = 0; i < mc; i++)
                {
                    var mid = team.members[i];
                    string empName = "?";
                    if (state.employeeState?.employees != null &&
                        state.employeeState.employees.TryGetValue(mid, out var memberEmp))
                        empName = memberEmp.name;

                    sb.AppendLine($"      {mid.Value}: {empName}");
                }
            }
        }
        console.Print(sb.ToString());
    }

    private void DumpReputation(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.reputationState?.reputationScores == null)
        {
            console.PrintError("[State] No reputation state.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[State] Reputation:");
        foreach (var kvp in state.reputationState.reputationScores)
        {
            var tier = ReputationSystem.CalculateTier(kvp.Value, _gameController.Tuning);
            sb.AppendLine($"  {kvp.Key}: {kvp.Value} pts – Tier {tier}");
        }

        if (state.recruitmentReputationState != null)
            sb.AppendLine($"  Recruitment Score: {state.recruitmentReputationState.score}/100");

        console.Print(sb.ToString());
    }

    private void DumpCandidates(DebugConsole console)
    {
        var state = _gameController.GetGameState();
        if (state?.employeeState?.availableCandidates == null)
        {
            console.PrintError("[State] No candidate pool.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[State] Candidates:");
        sb.AppendLine($"  {"ID",-4} {"Name",-18} {"Role",-14} {"Source",-12} {"Stage",6} {"Expiry",8}");
        sb.AppendLine(new string('-', 70));

        var candidates = state.employeeState.availableCandidates;
        int count = candidates.Count;
        int currentTick = state.currentTick;
        for (int i = 0; i < count; i++)
        {
            var c = candidates[i];
            string source = c.IsTargeted ? "HR" : "Auto";
            float timeLeft = CandidateExpiryHelper.GetTimeRemainingPercent(state.employeeState, c.CandidateId, currentTick, _gameController.Tuning);
            string expiryStr = timeLeft >= 0f ? $"{timeLeft * 100f:F0}%" : "N/A";
            sb.AppendLine($"  {c.CandidateId,-4} {c.Name,-18} {c.Role,-14} {source,-12} {c.InterviewStage,6} {expiryStr,8}");
        }
        sb.AppendLine($"  Total: {count}");
        console.Print(sb.ToString());
    }

    // ─── Tuning Commands ─────────────────────────────────────────────────────────

    private void TuningList(string[] args, DebugConsole console)
    {
        var tuning = _gameController.Tuning;
        if (tuning == null)
        {
            console.PrintError("[Tuning] TuningConfig not available.");
            return;
        }

        string filter = args.Length > 0 ? args[0].ToLowerInvariant() : null;
        var allParams = tuning.GetAllParameters();
        var sb = new StringBuilder();
        sb.AppendLine($"[Tuning] Parameters ({allParams.Count}):");

        foreach (var kvp in allParams)
        {
            if (filter != null && !kvp.Key.ToLowerInvariant().Contains(filter))
                continue;
            sb.AppendLine($"  {kvp.Key,-45} = {kvp.Value}");
        }
        console.Print(sb.ToString());
    }

    private void TuningSet(string[] args, DebugConsole console)
    {
        var tuning = _gameController.Tuning;
        if (tuning == null)
        {
            console.PrintError("[Tuning] TuningConfig not available.");
            return;
        }

        if (args.Length < 2)
        {
            console.PrintError("[Tuning] Usage: tuning.set NAME VALUE");
            return;
        }

        string name = args[0];
        string rawValue = args[1];

        // Check parameter exists before setting
        var allParams = tuning.GetAllParameters();
        if (!allParams.ContainsKey(name))
        {
            console.PrintError($"[Tuning] Unknown parameter: {name}. Use 'tuning.list' to see all.");
            return;
        }

        try
        {
            tuning.SetParameter(name, rawValue);
            console.Print($"[Tuning] {name} = {rawValue}");
        }
        catch (System.Exception ex)
        {
            console.PrintError($"[Tuning] Failed to set {name}: {ex.Message}");
        }
    }

    private void TuningSummary(DebugConsole console)
    {
        var tuning = _gameController.Tuning;
        if (tuning == null)
        {
            console.PrintError("[Tuning] TuningConfig not available.");
            return;
        }

        var defaults = TuningConfig.Defaults();
        var current = tuning.GetAllParameters();
        var defParams = defaults.GetAllParameters();

        var sb = new StringBuilder();
        sb.AppendLine("[Tuning] Modified Parameters (vs. defaults):");

        bool anyModified = false;
        foreach (var kvp in current)
        {
            if (!defParams.TryGetValue(kvp.Key, out var defVal)) continue;
            string currentStr = FormatValue(kvp.Value);
            string defaultStr = FormatValue(defVal);
            if (currentStr != defaultStr)
            {
                sb.AppendLine($"  {kvp.Key,-45} = {currentStr}  (default: {defaultStr})");
                anyModified = true;
            }
        }

        if (!anyModified)
            sb.AppendLine("  (all parameters at default values)");

        console.Print(sb.ToString());
    }

    private void TuningReset(DebugConsole console)
    {
        var tuning = _gameController.Tuning;
        if (tuning == null)
        {
            console.PrintError("[Tuning] TuningConfig not available.");
            return;
        }

        var defaults = TuningConfig.Defaults();
        var defParams = defaults.GetAllParameters();
        foreach (var kvp in defParams)
        {
            try { tuning.SetParameter(kvp.Key, kvp.Value); }
            catch { /* skip array types */ }
        }

        console.Print("[Tuning] All parameters reset to defaults.");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private string GetTeamNameForContract(ContractId contractId, GameState state)
    {
        if (state.contractState?.teamAssignments == null) return "Unassigned";
        foreach (var kvp in state.contractState.teamAssignments)
        {
            if (kvp.Value.Value == contractId.Value)
            {
                if (state.teamState?.teams != null && state.teamState.teams.TryGetValue(kvp.Key, out var t))
                    return t.name;
            }
        }
        return "Unassigned";
    }

    private static string FormatValue(object val)
    {
        if (val is float f) return f.ToString("G4");
        if (val is double d) return d.ToString("G4");
        return val?.ToString() ?? "null";
    }
}
