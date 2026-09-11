namespace StepGo.TeacherPortal.Models;

public sealed record PayoutBatch(DateOnly BatchDate, int EligibleOrderCount, decimal TotalAmount);
