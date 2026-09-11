namespace StepGo.StudentPortal.Models;

/// <summary>frontend-student-portal spec: "填資料選付款」步驟顯示的付款方式選項 SHALL 僅包含
/// 該課程老師設定開放的付款方式".</summary>
public sealed record CheckoutCourse(string Id, string Name, string TeacherName, decimal Price, bool AllowCreditCard, bool AllowAtm);
