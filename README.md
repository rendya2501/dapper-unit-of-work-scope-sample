# Dapper Unit of Work Scope Sample

**スコープベースのセッション管理と Result 型による、安全で構造的な Dapper アプリケーション設計**

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Dapper](https://img.shields.io/badge/Dapper-2.1-orange)](https://github.com/DapperLib/Dapper)
[![SQLite](https://img.shields.io/badge/SQLite-3-green)](https://www.sqlite.org/)

---

## 🎯 このプロジェクトについて

Dapper を使用した **実務で即採用可能な** Unit of Work パターンの実装サンプルです。

本サンプルの核心は **「スコープ」** にあります。DI の Scoped ライフタイム、`IDbSession`/`IDbSessionManager` の責務分離、`AsyncLocal` による非同期スコープの追跡、この 3 つを組み合わせることで、トランザクション管理を構造的に安全にします。

### 主な特徴

- ✅ **スコープベースのセッション管理** — `IDbSession`（読み取り専用）と `IDbSessionManager`（管理用）を分離し、Repository と UnitOfWork それぞれに適切な権限を付与
- ✅ **Result 型による自動トランザクション制御** — 成功/失敗を型で表現し、Commit/Rollback を自動化
- ✅ **完全な接続管理** — UnitOfWork が接続のライフサイクル全体を責任管理
- ✅ **2 重トランザクション検出** — `AsyncLocal` で非同期境界を跨いでも設計違反を実行時に即座に検出
- ✅ **クリーンアーキテクチャ** — 層間の責務を明確に分離
- ✅ **包括的なエラーハンドリング** — ビジネスエラーから技術的エラーまで統一的に処理

### なぜ「スコープ」が重要なのか

従来の Dapper 実装では以下の問題が頻発します：

❌ **トランザクション管理の問題**
- Commit/Rollback の書き忘れ
- 例外時の Rollback 漏れ
- 複数サービス呼び出しでのトランザクション重複

❌ **接続管理の問題**
- Repository が接続を直接持つと、同一リクエスト内でトランザクションを共有できない
- 接続の Open/Close のタイミングが分散する

本プロジェクトは **DbSession を Scoped で管理し、UnitOfWork に接続ライフサイクルを委譲する** ことで、これらの問題を構造的に解決します。

---

## 🔑 スコープ設計の核心

### DbSession：接続とトランザクションの保持役

```csharp
public class DbSession(IDbConnection connection) : IDbSessionManager
{
    public IDbConnection Connection => connection;
    public IDbTransaction? Transaction { get; set; }
}
```

`DbSession` は **接続とトランザクションを保持するだけ** のシンプルなクラスです。ライフサイクル管理は UnitOfWork に完全委譲します。

### 2 つのインターフェースによる権限分離

```csharp
// Repository 用：読み取り専用（Transaction の set 不可）
public interface IDbSession
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}

// UnitOfWork 用：管理用（Transaction の set が可能）
public interface IDbSessionManager : IDbSession
{
    new IDbTransaction? Transaction { get; set; }
}
```

Repository は `IDbSession` のみ受け取るため、トランザクションを勝手に開始・終了できません。UnitOfWork だけが `IDbSessionManager` を通じてトランザクションを制御します。

### DI 登録（具象クラスを 1 回だけ登録）

```csharp
// DbSession を1回だけ登録
services.AddScoped<DbSession>();

// IDbSessionManager → DbSession（UnitOfWork 用）
services.AddScoped<IDbSessionManager>(sp => sp.GetRequiredService<DbSession>());

// IDbSession → DbSession（Repository 用）
services.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());
```

同一リクエスト内では `DbSession` の同一インスタンスが共有されるため、複数の Repository が同じ接続・トランザクションを自動的に使用します。

---

## 📦 採用パターン：Result-Driven UoW

### サービス層の実装例

```csharp
public class OrderService(
    IUnitOfWork uow,
    IInventoryRepository inventoryRepo,
    IOrderRepository orderRepo,
    IAuditLogRepository auditLogRepo) : IOrderService
{
    public async Task<Result<int>> CreateOrderAsync(
        int customerId,
        List<OrderItem> items,
        CancellationToken cancellationToken)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // 1. ビジネスバリデーション
            if (items.Count == 0)
                return Result.Failure<int>(OrderErrors.EmptyOrder());

            // 2. 注文集約を構築
            var orderResult = Order.Create(customerId);
            if (orderResult.IsFailure)
                return Result.Failure<int>(orderResult.Error!);

            var order = orderResult.Value;

            // 3. 各商品の在庫確認と減算
            foreach (var item in items)
            {
                var inventory = await inventoryRepo.GetByProductIdAsync(item.ProductId, cancellationToken);
                if (inventory is null)
                    return Result.Failure<int>(InventoryErrors.NotFoundByProductId(item.ProductId));

                var decreaseResult = inventory.Decrease(item.Quantity);
                if (decreaseResult.IsFailure)
                    return Result.Failure<int>(decreaseResult.Error!);

                await inventoryRepo.UpdateStockAsync(item.ProductId, inventory.Stock, cancellationToken);

                var addDetailResult = order.AddDetail(new ProductId(item.ProductId), item.Quantity, inventory.UnitPrice);
                if (addDetailResult.IsFailure)
                    return Result.Failure<int>(addDetailResult.Error!);
            }

            // 4. 注文を永続化
            var orderId = await orderRepo.CreateAsync(order, cancellationToken);

            // 5. 監査ログ記録
            await auditLogRepo.CreateAsync(new AuditLogRecord
            {
                Action = "ORDER_CREATED",
                Details = $"OrderId={orderId}, CustomerId={customerId}, Items={items.Count}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            // ✅ 成功を返す → UoW が自動 Commit
            return Result.Success(orderId);

            // ❌ Failure を返す → UoW が自動 Rollback
            // 例外発生 → UoW が自動 Rollback + 例外再スロー

        }, cancellationToken);
    }
}
```

### なぜこの設計が優れているのか

#### 1. トランザクション制御の自動化

```csharp
// ❌ 従来の方法：手動管理が必要
uow.BeginTransaction();
try
{
    await orderRepo.CreateAsync(order);
    await inventoryRepo.UpdateStockAsync(productId, newStock);
    await uow.CommitAsync();  // ← 書き忘れリスク
}
catch
{
    await uow.RollbackAsync(); // ← 書き忘れリスク
    throw;
}

// ✅ Result 型ベース：自動管理
return await uow.ExecuteInTransactionAsync(async () =>
{
    await orderRepo.CreateAsync(order);
    await inventoryRepo.UpdateStockAsync(productId, newStock);
    return Result.Success(orderId); // → 自動 Commit
    // Failure を返す / 例外発生 → 自動 Rollback
}, cancellationToken);
```

#### 2. ビジネスエラーと技術的エラーの明確な分離

```csharp
// ビジネスエラー：Result で表現（ドメイン層でエラー定義）
if (inventory is null)
    return Result.Failure<int>(InventoryErrors.NotFoundByProductId(productId)); // → 404

var decreaseResult = inventory.Decrease(item.Quantity);
if (decreaseResult.IsFailure)
    return Result.Failure<int>(decreaseResult.Error!); // → 400（在庫不足）

// 技術的エラー：例外で表現（そのまま再スロー）
var data = await externalApiAsync(); // → 例外発生 → 500
```

#### 3. Error 型の定義

```csharp
// SharedKernel/Primitives/Error.cs
public static Error NotFound(string code, string description) => ...  // → 404
public static Error Problem(string code, string description) => ...   // → 400
public static Error Conflict(string code, string description) => ...  // → 409
public static Error Failure(string code, string description) => ...   // → 500

// ドメイン層でエラーをまとめて定義する
public static class InventoryErrors
{
    public static Error NotFoundByProductId(int productId) => Error.NotFound(
        "Inventory.NotFound",
        $"Inventory not found for productId: {productId}");
}

public static class OrderErrors
{
    public static Error EmptyOrder() => Error.Problem(
        "Order.EmptyOrder",
        "Order must have at least one item.");

    public static Error InsufficientStock(int productId, int available, int requested) =>
        Error.Problem(
            "Order.InsufficientStock",
            $"ProductId={productId}, Available={available}, Requested={requested}");
}
```

#### 4. Controller 層での変換

```csharp
[HttpPost]
public async Task<IActionResult> CreateOrderAsync(
    [FromBody] CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var items = request.Items
        .Select(i => new OrderItem(i.ProductId, i.Quantity))
        .ToList();

    var result = await orderService.CreateOrderAsync(request.CustomerId, items, cancellationToken);

    // 拡張メソッドで自動変換
    return result.ToResult(
        orderId => CreatedAtRoute(
            nameof(GetOrderByIdAsync),
            new { id = orderId },
            new CreateOrderResponse(orderId)));

    // ↓ 以下のように自動変換される
    // Success    → 201 Created
    // NotFound   → 404 Not Found
    // Problem    → 400 Bad Request
    // Conflict   → 409 Conflict
    // Failure    → 500 Internal Server Error
}
```

---

## 🚀 クイックスタート

### 前提条件

- .NET 10.0 SDK 以上
- 任意の IDE（Visual Studio / Rider / VS Code）

### 1. リポジトリをクローン

```bash
git clone https://github.com/rendya2501/dapper-unit-of-work-scope-sample.git
cd dapper-unit-of-work-scope-sample
```

### 2. プロジェクトを実行

```bash
cd src/Web.Api
dotnet run
```

### 3. API を試す

ブラウザで `http://localhost:5076/scalar/v1` を開く

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

### DI 登録

```csharp
// Program.cs → Infrastructure/DependencyInjection.cs

// IDbConnection を Scoped で登録
services.AddScoped<IDbConnection>(sp => new SqliteConnection(connectionString));

// DbSession（具象クラスを 1 回だけ登録）
services.AddScoped<DbSession>();

// IDbSessionManager（UnitOfWork 用）
services.AddScoped<IDbSessionManager>(sp => sp.GetRequiredService<DbSession>());

// IDbSession（Repository 用）
services.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());

// UnitOfWork
services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories
services.AddScoped<IAuditLogRepository, AuditLogRepository>();
services.AddScoped<IInventoryRepository, InventoryRepository>();
services.AddScoped<IOrderRepository, OrderRepository>();
```

### Service 層での実装

#### パターン 1：読み取り専用操作（トランザクション不要）

```csharp
public class OrderService(IOrderRepository orderRepo) : IOrderService
{
    public async Task<Result<Order>> GetOrderByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepo.GetByIdAsync(id, cancellationToken);

        if (order is null)
            return Result.Failure<Order>(OrderErrors.NotFoundByOrderId(id));

        return Result.Success(order);
    }
}
```

#### パターン 2：単一 Repository 操作（トランザクション必要）

```csharp
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventoryRepo,
    IAuditLogRepository auditLogRepo) : IInventoryService
{
    public async Task<Result<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var createResult = Inventory.Create(productName, stock, unitPrice);
            if (createResult.IsFailure)
                return Result.Failure<int>(createResult.Error!);

            var productId = await inventoryRepo.CreateAsync(createResult.Value, cancellationToken);

            await auditLogRepo.CreateAsync(new AuditLogRecord
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success(productId);
        }, cancellationToken);
    }
}
```

#### パターン 3：複数 Repository 横断操作（複雑なビジネスロジック）

上記「採用パターン：Result-Driven UoW」の `CreateOrderAsync` を参照。

### Repository 層での実装

```csharp
// Repository は IDbSession のみ受け取る（トランザクション制御は一切しない）
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
            session.Transaction,  // UoW が管理するトランザクションを透過的に使用
            cancellationToken: cancellationToken);

        var record = await session.Connection.QueryFirstOrDefaultAsync<InventoryRecord>(command);

        return record == null ? null : InventoryMapper.ToDomain(record);
    }
}
```

---

## 🏗️ アーキテクチャ

### プロジェクト構成

```
dapper-unit-of-work-scope-sample/
│
├── src/
│   ├── Web.Api/                            # Presentation 層
│   │   ├── Controllers/                    # API エンドポイント
│   │   ├── Contracts/                      # Request/Response DTO
│   │   ├── Extensions/                     # Result → IActionResult 変換
│   │   │   ├── ResultExtensions.cs         # Match パターンマッチング
│   │   │   ├── ResultHttpExtensions.cs     # ToOk / ToResult / ToNoContent
│   │   │   └── ErrorToProblemMapper.cs     # Error → ProblemDetails 変換
│   │   ├── ExceptionHandlers/              # 例外 → ProblemDetails 変換
│   │   └── Program.cs                      # DI 設定・起動
│   │
│   ├── Application/                        # Application 層
│   │   ├── Common/
│   │   │   ├── IDbSession.cs               # 読み取り専用アクセサー（Repository 用）
│   │   │   ├── IDbSessionManager.cs        # 管理用アクセサー（UnitOfWork 用）
│   │   │   └── IUnitOfWork.cs              # トランザクション管理インターフェース
│   │   ├── DTOs/                           # アプリケーション層 DTO
│   │   ├── Repositories/                   # Repository インターフェース
│   │   └── Services/                       # ビジネスロジック実装
│   │
│   ├── Domain/                             # Domain 層
│   │   ├── Inventory/
│   │   │   ├── Inventory.cs                # 在庫エンティティ（ファクトリ・ドメインロジック）
│   │   │   ├── InventoryErrors.cs          # 在庫ドメインのエラー定義
│   │   │   └── ProductId.cs               # 値オブジェクト
│   │   └── Orders/
│   │       ├── Order.cs                    # 注文エンティティ（集約ルート）
│   │       ├── OrderDetail.cs             # 注文明細エンティティ
│   │       ├── OrderErrors.cs             # 注文ドメインのエラー定義
│   │       └── OrderId.cs                 # 値オブジェクト
│   │
│   ├── Infrastructure/                     # Infrastructure 層
│   │   └── Persistence/
│   │       ├── UnitOfWork.cs               # トランザクション実装
│   │       ├── DbSession.cs                # 接続・トランザクション保持
│   │       ├── Mappers/                    # ドメインモデル ↔ 永続化モデル変換
│   │       ├── Models/                     # 永続化モデル（〇〇Record）
│   │       ├── Repositories/              # Repository 実装
│   │       └── Database/
│   │           └── DatabaseInitializer.cs  # スキーマ初期化
│   │
│   └── SharedKernel/                       # 共有カーネル
│       ├── Models/
│       │   └── AuditLogRecord.cs          # 全層で共有するモデル
│       └── Primitives/
│           ├── Result.cs                   # Result / Result<T>
│           ├── Error.cs                    # エラー情報（Code / Description / Type）
│           ├── ErrorType.cs               # エラー分類（→ HTTP ステータスコード）
│           └── ValidationError.cs         # バリデーションエラー集約
│
└── tests/                                  # テスト（準備中）
```

### レイヤーの責務

#### Web.Api（Presentation 層）

- HTTP 要求/応答の処理
- バリデーション（FluentValidation）
- `Result` → HTTP ステータスコード変換（`ToOk` / `ToResult` / `ToNoContent`）
- 例外 → ProblemDetails 変換

#### Application 層

- ビジネスロジックの実装
- トランザクション境界の定義
- 複数 Repository の協調
- `Result` 型によるエラーハンドリング

#### Domain 層

- ビジネスルールの定義（ファクトリメソッド・ドメインメソッド）
- エンティティと値オブジェクト
- エラー定義（`〇〇Errors` クラス）

#### Infrastructure 層

- データアクセスの実装（Dapper + Mapper パターン）
- トランザクション管理の実装（`UnitOfWork`）
- 接続スコープ管理（`DbSession`）
- データベース初期化

### ドメインモデルと永続化モデルの分離

Infrastructure 層は Mapper を使用してドメインモデルと永続化モデル（`〇〇Record`）を完全に分離します。ドメイン層はデータベースの構造を知りません。

```csharp
// 永続化モデル（DBテーブルと 1:1 対応）
public record InventoryRecord
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Stock { get; init; }
    public decimal UnitPrice { get; init; }
}

// Mapper（Infrastructure 内部でのみ使用）
internal static class InventoryMapper
{
    public static Inventory ToDomain(InventoryRecord record) =>
        Inventory.Restore(new ProductId(record.ProductId), record.ProductName, record.Stock, record.UnitPrice);

    public static InventoryRecord ToRecord(Inventory inventory) =>
        new() { ProductId = (int)inventory.ProductId, ProductName = inventory.ProductName, ... };
}
```

ドメイン層の `internal` メソッド（`Restore`, `SetId` など）は `InternalsVisibleTo` で Infrastructure 層にのみ公開します。

---

## ✅ ベストプラクティス

### 1. トランザクションは最小限に保つ

```csharp
// ✅ 良い例：DB 操作のみトランザクション内
var result = await uow.ExecuteInTransactionAsync(async () =>
{
    var orderId = await orderRepo.CreateAsync(order);
    return Result.Success(orderId);
}, cancellationToken);

// トランザクション外で外部 API を呼ぶ
if (result.IsSuccess)
    await emailService.SendConfirmationAsync(result.Value);

// ❌ 悪い例：外部 API 呼び出しをトランザクション内に含める
return await uow.ExecuteInTransactionAsync(async () =>
{
    var orderId = await orderRepo.CreateAsync(order);
    await externalApi.CallAsync();  // トランザクションが長時間ロック
    return Result.Success(orderId);
}, cancellationToken);
```

### 2. ビジネスエラーは Result 型で表現

```csharp
// ✅ 良い例：Result で表現（ドメイン層でエラー定義）
var result = inventory.Decrease(quantity);
if (result.IsFailure)
    return Result.Failure<int>(result.Error!);

// ❌ 悪い例：例外でビジネスエラーを表現
if (stock < quantity)
    throw new BusinessRuleException("Insufficient stock");
```

### 3. Repository は純粋にデータアクセスのみ

```csharp
// ✅ Repository：トランザクション管理は一切しない
public class OrderRepository(IDbSession session) : IOrderRepository
{
    public async Task<int> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(sql, record, session.Transaction, ...);
        return await session.Connection.ExecuteScalarAsync<int>(command);
    }
}
```

### 4. 2 重トランザクションを避ける

```csharp
// ❌ 悪い設計：Service A が UoW を使いながら Service B（も UoW を使用）を呼ぶ
// → 2 重トランザクション！UnitOfWork が InvalidOperationException をスロー

// ✅ 良い設計：Service は Repository を直接注入する
public class OrderService(
    IUnitOfWork uow,
    IOrderRepository orderRepo,      // ← Repository を直接注入
    IInventoryRepository inventoryRepo) // ← Repository を直接注入
{
    public async Task<Result<int>> CreateOrderAsync(...)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            await inventoryRepo.UpdateStockAsync(...); // Repository を直接呼ぶ
            var orderId = await orderRepo.CreateAsync(...);
            return Result.Success(orderId);
        }, cancellationToken);
    }
}
```

---

## 🔍 トラブルシューティング

### 2 重トランザクションエラーが発生する

**エラーメッセージ**:
```
InvalidOperationException: Nested transaction detected!
ExecuteInTransactionAsync was called while another transaction is already active.
```

**原因**: Service が `ExecuteInTransactionAsync` を使いながら、同じく UoW を使う別の Service を呼んでいる

**解決策**: Service 間の呼び出しを避け、Repository を直接注入する

```csharp
// ❌ 悪い設計
Service A (UoW) → Service B (UoW)  // 2 重トランザクション！

// ✅ 良い設計
Service A (UoW) → Repository B
Service A (UoW) → Repository C
```

### トランザクションがコミットされない

**原因**: ビジネスバリデーションで `Result.Failure` が返されているため、UoW が自動的に Rollback している

**解決策**: ログを確認して失敗しているバリデーションを特定する

```json
// appsettings.json でデバッグログを有効化
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**ログ出力例**:
```
[Warning] Transaction rolled back due to business failure: Error { Code = Inventory.InsufficientStock, ... }
```

### デッドロックが発生する

**原因**: トランザクション内で長時間処理（外部 API 呼び出し、重い計算）を実行している

**解決策**: DB 操作のみトランザクション内に収め、外部処理はトランザクション外へ移動する

---

## 📚 Result 型リファレンス

### Result / Result\<T\>

```csharp
// 値なし成功
Result.Success()

// 値あり成功
Result.Success<T>(value)

// 値なし失敗
Result.Failure(error)

// 値あり失敗
Result.Failure<T>(error)

// プロパティ
result.IsSuccess   // bool
result.IsFailure   // bool
result.Error       // Error?（失敗時のみ値を持つ）
result.Value       // T（成功時のみアクセス可。失敗時は InvalidOperationException）
```

### Error ファクトリ

| メソッド | HTTP ステータス | 用途 |
|---|---|---|
| `Error.NotFound(code, desc)` | 404 | リソースが見つからない |
| `Error.Problem(code, desc)` | 400 | ビジネスルール違反 |
| `Error.Conflict(code, desc)` | 409 | リソース競合 |
| `Error.Failure(code, desc)` | 500 | システムエラー |

### IActionResult 変換拡張メソッド

| メソッド | 成功時 | 失敗時 |
|---|---|---|
| `result.ToOk()` | 200 OK（値そのまま） | ProblemDetails |
| `result.ToOk(mapper)` | 200 OK（mapper 適用後） | ProblemDetails |
| `result.ToNoContent()` | 204 No Content | ProblemDetails |
| `result.ToResult(onSuccess)` | `onSuccess` の戻り値 | ProblemDetails |

---

## 🧪 テストの実行

```bash
dotnet test
```

---

## 📚 設計ドキュメント

### UnitOfWork のライフサイクル

```
ExecuteInTransactionAsync 呼び出し
  │
  ├─ 1. 引数チェック / 破棄チェック / 2重スコープチェック（AsyncLocal）
  ├─ 2. EnsureConnectionOpenAsync（接続が閉じていれば Open）
  ├─ 3. BeginTransactionAsync（トランザクション開始）
  ├─ 4. operation() 実行
  │     ├─ IsSuccess → CommitTransactionAsync
  │     └─ IsFailure → RollbackTransactionAsync
  └─ 5. finally：DisposeTransactionAsync / IsInTransaction.Value = false

Dispose / DisposeAsync
  └─ 接続を Close & Dispose（UoW が完全管理）
```

---

## 🙏 謝辞

このプロジェクトは以下の素晴らしいオープンソースプロジェクトに基づいています：

- [Dapper](https://github.com/DapperLib/Dapper) — シンプルで高速な O/R マッパー
- [FluentValidation](https://github.com/FluentValidation/FluentValidation) — 強力なバリデーションライブラリ
- [Scalar](https://scalar.com/) — モダンな API ドキュメント UI
- [SQLite](https://www.sqlite.org/) — 組み込み型データベース
