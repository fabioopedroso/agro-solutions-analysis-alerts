using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ConnectionString")));

        // Repositories
        services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        // Application Services
        services.AddScoped<ISensorAnalysisService, SensorAnalysisService>();

        // RabbitMQ Settings
        services.Configure<RabbitMqSettings>(options =>
        {
            options.Host = configuration["RabbitMQ:Host"] ?? string.Empty;
            options.Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672;
            options.Username = configuration["RabbitMQ:Username"] ?? string.Empty;
            options.Password = configuration["RabbitMQ:Password"] ?? string.Empty;
            options.QueueName = configuration["RabbitMQ:QueueName"] ?? string.Empty;
        });

        // RabbitMQ Consumer
        services.AddHostedService<RabbitMqConsumer>();

        return services;
    }
}
