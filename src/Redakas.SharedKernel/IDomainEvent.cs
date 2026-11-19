namespace Redakas.SharedKernel;

public interface IDomainEvent : INotification
{
    DateTime DateOccurred { get; }
}
