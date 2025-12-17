using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SearchService.Models;
using SearchService.Repositories;
using SharedModels.Events;

namespace SearchService.Messaging;

public class SkillRatedEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkillRatedEventConsumer> _logger;
    private readonly string _exchange;
    private readonly string _queueName;

    public SkillRatedEventConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<SkillRatedEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var rabbitSection = configuration.GetSection("RabbitMQ");
        _exchange = rabbitSection["SkillExchange"] ?? "skill.events";
        var exchangeType = rabbitSection["SkillExchangeType"] ?? "fanout";

        var factory = new ConnectionFactory
        {
            HostName = rabbitSection["HostName"] ?? "localhost",
            UserName = rabbitSection["UserName"] ?? "guest",
            Password = rabbitSection["Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _exchange, type: exchangeType, durable: true, autoDelete: false);

        _queueName = _channel.QueueDeclare("searchservice_skill_rated", durable: true, exclusive: false, autoDelete: false).QueueName;
        _channel.QueueBind(queue: _queueName, exchange: _exchange, routingKey: string.Empty);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var skillRatedEvent = JsonSerializer.Deserialize<SkillRatedEvent>(message);
                if (skillRatedEvent != null)
                {
                    await HandleSkillRatedAsync(skillRatedEvent, stoppingToken);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SkillRatedEvent: {Message}", message);
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("SkillRatedEvent consumer started. Waiting for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleSkillRatedAsync(SkillRatedEvent @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var summaryRepository = scope.ServiceProvider.GetRequiredService<IEmployeeSummaryRepository>();
        var skillRepository = scope.ServiceProvider.GetRequiredService<IEmployeeSummarySkillRepository>();

        _logger.LogInformation("Received SkillRatedEvent: EmployeeId={EmployeeId}, SkillName={SkillName}, Rating={Rating}",
            @event.EmployeeId, @event.SkillName, @event.Rating);

        // Get or create employee summary
        var employeeSummary = await summaryRepository.GetByEmployeeIdAsync(@event.EmployeeId, cancellationToken);
        if (employeeSummary == null)
        {
            _logger.LogWarning("EmployeeSummary not found for EmployeeId={EmployeeId}. Creating placeholder.", @event.EmployeeId);
            // Create a placeholder employee summary if it doesn't exist
            // This can happen if SkillRatedEvent arrives before EmployeeCreatedEvent
            employeeSummary = new EmployeeSummary
            {
                SummaryId = Guid.NewGuid(),
                EmployeeId = @event.EmployeeId,
                Name = "Unknown", // Will be updated when EmployeeCreatedEvent arrives
                Role = "Unknown"
            };
            await summaryRepository.AddAsync(employeeSummary, cancellationToken);
            await summaryRepository.SaveChangesAsync(cancellationToken);
        }

        // Check if skill already exists for this employee
        var existingSkill = await skillRepository.GetBySummaryIdAndSkillNameAsync(
            employeeSummary.SummaryId, @event.SkillName, cancellationToken);

        if (existingSkill != null)
        {
            // Update existing rating
            existingSkill.Rating = @event.Rating;
            await skillRepository.UpdateAsync(existingSkill, cancellationToken);
            _logger.LogInformation("Updated skill rating: EmployeeId={EmployeeId}, SkillName={SkillName}, Rating={Rating}",
                @event.EmployeeId, @event.SkillName, @event.Rating);
        }
        else
        {
            // Insert new skill entry
            var skill = new EmployeeSummarySkill
            {
                SummarySkillId = Guid.NewGuid(),
                SummaryId = employeeSummary.SummaryId,
                SkillName = @event.SkillName,
                Rating = @event.Rating
            };
            await skillRepository.AddAsync(skill, cancellationToken);
            _logger.LogInformation("Inserted new skill: EmployeeId={EmployeeId}, SkillName={SkillName}, Rating={Rating}",
                @event.EmployeeId, @event.SkillName, @event.Rating);
        }

        await skillRepository.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

