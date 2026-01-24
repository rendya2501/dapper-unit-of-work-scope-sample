# Dapper Unit of Work Modern Sample

**Result型ベースの自動トランザクション管理による、モダンで安全なDapperアプリケーション設計**

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Dapper](https://img.shields.io/badge/Dapper-2.1-orange)](https://github.com/DapperLib/Dapper)
[![SQLite](https://img.shields.io/badge/SQLite-3-green)](https://www.sqlite.org/)

---

## 🎯 このプロジェクトについて

Dapperを使用した**実務で即採用可能な**Unit of Workパターンの実装サンプルです。

### 主な特徴

- ✅ **Result型による自動トランザクション制御** - 成功/失敗を型で表現し、Commit/Rollbackを自動化
- ✅ **完全な接続管理** - UnitOfWorkが接続のライフサイクル全体を責任管理
- ✅ **2重トランザクション検出** - 設計違反を実行時に即座に検出
- ✅ **クリーンアーキテクチャ** - 層間の責務を明確に分離
- ✅ **包括的なエラーハンドリング** - ビジネスエラーから技術的エラーまで統一的に処理

### なぜこのサンプルを作ったのか

従来のDapper実装では以下の問題が頻発します：

❌ **トランザクション管理の問題**
- Commit/Rollbackの書き忘れ
- 例外時のRollback漏れ
- 複数サービス呼び出しでのトランザクション重複

❌ **エラーハンドリングの問題**
- 例外とビジネスエラーの混在
- 404/400/409の判断が曖昧
- フロントエンドでのエラー処理の複雑化

本プロジェクトは**Result型とUnitOfWorkパターン**を組み合わせることで、これらの問題を構造的に解決します。

---

## 📦 採用パターン：Result-Driven UoW

このプロジェクトでは**Result型ベースのUnit of Work**を採用しています。

```csharp
// サービス層：ビジネスロジックに集中
public async Task<OperationResult<int>> CreateOrderAsync(
    int customerId, 
    List<OrderItem> items,
    CancellationToken cancellationToken)
{
    return await _uow.ExecuteInTransactionAsync(async () =>
    {
        // ビジネスバリデーション
        if (items.Count == 0)
            return Outcome.BusinessRule(
                BusinessErrorCode.EmptyOrder.ToErrorCode(),
                "Order must have at least one item.");

        // 在庫確認
        var inventory = await _inventory.GetByProductIdAsync(productId);
        if (inventory is null)
            return Outcome.NotFound($"Product {productId} not found");
        
        if (inventory.Stock < quantity)
            return Outcome.BusinessRule(
                BusinessErrorCode.InsufficientStock.ToErrorCode(),
                $"Available: {inventory.Stock}, Requested: {quantity}");

        // 在庫減算
        await _inventory.UpdateStockAsync(productId, inventory.Stock - quantity);

        // 注文作成
        var orderId = await _order.CreateAsync(order);

        // 監査ログ
        await _auditLog.CreateAsync(new AuditLog { /* ... */ });

        // ✅ 成功を返す → UoWが自動Commit
        return Outcome.Success(orderId);
        
        // ❌ エラーを返す → UoWが自動Rollback
        // 例外発生 → UoWが自動Rollback + 例外再スロー
    }, cancellationToken);
}
```

### なぜこの設計が優れているのか

#### 1. **トランザクション制御の自動化**

```csharp
// ❌ 従来の方法：手動管理が必要
await using var uow = _unitOfWorkFactory();
uow.BeginTransaction();
try
{
    await uow.Orders.CreateAsync(order);
    await uow.Inventory.UpdateStockAsync(productId, newStock);
    await uow.CommitAsync();  // ← 書き忘れリスク
}
catch
{
    await uow.RollbackAsync(); // ← 書き忘れリスク
    throw;
}

// ✅ Result型ベース：自動管理
return await _uow.ExecuteInTransactionAsync(async () =>
{
    await _order.CreateAsync(order);
    await _inventory.UpdateStockAsync(productId, newStock);
    return Outcome.Success(); // → 自動Commit
    // エラー時は自動Rollback
}, cancellationToken);
```

#### 2. **ビジネスエラーと技術的エラーの明確な分離**

```csharp
// ビジネスエラー：Resultで表現
if (stock < quantity)
    return Outcome.BusinessRule(
        BusinessErrorCode.InsufficientStock.ToErrorCode(),
        "Insufficient stock");  // → 400 Bad Request (INSUFFICIENT_STOCK)

// 技術的エラー：例外で表現
var data = await CallExternalApiAsync();  // → 例外発生 → 500 Internal Server Error
```

#### 3. **型安全な成功/失敗の判定**

```csharp
var result = await service.CreateOrderAsync(customerId, items);

// パターンマッチングで処理分岐
return result.Match(
    onSuccess: orderId => CreatedAtAction(...),
    onSuccessEmpty: () => NoContent(),
    onFailure: error => HandleError(error)  // 404/400/409など自動判定
);
```

#### 4. **フロントエンドでのエラーハンドリングが容易**

```typescript
// フロントエンド (TypeScript)
try {
    const response = await createOrder(customerId, items);
    // 成功処理
} catch (error) {
    if (error.status === 400 && error.code === 'INSUFFICIENT_STOCK') {
        showNotification('在庫不足です。数量を減らしてください。');
    } else if (error.status === 404) {
        showNotification('商品が見つかりません。');
    } else {
        showNotification('予期しないエラーが発生しました。');
    }
}
```

---

## 🚀 クイックスタート

### 前提条件

- .NET 10.0 SDK以上
- 任意のIDE（Visual Studio / Rider / VS Code）

### 1. リポジトリをクローン

```bash
git clone https://github.com/rendya2501/Dapper.UnitOfWork.Sample.git
cd Dapper.UnitOfWork.Sample
```

### 2. プロジェクトを実行

```bash
cd src/OrderManagement.Api
dotnet run
```

### 3. APIを試す

ブラウザで http://localhost:5076/scalar/v1 を開く

**または**

```bash
# 注文を作成
curl -X POST http://localhost:5076/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "items": [
      { "productId": 1, "quantity": 2 }
    ]
  }'

# 在庫を確認
curl http://localhost:5076/api/inventory

# 監査ログを確認
curl http://localhost:5076/api/auditlogs
```

---

## 📖 基本的な使い方

### DI登録

```csharp
// Program.cs
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

// IDbConnectionを登録
builder.Services.AddScoped<IDbConnection>(sp => 
    new SqliteConnection(connectionString));

// DbSessionを登録（接続とトランザクションの保持役）
builder.Services.AddScoped<DbSession>();

// IDbSessionManagerとIDbSessionの両方をDbSessionで解決
builder.Services.AddScoped<IDbSessionManager>(sp => 
    sp.GetRequiredService<DbSession>());
builder.Services.AddScoped<IDbSession>(sp => 
    sp.GetRequiredService<DbSession>());

// UnitOfWorkを登録
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositoriesを登録
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Servicesを登録
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
```

### Service層での実装

#### パターン1：読み取り専用操作（トランザクション不要）

```csharp
public class OrderService(
    IOrderRepository order) : IOrderService
{
    public async Task<OperationResult<Order>> GetOrderByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        // トランザクション不要な読み取り操作
        var orderEntity = await order.GetByIdAsync(id, cancellationToken);
        
        if (orderEntity is null)
            return Outcome.NotFound($"Order {id} not found");
        
        return Outcome.Success(orderEntity);
    }
}
```

#### パターン2：単一Repository操作（トランザクション必要）

```csharp
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IAuditLogRepository auditLog) : IInventoryService
{
    public async Task<OperationResult<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // 在庫作成
            var productId = await inventory.CreateAsync(new Inventory
            {
                ProductName = productName,
                Stock = stock,
                UnitPrice = unitPrice
            }, cancellationToken);

            // 監査ログ記録
            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Outcome.Success(productId);
        }, cancellationToken);
    }
}
```

#### パターン3：複数Repository横断操作（複雑なビジネスロジック）

```csharp
public class OrderService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IOrderRepository order,
    IAuditLogRepository auditLog) : IOrderService
{
    public async Task<OperationResult<int>> CreateOrderAsync(
        int customerId, 
        List<OrderItem> items,
        CancellationToken cancellationToken)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // 1. ビジネスバリデーション
            if (items.Count == 0)
                return Outcome.BusinessRule(
                    BusinessErrorCode.EmptyOrder.ToErrorCode(),
                    "Order must have at least one item.");

            // 2. 注文集約を構築
            var orderEntity = new Order
            {
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow
            };

            // 3. 各商品の在庫確認と注文明細追加
            foreach (var item in items)
            {
                var product = await inventory.GetByProductIdAsync(item.ProductId);
                
                if (product is null)
                    return Outcome.NotFound($"Product {item.ProductId} not found");
                
                if (product.Stock < item.Quantity)
                    return Outcome.BusinessRule(
                        BusinessErrorCode.InsufficientStock.ToErrorCode(),
                        $"Insufficient stock for {product.ProductName}. " +
                        $"Available: {product.Stock}, Requested: {item.Quantity}");

                // 在庫減算
                await inventory.UpdateStockAsync(
                    item.ProductId,
                    product.Stock - item.Quantity);

                // 注文明細を追加
                orderEntity.AddDetail(item.ProductId, item.Quantity, product.UnitPrice);
            }

            // 4. 注文を永続化
            var orderId = await order.CreateAsync(orderEntity);

            // 5. 監査ログ記録
            await auditLog.CreateAsync(new AuditLog
            {
                Action = "ORDER_CREATED",
                Details = $"OrderId={orderId}, CustomerId={customerId}, " +
                          $"Items={items.Count}, Total={orderEntity.TotalAmount:C}",
                CreatedAt = DateTime.UtcNow
            });

            return Outcome.Success(orderId);
        }, cancellationToken);
    }
}
```

### Controller層での実装

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BusinessErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrderAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new OrderItem(i.ProductId, i.Quantity))
            .ToList();

        var result = await orderService.CreateOrderAsync(
            request.CustomerId, 
            items, 
            cancellationToken);

        // 拡張メソッドで自動変換
        return result.ToActionResult(this, orderId => CreatedAtAction(
            nameof(GetOrderByIdAsync),
            new { id = orderId },
            new CreateOrderResponse(orderId)));
        
        // ↓ 以下のように自動変換される
        // Success → 201 Created
        // NotFound → 404 Not Found
        // BusinessRule (INSUFFICIENT_STOCK) → 400 Bad Request + エラーコード
        // ValidationFailed → 400 Bad Request + フィールドエラー
        // Conflict → 409 Conflict
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderByIdAsync(
        int id, 
        CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrderByIdAsync(id, cancellationToken);
        return result.ToActionResult(this, Ok);
    }
}
```

---

## 🏗️ アーキテクチャ

### プロジェクト構成

```
Dapper.UnitOfWork.ModernSample/
│
├── OrderManagement.Api/                      # Presentation層
│   ├── Controllers/                          # APIエンドポイント
│   ├── Contracts/                            # Request/Response DTO
│   ├── Extensions/                           # Result→IActionResult変換
│   ├── Filters/                              # FluentValidation自動実行
│   ├── Middleware/                           # 例外→ProblemDetails変換
│   └── Program.cs                            # DI設定・起動
│
├── OrderManagement.Application/              # Application層
│   ├── Common/                               # 共通インターフェース
│   │   ├── IDbSession.cs                    # 読み取り専用アクセサー
│   │   ├── IDbSessionManager.cs             # 管理用アクセサー
│   │   └── IUnitOfWork.cs                   # トランザクション管理
│   ├── Models/                               # アプリケーション層DTO
│   ├── Repositories/                         # Repositoryインターフェース
│   └── Services/                             # ビジネスロジック実装
│
├── OrderManagement.Domain/                   # Domain層
│   ├── Common/Results/                       # Result型定義
│   │   ├── OperationResult.cs               # 成功/失敗の型
│   │   ├── OperationError.cs                # エラー種別の型
│   │   ├── Outcome.cs                       # Resultファクトリ
│   │   └── BusinessErrorCode.cs             # ビジネスエラーコード定義
│   ├── Entities/                             # ドメインエンティティ
│   └── Exceptions/                           # ドメイン例外
│
└── OrderManagement.Infrastructure/           # Infrastructure層
    ├── Persistence/
    │   ├── UnitOfWork.cs                    # トランザクション実装
    │   ├── DbSession.cs                     # 接続・トランザクション保持
    │   ├── Repositories/                    # Repository実装
    │   └── Database/
    │       └── DatabaseInitializer.cs       # スキーマ初期化
```

### レイヤーの責務

#### 1. **Presentation層（OrderManagement.Api）**

- HTTP要求/応答の処理
- バリデーション（FluentValidation）
- Result型→HTTPステータスコード変換
- 例外→ProblemDetails変換

#### 2. **Application層（OrderManagement.Application）**

- ビジネスロジックの実装
- トランザクション境界の定義
- 複数Repositoryの協調
- Result型によるエラーハンドリング

#### 3. **Domain層（OrderManagement.Domain）**

- ビジネスルールの定義
- エンティティとバリューオブジェクト
- ドメインイベント
- Result型とエラーコード定義

#### 4. **Infrastructure層（OrderManagement.Infrastructure）**

- データアクセスの実装
- トランザクション管理の実装
- 外部サービス連携
- データベース初期化

### 重要な設計パターン

#### DbSession：接続とトランザクションの保持役

```csharp
public class DbSession(IDbConnection connection) : IDbSessionManager
{
    public IDbConnection Connection => connection;
    public IDbTransaction? Transaction { get; set; }
}
```

- **責務**：現在の接続とトランザクションを保持するだけ
- **ライフサイクル管理**：UnitOfWorkに完全委譲
- **2つのインターフェース**：
  - `IDbSession`：読み取り専用（Repository用）
  - `IDbSessionManager`：書き込み可能（UnitOfWork用）

#### UnitOfWork：トランザクションのライフサイクル管理

```csharp
public class UnitOfWork(
    IDbSessionManager sessionManager,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async Task<OperationResult<T>> ExecuteInTransactionAsync<T>(
        Func<Task<OperationResult<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        // 2重スコープ検出
        CheckNestedTransaction(IsInTransaction.Value);
        IsInTransaction.Value = true;

        try
        {
            // 1. 接続開始
            await EnsureConnectionOpenAsync(cancellationToken);
            
            // 2. トランザクション開始
            sessionManager.Transaction = await BeginTransactionAsync(cancellationToken);

            // 3. 操作を実行
            var result = await operation();

            // 4. Resultに基づいて自動Commit/Rollback
            if (result.IsSuccess)
                await CommitTransactionAsync(sessionManager.Transaction, cancellationToken);
            else
                await RollbackTransactionAsync(sessionManager.Transaction, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync(sessionManager.Transaction);
            sessionManager.Transaction = null;
            IsInTransaction.Value = false;
        }
    }

    // Dispose時に接続も確実に閉じる
    public void Dispose()
    {
        sessionManager.Transaction?.Dispose();
        sessionManager.Connection.Close();
        sessionManager.Connection.Dispose();
    }
}
```

#### Repository：純粋なデータアクセス

```csharp
public class InventoryRepository(IDbSession session) : IInventoryRepository
{
    public async Task<Inventory?> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Inventory WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            session.Transaction,  // ← UoWが管理するトランザクションを使用
            cancellationToken: cancellationToken);

        return await session.Connection.QueryFirstOrDefaultAsync<Inventory>(command);
    }

    // トランザクション管理は一切しない（UoWに完全委譲）
}
```

---

## 💡 よくあるパターン

### パターン1：早期returnがあるトランザクション処理

```csharp
public async Task<OperationResult<int>> ProcessOrderAsync(
    OrderRequest request,
    CancellationToken cancellationToken)
{
    return await _uow.ExecuteInTransactionAsync(async () =>
    {
        var inventory = await _inventory.GetByProductIdAsync(request.ProductId);
        
        // 在庫不足の場合は早期return
        if (inventory.Stock < request.Quantity)
            return Outcome.BusinessRule(
                BusinessErrorCode.InsufficientStock.ToErrorCode(),
                "Insufficient stock");  // → 自動Rollback

        // 通常処理
        await _inventory.UpdateStockAsync(request.ProductId, inventory.Stock - request.Quantity);
        var orderId = await _order.CreateAsync(order);

        return Outcome.Success(orderId);  // → 自動Commit
    }, cancellationToken);
}
```

### パターン2：条件分岐が多い処理

```csharp
public async Task<OperationResult<ProcessResult>> ProcessComplexOrderAsync(
    OrderRequest request,
    CancellationToken cancellationToken)
{
    return await _uow.ExecuteInTransactionAsync(async () =>
    {
        // ステップ1：顧客確認
        var customer = await _customer.GetByIdAsync(request.CustomerId);
        if (customer is null)
            return Outcome.NotFound($"Customer {request.CustomerId} not found");

        // ステップ2：在庫確認
        var inventory = await _inventory.GetByProductIdAsync(request.ProductId);
        if (inventory is null)
            return Outcome.NotFound($"Product {request.ProductId} not found");
        
        if (inventory.Stock < request.Quantity)
            return Outcome.BusinessRule(
                BusinessErrorCode.InsufficientStock.ToErrorCode(),
                $"Available: {inventory.Stock}, Requested: {request.Quantity}");

        // ステップ3：クーポン検証
        if (request.CouponCode != null)
        {
            var coupon = await _coupon.GetByCodeAsync(request.CouponCode);
            if (coupon is null || coupon.IsExpired)
                return Outcome.BusinessRule(
                    BusinessErrorCode.InvalidCoupon.ToErrorCode(),
                    "Coupon is invalid or expired");
        }

        // ステップ4：注文作成
        var orderId = await _order.CreateAsync(order);

        return Outcome.Success(new ProcessResult 
        { 
            OrderId = orderId,
            Status = "Completed" 
        });
    }, cancellationToken);
}
```

### パターン3：外部API呼び出しとの組み合わせ

```csharp
public async Task<OperationResult<int>> ProcessOrderWithNotificationAsync(
    OrderRequest request,
    CancellationToken cancellationToken)
{
    // トランザクション内：DB操作のみ
    var result = await _uow.ExecuteInTransactionAsync(async () =>
    {
        var orderId = await _order.CreateAsync(order);
        await _inventory.UpdateStockAsync(request.ProductId, newStock);
        return Outcome.Success(orderId);
    }, cancellationToken);

    // トランザクション外：外部API呼び出し
    if (result.IsSuccess)
    {
        await _emailService.SendOrderConfirmationAsync(result.Value!);
        await _smsService.SendNotificationAsync(result.Value!);
    }

    return result;
}
```

### パターン4：バッチ処理

```csharp
public async Task<OperationResult<BatchResult>> ProcessBatchOrdersAsync(
    List<OrderRequest> requests,
    CancellationToken cancellationToken)
{
    return await _uow.ExecuteInTransactionAsync(async () =>
    {
        var results = new BatchResult();

        foreach (var request in requests)
        {
            var inventory = await _inventory.GetByProductIdAsync(request.ProductId);
            
            if (inventory is null || inventory.Stock < request.Quantity)
            {
                results.Failed.Add(request.ProductId);
                continue;
            }

            await _inventory.UpdateStockAsync(
                request.ProductId,
                inventory.Stock - request.Quantity);
            
            var orderId = await _order.CreateAsync(new Order { /* ... */ });
            results.Succeeded.Add(orderId);
        }

        // バッチ全体を1トランザクションでCommit
        return Outcome.Success(results);
    }, cancellationToken);
}
```

---

## ✅ ベストプラクティス

### 1. トランザクションは最小限に保つ

```csharp
// ✅ 良い例：DB操作のみトランザクション内
var orderId = await CreateOrderInTransactionAsync(request);
await SendNotificationAsync(orderId);

// ❌ 悪い例：外部API呼び出しまでトランザクション内
return await _uow.ExecuteInTransactionAsync(async () =>
{
    var orderId = await _order.CreateAsync(order);
    await _externalApi.CallAsync();  // トランザクションが長時間ロック
    return Outcome.Success(orderId);
}, cancellationToken);
```

### 2. ビジネスエラーはResult型で表現

```csharp
// ✅ 良い例：Resultで表現
if (stock < quantity)
    return Outcome.BusinessRule(
        BusinessErrorCode.InsufficientStock.ToErrorCode(),
        "Insufficient stock");

// ❌ 悪い例：例外で表現
if (stock < quantity)
    throw new BusinessRuleException("Insufficient stock");
```

### 3. Repositoryは純粋にデータアクセスのみ

```csharp
// ✅ Repository：トランザクション管理は一切しない
public class OrderRepository(IDbSession session) : IOrderRepository
{
    public async Task<int> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        return await session.Connection.ExecuteScalarAsync<int>(
            sql, order, session.Transaction, cancellationToken: cancellationToken);
    }
}

// ✅ Service：トランザクション管理とビジネスロジック
public class OrderService(IUnitOfWork uow, IOrderRepository order) : IOrderService
{
    public async Task<OperationResult<int>> CreateOrderAsync(...)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // ビジネスロジック + Repository呼び出し
            var orderId = await order.CreateAsync(orderEntity);
            return Outcome.Success(orderId);
        }, cancellationToken);
    }
}
```

### 4. エラーコードはenumで定義

```csharp
// ✅ 良い例：enumで定義
public enum BusinessErrorCode
{
    InsufficientStock,
    InvalidQuantity,
    OrderExpired
}

// 拡張メソッドでUPPER_SNAKE_CASEに変換
public static string ToErrorCode(this BusinessErrorCode code)
{
    return string.Concat(
        code.ToString()
            .Select((c, i) => i > 0 && char.IsUpper(c) ? $"_{c}" : c.ToString())
    ).ToUpperInvariant();
}

// 使用例
return Outcome.BusinessRule(
    BusinessErrorCode.InsufficientStock.ToErrorCode(), // "INSUFFICIENT_STOCK"
    "Insufficient stock");

// ❌ 悪い例：文字列をハードコーディング
return Outcome.BusinessRule("INSUFFICIENT_STOCK", "Insufficient stock");
```

### 5. 2重トランザクションを避ける

```csharp
// ✅ 良い例：サービスはUoWを使い、別サービスを呼ばない
public class OrderService(IUnitOfWork uow, IOrderRepository order) : IOrderService
{
    public async Task<OperationResult<int>> CreateOrderAsync(...)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var orderId = await order.CreateAsync(orderEntity);
            return Outcome.Success(orderId);
        }, cancellationToken);
    }
}

// ❌ 悪い例：サービスがUoWを使いながら別サービスを呼ぶ
public class OrderService(
    IUnitOfWork uow, 
    IInventoryService inventoryService) : IOrderService
{
    public async Task<OperationResult<int>> CreateOrderAsync(...)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // InventoryServiceも内部でUoWを使っていると2重トランザクション！
            await inventoryService.UpdateStockAsync(productId, newStock);
            
            var orderId = await order.CreateAsync(orderEntity);
            return Outcome.Success(orderId);
        }, cancellationToken);
    }
}

// ✅ 解決策：Repositoryを直接注入
public class OrderService(
    IUnitOfWork uow,
    IOrderRepository order,
    IInventoryRepository inventory) : IOrderService
{
    public async Task<OperationResult<int>> CreateOrderAsync(...)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            await inventory.UpdateStockAsync(productId, newStock);
            var orderId = await order.CreateAsync(orderEntity);
            return Outcome.Success(orderId);
        }, cancellationToken);
    }
}
```

**注意**: UnitOfWorkは2重トランザクションを実行時に検出し、InvalidOperationExceptionをスローします。

---

## 🔍 トラブルシューティング

### トランザクションがコミットされない

**原因**: ビジネスバリデーションエラーで失敗している

**解決策**: ログを確認し、どのバリデーションで失敗しているか特定

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "OrderManagement": "Debug"
    }
  }
}
```

**ログ出力例**:
```
[Warning] Transaction rolled back due to business failure: [INSUFFICIENT_STOCK] Available: 5, Requested: 10
```

### 2重トランザクションエラーが発生する

**エラーメッセージ**:
```
InvalidOperationException: Nested transaction detected!
ExecuteInTransactionAsync was called while another transaction is already active.
```

**原因**: サービスがUoWを使いながら、別のサービス（内部でUoWを使用）を呼んでいる

**解決策**: 
1. サービス間呼び出しを避け、Repositoryを直接注入する
2. または、片方のサービスからUoWを削除し、Repositoryのみ使う層とする

```csharp
// ❌ 悪い設計
Service A (UoW) → Service B (UoW)  // 2重トランザクション！

// ✅ 良い設計
Service A (UoW) → Repository B
Service A (UoW) → Repository C
```

### デッドロックが発生する

**原因**: トランザクション内で長時間処理（外部API呼び出し、重い計算）を実行している

**解決策**: トランザクション外に移動

```csharp
// ❌ 悪い例
return await uow.ExecuteInTransactionAsync(async () =>
{
    var orderId = await order.CreateAsync(orderEntity);
    await Task.Delay(10000);  // 長時間処理（例）
    await externalApi.CallAsync();  // 外部API
    return Outcome.Success(orderId);
}, cancellationToken);

// ✅ 良い例
var result = await uow.ExecuteInTransactionAsync(async () =>
{
    var orderId = await order.CreateAsync(orderEntity);
    return Outcome.Success(orderId);
}, cancellationToken);

if (result.IsSuccess)
{
    await Task.Delay(10000);  // トランザクション外
    await externalApi.CallAsync();  // トランザクション外
}
```

### Repositoryでトランザクションが効かない

**原因**: UoWを使わずに直接Repositoryを呼んでいる

**解決策**: 必ずUoWのExecuteInTransactionAsync経由で実行

```csharp
// ❌ 悪い例：トランザクションなし
public async Task<OperationResult<int>> CreateOrderAsync(...)
{
    var orderId = await _order.CreateAsync(orderEntity);  // トランザクションなし
    await _inventory.UpdateStockAsync(productId, newStock);  // 別トランザクション
    return Outcome.Success(orderId);
}

// ✅ 良い例：UoW経由
public async Task<OperationResult<int>> CreateOrderAsync(...)
{
    return await _uow.ExecuteInTransactionAsync(async () =>
    {
        var orderId = await _order.CreateAsync(orderEntity);
        await _inventory.UpdateStockAsync(productId, newStock);
        return Outcome.Success(orderId);
    }, cancellationToken);
}
```

---

## 🧪 テストの実行

```bash
cd Tests
dotnet test
```

---

## 📚 設計ドキュメント

### Result型の詳細

#### OperationResult<T> - 値を返す操作

```csharp
public abstract record OperationResult<T>
{
    public sealed record Success(T Value) : OperationResult<T>;
    public sealed record SuccessEmpty : OperationResult<T>;
    public sealed record Failure(OperationError Error) : OperationResult<T>;
}
```

**使い分け**:
- `Success(value)`: 値を返す成功（200 OK, 201 Created）
- `SuccessEmpty`: 値を返さない成功（204 No Content）
- `Failure(error)`: 失敗（400/404/409など）

#### OperationResult - 値を返さない操作

```csharp
public abstract record OperationResult
{
    public sealed record Success : OperationResult;
    public sealed record Failure(OperationError Error) : OperationResult;
}
```

**使い分け**:
- `Success`: 削除・更新など値不要な成功（204 No Content）
- `Failure(error)`: 失敗（400/404/409など）

#### OperationError - エラーの種類

```csharp
public abstract record OperationError
{
    // リソース系
    public sealed record NotFound(string? Message = null) : OperationError;
    
    // バリデーション系
    public sealed record ValidationFailed(Dictionary<string, string[]> Errors) : OperationError;
    
    // ビジネスルール系
    public sealed record Conflict(string Message) : OperationError;
    public sealed record BusinessRule(string Code, string Message) : OperationError;
    
    // 権限系
    public sealed record Unauthorized(string Message = "...") : OperationError;
    public sealed record Forbidden(string Message = "...") : OperationError;
}
```

**HTTPステータスコードへの対応**:
- `NotFound` → 404 Not Found
- `ValidationFailed` → 400 Bad Request（フィールドエラー付き）
- `BusinessRule` → 400 Bad Request（エラーコード付き）
- `Conflict` → 409 Conflict
- `Unauthorized` → 401 Unauthorized
- `Forbidden` → 403 Forbidden

#### Outcome - Resultファクトリ

```csharp
public static class Outcome
{
    // 成功系
    public static OperationResult<T> Success<T>(T value);
    public static OperationResult Success();
    
    // エラー系
    public static OperationError NotFound(string? message = null);
    public static OperationError ValidationFailed(Dictionary<string, string[]> errors);
    public static OperationError Conflict(string message);
    public static OperationError BusinessRule(string code, string message);
    public static OperationError Unauthorized(string message = "...");
    public static OperationError Forbidden(string message = "...");
}
```

### エラーコードの定義

```csharp
public enum BusinessErrorCode
{
    // 在庫関連
    InsufficientStock,           // → "INSUFFICIENT_STOCK"
    StockReservationFailed,      // → "STOCK_RESERVATION_FAILED"
    
    // 注文関連
    EmptyOrder,                  // → "EMPTY_ORDER"
    InvalidQuantity,             // → "INVALID_QUANTITY"
    OrderExpired,                // → "ORDER_EXPIRED"
    
    // 支払い関連
    PaymentFailed,               // → "PAYMENT_FAILED"
    InvalidPaymentMethod,        // → "INVALID_PAYMENT_METHOD"
    
    // クーポン関連
    InvalidCoupon,               // → "INVALID_COUPON"
    CouponConditionNotMet        // → "COUPON_CONDITION_NOT_MET"
}

// 拡張メソッド
public static string ToErrorCode(this BusinessErrorCode code)
{
    return string.Concat(
        code.ToString()
            .Select((c, i) => i > 0 && char.IsUpper(c) ? $"_{c}" : c.ToString())
    ).ToUpperInvariant();
}
```

**フロントエンドでの使用例** (TypeScript):

```typescript
try {
    const response = await createOrder(customerId, items);
    showSuccess('注文が完了しました');
} catch (error) {
    if (error.status === 400) {
        switch (error.code) {
            case 'INSUFFICIENT_STOCK':
                showError('在庫不足です。数量を減らしてください。');
                break;
            case 'INVALID_QUANTITY':
                showError('数量が不正です。');
                break;
            case 'EMPTY_ORDER':
                showError('商品を選択してください。');
                break;
            default:
                showError('入力内容を確認してください。');
        }
    } else if (error.status === 404) {
        showError('商品が見つかりません。');
    } else {
        showError('予期しないエラーが発生しました。');
    }
}
```

---

## 🎓 学習リソース

### 設計パターン

- [Martin Fowler - Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Martin Fowler - Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Microsoft - Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

### Result型パターン

- [Vladimir Khorikov - Railway Oriented Programming](https://enterprisecraftsmanship.com/posts/railway-oriented-programming/)
- [Scott Wlaschin - Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Error Handling in C# - Result Pattern](https://www.youtube.com/watch?v=WCCkEe_Hy2Y)

### Dapper

- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [Dapper Tutorial](https://dapper-tutorial.net/)

---

## 🙏 謝辞

このプロジェクトは以下の素晴らしいオープンソースプロジェクトに基づいています：

- [Dapper](https://github.com/DapperLib/Dapper) - シンプルで高速なORMマッパー
- [FluentValidation](https://github.com/FluentValidation/FluentValidation) - 強力なバリデーションライブラリ
- [SQLite](https://www.sqlite.org/) - 組み込み型データベース
