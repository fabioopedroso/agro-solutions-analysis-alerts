using Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Adicionar Infrastructure (DbContext, Repositories, Services, RabbitMQ Consumer)
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
