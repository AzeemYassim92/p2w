namespace P2W.DealFinder.Domain.Shared;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class AuditedEntity : Entity
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
