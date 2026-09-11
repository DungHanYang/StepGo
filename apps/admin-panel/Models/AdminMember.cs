namespace StepGo.AdminPanel.Models;

/// <summary>frontend-admin-panel spec: "帳號與權限管理維持 MVP 單一角色呈現" — only one
/// assignable role exists today; the role matrix layout still reserves extra columns for
/// Phase 2 roles without rendering any selectable option for them.</summary>
public enum AdminRole
{
    Admin,
}

public sealed record AdminMember(string Name, string Email, AdminRole Role);
