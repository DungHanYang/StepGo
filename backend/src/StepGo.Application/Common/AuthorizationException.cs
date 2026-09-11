namespace StepGo.Application.Common;

/// <summary>Maps to HTTP 403 at the StepGo.Api.* composition-root boundary.</summary>
public sealed class AuthorizationException(string message) : Exception(message);
