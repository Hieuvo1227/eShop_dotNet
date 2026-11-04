namespace eShop.Domain.Common;

/// <summary>
/// Lớp cơ sở cho mọi Entity trong Domain.
/// Chứa Id định danh và danh sách Domain Events nội bộ của Entity.
/// </summary>
public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public abstract class BaseEntity : BaseEntity<int>
{
}

