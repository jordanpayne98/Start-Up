public interface ILoanReadModel
{
    bool CanTakeLoan();
    bool HasActiveLoan { get; }
    int GetMaxLoanAmount();
    int GetTotalMonthlyRepayment();
    int GetTotalRemainingDebt();
    ActiveLoan? GetActiveLoan();
    LoanTermsPreview PreviewLoan(int amount, int durationMonths);

    // Credit score
    int GetCreditScore();
    CreditTier GetCreditTier();
}
