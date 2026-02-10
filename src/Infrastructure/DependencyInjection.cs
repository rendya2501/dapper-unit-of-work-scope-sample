using Application.Common;
using Application.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace Infrastructure;

/// <summary>
/// Infrastructure レイヤーの依存性注入設定
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Web.Api の依存性注入の設定
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        // DB接続
        // 先に IDbConnection を登録することで、後続の DbSession で再利用可能にする
        services.AddScoped<IDbConnection>(sp => new SqliteConnection(connectionString));

        // DbSession（具象クラスを1回だけ登録）
        services.AddScoped<DbSession>();
        // IDbSessionManager (UnitOfWork用)
        services.AddScoped<IDbSessionManager>(sp => sp.GetRequiredService<DbSession>());
        // IDbSession (リポジトリ用)
        services.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
