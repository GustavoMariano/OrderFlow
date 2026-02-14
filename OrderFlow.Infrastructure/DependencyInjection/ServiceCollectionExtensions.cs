using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Abstractions.Logging;
using OrderFlow.Application.Abstractions.Messaging;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Abstractions.Security;
using OrderFlow.Infrastructure.Logging.Mongo;
using OrderFlow.Infrastructure.Messaging.RabbitMQ;
using OrderFlow.Infrastructure.Options;
using OrderFlow.Infrastructure.Persistence.Postgres;
using OrderFlow.Infrastructure.Persistence.Repositories;
using OrderFlow.Infrastructure.Security.Jwt;
using OrderFlow.Application.Abstractions.Orders;
using OrderFlow.Infrastructure.Processing;


namespace OrderFlow.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PostgresOptions>(config.GetSection(PostgresOptions.SectionName));
        services.Configure<MongoOptions>(config.GetSection(MongoOptions.SectionName));
        services.Configure<RabbitMqOptions>(config.GetSection(RabbitMqOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        var pg = config.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>()!;
        services.AddDbContext<OrderFlowDbContext>(opt => opt.UseNpgsql(pg.ConnectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

        services.AddSingleton<IProcessingLogWriter, MongoLogWriter>();
        services.AddSingleton<IEventHistoryWriter, MongoLogWriter>();

        services.AddScoped<IOrderProcessor, OrderProcessor>();

        return services;
    }
}
