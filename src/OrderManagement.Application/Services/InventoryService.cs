using OrderManagement.Application.Common;
using OrderManagement.Application.Repositories;
using OrderManagement.Application.Services.Abstractions;
using OrderManagement.Domain.Common.Results;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Services;

/// <summary>
/// 在庫サービスの実装
/// </summary>
/// <remarks>
/// 在庫管理のビジネスロジックを実装します。
/// </remarks>
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IAuditLogRepository auditLog) : IInventoryService
{
    /// <inheritdoc />
    public async Task<OperationResult<IEnumerable<Inventory>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var inventories = await inventory.GetAllAsync(cancellationToken);
        return Outcome.Success(inventories);
    }

    /// <inheritdoc />
    public async Task<OperationResult<Inventory>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventoryEntity = await inventory.GetByProductIdAsync(productId, cancellationToken);
        if (inventoryEntity is null)
        {
            return Outcome.NotFound($"Inventory not found for productId: {productId}");
        }
        return Outcome.Success(inventoryEntity);
    }

    /// <inheritdoc />
    public async Task<OperationResult<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var productId = await inventory.CreateAsync(new Inventory
            {
                ProductName = productName,
                Stock = stock,
                UnitPrice = unitPrice
            }, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Outcome.Success(productId);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OperationResult> UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var existing = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (existing is null)
            {
                return Outcome.NotFound($"Product not found for productId: {productId}");
            }

            await inventory.UpdateAsync(productId, productName, stock, unitPrice, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_UPDATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Outcome.Success();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OperationResult> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var existing = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (existing is null)
            {
                return Outcome.NotFound($"Product not found for productId: {productId}");
            }

            await inventory.DeleteAsync(productId, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_DELETED",
                Details = $"ProductId={productId}, Name={existing.ProductName}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Outcome.Success();
        }, cancellationToken);
    }
}
