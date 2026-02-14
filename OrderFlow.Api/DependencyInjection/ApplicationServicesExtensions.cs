using OrderFlow.Application.UseCases.Auth;
using OrderFlow.Application.UseCases.Orders;

namespace OrderFlow.Api.DependencyInjection;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();

        services.AddScoped<CreateOrderUseCase>();
        services.AddScoped<GetOrdersUseCase>();
        services.AddScoped<GetOrderByIdUseCase>();

        return services;
    }
}
