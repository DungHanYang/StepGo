using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Application.Courses;
using StepGo.Contracts.Courses;
using StepGo.Contracts.Json;
using StepGo.Domain.Courses;
using StepGo.Domain.SharedKernel;

namespace StepGo.Api.Courses;

public static class CoursesRoutes
{
    public static MiniRouter Map(MiniRouter router, CompositionRoot root, StepGoJsonContext contractsJson) => router
        .MapHealthCheck("/courses/health")
        .MapPost("/courses", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.CreateCourseRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");

            var handler = new CreateCourseHandler(root.CourseRepository, root.TeacherProfileRepository, root.PlatformFeeSettingRepository);
            var course = await handler.HandleAsync(new CreateCourseCommand(
                ctx.CurrentUser.UserId, request.Title, Money.FromWholeDollars(request.Price), (PaymentMethod)request.AcceptedPaymentMethods,
                [.. request.RefundRules.Select(t => new RefundTier(t.DaysBeforeCourseStart, t.RefundPercentage))], request.StartsAt), ct);

            return JsonResponses.Created(ToDto(course), contractsJson.CourseDto);
        })
        .MapPost("/courses/{courseId}/publish", async (ctx, ct) =>
        {
            var courseId = Guid.Parse(ctx.PathParameters["courseId"]);
            var handler = new PublishCourseHandler(
                root.CourseRepository, root.TeacherProfileRepository, root.TermsVersionRepository, root.TeacherConsentRepository,
                root.PlatformFeeSettingRepository, root.Clock);

            var course = await handler.HandleAsync(new PublishCourseCommand(courseId, ctx.CurrentUser.UserId), ct);
            return JsonResponses.Ok(ToDto(course), contractsJson.CourseDto);
        })
        .MapGet("/courses/{courseId}", async (ctx, ct) =>
        {
            var courseId = Guid.Parse(ctx.PathParameters["courseId"]);
            var course = await root.CourseRepository.FindAsync(courseId, ct) ?? throw new DomainException("course_not_found", "找不到課程。");
            return JsonResponses.Ok(ToDto(course), contractsJson.CourseDto);
        }, requiresAuth: false)
        .MapGet("/teachers/{teacherId}/courses", async (ctx, ct) =>
        {
            var teacherId = Guid.Parse(ctx.PathParameters["teacherId"]);
            var courses = await root.CourseRepository.ListByTeacherAsync(teacherId, ct);
            return JsonResponses.Ok(new StepGo.Contracts.Common.PagedResultDto<CourseDto>([.. courses.Select(ToDto)], null), contractsJson.PagedResultDtoCourseDto);
        }, requiresAuth: false);

    private static CourseDto ToDto(Course course) => new(
        course.Id, course.TeacherId, course.Title, course.Price.Cents, (PaymentMethodDto)course.AcceptedPaymentMethods,
        [.. course.RefundRules.Tiers.Select(t => new RefundTierDto(t.DaysBeforeCourseStart, t.RefundPercentage))],
        (CourseStatusDto)course.Status, course.StartsAt);
}
