// TaxSystem Version: Clean v1
using System.Collections.Generic;

public class TaxSystem : ISystem
{
    private enum PendingEventType
    {
        TaxDue,
        TaxOverdue,
        TaxReminder,
        TaxBankruptcy,
        TaxPaid,
    }

    private struct PendingEvent
    {
        public PendingEventType Type;
        public int Tick;
        public int Days;
        public int Months;
        public long Amount;
        public long Fees;
        public long Total;
        public bool IsFinalWarning;
    }

    private TaxState _state;
    private FinanceSystem _financeSystem;
    private TimeSystem _timeSystem;
    private GameEventBus _eventBus;
    private ILogger _logger;
    private readonly List<PendingEvent> _pendingEvents;
    private int _lastKnownMoney;
    private int _lastProcessedDay;
    private int _currentTick;

    public long CurrentProfitAccumulated => _state.profitSinceLastCycle;
    public long EstimatedTaxOwed => (long)(_state.profitSinceLastCycle * _state.taxRate);
    public long PendingTaxAmount => _state.pendingTaxAmount;
    public long PendingLateFees => _state.pendingLateFees;
    public long TotalPendingPayment => _state.pendingTaxAmount + _state.pendingLateFees;
    public bool HasPendingTax => _state.hasPendingTax;
    public int NextDueTick => _state.nextDueTick;
    public float TaxRate => _state.taxRate;
    public int OverdueMonths => _state.overdueMonthsApplied;

    public int DaysUntilDue
    {
        get
        {
            int ticksRemaining = _state.nextDueTick - _currentTick;
            return ticksRemaining / TimeState.TicksPerDay;
        }
    }

    public TaxSystem(TaxState state, FinanceSystem financeSystem, TimeSystem timeSystem, GameEventBus eventBus, ILogger logger)
    {
        _state = state ?? TaxState.CreateNew();
        _financeSystem = financeSystem;
        _timeSystem = timeSystem;
        _eventBus = eventBus;
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<PendingEvent>(8);
        _lastKnownMoney = _financeSystem != null ? _financeSystem.Money : 0;
        _lastProcessedDay = -1;
        _currentTick = 0;

        if (_state.sentReminderDays == null)
            _state.sentReminderDays = new List<int>();
    }

    public void PreTick(int tick) { }

    public void Tick(int tick)
    {
        _currentTick = tick;
        int currentDay = tick / TimeState.TicksPerDay;
        if (currentDay == _lastProcessedDay) return;
        _lastProcessedDay = currentDay;

        // Track daily profit delta
        int currentMoney = _financeSystem != null ? _financeSystem.Money : 0;
        int delta = currentMoney - _lastKnownMoney;
        _lastKnownMoney = currentMoney;
        if (delta > 0)
            _state.profitSinceLastCycle += delta;

        // Check due date first
        if (tick >= _state.nextDueTick)
        {
            if (_state.hasPendingTax)
            {
                // BANKRUPTCY — next cycle arrived while previous tax is unpaid
                long unpaid = _state.pendingTaxAmount + _state.pendingLateFees;
                _logger.LogError($"[TaxSystem] Tax bankruptcy at tick {tick}. Unpaid: {unpaid}");
                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TaxBankruptcy, Tick = tick, Amount = unpaid });
                return;
            }
            else
            {
                // Freeze current profit into pending tax
                long taxOwed = (long)(_state.profitSinceLastCycle * _state.taxRate);
                _state.pendingTaxAmount = taxOwed;
                _state.hasPendingTax = true;
                _state.pendingTaxDueTick = tick;
                _state.overdueMonthsApplied = 0;
                _state.pendingLateFees = 0;
                _state.profitSinceLastCycle = 0;
                _state.cycleStartTick = tick;
                _state.nextDueTick += _state.cycleLengthDays * TimeState.TicksPerDay;
                _state.sentReminderDays.Clear();

                _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TaxDue, Tick = tick, Amount = taxOwed });
                _logger.Log($"[TaxSystem] Tax due at tick {tick}. Amount: {taxOwed}");
            }
        }

        // Check late fees
        if (_state.hasPendingTax && _state.pendingTaxAmount > 0)
        {
            int monthsOverdue = (tick - _state.pendingTaxDueTick) / (30 * TimeState.TicksPerDay);
            if (monthsOverdue > _state.overdueMonthsApplied && _state.overdueMonthsApplied < 3)
            {
                _state.overdueMonthsApplied++;
                long fee = 0L;
                if (_state.overdueMonthsApplied == 1)
                    fee = (long)(_state.pendingTaxAmount * 0.02f);
                else if (_state.overdueMonthsApplied == 2)
                    fee = (long)(_state.pendingTaxAmount * 0.05f);
                else if (_state.overdueMonthsApplied == 3)
                    fee = (long)(_state.pendingTaxAmount * 0.10f);

                _state.pendingLateFees += fee;

                _pendingEvents.Add(new PendingEvent
                {
                    Type = PendingEventType.TaxOverdue,
                    Tick = tick,
                    Amount = _state.pendingTaxAmount,
                    Fees = _state.pendingLateFees,
                    Months = _state.overdueMonthsApplied,
                });
                _logger.LogWarning($"[TaxSystem] Late fee tier {_state.overdueMonthsApplied} applied. Fee: {fee}. Total late fees: {_state.pendingLateFees}");
            }
        }

        // Check reminders for next due date
        int daysUntilDue = (_state.nextDueTick - tick) / TimeState.TicksPerDay;

        // Bankruptcy warning: 30 days before next cycle if previous tax is unpaid
        if (_state.hasPendingTax && daysUntilDue <= 30 && !HasSentReminder(-1))
        {
            _state.sentReminderDays.Add(-1);
            long totalOwed = _state.pendingTaxAmount + _state.pendingLateFees;
            _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TaxReminder, Tick = tick, Days = daysUntilDue, Amount = totalOwed, IsFinalWarning = true });
        }

        // Standard reminders at 90, 60, 30, 7 days before due date
        if (!_state.hasPendingTax)
        {
            CheckAndSendReminder(tick, daysUntilDue, 90);
            CheckAndSendReminder(tick, daysUntilDue, 60);
            CheckAndSendReminder(tick, daysUntilDue, 30);
            CheckAndSendReminder(tick, daysUntilDue, 7);
        }
    }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
        {
            PendingEvent e = _pendingEvents[i];
            switch (e.Type)
            {
                case PendingEventType.TaxDue:        _eventBus.Raise(new TaxDueEvent(e.Tick, e.Amount)); break;
                case PendingEventType.TaxOverdue:    _eventBus.Raise(new TaxOverdueEvent(e.Tick, e.Amount, e.Fees, e.Months)); break;
                case PendingEventType.TaxReminder:   _eventBus.Raise(new TaxReminderEvent(e.Tick, e.Days, e.Amount, e.IsFinalWarning)); break;
                case PendingEventType.TaxBankruptcy: _eventBus.Raise(new TaxBankruptcyEvent(e.Tick, e.Amount)); break;
                case PendingEventType.TaxPaid:       _eventBus.Raise(new TaxPaidEvent(e.Tick, e.Amount, e.Fees, e.Total)); break;
            }
        }
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is PayTaxCommand)
            ProcessPayment(command.Tick);
    }

    public void Dispose()
    {
        _pendingEvents.Clear();
    }

    private void ProcessPayment(int tick)
    {
        if (!_state.hasPendingTax) return;

        long taxAmount = _state.pendingTaxAmount;
        long lateFees = _state.pendingLateFees;
        long total = taxAmount + lateFees;

        _financeSystem.RecordTransaction(-(int)total, FinanceCategory.TaxPayment, tick, "tax");

        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TaxPaid, Tick = tick, Amount = taxAmount, Fees = lateFees, Total = total });

        _state.hasPendingTax = false;
        _state.pendingTaxAmount = 0;
        _state.pendingLateFees = 0;
        _state.overdueMonthsApplied = 0;
        _state.pendingTaxDueTick = 0;

        _logger.Log($"[TaxSystem] Tax paid at tick {tick}. Tax: {taxAmount}, Late fees: {lateFees}, Total: {total}");
    }

    private bool HasSentReminder(int threshold)
    {
        int count = _state.sentReminderDays.Count;
        for (int i = 0; i < count; i++)
        {
            if (_state.sentReminderDays[i] == threshold) return true;
        }
        return false;
    }

    private void CheckAndSendReminder(int tick, int daysUntilDue, int threshold)
    {
        if (daysUntilDue > threshold || daysUntilDue < threshold - 1) return;
        if (HasSentReminder(threshold)) return;

        _state.sentReminderDays.Add(threshold);
        long estimated = EstimatedTaxOwed;
        _pendingEvents.Add(new PendingEvent { Type = PendingEventType.TaxReminder, Tick = tick, Days = daysUntilDue, Amount = estimated, IsFinalWarning = false });
        _logger.Log($"[TaxSystem] Tax reminder sent: {threshold} days until due. Estimated: {estimated}");
    }
}
