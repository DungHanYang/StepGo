namespace StepGo.Domain.Orders;

/// <summary>The `ChoosePayment` parameter sent to the payment gateway (綠界/藍新), derived from the course's accepted methods.</summary>
public enum ChoosePayment
{
    Atm,
    Credit,
    All,
}
