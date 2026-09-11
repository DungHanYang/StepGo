using StepGo.UI.Status;

namespace StepGo.AdminPanel.Models;

/// <summary>frontend-admin-panel spec: "老師與課程的合併目錄列表".</summary>
public sealed record DirectoryEntry(
    string TeacherName,
    VerificationStatus VerificationStatus,
    string CourseName,
    string CourseStatus);
