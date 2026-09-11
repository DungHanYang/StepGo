using StepGo.Application.Common;
using StepGo.Domain.Identity;

namespace StepGo.Application.Identity;

/// <summary>
/// Task 2.4: row-level authorization. Teachers/students may only act on resources scoped to their own
/// id; Admin bypasses every check. Every Application handler that reads or mutates a teacher/student-owned
/// aggregate calls one of these guards before touching repository results.
/// </summary>
public static class RowLevelAccessGuard
{
    public static void GuardOwnsTeacherResource(ICurrentUserAccessor currentUser, Guid resourceTeacherId)
    {
        if (currentUser.Role == Role.Admin)
        {
            return;
        }

        if (currentUser.Role != Role.Teacher || currentUser.UserId != resourceTeacherId)
        {
            throw new AuthorizationException("無權存取其他老師的資料。");
        }
    }

    public static void GuardOwnsStudentResource(ICurrentUserAccessor currentUser, Guid resourceStudentId)
    {
        if (currentUser.Role == Role.Admin)
        {
            return;
        }

        if (currentUser.Role != Role.Student || currentUser.UserId != resourceStudentId)
        {
            throw new AuthorizationException("無權存取其他學生的資料。");
        }
    }

    public static void GuardIsAdmin(ICurrentUserAccessor currentUser)
    {
        if (currentUser.Role != Role.Admin)
        {
            throw new AuthorizationException("僅管理者可執行此操作。");
        }
    }
}
