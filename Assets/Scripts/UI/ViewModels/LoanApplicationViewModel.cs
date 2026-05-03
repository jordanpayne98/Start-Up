using System;

public class LoanApplicationViewModel : IViewModel
{
    public bool CanTakeLoan { get; private set; }
    public int MaxAmount { get; private set; }
    public string MaxAmountDisplay { get; private set; }
    public string CurrentDebtDisplay { get; private set; }
    public int DaysInDebt { get; private set; }

    // Duration selection
    public int MinDuration => 1;
    public int MaxDuration => 12;
    public int SelectedDuration { get; private set; } = 3;

    // Preview
    public int PreviewAmount { get; private set; }
    public string PreviewTotalOwedDisplay { get; private set; }
    public string PreviewMonthlyPaymentDisplay { get; private set; }
    public string PreviewInterestRateDisplay { get; private set; }
    public string PreviewInterestCostDisplay { get; private set; }
    public string PreviewRiskBandDisplay { get; private set; }
    public string PreviewRiskBandClass { get; private set; }
    public string PreviewUtilizationDisplay { get; private set; }

    private ILoanReadModel _loanReadModel;

    public event Action<int, int> OnTakeLoan;  // amount, durationMonths
    public event Action OnDismiss;

    public LoanApplicationViewModel() {
        MaxAmountDisplay = "$0";
        CurrentDebtDisplay = "$0";
        PreviewTotalOwedDisplay = "$0";
        PreviewMonthlyPaymentDisplay = "$0";
        PreviewInterestRateDisplay = "0%";
        PreviewInterestCostDisplay = "$0";
        PreviewRiskBandDisplay = "Safe";
        PreviewRiskBandClass = "risk-safe";
        PreviewUtilizationDisplay = "0%";
    }

    // Called from LoanApplicationView.Bind to give access to live preview
    public void SetLoanReadModel(ILoanReadModel loanReadModel)
    {
        _loanReadModel = loanReadModel;
    }

    public bool IsDirty { get; private set; }
    public void ClearDirty() => IsDirty = false;

    public void Refresh(GameStateSnapshot snapshot) {
        IReadOnlyGameState state = snapshot;
        if (state == null) return;

        CanTakeLoan = state.CanTakeLoan;
        MaxAmount = state.MaxLoanAmount;
        MaxAmountDisplay = UIFormatting.FormatMoney(state.MaxLoanAmount);
        CurrentDebtDisplay = UIFormatting.FormatMoney(state.TotalLoanDebt);
        DaysInDebt = state.DaysInDebt;
    }

    public void SetDuration(int months)
    {
        if (months < MinDuration) months = MinDuration;
        if (months > MaxDuration) months = MaxDuration;
        SelectedDuration = months;
    }

    public void PreviewLoan(int amount, int durationMonths)
    {
        if (amount <= 0 || MaxAmount <= 0) return;
        PreviewAmount = amount;

        if (_loanReadModel != null)
        {
            var preview = _loanReadModel.PreviewLoan(amount, durationMonths);
            PreviewInterestRateDisplay = UIFormatting.FormatPercent(preview.interestRate);
            PreviewTotalOwedDisplay = UIFormatting.FormatMoney(preview.totalOwed);
            PreviewMonthlyPaymentDisplay = UIFormatting.FormatMoney(preview.monthlyPayment) + "/mo";
            PreviewInterestCostDisplay = UIFormatting.FormatMoney(preview.interestCost);
            PreviewRiskBandDisplay = preview.riskBand.ToString();
            PreviewRiskBandClass = RiskBandToClass(preview.riskBand);
            PreviewUtilizationDisplay = UIFormatting.FormatPercent(preview.utilization);
        }
        else
        {
            // Fallback estimate
            float rate = 0.10f;
            int totalOwed = (int)Math.Ceiling(amount * (1f + rate));
            int monthlyPayment = durationMonths > 0 ? (int)Math.Ceiling((float)totalOwed / durationMonths) : totalOwed;
            PreviewInterestRateDisplay = UIFormatting.FormatPercent(rate);
            PreviewTotalOwedDisplay = UIFormatting.FormatMoney(totalOwed);
            PreviewMonthlyPaymentDisplay = UIFormatting.FormatMoney(monthlyPayment) + "/mo";
            PreviewInterestCostDisplay = UIFormatting.FormatMoney(totalOwed - amount);
            PreviewRiskBandDisplay = "Standard";
            PreviewRiskBandClass = "risk-standard";
            PreviewUtilizationDisplay = MaxAmount > 0 ? UIFormatting.FormatPercent((float)amount / MaxAmount) : "0%";
        }
    }

    public void SubmitLoan(int amount, int durationMonths) {
        OnTakeLoan?.Invoke(amount, durationMonths);
    }

    public void RequestDismiss() {
        OnDismiss?.Invoke();
    }

    private static string RiskBandToClass(LoanRiskBand band)
    {
        switch (band)
        {
            case LoanRiskBand.Safe:       return "risk-safe";
            case LoanRiskBand.Standard:   return "risk-standard";
            case LoanRiskBand.Aggressive: return "risk-aggressive";
            case LoanRiskBand.Extreme:    return "risk-extreme";
            default:                      return "risk-safe";
        }
    }
}
