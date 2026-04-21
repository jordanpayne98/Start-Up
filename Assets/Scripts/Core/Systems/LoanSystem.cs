// LoanSystem Version: Clean v1
using System;
using System.Collections.Generic;

public class LoanSystem : ISystem, ILoanReadModel
{
    public event Action<int, float> OnLoanTaken;       // principal, rate
    public event Action<int> OnLoanPaymentMade;         // payment amount
    public event Action<int> OnLoanEarlyRepaid;         // interest avoided
    public event Action OnLoanFullyRepaid;

    private LoanState _state;
    private ReputationSystem _reputationSystem;
    private FinanceSystem _financeSystem;
    private ILogger _logger;
    private List<Action> _pendingEvents;

    private const int BaseAmount = 5000;
    private const float BaseInterestRate = 0.10f;
    private const int MinDurationMonths = 1;
    private const int MaxDurationMonths = 12;

    // Reputation tier multipliers — how established the company appears to lenders
    private static readonly float[] ReputationTierMultipliers = { 1.0f, 1.5f, 2.5f, 4.0f, 6.0f };

    // Credit tier multipliers — how trustworthy the company's finances look
    private static readonly float[] CreditTierMultipliers = { 0.3f, 0.7f, 1.0f, 1.5f };

    private TuningConfig _tuning;

    public LoanSystem(LoanState state, ReputationSystem reputationSystem,
        FinanceSystem financeSystem, ILogger logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _reputationSystem = reputationSystem ?? throw new ArgumentNullException(nameof(reputationSystem));
        _financeSystem = financeSystem ?? throw new ArgumentNullException(nameof(financeSystem));
        _logger = logger ?? new NullLogger();
        _pendingEvents = new List<Action>();
    }

    public void SetTuningConfig(TuningConfig tuning)
    {
        _tuning = tuning;
    }

    // ─── Read Model ──────────────────────────────────────────────────────────────

    public bool HasActiveLoan => _state.hasActiveLoan;

    public ActiveLoan? GetActiveLoan() => _state.ActiveLoan;

    public bool CanTakeLoan() => !_state.hasActiveLoan;

    // ─── Credit Score ─────────────────────────────────────────────────────────────

    public int GetCreditScore()
    {
        int score = 40; // baseline — no credit history

        // Financial health state (biggest factor — like payment reliability)
        switch (_financeSystem.FinancialHealth)
        {
            case FinancialHealthState.Stable:     score += 10; break;
            case FinancialHealthState.Tight:      score += 3;  break;
            case FinancialHealthState.Distressed: score -= 12; break;
            case FinancialHealthState.Insolvent:  score -= 22; break;
            case FinancialHealthState.Bankrupt:   score -= 38; break;
        }

        // Cash position
        if (_financeSystem.Money >= 0) score += 5;
        else                           score -= 12;

        // Runway — ability to cover upcoming obligations
        int runway = _financeSystem.RunwayDays;
        if (runway == int.MaxValue || runway > 30) score += 5;
        else if (runway < 7)                       score -= 8;

        // Consecutive days negative cash (payment history equivalent)
        int negDays = _financeSystem.ConsecutiveDaysNegativeCash;
        int negPenalty = negDays * 3;
        if (negPenalty > 20) negPenalty = 20;
        score -= negPenalty;

        // Missed obligations (defaults)
        int missedPenalty = _financeSystem.MissedObligationCount * 8;
        if (missedPenalty > 25) missedPenalty = 25;
        score -= missedPenalty;

        // Completed loans — proven track record
        int completedBonus = _state.completedLoanCount * 12;
        if (completedBonus > 25) completedBonus = 25;
        score += completedBonus;

        // Active loan reduces available credit
        if (_state.hasActiveLoan) score -= 5;

        if (score < 0)   score = 0;
        if (score > 100) score = 100;
        return score;
    }

    public CreditTier GetCreditTier()
    {
        int score = GetCreditScore();
        if (score < 30) return CreditTier.Poor;
        if (score < 55) return CreditTier.Fair;
        if (score < 75) return CreditTier.Good;
        return CreditTier.Excellent;
    }

    // ─── Max Loan ─────────────────────────────────────────────────────────────────

    public int GetMaxLoanAmount()
    {
        int tierIndex = (int)_reputationSystem.CurrentTier;
        if (tierIndex < 0) tierIndex = 0;
        if (tierIndex >= ReputationTierMultipliers.Length) tierIndex = ReputationTierMultipliers.Length - 1;

        float reputationMultiplier = ReputationTierMultipliers[tierIndex];

        // Credit tier — replaces flat confidence factor with scored tiers
        int creditTierIndex = (int)GetCreditTier();
        float creditMultiplier = CreditTierMultipliers[creditTierIndex];

        int baseAmount = _tuning != null ? _tuning.LoanBaseAmount : BaseAmount;
        return (int)(baseAmount * reputationMultiplier * creditMultiplier);
    }

    public float CalculateInterestRate(float utilization, float durationFactor)
    {
        int tierIndex = (int)_reputationSystem.CurrentTier;
        if (tierIndex < 0) tierIndex = 0;
        if (tierIndex >= ReputationTierMultipliers.Length) tierIndex = ReputationTierMultipliers.Length - 1;

        float reputationDiscount = tierIndex * 0.01f;
        float activeLoanPenalty = _state.hasActiveLoan ? 0.04f : 0.0f;
        float debtPenalty = _financeSystem.Money < 0 ? 0.05f : 0.0f;
        float distressPenalty = _financeSystem.FinancialHealth == FinancialHealthState.Distressed ? 0.03f : 0.0f;

        // Credit score discount: Excellent = -0.03, Good = -0.01, Fair = 0, Poor = +0.04
        float creditDiscount;
        switch (GetCreditTier())
        {
            case CreditTier.Excellent: creditDiscount = -0.03f; break;
            case CreditTier.Good:      creditDiscount = -0.01f; break;
            case CreditTier.Fair:      creditDiscount =  0.00f; break;
            default:                   creditDiscount =  0.04f; break; // Poor
        }

        float baseRate = _tuning != null ? _tuning.LoanBaseInterestRate : BaseInterestRate;
        float rate = baseRate
            + (utilization * 0.10f)
            + (durationFactor * 0.10f)
            + activeLoanPenalty
            + debtPenalty
            + distressPenalty
            + creditDiscount
            - reputationDiscount;

        if (rate < 0.08f) rate = 0.08f;
        if (rate > 0.30f) rate = 0.30f;
        return rate;
    }

    public LoanRiskBand CalculateRiskBand(float utilization, float durationFactor)
    {
        float riskScore = (utilization * 0.7f) + (durationFactor * 0.3f);
        if (riskScore < 0.3f)  return LoanRiskBand.Safe;
        if (riskScore < 0.6f)  return LoanRiskBand.Standard;
        if (riskScore < 0.85f) return LoanRiskBand.Aggressive;
        return LoanRiskBand.Extreme;
    }

    public LoanTermsPreview PreviewLoan(int amount, int durationMonths)
    {
        int maxAmount = GetMaxLoanAmount();
        if (maxAmount <= 0) maxAmount = 1;

        int minMonths = _tuning != null ? _tuning.LoanMinDurationMonths : MinDurationMonths;
        int maxMonths = _tuning != null ? _tuning.LoanMaxDurationMonths : MaxDurationMonths;

        durationMonths = Clamp(durationMonths, minMonths, maxMonths);
        float utilization = (float)amount / maxAmount;
        float durationFactor = maxMonths > minMonths ? (float)(durationMonths - minMonths) / (maxMonths - minMonths) : 0f;

        float rate = CalculateInterestRate(utilization, durationFactor);
        int totalOwed = (int)Math.Ceiling(amount * (1f + rate));
        int monthlyPayment = (int)Math.Ceiling((float)totalOwed / durationMonths);
        int interestCost = totalOwed - amount;
        LoanRiskBand riskBand = CalculateRiskBand(utilization, durationFactor);

        return new LoanTermsPreview
        {
            interestRate = rate,
            totalOwed = totalOwed,
            monthlyPayment = monthlyPayment,
            interestCost = interestCost,
            riskBand = riskBand,
            utilization = utilization
        };
    }

    public int GetTotalMonthlyRepayment()
    {
        if (!_state.hasActiveLoan) return 0;
        int payment = _state.activeLoan.monthlyPayment;
        return payment > _state.activeLoan.remainingOwed ? _state.activeLoan.remainingOwed : payment;
    }

    public int GetTotalRemainingDebt()
    {
        return _state.hasActiveLoan ? _state.activeLoan.remainingOwed : 0;
    }

    // ─── Actions ─────────────────────────────────────────────────────────────────

    public bool TakeLoan(int amount, int durationMonths, int currentTick)
    {
        if (!CanTakeLoan())
        {
            _logger.LogWarning("[LoanSystem] Cannot take loan: already has an active loan");
            return false;
        }

        int maxAmount = GetMaxLoanAmount();
        if (amount <= 0 || amount > maxAmount)
        {
            _logger.LogWarning($"[LoanSystem] Cannot take loan: invalid amount {amount} (max {maxAmount})");
            return false;
        }

        int minMonths = _tuning != null ? _tuning.LoanMinDurationMonths : MinDurationMonths;
        int maxMonths = _tuning != null ? _tuning.LoanMaxDurationMonths : MaxDurationMonths;
        durationMonths = Clamp(durationMonths, minMonths, maxMonths);

        float utilization = (float)amount / maxAmount;
        float durationFactor = maxMonths > minMonths ? (float)(durationMonths - minMonths) / (maxMonths - minMonths) : 0f;
        float rate = CalculateInterestRate(utilization, durationFactor);
        int totalOwed = (int)Math.Ceiling(amount * (1f + rate));
        int monthlyPayment = (int)Math.Ceiling((float)totalOwed / durationMonths);
        LoanRiskBand riskBand = CalculateRiskBand(utilization, durationFactor);

        var loan = new ActiveLoan
        {
            principal = amount,
            interestRate = rate,
            totalOwed = totalOwed,
            remainingOwed = totalOwed,
            durationMonths = durationMonths,
            remainingMonths = durationMonths,
            monthlyPayment = monthlyPayment,
            startTick = currentTick,
            utilization = utilization,
            riskBand = riskBand
        };

        _state.SetActiveLoan(loan);

        _financeSystem.RecordTransaction(amount, FinanceCategory.LoanPrincipal, currentTick, "loan");
        _financeSystem.AddRecurringCost("loan-repayment", FinanceCategory.LoanPrincipalPayment, monthlyPayment, RecurringInterval.Monthly, "loan");

        _pendingLoanTakenAmount = amount;
        _pendingLoanTakenRate = rate;
        _pendingEvents.Add(FireLoanTaken);
        _logger.Log($"[LoanSystem] Loan taken: ${amount} at {rate:P0} interest, ${monthlyPayment}/mo for {durationMonths} months, risk: {riskBand}");

        return true;
    }

    // Called after FinanceSystem.ProcessMonthly has already deducted the recurring cost.
    // This only updates the loan state (remainingOwed, remainingMonths).
    public void ProcessMonthlyRepayment()
    {
        if (!_state.hasActiveLoan) return;

        var loan = _state.activeLoan;

        int payment = loan.monthlyPayment;
        if (payment > loan.remainingOwed) payment = loan.remainingOwed;

        loan.remainingOwed -= payment;
        loan.remainingMonths--;
        if (loan.remainingMonths < 0) loan.remainingMonths = 0;

        _pendingRepaymentAmount = payment;

        if (loan.remainingOwed <= 0)
        {
            _state.ClearActiveLoan();
            _financeSystem.RemoveRecurringCost("loan-repayment");
            _pendingEvents.Add(FireLoanFullyRepaidFromRepayment);
            _pendingEvents.Add(FireLoanPaymentMadeFromRepayment);
            _logger.Log("[LoanSystem] Loan fully repaid");
        }
        else
        {
            _state.SetActiveLoan(loan);
            _pendingEvents.Add(FireLoanPaymentMadeFromRepayment);
        }
    }

    public int RepayEarly(int amount, int currentTick)
    {
        if (!_state.hasActiveLoan) return 0;

        var loan = _state.activeLoan;
        int paymentAmount = amount > loan.remainingOwed ? loan.remainingOwed : amount;
        if (paymentAmount <= 0) return 0;

        // Approximate saved interest: ratio of what is being paid off early
        // Interest per remaining month = (totalOwed - principal) / durationMonths
        int totalInterest = loan.totalOwed - loan.principal;
        int interestPerMonth = totalInterest > 0 && loan.durationMonths > 0
            ? (int)Math.Ceiling((float)totalInterest / loan.durationMonths)
            : 0;
        int monthsSaved = interestPerMonth > 0
            ? (int)Math.Ceiling((float)paymentAmount / loan.monthlyPayment)
            : 0;
        int interestAvoided = monthsSaved * interestPerMonth;

        loan.remainingOwed -= paymentAmount;

        bool fullyRepaid = loan.remainingOwed <= 0;

        if (fullyRepaid)
        {
            _state.ClearActiveLoan();
            _financeSystem.RemoveRecurringCost("loan-repayment");
        }
        else
        {
            // Recalculate remaining months, monthly payment stays constant
            loan.remainingMonths = (int)Math.Ceiling((float)loan.remainingOwed / loan.monthlyPayment);
            _state.SetActiveLoan(loan);
        }

        _financeSystem.RecordTransaction(-paymentAmount, FinanceCategory.LoanPrincipalPayment, currentTick, "loan-early-repay");

        _pendingEarlyRepaidAvoided = interestAvoided;
        _pendingEarlyRepaidFull = fullyRepaid;
        _pendingEvents.Add(FireLoanEarlyRepaid);
        if (fullyRepaid)
            _pendingEvents.Add(FireLoanFullyRepaidFromEarly);

        _logger.Log($"[LoanSystem] Early repayment: ${paymentAmount}, interest avoided: ${interestAvoided}, fully repaid: {fullyRepaid}");

        return interestAvoided;
    }

    // ─── ISystem ─────────────────────────────────────────────────────────────────

    public void PreTick(int tick) { }

    public void Tick(int tick) { }

    public void PostTick(int tick)
    {
        int count = _pendingEvents.Count;
        for (int i = 0; i < count; i++)
            _pendingEvents[i]?.Invoke();
        _pendingEvents.Clear();
    }

    public void ApplyCommand(ICommand command)
    {
        if (command is TakeLoanCommand loanCmd)
            TakeLoan(loanCmd.Amount, loanCmd.DurationMonths, command.Tick);
        else if (command is RepayLoanEarlyCommand repayCmd)
            RepayEarly(repayCmd.Amount, command.Tick);
    }

    public void Dispose()
    {
        _pendingEvents.Clear();
        OnLoanTaken = null;
        OnLoanPaymentMade = null;
        OnLoanEarlyRepaid = null;
        OnLoanFullyRepaid = null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private int _pendingLoanTakenAmount;
    private float _pendingLoanTakenRate;
    private int _pendingRepaymentAmount;
    private int _pendingEarlyRepaidAvoided;
    private bool _pendingEarlyRepaidFull;

    private void FireLoanTaken() => OnLoanTaken?.Invoke(_pendingLoanTakenAmount, _pendingLoanTakenRate);
    private void FireLoanPaymentMadeFromRepayment() => OnLoanPaymentMade?.Invoke(_pendingRepaymentAmount);
    private void FireLoanFullyRepaidFromRepayment() => OnLoanFullyRepaid?.Invoke();
    private void FireLoanEarlyRepaid() => OnLoanEarlyRepaid?.Invoke(_pendingEarlyRepaidAvoided);
    private void FireLoanFullyRepaidFromEarly() => OnLoanFullyRepaid?.Invoke();

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
