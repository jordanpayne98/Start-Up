// FinanceSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class FinanceSystem : ISystem
{
    public event Action<FinanceTransaction> OnTransactionRecorded;
    public event Action OnRecurringCostChanged;
    public event Action<FinancialHealthState> OnFinancialHealthChanged;
    public event Action OnBankruptcyWarning;
    public event Action OnBankrupt;

    private FinanceState _state;
    private ILogger _logger;
    private TuningConfig _tuning;

    private bool _hasTransactionThisTick;
    private FinanceTransaction _lastRecordedTransaction;
    private bool _recurringCostDirty;
    private bool _healthChanged;
    private FinancialHealthState _capturedHealth;
    private bool _bankruptcyWarning;
    private bool _bankrupt;

    public int Money => _state.money;
    public FinancialHealthState FinancialHealth => _state.financialHealth;
    public int ConsecutiveDaysNegativeCash => _state.consecutiveDaysNegativeCash;
    public int MissedObligationCount => _state.missedObligationCount;
    public bool IsInDebt => _state.IsInDebt;
    public bool IsBankrupt => _state.IsBankrupt;

    public int TotalRevenue
    {
        get
        {
            if (_state.transactions == null) return 0;
            int total = 0;
            int count = _state.transactions.Count;
            for (int i = 0; i < count; i++)
            {
                if (_state.transactions[i].amount > 0) total += _state.transactions[i].amount;
            }
            return total;
        }
    }

    public int TotalExpenses
    {
        get
        {
            if (_state.transactions == null) return 0;
            int total = 0;
            int count = _state.transactions.Count;
            for (int i = 0; i < count; i++)
            {
                if (_state.transactions[i].amount < 0) total += -_state.transactions[i].amount;
            }
            return total;
        }
    }

    public int DailyObligations
    {
        get
        {
            int total = 0;
            int count = _state.recurringCosts?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                var entry = _state.recurringCosts[i];
                if (entry.isActive && entry.interval == RecurringInterval.Daily)
                    total += entry.amount;
            }
            return total;
        }
    }

    public int MonthlyObligations
    {
        get
        {
            int total = 0;
            int count = _state.recurringCosts?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                var entry = _state.recurringCosts[i];
                if (entry.isActive && entry.interval == RecurringInterval.Monthly)
                    total += entry.amount;
            }
            return total;
        }
    }

    // Derived monthly view: monthly costs + daily costs * 30
    public int MonthlyExpenses => MonthlyObligations + DailyObligations * 30;

    public int RunwayDays
    {
        get
        {
            int dailyEquivalent = DailyObligations + (MonthlyObligations / 30);
            if (dailyEquivalent <= 0) return int.MaxValue;
            int runway = _state.money / dailyEquivalent;
            return runway > 0 ? runway : 0;
        }
    }

    public IReadOnlyList<FinanceTransaction> RecentTransactions => _state.transactions;
    public IReadOnlyList<RecurringCostEntry> RecurringCosts => _state.recurringCosts;

    public FinanceSystem(FinanceState state, ILogger logger)
    {
        _state = state ?? FinanceState.CreateNew();
        _logger = logger ?? new NullLogger();

        if (_state.transactions == null) _state.transactions = new System.Collections.Generic.List<FinanceTransaction>();
        if (_state.recurringCosts == null) _state.recurringCosts = new System.Collections.Generic.List<RecurringCostEntry>();
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    // ─── Transaction Recording ──────────────────────────────────────────────────

    public void RecordTransaction(int amount, FinanceCategory category, int tick, string sourceId = null)
    {
        if (amount == 0) return;

        _state.money += amount;

        var transaction = new FinanceTransaction
        {
            amount = amount,
            category = category,
            tick = tick,
            sourceId = sourceId
        };
        _state.AddTransaction(transaction);

        _hasTransactionThisTick = true;
        _lastRecordedTransaction = transaction;
    }

    // ─── Stock System Integration ────────────────────────────────────────────────

    public void AddDividendIncome(long amount, int tick)
    {
        if (amount <= 0) return;
        RecordTransaction((int)amount, FinanceCategory.MiscIncome, tick, "dividend");
    }

    public void AddProductSaleProceeds(long amount, int tick)
    {
        if (amount <= 0) return;
        RecordTransaction((int)amount, FinanceCategory.MiscIncome, tick, "product-sale");
    }

    // ─── Backward Compat Wrappers ────────────────────────────────────────────────

    public void AddMoney(int amount)
    {
        if (amount == 0) return;
        var category = amount > 0 ? FinanceCategory.MiscIncome : FinanceCategory.MiscExpense;
        RecordTransaction(amount, category, 0);
    }

    public bool TrySubtractMoney(int amount, out string error)
    {
        if (amount <= 0)
        {
            error = "Amount must be positive";
            return false;
        }
        RecordTransaction(-amount, FinanceCategory.MiscExpense, 0);
        error = null;
        return true;
    }

    // ─── Recurring Cost Management ───────────────────────────────────────────────

    public void AddRecurringCost(string id, FinanceCategory category, int amount, RecurringInterval interval, string sourceId = null)
    {
        if (string.IsNullOrEmpty(id)) return;

        // Update if already exists
        int count = _state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = _state.recurringCosts[i];
            if (entry.id == id)
            {
                _state.recurringCosts[i] = new RecurringCostEntry
                {
                    id = id,
                    category = category,
                    amount = amount,
                    interval = interval,
                    sourceId = sourceId,
                    isActive = true
                };
                _recurringCostDirty = true;
                return;
            }
        }

        _state.recurringCosts.Add(new RecurringCostEntry
        {
            id = id,
            category = category,
            amount = amount,
            interval = interval,
            sourceId = sourceId,
            isActive = true
        });
        _recurringCostDirty = true;
    }

    public void RemoveRecurringCost(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        int count = _state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            if (_state.recurringCosts[i].id == id)
            {
                _state.recurringCosts.RemoveAt(i);
                _recurringCostDirty = true;
                return;
            }
        }
    }

    public void SetRecurringCostActive(string id, bool isActive)
    {
        int count = _state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = _state.recurringCosts[i];
            if (entry.id == id)
            {
                _state.recurringCosts[i] = new RecurringCostEntry
                {
                    id = entry.id,
                    category = entry.category,
                    amount = entry.amount,
                    interval = entry.interval,
                    sourceId = entry.sourceId,
                    isActive = isActive
                };
                _recurringCostDirty = true;
                return;
            }
        }
    }

    // ─── Day / Month Processing ──────────────────────────────────────────────────

    public void ProcessDaily(int tick)
    {
        int count = _state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = _state.recurringCosts[i];
            if (!entry.isActive || entry.interval != RecurringInterval.Daily) continue;
            RecordTransaction(-entry.amount, entry.category, tick, entry.sourceId);
        }

        if (_state.money < 0)
            _state.consecutiveDaysNegativeCash++;
        else
            _state.consecutiveDaysNegativeCash = 0;

        UpdateFinancialHealth(tick);
    }

    public void ProcessMonthly(int tick)
    {
        int count = _state.recurringCosts.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = _state.recurringCosts[i];
            if (!entry.isActive || entry.interval != RecurringInterval.Monthly) continue;
            RecordTransaction(-entry.amount, entry.category, tick, entry.sourceId);
        }
    }

    // ─── Financial Health ────────────────────────────────────────────────────────

    private void UpdateFinancialHealth(int tick)
    {
        var oldHealth = _state.financialHealth;
        FinancialHealthState newHealth;

        int days = _state.consecutiveDaysNegativeCash;
        int missed = _state.missedObligationCount;
        int runway = RunwayDays;

        int bankruptDays    = _tuning != null ? _tuning.FinanceBankruptDaysThreshold    : 14;
        int bankruptMissed  = _tuning != null ? _tuning.FinanceBankruptMissedThreshold  : 5;
        int insolventDays   = _tuning != null ? _tuning.FinanceInsolventDaysThreshold   : 7;
        int insolventMissed = _tuning != null ? _tuning.FinanceInsolventMissedThreshold : 3;
        int distressedDays  = _tuning != null ? _tuning.FinanceDistressedDaysThreshold  : 3;
        int tightRunway     = _tuning != null ? _tuning.FinanceTightRunwayThreshold     : 5;

        if (days >= bankruptDays || missed >= bankruptMissed)
            newHealth = FinancialHealthState.Bankrupt;
        else if (days >= insolventDays || missed >= insolventMissed)
            newHealth = FinancialHealthState.Insolvent;
        else if (days >= distressedDays)
            newHealth = FinancialHealthState.Distressed;
        else if (runway <= tightRunway)
            newHealth = FinancialHealthState.Tight;
        else
            newHealth = FinancialHealthState.Stable;

        _state.financialHealth = newHealth;

        if (newHealth != oldHealth)
        {
            _healthChanged = true;
            _capturedHealth = newHealth;

            if (newHealth == FinancialHealthState.Bankrupt)
            {
                _bankrupt = true;
                _logger.LogError($"[Tick {tick}] GAME OVER: Company bankrupt! {days} consecutive days negative cash.");
            }
            else if (newHealth >= FinancialHealthState.Distressed && oldHealth < FinancialHealthState.Distressed)
            {
                _bankruptcyWarning = true;
                _logger.LogWarning($"[Tick {tick}] BANKRUPTCY WARNING: Financial health is {newHealth}");
            }
        }
    }

    // ─── ISystem ────────────────────────────────────────────────────────────────

    public void PreTick(int tick) { }

    public void Tick(int tick) { }

    public void PostTick(int tick)
    {
        if (_hasTransactionThisTick)
        {
            OnTransactionRecorded?.Invoke(_lastRecordedTransaction);
            _hasTransactionThisTick = false;
        }
        if (_recurringCostDirty)
        {
            OnRecurringCostChanged?.Invoke();
            _recurringCostDirty = false;
        }
        if (_healthChanged)
        {
            OnFinancialHealthChanged?.Invoke(_capturedHealth);
            _healthChanged = false;
        }
        if (_bankruptcyWarning)
        {
            OnBankruptcyWarning?.Invoke();
            _bankruptcyWarning = false;
        }
        if (_bankrupt)
        {
            OnBankrupt?.Invoke();
            _bankrupt = false;
        }
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is AddMoneyCommand addMoney)
            AddMoney(addMoney.Amount);
    }

    public void Dispose()
    {
        _hasTransactionThisTick = false;
        _recurringCostDirty = false;
        _healthChanged = false;
        _bankruptcyWarning = false;
        _bankrupt = false;
        OnTransactionRecorded = null;
        OnRecurringCostChanged = null;
        OnFinancialHealthChanged = null;
        OnBankruptcyWarning = null;
        OnBankrupt = null;
    }
}
