namespace Tutoring.Domain.Common;

public interface DomainId<TSelf>
	where TSelf : struct, DomainId<TSelf>
{
	Guid Value { get; }

	static abstract TSelf New();
}
