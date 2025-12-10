using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SharedModels.Events;

namespace EmployeeService.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;
    private readonly string _exchange;

    public RabbitMqEventPublisher(IConfiguration configuration)
    {
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
    }

    public Task PublishEmployeeCreatedAsync(EmployeeCreatedEvent message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange: _exchange, routingKey: string.Empty, basicProperties: null, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

