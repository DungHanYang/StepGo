namespace StepGo.Courses.Domain;

[Flags]
public enum PaymentMethod
{
    None = 0,
    Atm = 1,
    CreditCard = 2,
    All = Atm | CreditCard,
}
