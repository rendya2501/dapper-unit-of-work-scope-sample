using Application.Common;
using Domain.Common.Results;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> の具象実装クラス。
/// <see cref="IDbSessionManager"/> を通じてトランザクションと接続のライフサイクルを完全管理
/// </summary> 
/// <remarks>
/// <para><strong>DIライフタイム: Scoped必須</strong></para>
/// <para>
/// このクラスはスレッドセーフではありません。
/// 必ず Scoped でDI登録してください（Singleton/Transient は使用不可）。
/// </para>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>1つのHTTPリクエスト = 1つのUoWインスタンス</item>
/// <item>トランザクションのネストは設計違反として検出</item>
/// <item>接続とトランザクションのライフサイクルを完全管理</item>
/// </list>
/// </remarks>
public class UnitOfWork(
    IDbSessionManager sessionManager,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    /// <summary>
    /// 2重スコープ検出エラーメッセージ
    /// </summary>
    private const string NestedTransactionErrorMessage = """
        Nested transaction detected!
        ExecuteInTransactionAsync was called while another transaction is already active.
        This indicates a design issue.

        Possible causes:
        1. Service calling another service that also uses UoW
        2. Controller mistakenly wrapping service call in UoW
        3. Multiple UoW scopes in the same call chain

        Stack trace will show the call chain.
        """;

    /// <summary>
    /// 破棄時にアクティブなトランザクションがある場合の警告メッセージ
    /// </summary>
    private const string DisposeWithActiveTransactionWarningMessage = """
        ⚠️ UnitOfWork is being disposed with an active transaction.
        This should not happen in normal flow. Rolling back as safety measure.
        """;

    /// <summary>
    /// 破棄時にアクティブなトランザクションがある場合の警告メッセージ（非同期版）
    /// </summary>
    private const string DisposeAsyncWithActiveTransactionWarningMessage = """
        ⚠️ UnitOfWork is being disposed asynchronously with an active transaction.
        This should not happen in normal flow. Rolling back as safety measure.
        """;

    /// <summary>
    /// 破棄フラグ
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// トランザクションスコープの状態を追跡
    /// </summary>
    /// <remarks>
    /// <para>AsyncLocalを使用することで、async/await境界を跨いでも正確にスコープを追跡できます。</para>
    /// <para>同一HTTPリクエスト内（同一非同期コンテキスト）でのみ値が共有され、並行リクエストとは分離されます。</para>
    /// </remarks>
    private static readonly AsyncLocal<bool> IsInTransaction = new();


    /// <inheritdoc />
    public async Task<OperationResult<T>> ExecuteInTransactionAsync<T>(
        Func<Task<OperationResult<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        // 引数チェック
        ArgumentNullException.ThrowIfNull(operation);
        // 破棄チェック
        ObjectDisposedException.ThrowIf(_disposed, this);
        // 2重スコープ検出チェック
        CheckNestedTransaction(IsInTransaction.Value);

        // スコープ開始
        IsInTransaction.Value = true;

        try
        {
            // 1. 接続開始
            await EnsureConnectionOpenAsync(cancellationToken);
            // 2. トランザクション開始
            sessionManager.Transaction = await BeginTransactionAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Transaction started for operation {OperationType}",
                    typeof(T).Name);
            }

            // 3. 操作を実行
            var result = await operation();

            if (result.IsSuccess)
            {
                // 4. コミット
                await CommitTransactionAsync(sessionManager.Transaction, cancellationToken);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Transaction committed successfully for {OperationType}",
                        typeof(T).Name);
                }
            }
            else
            {
                // 4. ロールバック
                await RollbackTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogWarning(
                    "Transaction rolled back due to business failure: {ErrorMessage}",
                    result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogWarning("Transaction rolled back due to cancellation");
            }
            throw;
        }
        catch (Exception ex)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogError(
                    ex,
                    "Transaction rolled back due to exception in {OperationType}",
                    typeof(T).Name);
            }
            throw;
        }
        finally
        {
            if (sessionManager.Transaction != null)
            {
                await DisposeTransactionAsync(sessionManager.Transaction);
                sessionManager.Transaction = null;
            }

            IsInTransaction.Value = false; // スコープ終了
        }
    }


    /// <inheritdoc />
    public async Task<OperationResult> ExecuteInTransactionAsync(
        Func<Task<OperationResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // 引数チェック
        ArgumentNullException.ThrowIfNull(operation);
        // 破棄チェック
        ObjectDisposedException.ThrowIf(_disposed, this);
        // 2重スコープ検出チェック
        CheckNestedTransaction(IsInTransaction.Value);
        // スコープ開始
        IsInTransaction.Value = true;

        try
        {
            // 1. 接続開始
            await EnsureConnectionOpenAsync(cancellationToken);
            // 2. トランザクション開始
            sessionManager.Transaction = await BeginTransactionAsync(cancellationToken);

            logger.LogDebug("Transaction started");

            // 3. 操作を実行
            var result = await operation();

            if (result.IsSuccess)
            {
                // 4. コミット
                await CommitTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogInformation("Transaction committed successfully");
            }
            else
            {
                // 4. ロールバック
                await RollbackTransactionAsync(sessionManager.Transaction, cancellationToken);
                logger.LogWarning(
                    "Transaction rolled back due to business failure: {ErrorMessage}",
                    result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogWarning("Transaction rolled back due to cancellation");
            }
            throw;
        }
        catch (Exception ex)
        {
            if (sessionManager.Transaction != null)
            {
                await RollbackTransactionAsync(sessionManager.Transaction, CancellationToken.None);
                logger.LogError(ex, "Transaction rolled back due to exception");
            }
            throw;
        }
        finally
        {
            if (sessionManager.Transaction != null)
            {
                await DisposeTransactionAsync(sessionManager.Transaction);
                sessionManager.Transaction = null;
            }

            IsInTransaction.Value = false;
        }
    }


    /// <summary>
    /// データベース接続が使用可能な状態であることを保証します。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>接続が Closed の場合: 新規に Open します</item>
    /// <item>接続が Broken の場合: 一度 Close してから再 Open します</item>
    /// <item>DbConnection (非同期対応) と IDbConnection (同期のみ) の両方をサポート</item>
    /// </list>
    /// </remarks>
    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (sessionManager.Connection.State == ConnectionState.Closed)
        {
            await OpenConnectionAsync(cancellationToken);
            logger.LogDebug("Database connection opened");
        }
        else if (sessionManager.Connection.State == ConnectionState.Broken)
        {
            sessionManager.Connection.Close();
            await OpenConnectionAsync(cancellationToken);
            logger.LogWarning("Database connection was broken and has been reopened");
        }
    }

    /// <summary>
    /// データベース接続を非同期で開きます。
    /// </summary>
    /// <remarks>
    /// DbConnection の場合は OpenAsync を使用し、
    /// それ以外の IDbConnection は同期的に Open します。
    /// </remarks>
    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (sessionManager.Connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            sessionManager.Connection.Open();
        }
    }

    /// <summary>
    /// データベーストランザクションを開始します。
    /// </summary>
    /// <remarks>
    /// DbConnection の場合は BeginTransactionAsync を使用し、
    /// それ以外の IDbConnection は同期的に BeginTransaction します。
    /// </remarks>
    private async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (sessionManager.Connection is DbConnection dbConnection)
        {
            return await dbConnection.BeginTransactionAsync(cancellationToken);
        }

        return sessionManager.Connection.BeginTransaction();
    }

    /// <summary>
    /// トランザクションをコミットします。
    /// </summary>
    /// <remarks>
    /// DbTransaction の場合は CommitAsync を使用し、
    /// それ以外の IDbTransaction は同期的に Commit します。
    /// </remarks>
    private static async Task CommitTransactionAsync(
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(cancellationToken);
        }
        else
        {
            transaction.Commit();
        }
    }

    /// <summary>
    /// トランザクションをロールバックします。
    /// </summary>
    /// <remarks>
    /// <para>例外が発生してもログに記録するだけで、再スローしません。</para>
    /// <para>ロールバック失敗は通常、既にエラー状態にあるため、追加の例外は抑制されます。</para>
    /// </remarks>
    private async Task RollbackTransactionAsync(
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            if (transaction is DbTransaction dbTransaction)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
            }
            else
            {
                transaction.Rollback();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction rollback");
        }
    }

    /// <summary>
    /// トランザクションオブジェクトを破棄します。
    /// </summary>
    /// <remarks>
    /// <para>例外が発生してもログに記録するだけで、再スローしません。</para>
    /// <para>Dispose失敗は通常無視されます（ベストエフォート）。</para>
    /// </remarks>
    private async Task DisposeTransactionAsync(IDbTransaction transaction)
    {
        try
        {
            if (transaction is DbTransaction dbTransaction)
            {
                await dbTransaction.DisposeAsync();
            }
            else
            {
                transaction.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction dispose");
        }
    }

    /// <summary>
    /// 2重スコープ検出をチェックします。
    /// </summary>
    private void CheckNestedTransaction(bool isInTransaction){
        if (isInTransaction)
        {
            logger.LogError(NestedTransactionErrorMessage);
            throw new InvalidOperationException(NestedTransactionErrorMessage);
        }
    }


    // ============================================================
    // 標準Dispose Pattern実装
    // UoWが接続の完全なライフサイクルを管理
    // ============================================================

    /// <summary>
    /// トランザクションと接続を破棄し、リソースを解放します。
    /// </summary>
    /// <remarks>
    /// <para><strong>重要: 通常、このメソッドが呼ばれる時点でトランザクションは完了しているべきです。</strong></para>
    /// <para>
    /// アクティブなトランザクションがある場合、これは以下のいずれかを意味します:
    /// </para>
    /// <list type="bullet">
    /// <item>設計違反: ExecuteInTransactionAsync の外でトランザクションが開始された</item>
    /// <item>例外処理漏れ: finally ブロックでトランザクションが破棄されなかった</item>
    /// <item>DIスコープ終了: リクエスト終了時の最終クリーンアップ</item>
    /// </list>
    /// <para>
    /// このメソッドは「最後の砦」として安全にロールバックを試みますが、
    /// 通常のフローでここに到達することは設計上想定されていません。
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// マネージドおよびアンマネージドリソースを解放します。
    /// </summary>
    /// <param name="disposing">
    /// マネージドリソースを解放する場合は true。
    /// ファイナライザーから呼ばれた場合は false（マネージドリソースは既にGC済み）。
    /// </param>
    /// <remarks>
    /// <para><strong>Dispose Pattern実装の標準形式</strong></para>
    /// <list type="bullet">
    /// <item>disposing = true: Dispose() から呼ばれた（全リソース解放可能）</item>
    /// <item>disposing = false: ファイナライザーから呼ばれた（アンマネージドのみ解放）</item>
    /// </list>
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // 1. トランザクションをロールバック＆破棄
            // 注: 通常ここに到達することは設計違反（最後の砦）
            if (sessionManager.Transaction != null)
            {
                logger.LogWarning(DisposeWithActiveTransactionWarningMessage);
            
                try
                {
                    sessionManager.Transaction.Rollback();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error rolling back transaction during disposal");
                }
                finally
                {
                    try
                    {
                        sessionManager.Transaction.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error disposing transaction");
                    }

                    sessionManager.Transaction = null;
                }
            }

            // 2. 接続を閉じて破棄（UoWが完全管理）
            try
            {
                if (sessionManager.Connection.State != ConnectionState.Closed)
                {
                    sessionManager.Connection.Close();
                }

                sessionManager.Connection.Dispose();
                logger.LogDebug("UnitOfWork disposed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing database connection");
            }
        }

        // アンマネージドリソースの解放
        // (この実装では該当なし)

        _disposed = true;
    }

    /// <summary>
    /// 非同期でリソースを解放します。
    /// </summary>
    /// <remarks>
    /// <para>IAsyncDisposable パターンの実装。</para>
    /// <para>非同期リソース（DbConnection, DbTransaction）を適切に破棄します。</para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 非同期リソース解放の実装。
    /// </summary>
    /// <remarks>
    /// <para>アクティブなトランザクションがある場合、ロールバックを試みます。</para>
    /// <para>通常のフローでここに到達することは設計上想定されていません（最後の砦）。</para>
    /// </remarks>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        // 1. トランザクションをロールバック＆破棄
        // 注: 通常ここに到達することは設計違反（最後の砦）
        if (sessionManager.Transaction != null)
        {
            logger.LogWarning(DisposeAsyncWithActiveTransactionWarningMessage);

            try
            {
                if (sessionManager.Transaction is DbTransaction dbTransaction)
                {
                    await dbTransaction.RollbackAsync();
                }
                else
                {
                    sessionManager.Transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rolling back transaction during async disposal");
            }
            finally
            {
                try
                {
                    if (sessionManager.Transaction is DbTransaction dbTrans)
                    {
                        await dbTrans.DisposeAsync();
                    }
                    else
                    {
                        sessionManager.Transaction.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error disposing transaction asynchronously");
                }

                sessionManager.Transaction = null;
            }
        }

        // 2. 接続を閉じて破棄（UoWが完全管理）
        try
        {
            if (sessionManager.Connection.State != ConnectionState.Closed)
            {
                if (sessionManager.Connection is DbConnection dbConnection)
                {
                    await dbConnection.CloseAsync();
                }
                else
                {
                    sessionManager.Connection.Close();
                }
            }

            if (sessionManager.Connection is DbConnection asyncConnection)
            {
                await asyncConnection.DisposeAsync();
            }
            else
            {
                sessionManager.Connection.Dispose();
            }

            logger.LogDebug("UnitOfWork disposed asynchronously");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disposing database connection asynchronously");
        }
    }


    /// <summary>
    /// ファイナライザー（デストラクタ）
    /// </summary>
    /// <remarks>
    /// <para>GC によって呼び出されます。通常は Dispose() または DisposeAsync() が先に呼ばれるため、実行されません。</para>
    /// <para>disposing = false で呼び出すことで、マネージドリソースには触れません。</para>
    /// </remarks>
    ~UnitOfWork()
    {
        Dispose(false);
    }
}


/// <code>
/// public class UnitOfWork : IUnitOfWork
/// {
///     // トランザクション境界 = 接続境界
///     public async Task<OperationResult<T>> ExecuteInTransactionAsync<T>(...)
///     {
///         try
///         {
///             await EnsureConnectionOpenAsync();     // 1. 接続開始
///             transaction = await BeginTransactionAsync(); // 2. トランザクション開始
/// 
///             var result = await operation();
/// 
///             if (result.IsSuccess)
///                 await CommitAsync();               // 3. コミット
///             else
///                 await RollbackAsync();             // 3. ロールバック
///         }
///         finally
///         {
///             await DisposeTransactionAsync();       // 4. トランザクション破棄
///         }
///     }
/// 
///     public void Dispose()
///     {
///         transaction?.Dispose();
///         connection.Close();                        // 5. 接続終了
///         connection.Dispose();
///     }
/// }
/// </code>

