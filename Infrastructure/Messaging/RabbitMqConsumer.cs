using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _settings.Validate();
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Consumer iniciando...");
        InitializeRabbitMqConnection();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer executando e aguardando mensagens na fila: {QueueName}", _settings.QueueName);

        stoppingToken.Register(() => _logger.LogInformation("RabbitMQ Consumer está parando..."));

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Mensagem recebida da fila: {Message}", message);

                var sensorData = JsonSerializer.Deserialize<SensorDataDto>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sensorData != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var sensorAnalysisService = scope.ServiceProvider.GetRequiredService<ISensorAnalysisService>();

                    await sensorAnalysisService.ProcessSensorReadingAsync(sensorData);

                    _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Mensagem processada e confirmada (ACK)");
                }
                else
                {
                    _logger.LogWarning("Falha ao deserializar mensagem, enviando NACK");
                    _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem da fila");
                _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel?.BasicConsume(queue: _settings.QueueName, autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private void InitializeRabbitMqConnection()
    {
        try
        {
            _logger.LogInformation("Conectando ao RabbitMQ em {Host}:{Port}", _settings.Host, _settings.Port);

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar a fila (garante que existe)
            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Configurar QoS (processar 1 mensagem por vez)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("Conectado ao RabbitMQ com sucesso. Fila: {QueueName}", _settings.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar ao RabbitMQ");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Consumer parando...");

        try
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("Conexão com RabbitMQ fechada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fechar conexão com RabbitMQ");
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
