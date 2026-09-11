using StepGo.Identity.Domain;

namespace StepGo.RefundTickets.Domain;

/// <summary>A message on the shared thread, visible to both the student and the teacher.</summary>
public sealed record ThreadMessage(Role AuthorRole, Guid AuthorId, string Text, DateTimeOffset CreatedAt);

/// <summary>An admin-only contact log entry (e.g. a phone call), never mixed into the shared thread.</summary>
public sealed record InternalNote(Guid AdminId, string Text, DateTimeOffset CreatedAt);
