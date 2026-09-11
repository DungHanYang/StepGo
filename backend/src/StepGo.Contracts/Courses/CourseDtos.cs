namespace StepGo.Contracts.Courses;

[Flags]
public enum PaymentMethodDto
{
    None = 0,
    Atm = 1,
    CreditCard = 2,
    All = Atm | CreditCard,
}

public enum CourseStatusDto
{
    Draft,
    Published,
}

public sealed record RefundTierDto(int DaysBeforeCourseStart, decimal RefundPercentage);

public sealed record CreateCourseRequestDto(
    string Title, long Price, PaymentMethodDto AcceptedPaymentMethods, IReadOnlyList<RefundTierDto> RefundRules, DateTimeOffset StartsAt);

public sealed record CourseDto(
    Guid Id, Guid TeacherId, string Title, long Price, PaymentMethodDto AcceptedPaymentMethods,
    IReadOnlyList<RefundTierDto> RefundRules, CourseStatusDto Status, DateTimeOffset StartsAt);
