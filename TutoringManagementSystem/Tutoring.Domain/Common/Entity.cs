namespace Tutoring.Domain.Common;

public abstract class Entity<TId>
    : IEquatable<Entity<TId>>
    where TId : struct, IDomainId
{
    public TId Id { get; private set; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id.Value != Guid.Empty
            && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> other
            && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
