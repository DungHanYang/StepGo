namespace StepGo.ApiClient.Dtos;

/// <summary>frontend-teacher-portal spec: "下次撥款狀態卡片"; frontend-admin-panel spec:
/// "撥款佇列顯示待處理批次".</summary>
public sealed record PayoutBatchDto(DateOnly BatchDate, int EligibleOrderCount, decimal TotalAmount);
