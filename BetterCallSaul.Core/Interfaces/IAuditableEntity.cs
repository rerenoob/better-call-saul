namespace BetterCallSaul.Core.Interfaces;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}