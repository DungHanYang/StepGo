using Amazon.S3;
using Amazon.S3.Model;
using StepGo.Shared.Application;

namespace StepGo.Shared.Infrastructure;

/// <summary>Teacher ID-photo storage. The bucket has no public access; every read goes through a short-lived presigned URL (design.md decision 6).</summary>
public sealed class S3ObjectStorage(IAmazonS3 client, string bucketName) : IObjectStorage
{
    public async Task<string> PutObjectAsync(string objectKey, Stream content, string contentType, CancellationToken ct)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
        }, ct);

        return objectKey;
    }

    public Task<string> CreatePresignedGetUrlAsync(string objectKey, TimeSpan expiresIn, CancellationToken ct) => Task.FromResult(
        client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET,
        }));
}
