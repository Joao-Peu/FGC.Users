using FGC.Users.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FGC.Users.Infrastructure.EventPublishers;

public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string topic, object @event, string correlationId)
    {
        _logger.LogInformation("[InMemoryEvent] Topic={Topic} CorrelationId={Cid} Event={Event}", topic, correlationId, JsonSerializer.Serialize(@event));
        return Task.CompletedTask;
    }
}
