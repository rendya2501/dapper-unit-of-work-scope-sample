using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<AuditLogService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<OrderService>();

        return services;
    }
}