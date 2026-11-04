using Microsoft.Data.Sqlite;
using Prometeus.Core.Data.Repositories;
using System.Collections.Frozen;

namespace Prometeus.Core.Data.Context;

/// <summary>
/// Контекст базы данных используется для централизованого хранения и получения репозиториев, и управления транзакциями
/// </summary>
public sealed class DatabaseContext : IDisposable
{
    private readonly FrozenDictionary<Type, Func<SqliteConnection, RepositoryBase>> factories;
    private SqliteConnection? sharedConnection = null;
    private SqliteTransaction? transaction = null;
    private bool disposed = false;
    private readonly string[] initQueries;
    public string ConnectionString { get; private set; }

    private DatabaseContext(Dictionary<Type, Func<SqliteConnection, RepositoryBase>> dict, string connStr, string[] initQueries)
    {
        factories = dict.ToFrozenDictionary();
        ConnectionString = connStr;
        this.initQueries = initQueries;
    }

    /// <summary>
    /// Используется для получения экземпляра репозитория
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого репозитория</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T Get<T>() where T : RepositoryBase
    {
        ObjectDisposedException.ThrowIf(disposed, typeof(DatabaseContext));
        return factories[typeof(T)](GetConnection()) is T r ? r : throw new InvalidOperationException($"Repository: {nameof(T)} is not registred at this context");
    }

    /// <summary>
    /// Используется для инициализации базы данных
    /// <para>Отвечает только за схему, папки и файл необходимо создать заранее</para>
    /// </summary>
    /// <returns></returns>
    public async Task EnsureCreateAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, typeof(DatabaseContext));
        sharedConnection ??= new SqliteConnection(ConnectionString);
        if (sharedConnection.State != System.Data.ConnectionState.Open)
            sharedConnection.Open();
        foreach (var query in initQueries)
        {
            try
            {
                using var command = sharedConnection.CreateCommand();
                command.CommandText = query;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(query, ex);
            }
        }
    }

    /// <summary>
    /// Начинаяет новую транзакцию
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void BeginTransaction()
    {
        ObjectDisposedException.ThrowIf(disposed, typeof(DatabaseContext));
        if (transaction != null)
            throw new InvalidOperationException("There is an unfinished transaction");
        sharedConnection ??= new SqliteConnection(ConnectionString);
        if (sharedConnection.State != System.Data.ConnectionState.Open)
            sharedConnection.Open();
        transaction = sharedConnection.BeginTransaction();
    }

    /// <summary>
    /// Коммитит изменения провёденные внутри транзакции
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(disposed, typeof(DatabaseContext));
        if (transaction == null)
            throw new InvalidOperationException("There is no transaction to commit");
        transaction.Commit();
        transaction.Dispose();
        transaction = null;
    }

    /// <summary>
    /// Откатывает изменения провёденные внутри транзакции
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Rollback()
    {
        ObjectDisposedException.ThrowIf(disposed, typeof(DatabaseContext));
        if (transaction == null)
            throw new InvalidOperationException("There is no transaction to rollback");
        transaction.Rollback();
        transaction.Dispose();
        transaction = null;
    }

    public void Dispose()
    {
        sharedConnection?.Dispose();
        transaction?.Dispose();
        sharedConnection = null;
        transaction = null;
        disposed = true;
    }

    /// <summary>
    /// Используется для получения соединения с базой данных для выполнения пользовательских запросов
    /// </summary>
    /// <returns></returns>
    public SqliteConnection GetConnection()
    {
        sharedConnection ??= new SqliteConnection(ConnectionString);
        if (sharedConnection.State != System.Data.ConnectionState.Open)
            sharedConnection.Open();
        return sharedConnection;
    }

    /// <summary>
    /// Класс для создания экземпляра DatabaseContext
    /// </summary>
    public sealed class Builder
    {
        private string connStr = "";
        private Dictionary<Type, Func<SqliteConnection, RepositoryBase>> dict = [];
        private readonly List<string> initQueries = [];

        /// <summary>
        /// Указывает использовать ли базу данных в памяти
        /// </summary>
        /// <returns></returns>
        public Builder InMemory()
        {
            connStr = "Data Source = :memory:";
            return this;
        }

        /// <summary>
        /// Указывает путь к файлу базы данных
        /// </summary>
        /// <param name="filePath">Путь к файлу базы данных</param>
        /// <returns></returns>
        public Builder UseFile(string filePath)
        {
            connStr = $"Data Source={filePath}";
            return this;
        }

        /// <summary>
        /// Добавляет новую фабрику для создания репозиториев
        /// </summary>
        /// <typeparam name="T">Тип репозитория</typeparam>
        /// <param name="factory">Фабрика для создания</param>
        /// <returns></returns>
        public Builder Add<T>(Func<SqliteConnection, T> factory) where T : RepositoryBase
        {
            dict[typeof(T)] = conn => factory(conn);
            return this;
        }

        /// <summary>
        /// Добавляет запрос для инициализации схемы базы данных
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Builder AddInitializationQuery(string query)
        {
            if (initQueries.Contains(query))
                throw new InvalidOperationException("This query already exists");

            initQueries.Add(query);
            return this;
        }

        /// <summary>
        /// Удаляет фабрику для создания репозитория
        /// </summary>
        /// <typeparam name="T">Тип репозитория для удаления</typeparam>
        /// <returns></returns>
        public Builder Remove<T>() where T : RepositoryBase
        {
            dict.Remove(typeof(T));
            return this;
        }

        /// <summary>
        /// Удаляет запрос инициализации схемы базы данных
        /// </summary>
        /// <param name="query">Запрос инициализации</param>
        /// <returns></returns>
        public Builder RemoveInitializationQuery(string query)
        {
            initQueries.Remove(query);
            return this;
        }

        /// <summary>
        /// Очищает набор фабрик для создания репозиториев
        /// </summary>
        /// <returns></returns>
        public Builder Clear()
        {
            dict.Clear();
            return this;
        }

        /// <summary>
        /// Очищает список запросов инициализации схемы базы данных
        /// </summary>
        /// <returns></returns>
        public Builder ClearInitializationQueries()
        {
            initQueries.Clear();
            return this;
        }

        /// <summary>
        /// Создаёт новый экземпляр базы данных
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public DatabaseContext Build()
        {
            if (dict.Count == 0)
                throw new InvalidOperationException("Repository factories is not configured. Call Add<T> first.");
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("Connection string is not configured. Call UseMemory() or UseFile() first.");
            return new DatabaseContext(dict, connStr, [.. initQueries]);
        }

    }
}
