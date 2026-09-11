using StepGo.Identity.Domain;

namespace StepGo.Identity.Application;

public interface ITeacherProfileRepository
{
    Task<TeacherProfile?> FindAsync(Guid teacherId, CancellationToken ct);
    Task SaveAsync(TeacherProfile teacherProfile, CancellationToken ct);
}
