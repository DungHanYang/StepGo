using StepGo.Shared.Application;
using StepGo.Identity.Application;
using StepGo.Identity.Domain;

namespace StepGo.Application.Tests.Identity;

file sealed class FakeCurrentUser(Guid userId, Role role) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public Role Role { get; } = role;
}

/// <summary>Task 2.4: teachers/students may only touch their own resources; Admin bypasses every check.</summary>
public class RowLevelAccessGuardTests
{
    [Fact]
    public void GuardOwnsTeacherResource_TeacherAccessingAnotherTeachersResource_Throws()
    {
        var teacherA = new FakeCurrentUser(Guid.NewGuid(), Role.Teacher);
        var teacherBResourceId = Guid.NewGuid();

        Assert.Throws<AuthorizationException>(() => RowLevelAccessGuard.GuardOwnsTeacherResource(teacherA, teacherBResourceId));
    }

    [Fact]
    public void GuardOwnsTeacherResource_TeacherAccessingOwnResource_Passes()
    {
        var teacherId = Guid.NewGuid();
        var teacher = new FakeCurrentUser(teacherId, Role.Teacher);

        RowLevelAccessGuard.GuardOwnsTeacherResource(teacher, teacherId); // does not throw
    }

    [Fact]
    public void GuardOwnsTeacherResource_Admin_AlwaysPasses()
    {
        var admin = new FakeCurrentUser(Guid.NewGuid(), Role.Admin);

        RowLevelAccessGuard.GuardOwnsTeacherResource(admin, Guid.NewGuid()); // does not throw
    }

    [Fact]
    public void GuardOwnsStudentResource_StudentAccessingAnotherStudentsResource_Throws()
    {
        var studentA = new FakeCurrentUser(Guid.NewGuid(), Role.Student);

        Assert.Throws<AuthorizationException>(() => RowLevelAccessGuard.GuardOwnsStudentResource(studentA, Guid.NewGuid()));
    }

    [Fact]
    public void GuardOwnsStudentResource_TeacherRoleNeverPasses()
    {
        var teacher = new FakeCurrentUser(Guid.NewGuid(), Role.Teacher);

        Assert.Throws<AuthorizationException>(() => RowLevelAccessGuard.GuardOwnsStudentResource(teacher, teacher.UserId));
    }

    [Fact]
    public void GuardIsAdmin_NonAdmin_Throws()
    {
        var teacher = new FakeCurrentUser(Guid.NewGuid(), Role.Teacher);

        Assert.Throws<AuthorizationException>(() => RowLevelAccessGuard.GuardIsAdmin(teacher));
    }
}
