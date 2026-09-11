using StepGo.Courses.Domain;

namespace StepGo.Courses.Application;

public interface ICourseRepository
{
    Task<Course?> FindAsync(Guid courseId, CancellationToken ct);
    Task<IReadOnlyList<Course>> ListByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task SaveAsync(Course course, CancellationToken ct);
}
