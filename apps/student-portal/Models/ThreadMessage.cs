namespace StepGo.StudentPortal.Models;

/// <summary>frontend-student-portal spec: "Message Thread 頁...此留言串僅用於退課/仲裁溝通".</summary>
public sealed record ThreadMessage(string Sender, string Text, DateTimeOffset SentAt);
