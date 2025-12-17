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

public class EmployeeCreatedEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmployeeCreatedEventConsumer> _logger;
    private readonly string _exchange;
    private readonly string _queueName;

    public EmployeeCreatedEventConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<EmployeeCreatedEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var rabbitSection = configuration.GetSection("RabbitMQ");
        _exchange = rabbitSection["Exchange"] ?? "employee.events";
        var exchangeType = rabbitSection["ExchangeType"] ?? "fanout";

        var factory = new ConnectionFactory
        {
            HostName = rabbitSection["HostName"] ?? "localhost",
            UserName = rabbitSection["UserName"] ?? "guest",
            Password = rabbitSection["Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _exchange, type: exchangeType, durable: true, autoDelete: false);

        _queueName = _channel.QueueDeclare("searchservice_employee_created", durable: true, exclusive: false, autoDelete: false).QueueName;
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
                var employeeCreatedEvent = JsonSerializer.Deserialize<EmployeeCreatedEvent>(message);
                if (employeeCreatedEvent != null)
                {
                    await HandleEmployeeCreatedAsync(employeeCreatedEvent, stoppingToken);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EmployeeCreatedEvent: {Message}", message);
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("EmployeeCreatedEvent consumer started. Waiting for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleEmployeeCreatedAsync(EmployeeCreatedEvent @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmployeeSummaryRepository>();

        _logger.LogInformation("Received EmployeeCreatedEvent: EmployeeId={EmployeeId}, Name={Name}, Role={Role}",
            @event.EmployeeId, @event.Name, @event.Role);

        var existing = await repository.GetByEmployeeIdAsync(@event.EmployeeId, cancellationToken);
        if (existing != null)
        {
            // Update existing employee summary
            existing.Name = @event.Name;
            existing.Role = @event.Role;
            await repository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            // Insert new employee summary
            var employeeSummary = new EmployeeSummary
            {
                SummaryId = Guid.NewGuid(),
                EmployeeId = @event.EmployeeId,
                Name = @event.Name,
                Role = @event.Role
            };
            await repository.AddAsync(employeeSummary, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stored employee summary: EmployeeId={EmployeeId}, Name={Name}", 
            @event.EmployeeId, @event.Name);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

