using StepGo.ApiClient.Dtos;
using StepGo.UI.Status;

namespace StepGo.MockApi.Scenarios;

/// <summary>Refund ticket multi-stage flow — frontend-teacher-portal spec: "退課工單審核與金額微調限制";
/// frontend-student-portal spec: "退課申請與正式留言串".</summary>
public static class RefundTicketScenarios
{
    public static readonly IReadOnlyDictionary<string, RefundTicketDto> All = new Dictionary<string, RefundTicketDto>(StringComparer.OrdinalIgnoreCase)
    {
        ["pendingteacherreview"] = new(
            "rt-mock-1", "o-mock-1", "李小華", "兒童繪畫班", 3200m, 3200m,
            RefundTicketStatus.PendingTeacherReview, DateTimeOffset.UtcNow.AddDays(4), "臨時有事無法上課", null, null),

        ["teacherapproved"] = new(
            "rt-mock-2", "o-mock-2", "陳小美", "親子瑜伽", 2400m, 1200m,
            RefundTicketStatus.TeacherApproved, DateTimeOffset.UtcNow.AddDays(-1), "課程時間衝突", 1200m, null),

        ["teacherrejected"] = new(
            "rt-mock-3", "o-mock-3", "張小芳", "程式設計入門", 4500m, 0m,
            RefundTicketStatus.TeacherRejected, DateTimeOffset.UtcNow.AddDays(-1), "已開課超過一半課程", null, "課程已進行過半，依規則不予退費"),

        ["autoapproved"] = new(
            "rt-mock-4", "o-mock-4", "林小強", "親子烘焙", 1800m, 1800m,
            RefundTicketStatus.AutoApproved, DateTimeOffset.UtcNow.AddDays(-6), "老師逾期未回覆", null, null),

        ["escalated"] = new(
            "rt-mock-5", "o-mock-5", "王小美", "兒童鋼琴啟蒙", 5200m, 2600m,
            RefundTicketStatus.Escalated, DateTimeOffset.UtcNow.AddDays(-2), "雙方對退費金額有爭議", null, null),

        ["arbitrationresolved"] = new(
            "rt-mock-6", "o-mock-6", "黃小華", "兒童鋼琴啟蒙", 5200m, 2600m,
            RefundTicketStatus.ArbitrationResolved, DateTimeOffset.UtcNow.AddDays(-3), "雙方對退費金額有爭議", 3900m, "平台仲裁：核准 75% 退款"),

        ["completed"] = new(
            "rt-mock-7", "o-mock-7", "周小明", "程式設計入門", 4500m, 4500m,
            RefundTicketStatus.Completed, DateTimeOffset.UtcNow.AddDays(-10), "臨時有事無法上課", null, null),
    };
}
