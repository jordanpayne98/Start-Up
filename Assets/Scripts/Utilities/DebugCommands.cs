using UnityEngine;
using UnityEngine.InputSystem;

public class DebugCommands : MonoBehaviour
{
    [Header("Debug Key Bindings")]
    [SerializeField] private Key hireRandomEmployeeKey = Key.H;
    [SerializeField] private Key fireFirstEmployeeKey = Key.F;
    [SerializeField] private Key listEmployeesKey = Key.L;
    [SerializeField] private Key giveMoney1000Key = Key.M;
    [SerializeField] private Key openHireDialogKey = Key.D;
    [SerializeField] private Key printFinancesKey = Key.P;
    [SerializeField] private Key printAbilityKey   = Key.A;
    
    private GameController _gameController;
    private IRng _nameRng;
    
    private void Start()
    {
        _gameController = FindFirstObjectByType<GameController>();
        
        if (_gameController != null)
        {
            int seed = _gameController.GetDeterministicSeed("debugcommands");
            _nameRng = RngFactory.CreateStream(seed, "debugnames");
        }
        else
        {
            int fallbackSeed = System.Environment.TickCount;
            _nameRng = RngFactory.CreateStream(fallbackSeed, "debugnames");
        }
    }
    
    // Key-bound debug commands removed — use the DebugConsole (backtick) instead.
    // Public methods below are retained for programmatic use.
    
    public void HireRandomEmployee()
    {
        Gender gender = (Gender)_nameRng.Range(0, 3);
        string name = NameGenerator.GenerateRandomName(_nameRng, gender);
        int salary = _nameRng.Range(3000, 8000);

        RoleId role = (RoleId)_nameRng.Range(0, SkillIdHelper.SkillCount);

        var stats = EmployeeStatBlock.Create();
        for (int i = 0; i < SkillIdHelper.SkillCount; i++)
            stats.SetSkill((SkillId)i, _nameRng.Range(4, 20));

        var cmd = new HireEmployeeCommand
        {
            Tick = _gameController.CurrentTick,
            Name = name,
            Gender = gender,
            Stats = stats,
            Salary = salary,
            Role = role
        };

        _gameController.QueueCommand(cmd);

        Debug.Log($"[DEBUG] Hired {name} [{role}] Salary:${salary}");
    }
    
    public void FireFirstEmployee()
    {
        var employees = _gameController.EmployeeSystem.GetAllActiveEmployees();
        
        Employee firstEmployee = null;
        foreach (var emp in employees)
        {
            firstEmployee = emp;
            break;
        }
        
        if (firstEmployee == null)
        {
            Debug.LogWarning("[DEBUG] No employees to fire");
            return;
        }
        
        var cmd = new FireEmployeeCommand
        {
            Tick = _gameController.CurrentTick,
            EmployeeId = firstEmployee.id
        };
        
        _gameController.QueueCommand(cmd);
        
        Debug.Log($"[DEBUG] Fired {firstEmployee.name} (ID: {firstEmployee.id.Value})");
    }
    
    public void ListEmployees()
    {
        var employees = _gameController.EmployeeSystem.GetAllActiveEmployees();
        
        int count = 0;
        foreach (var emp in employees)
        {
            count++;
        }
        
        Debug.Log($"[DEBUG] === Employee List ({count} employees) ===");
        
        foreach (var employee in employees)
        {
            int prog = employee.GetSkill(SkillId.Programming);
            int des  = employee.GetSkill(SkillId.ProductDesign);
            int qa   = employee.GetSkill(SkillId.QaTesting);
            Debug.Log($"  ID:{employee.id.Value} | {employee.name} | Prog:{prog} Des:{des} QA:{qa} | ${employee.salary}/month | Morale:{employee.morale}");
        }
        
        Debug.Log($"[DEBUG] Total Daily Salaries: ${_gameController.EmployeeSystem.TotalDailySalaries}");
    }

    public void PrintAbility()
    {
        var abilitySystem = _gameController.AbilitySystem;
        if (abilitySystem == null)
        {
            Debug.LogWarning("[DEBUG] AbilitySystem not available.");
            return;
        }

        var employees = _gameController.EmployeeSystem.GetAllActiveEmployees();
        Debug.Log("[DEBUG] === Ability/Potential Report ===");
        foreach (var emp in employees)
        {
            int ca = abilitySystem.GetCA(emp.id, emp.role);
            int potential = emp.Stats.PotentialAbility;
            int potentialStars = AbilityCalculator.PotentialToStars(potential);
            string potentialDisplay = AbilityCalculator.PotentialStarsDisplay(potentialStars);
            int lr  = emp.Stats.GetHiddenAttribute(HiddenAttributeId.LearningRate);
            int amb = emp.Stats.GetHiddenAttribute(HiddenAttributeId.Ambition);
            int we  = emp.Stats.GetVisibleAttribute(VisibleAttributeId.WorkEthic);
            int ada = emp.Stats.GetVisibleAttribute(VisibleAttributeId.Adaptability);
            Debug.Log($"  [{emp.id.Value}] {emp.name} | Role:{emp.role} | Ability:{ca} | Potential:{potential} ({potentialDisplay}) | LR:{lr} Amb:{amb} WE:{we} Ada:{ada}");
        }
    }
    
    public void GiveMoney(int amount)
    {
        var cmd = new AddMoneyCommand
        {
            Tick = _gameController.CurrentTick,
            Amount = amount
        };
        
        _gameController.QueueCommand(cmd);
        
        Debug.Log($"[DEBUG] Gave ${amount}");
    }
    
    public void SetMoney(int amount)
    {
        int currentMoney = _gameController.FinanceSystem.Money;
        int difference = amount - currentMoney;
        
        var cmd = new AddMoneyCommand
        {
            Tick = _gameController.CurrentTick,
            Amount = difference
        };
        
        _gameController.QueueCommand(cmd);
        
        Debug.Log($"[DEBUG] Set money to ${amount} (adjusted by ${difference})");
    }
    
    public void OpenHireDialog()
    {
        // if (UIManager.Instance != null)
        // {
        //     UIManager.Instance.ShowHireEmployeeDialog((candidate) =>
        //     {
        //         Debug.Log($"[DEBUG] Dialog hire callback: {candidate?.Name ?? "null"}");
        //     });
        //     Debug.Log("[DEBUG] Opened hire employee dialog");
        // }
        // else
        // {
        //     Debug.LogWarning("[DEBUG] UIManager not found");
        // }
        Debug.LogWarning("[DEBUG] OpenHireDialog called but UIManager not implemented yet");
    }
    
    public void PrintFinances()
    {
        var finance = _gameController.FinanceSystem;
        var time = _gameController.TimeSystem;
        
        int runwayDays = 0;
        if (finance.MonthlyExpenses > 0)
        {
            int dailyBurn = finance.MonthlyExpenses / 30;
            runwayDays = Max(0, dailyBurn > 0 ? finance.Money / dailyBurn : int.MaxValue);
        }
        
        Debug.Log($"[DEBUG] === Financial Status ===");
        Debug.Log($"  Money: ${finance.Money}");
        Debug.Log($"  Monthly Expenses: ${finance.MonthlyExpenses}");
        Debug.Log($"  Runway: {runwayDays} days");
        Debug.Log($"  Total Revenue: ${finance.TotalRevenue}");
        Debug.Log($"  Total Expenses: ${finance.TotalExpenses}");
        Debug.Log($"  In Debt: {finance.IsInDebt}");
        Debug.Log($"  Bankrupt: {finance.IsBankrupt}");
        Debug.Log($"  Current Day: {time.CurrentDay}");
    }
    
    private int Max(int a, int b)
    {
        return a > b ? a : b;
    }
}
