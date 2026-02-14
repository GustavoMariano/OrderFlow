using OrderFlow.Infrastructure.DependencyInjection;
using OrderFlow.Worker.Consumers;
using OrderFlow.Worker.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton<OrderCreatedConsumer>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService>();

var host = builder.Build();
host.Run();
