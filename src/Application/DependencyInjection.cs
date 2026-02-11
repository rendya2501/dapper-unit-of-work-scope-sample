using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>
/// Application レイヤーの依存性注入設定
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Application レイヤーの依存性を登録します。
    /// </summary>
    /// <remarks>
    /// <para>以下のサービスを Scoped ライフタイムで登録します。</para>
    /// <list type="bullet">
    /// <item><see cref="AuditLogService"/> - 監査ログのビジネスロジック</item>
    /// <item><see cref="InventoryService"/> - 在庫管理のビジネスロジック</item>
    /// <item><see cref="OrderService"/> - 注文管理のビジネスロジック</item>
    /// </list>
    /// </remarks>
    /// <param name="services">サービスコレクション</param>
    /// <returns>メソッドチェーン用の <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<AuditLogService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<OrderService>();

        return services;
    }
}
