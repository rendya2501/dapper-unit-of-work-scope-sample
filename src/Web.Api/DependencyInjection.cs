using FluentValidation;
using Web.Api.ExceptionHandlers;
using Web.Api.Filters;

namespace Web.Api;

/// <summary>
/// 依存性注入の設定クラス
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Web.Api の依存性注入の設定
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Controllers + ValidationFilter
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        // ===================================================================
        // OpenAPI
        // ===================================================================
        services.AddEndpointsApiExplorer(); // APIエンドポイントの情報を探索可能にする
        services.AddOpenApi();


        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();

        // ===================================================================
        // 例外ハンドラー（順序が重要！）
        // ===================================================================
        services.AddExceptionHandler<ValidationExceptionHandler>();// 特定の例外を先に登録
        services.AddExceptionHandler<GlobalExceptionHandler>();// グローバルハンドラーを最後に登録
        services.AddProblemDetails();


        return services;
    }
}
