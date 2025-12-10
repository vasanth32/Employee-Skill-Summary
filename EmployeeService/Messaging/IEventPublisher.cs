using SharedModels.Events;

namespace EmployeeService.Messaging;

public interface IEventPublisher
{
    Task PublishEmployeeCreatedAsync(EmployeeCreatedEvent message, CancellationToken cancellationToken);
}

