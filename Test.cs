using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;


/// <summary>
/// 実行結果の基底（共通のキャンセル情報を持つ）
/// </summary>
public abstract class ResultBase
{
    public bool IsSuccess { get; protected init; }
    public string? ErrorMessage { get; protected init; }

    private protected ResultBase() { }
}

/// <summary>
/// 値なし結果（成功/キャンセルのみ）
/// </summary>
public sealed class Result : ResultBase
{
    private Result(bool success, string? errorMessage = null)
    {
        IsSuccess = success;
        ErrorMessage = errorMessage;
    }

    public static Result Ok() => new(true);
    public static Result Cancel(string message) => new(false, message);
}

/// <summary>
/// 値あり結果（成功時のみ値を持つ）
/// </summary>
public sealed class Result<T> : ResultBase
{
    public T? Value { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    // ★ 成功時のみ値を持つ
    public static Result<T> Ok(T value) => new(value);

    // ★ キャンセル用の静的メソッド（型指定なし版）
    public static Result<T> Cancel(string message) => new(message);

    // パターンマッチング
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onCancel)
    {
        return IsSuccess && Value != null
            ? onSuccess(Value)
            : onCancel(ErrorMessage ?? "Unknown error");
    }

    public void Match(
        Action<T> onSuccess,
        Action<string> onCancel)
    {
        if (IsSuccess && Value != null)
        {
            onSuccess(Value);
        }
        else
        {
            onCancel(ErrorMessage ?? "Unknown error");
        }
    }

    // ★ 暗黙的変換（値なしResultから）
    public static implicit operator Result<T>(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert successful Result to Result<T> without a value");

        return new Result<T>(result.ErrorMessage ?? "Unknown error");
    }
}

// ==========================================
// 2. DbContext & Unit of Work
// ==========================================

public interface IDbContext
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}

public interface IUnitOfWork : IDbContext, IDisposable
{
    T GetRepository<T>() where T : BaseRepository;

    /// <summary>
    /// トランザクション付きコマンド実行（値なし）
    /// </summary>
    Task<Result> ExecuteAsync(Func<Task<Result>> action);

    /// <summary>
    /// トランザクション付きコマンド実行（値あり）
    /// </summary>
    Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> action);

    /// <summary>
    /// トランザクションなしクエリ実行
    /// </summary>
    Task<T> QueryAsync<T>(Func<Task<T>> query);
}

public class UnitOfWork(IDbConnection connection, ILogger<UnitOfWork>? logger = null) : IUnitOfWork
{
    private IDbTransaction? _transaction;
    private readonly Dictionary<Type, object> _repositories = new();

    public IDbConnection Connection => connection;
    public IDbTransaction? Transaction => _transaction;

    public T GetRepository<T>() where T : BaseRepository
    {
        var type = typeof(T);

        if (!_repositories.TryGetValue(type, out object? value))
        {
            var ctor = type.GetConstructor([typeof(IDbContext)])
                ?? throw new InvalidOperationException(
                    $"{type.Name} must have a constructor with IDbContext parameter");

            var repository = (T)ctor.Invoke([this]);
            value = repository;
            _repositories.Add(type, value);
        }

        return (T)value;
    }

    // ★ 値なし版
    public async Task<Result> ExecuteAsync(Func<Task<Result>> action)
    {
        if (_transaction != null)
            return await action();

        _transaction = connection.BeginTransaction();

        try
        {
            var result = await action();

            if (result.IsSuccess)
            {
                _transaction.Commit();
                logger?.LogInformation("Transaction committed");
            }
            else
            {
                _transaction.Rollback();
                logger?.LogInformation("Transaction rolled back: {Message}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _transaction.Rollback();
            logger?.LogError(ex, "Transaction failed");
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    // ★ 値あり版
    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> action)
    {
        if (_transaction != null)
            return await action();

        _transaction = connection.BeginTransaction();

        try
        {
            var result = await action();

            if (result.IsSuccess)
            {
                _transaction.Commit();
                logger?.LogInformation("Transaction committed");
            }
            else
            {
                _transaction.Rollback();
                logger?.LogInformation("Transaction rolled back: {Message}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _transaction.Rollback();
            logger?.LogError(ex, "Transaction failed");
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task<T> QueryAsync<T>(Func<Task<T>> query)
    {
        return await query();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _repositories.Clear();
    }
}

// ==========================================
// 3. Repository
// ==========================================

public abstract class BaseRepository(IDbContext context)
{
    protected readonly IDbContext Context = context ?? throw new ArgumentNullException(nameof(context));
}

public class ProductRepository(IDbContext context) : BaseRepository(context)
{
    public async Task<IEnumerable<Product>> GetAllAsync()
        => await Context.Connection.QueryAsync<Product>(
            "SELECT * FROM Products",
            Context.Transaction);

    public async Task<Product?> GetByIdAsync(int id)
        => await Context.Connection.QuerySingleOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE Id = @id",
            new { id },
            Context.Transaction);

    public async Task<int> InsertAsync(Product product)
        => await Context.Connection.QuerySingleAsync<int>(
            "INSERT INTO Products (Name, Price) VALUES (@Name, @Price); SELECT last_insert_rowid();",
            product,
            Context.Transaction);

    public async Task UpdateAsync(Product product)
        => await Context.Connection.ExecuteAsync(
            "UPDATE Products SET Name = @Name, Price = @Price WHERE Id = @Id",
            product,
            Context.Transaction);

    public async Task DeleteAsync(int id)
        => await Context.Connection.ExecuteAsync(
            "DELETE FROM Products WHERE Id = @id",
            new { id },
            Context.Transaction);
}

// ==========================================
// 4. Service（自然な書き心地！）
// ==========================================

public class ProductService(IUnitOfWork uow, ILogger<ProductService>? logger = null)
{
    public async Task<IEnumerable<Product>> GetListAsync()
    {
        return await uow.QueryAsync(async () =>
        {
            var repo = uow.GetRepository<ProductRepository>();
            return await repo.GetAllAsync();
        });
    }

    /// <summary>
    /// 商品登録 - 自然な書き方！
    /// </summary>
    public async Task<Result<int>> RegisterProductAsync(Product product)
    {
        // ビジネスバリデーション
        if (string.IsNullOrWhiteSpace(product.Name))
            return Result.Cancel("商品名は必須です"); // ★ シンプル！

        if (product.Price < 0)
            return Result.Cancel("価格は0以上である必要があります"); // ★ シンプル！

        return await uow.ExecuteAsync(async () =>
        {
            var repo = uow.GetRepository<ProductRepository>();

            // 重複チェック
            var existing = await repo.GetAllAsync();
            if (existing.Any(p => p.Name == product.Name))
                return Result.Cancel("同じ名前の商品が既に存在します"); // ★ 自然！

            var id = await repo.InsertAsync(product);

            logger?.LogInformation("Product registered: {ProductId}", id);

            return Result<int>.Ok(id); // ★ 値ありはこっち
        });
    }

    /// <summary>
    /// 商品削除 - 値なし版
    /// </summary>
    public async Task<Result> DeleteProductAsync(int id)
    {
        return await uow.ExecuteAsync(async () =>
        {
            var repo = uow.GetRepository<ProductRepository>();

            var target = await repo.GetByIdAsync(id);
            if (target == null)
                return Result.Cancel("削除対象が見つかりません"); // ★ シンプル！

            await repo.DeleteAsync(id);

            logger?.LogInformation("Product deleted: {ProductId}", id);

            return Result.Ok(); // ★ 値なしはこっち
        });
    }

    /// <summary>
    /// 在庫更新 - 複雑なビジネスロジック例
    /// </summary>
    public async Task<Result<int>> UpdateStockAsync(int productId, int quantity)
    {
        if (quantity < 0)
            return Result.Cancel("数量は0以上である必要があります");

        return await uow.ExecuteAsync(async () =>
        {
            var repo = uow.GetRepository<ProductRepository>();

            var product = await repo.GetByIdAsync(productId);
            if (product == null)
                return Result.Cancel("商品が見つかりません");

            // 在庫計算（例）
            var newStock = product.Price + quantity; // 実際はStockプロパティを使う

            await repo.UpdateAsync(product);

            return Result<int>.Ok((int)newStock);
        });
    }
}

// ==========================================
// 5. Controller
// ==========================================

[ApiController]
[Route("api/products")]
public class ProductController(ProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await service.GetListAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        var result = await service.RegisterProductAsync(product);

        // ★ 値ありResult<T>の処理
        return result.Match<IActionResult>(
            onSuccess: id => CreatedAtAction(nameof(GetAll), new { id }, new { Id = id }),
            onCancel: error => BadRequest(new { Error = error })
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteProductAsync(id);

        // ★ 値なしResultの処理
        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { Error = result.ErrorMessage });
    }
}

// ==========================================
// 6. Models
// ==========================================

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// ==========================================
// Q&A: なぜ abstract class なのか？
// ==========================================

/*
質問：「interfaceじゃダメ？」

回答：interfaceでもできるが、abstract classの方が良い理由：

【abstract classの利点】
✅ 共通プロパティ（IsSuccess, ErrorMessage）を実装できる
✅ protectedコンストラクタで外部からの継承を防げる
✅ 共通ロジックを基底クラスに書ける

【interfaceの問題】
❌ プロパティは宣言のみ（実装は各クラス）
❌ コンストラクタを強制できない
❌ 共通ロジックを書けない

例：interfaceだとこうなる
```
public interface IResult
{
    bool IsSuccess { get; }
    string? ErrorMessage { get; }
}

public class Result : IResult
{
    public bool IsSuccess { get; set; } // ← 外部から変更可能！
    public string? ErrorMessage { get; set; } // ← 外部から変更可能！
}
```

abstract classなら：
```
public abstract class ResultBase
{
    public bool IsSuccess { get; protected init; } // ← 派生クラスのみ設定可能
    public string? ErrorMessage { get; protected init; }
    protected ResultBase() { } // ← 外部から継承不可
}
```

結論：
型安全性とカプセル化のため、abstract classの方が適している
*/

// ==========================================
// この設計の自然さ
// ==========================================

/*
【従来の問題】
return Result<int>.Cancel("エラー"); // ← int型なのに文字列？変！

【改善後】
return Result.Cancel("エラー");     // ← 自然！値なし
return Result<int>.Ok(123);         // ← 自然！値あり

【なぜ自然か】
- Cancelは「失敗」なので値がない → Result型
- Okは「成功」なので値がある → Result<T>型
- 型が意味と一致している
*/