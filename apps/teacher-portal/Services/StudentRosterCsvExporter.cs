using System.Text;
using StepGo.TeacherPortal.Models;

namespace StepGo.TeacherPortal.Services;

/// <summary>frontend-teacher-portal spec: "提供 CSV/Excel 匯出功能且不得向老師收費" — pure
/// string-building so it's testable without a browser download interaction.</summary>
public static class StudentRosterCsvExporter
{
    public const string Header = "學生姓名,課程名稱,金額,付款狀態,訂單時間";

    public static string ToCsv(IEnumerable<Order> orders)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var order in orders)
        {
            sb.AppendLine($"{order.StudentName},{order.CourseName},{order.Amount},{order.PaymentStatus},{order.OrderedAt:yyyy-MM-dd}");
        }

        return sb.ToString();
    }
}
