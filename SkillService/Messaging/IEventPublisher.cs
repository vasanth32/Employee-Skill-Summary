using SharedModels.Events;

namespace SkillService.Messaging;

public interface IEventPublisher
{
    Task PublishSkillRatedAsync(SkillRatedEvent message, CancellationToken cancellationToken);
}

