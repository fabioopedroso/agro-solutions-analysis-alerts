namespace Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("RabbitMQ:Host não configurado");

        if (string.IsNullOrWhiteSpace(Username))
            throw new InvalidOperationException("RabbitMQ:Username não configurado");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("RabbitMQ:Password não configurado");

        if (string.IsNullOrWhiteSpace(QueueName))
            throw new InvalidOperationException("RabbitMQ:QueueName não configurado");

        if (Port <= 0)
            throw new InvalidOperationException("RabbitMQ:Port deve ser maior que zero");
    }
}
