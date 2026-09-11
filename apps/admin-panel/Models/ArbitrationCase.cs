namespace StepGo.AdminPanel.Models;

public enum ArbitrationRuling
{
    FullRefund,
    PartialRefund,
    UpholdRejection,
}

public sealed record ArbitrationStatement(string Author, string Text);

/// <summary>Mutable — a ruling updates it in place inside <see cref="Services.AdminDataStore"/>.</summary>
public sealed class ArbitrationCase
{
    public required string Id { get; init; }
    public required string StudentName { get; init; }
    public required string TeacherName { get; init; }
    public required string CourseName { get; init; }
    public required decimal DisputedAmount { get; init; }
    public required IReadOnlyList<ArbitrationStatement> Statements { get; init; }

    public bool IsResolved { get; set; }
    public ArbitrationRuling? Ruling { get; set; }
    public string? RulingReason { get; set; }
}
