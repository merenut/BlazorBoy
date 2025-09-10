using System;

namespace GameBoy.Core.Debug;

/// <summary>
/// Represents a conditional breakpoint with an expression that must evaluate to true.
/// </summary>
public sealed class ConditionalBreakpoint
{
    public Guid Id { get; }
    public string Expression { get; }
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public DateTime Created { get; }
    public int HitCount { get; set; } = 0;

    public ConditionalBreakpoint(string expression, string description = "")
    {
        Id = Guid.NewGuid();
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Description = description;
        Created = DateTime.UtcNow;
    }

    public override string ToString()
    {
        var status = Enabled ? "" : " (DISABLED)";
        var hits = HitCount > 0 ? $" [hits: {HitCount}]" : "";
        var desc = string.IsNullOrEmpty(Description) ? "" : $" - {Description}";

        return $"COND: {Expression}{status}{hits}{desc}";
    }
}