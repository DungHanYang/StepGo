namespace StepGo.Shared.Application;

/// <summary>Private-bucket access (teacher ID photos). All reads go through short-lived presigned URLs, Admin-only.</summary>
public interface IObjectStorage
{
    Task<string> PutObjectAsync(string objectKey, Stream content, string contentType, CancellationToken ct);
    Task<string> CreatePresignedGetUrlAsync(string objectKey, TimeSpan expiresIn, CancellationToken ct);
}
