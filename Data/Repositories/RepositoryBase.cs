using Microsoft.Data.Sqlite;

namespace Prometeus.Core.Data.Repositories;

public abstract class RepositoryBase(SqliteConnection connection) : IDisposable
{
    protected SqliteConnection Connection => connection;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Connection.Dispose();
    }
}
