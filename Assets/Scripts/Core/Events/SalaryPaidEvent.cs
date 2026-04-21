public class SalaryPaidEvent : GameEvent
{
    public int TotalAmount { get; }
    public int CashAfterPayment { get; }

    public SalaryPaidEvent(int tick, int totalAmount, int cashAfterPayment) : base(tick)
    {
        TotalAmount = totalAmount;
        CashAfterPayment = cashAfterPayment;
    }
}
