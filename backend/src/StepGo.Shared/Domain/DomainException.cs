namespace StepGo.Shared.Domain;

/// <summary>Thrown when an operation would violate a domain invariant (business rule rejection).</summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
