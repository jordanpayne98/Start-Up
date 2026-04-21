using System;
using System.Collections.Generic;

public enum LoanRiskBand
{
    Safe,
    Standard,
    Aggressive,
    Extreme
}

public enum CreditTier
{
    Poor,       // 0–29   — limited borrowing, high rates
    Fair,       // 30–54  — below average
    Good,       // 55–74  — standard terms
    Excellent   // 75–100 — best rates, highest cap
}

[Serializable]
public struct ActiveLoan
{
    public int principal;
    public float interestRate;
    public int totalOwed;
    public int remainingOwed;
    public int durationMonths;
    public int remainingMonths;
    public int monthlyPayment;
    public int startTick;
    public float utilization;
    public LoanRiskBand riskBand;
}

public struct LoanTermsPreview
{
    public float interestRate;
    public int totalOwed;
    public int monthlyPayment;
    public int interestCost;
    public LoanRiskBand riskBand;
    public float utilization;
}

[Serializable]
public class LoanState
{
    public bool hasActiveLoan;
    public ActiveLoan activeLoan;
    public int completedLoanCount;

    public ActiveLoan? ActiveLoan => hasActiveLoan ? (ActiveLoan?)activeLoan : null;

    public void SetActiveLoan(ActiveLoan loan)
    {
        activeLoan = loan;
        hasActiveLoan = true;
    }

    public void ClearActiveLoan()
    {
        hasActiveLoan = false;
        activeLoan = default;
        completedLoanCount++;
    }

    public static LoanState CreateNew()
    {
        return new LoanState
        {
            hasActiveLoan = false,
            activeLoan = default,
            completedLoanCount = 0
        };
    }
}
