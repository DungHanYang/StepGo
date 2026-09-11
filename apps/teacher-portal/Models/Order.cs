using StepGo.PricingRules;
using StepGo.UI.Status;

namespace StepGo.TeacherPortal.Models;

public sealed record Order(
    string Id,
    string StudentName,
    string CourseId,
    string CourseName,
    decimal Amount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    DateTimeOffset OrderedAt);
