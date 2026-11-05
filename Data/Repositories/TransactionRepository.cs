using Microsoft.Data.Sqlite;
using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Extensions;
using Prometeus.Core.Data.Queries.DML;
using System.Data;
using System.Text.Json;
using TimeMachine.Core;

namespace Prometeus.Core.Data.Repositories;

/// <summary>
/// Репозиторий для работы с транзакциями и их проводками
/// </summary>
/// <param name="connection">Соединение с базой данных SQLite</param>
public class TransactionRepository(SqliteConnection connection) : RepositoryBase(connection)
{
    /// <summary>
    /// Проверяет существование транзакции по идентификатору
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>true если транзакция существует, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsExistsAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.IsExists;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет завершена ли транзакция
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>true если транзакция завершена, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsCompletedAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.IsCompleted;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет отклонена ли транзакция
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>true если транзакция отклонена, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsRejectedAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.IsRejected;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет возможность завершения транзакции
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>true если транзакцию можно завершить, иначе false</returns>
    /// <remarks>
    /// Транзакцию можно завершить, если:
    /// <list type="bullet">
    /// <item><description>Она существует и не завершена</description></item>
    /// <item><description>Количество проводок не менее 2</description></item>
    /// <item><description>Сумма всех проводок с учётом знака = 0</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> CanCompleteAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.CanComplete;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Создаёт новую транзакцию
    /// </summary>
    /// <returns>Transaction с заполненными данными если удалось создать, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Transaction?> CreateAsync()
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.Create;
        command.Parameters.AddWithValue("@Id", Guid.CreateVersion7().ToString());
        command.Parameters.AddWithValue("@CurrentDateTime", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());

        var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new Transaction
        {
            Id = reader.GetGuid("Id"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            Status = TransactionStatus.Pending,
            Metadata = []
        } : null;
    }

    /// <summary>
    /// Создаёт новую проводку для транзакции
    /// </summary>
    /// <param name="entry">Данные проводки</param>
    /// <returns>TransactionEntry если удалось создать, иначе null</returns>
    /// <remarks>
    /// Проводка создаётся только если:
    /// <list type="bullet">
    /// <item><description>Транзакция существует</description></item>
    /// <item><description>Счёт существует и не закрыт</description></item>
    /// <item><description>Сумма больше 0</description></item>
    /// <item><description>Тип -1 (Source) или 1 (Destination)</description></item>
    /// <item><description>Проводка с таким Id ещё не существует</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<TransactionEntry?> CreateEntryAsync(TransactionEntry entry)
    {
        using var command = Connection.CreateCommand();
        var newId = Guid.CreateVersion7();
        command.CommandText = Transactions.CreateEntry;
        command.Parameters.AddWithValue("@Id", newId.ToString());
        command.Parameters.AddWithValue("@TransactionId", entry.TransactionId.ToString());
        command.Parameters.AddWithValue("@AccountNumber", entry.AccountNumber);
        command.Parameters.AddWithValue("@Amount", entry.Amount);
        command.Parameters.AddWithValue("@Type", (int)entry.Type);
        
        var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            entry = entry with { Id = newId }; // ← ПРИСВАИВАЕМ!
            return entry;
        }
        return null;
    }

    /// <summary>
    /// Получает транзакцию по идентификатору со всеми проводками
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>Transaction с проводками или null, если не найдена</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Transaction?> GetByIdAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.GetById;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        var reader = await command.ExecuteReaderAsync();
        Transaction? transaction = null;
        while (await reader.ReadAsync())
        {
            transaction ??= new Transaction
                {
                    Id = reader.GetGuid("Id"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    CompletedAt = reader.IsDBNull("CompletedAt") ? null : reader.GetDateTime("CompletedAt"),
                    RejectReason = reader.IsDBNull("RejectReason") ? null : reader.GetString("RejectReason"),
                    Status = (TransactionStatus)reader.GetInt32("Status"),
                    Metadata = ParseMetadata(reader.GetString("Metadata"))
                };

            if (!reader.IsDBNull("TransactionEntryId"))
            {
                transaction.Entries.Add(new TransactionEntry
                {
                    Id = reader.GetGuid("TransactionEntryId"),
                    TransactionId = transaction.Id,
                    AccountNumber = reader.GetString("TransactionEntryAccountNumber"),
                    Amount = reader.GetDecimal("TransactionEntryAmount"),
                    Type = (EntryType)reader.GetInt32("TransactionEntryType")
                });
            }
        }
        return transaction;
    }

    /// <summary>
    /// Получает все транзакции, в которых участвует указанный счёт
    /// </summary>
    /// <param name="accountNumber">Номер счёта</param>
    /// <param name="count">Количество записей на странице</param>
    /// <param name="page">Номер страницы (начиная с 1)</param>
    /// <returns>Список транзакций со всеми проводками</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<List<Transaction>> GetAllForAccountAsync(string accountNumber, int count, int page)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.GetAllForAccount;
        command.Parameters.AddWithValue("@AccountNumber", accountNumber);
        command.Parameters.AddWithValue("@Count", count);
        command.Parameters.AddWithValue("@Page", page);
        var reader = await command.ExecuteReaderAsync();
        var transactions = new Dictionary<Guid, Transaction>();
        while (await reader.ReadAsync())
        {
            var id = reader.GetGuid("Id");
            if (!transactions.TryGetValue(id, out var transaction))
            {
                transaction = new Transaction
                {
                    Id = id,
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    CompletedAt = reader.IsDBNull("CompletedAt") ? null : reader.GetDateTime("CompletedAt"),
                    RejectReason = reader.IsDBNull("RejectReason") ? null : reader.GetString("RejectReason"),
                    Status = (TransactionStatus)reader.GetInt32("Status"),
                    Metadata = ParseMetadata(reader.GetString("Metadata"))
                };
                transactions[id] = transaction;
            }

            if (!reader.IsDBNull("TransactionEntryId"))
            {
                transaction.Entries.Add(new TransactionEntry
                {
                    Id = reader.GetGuid("TransactionEntryId"),
                    TransactionId = id,
                    AccountNumber = reader.GetString("TransactionEntryAccountNumber"),
                    Amount = reader.GetDecimal("TransactionEntryAmount"),
                    Type = (EntryType)reader.GetInt32("TransactionEntryType")
                });
            }
        }
        return [.. transactions.Values];
    }

    /// <summary>
    /// Получает все транзакции, в которых участвуют счета указанного клиента
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <param name="count">Количество записей на странице</param>
    /// <param name="page">Номер страницы (начиная с 1)</param>
    /// <returns>Список транзакций со всеми проводками</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<List<Transaction>> GetAllForClientAsync(Guid clientId, int count, int page)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.GetAllForClient;
        command.Parameters.AddWithValue("@ClientId", clientId.ToString());
        command.Parameters.AddWithValue("@Count", count);
        command.Parameters.AddWithValue("@Page", page);
        var reader = await command.ExecuteReaderAsync();
        var transactions = new Dictionary<Guid, Transaction>();
        while (await reader.ReadAsync())
        {
            var id = reader.GetGuid("Id");
            if (!transactions.TryGetValue(id, out var transaction))
            {
                transaction = new Transaction
                {
                    Id = id,
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    CompletedAt = reader.IsDBNull("CompletedAt") ? null : reader.GetDateTime("CompletedAt"),
                    RejectReason = reader.IsDBNull("RejectReason") ? null : reader.GetString("RejectReason"),
                    Status = (TransactionStatus)reader.GetInt32("Status"),
                    Metadata = ParseMetadata(reader.GetString("Metadata"))
                };
                transactions[id] = transaction;
            }

            if (!reader.IsDBNull("TransactionEntryId"))
            {
                transaction.Entries.Add(new TransactionEntry
                {
                    Id = reader.GetGuid("TransactionEntryId"),
                    TransactionId = id,
                    AccountNumber = reader.GetString("TransactionEntryAccountNumber"),
                    Amount = reader.GetDecimal("TransactionEntryAmount"),
                    Type = (EntryType)reader.GetInt32("TransactionEntryType")
                });
            }
        }
        return [.. transactions.Values];
    }

    /// <summary>
    /// Получает метаданные транзакции
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>Словарь метаданных или null, если транзакция не найдена</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Dictionary<string, string>?> GetMetadataByIdAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.GetMetadataById;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        var result = await command.ExecuteScalarAsync();
        if (result == null) return null;
        var json = result.ToString()!;
        return string.IsNullOrEmpty(json)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    /// <summary>
    /// Получает статус транзакции
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>Статус транзакции или null, если не найдена</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<TransactionStatus?> GetStatusByIdAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.GetStatusById;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        var result = await command.ExecuteScalarAsync();
        return result != null ? (TransactionStatus)Convert.ToInt32(result) : null;
    }

    /// <summary>
    /// Обновляет метаданные транзакции
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <param name="metadata">Словарь метаданных</param>
    /// <returns>true если удалось обновить, иначе false</returns>
    /// <remarks>
    /// Обновление возможно только для незавершённой транзакции.
    /// Метаданные сериализуются в JSON и проверяются через json_valid.
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> UpdateMetadataAsync(Guid transactionId, Dictionary<string, string> metadata)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.UpdateMetadata;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(metadata));
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Завершает транзакцию
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>true если удалось завершить, иначе false</returns>
    /// <remarks>
    /// Завершение возможно только если:
    /// <list type="bullet">
    /// <item><description>Транзакция не завершена</description></item>
    /// <item><description>Количество проводок не менее 2</description></item>
    /// <item><description>Сумма всех проводок с учётом знака = 0</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> CompleteAsync(Guid transactionId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.Complete;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        command.Parameters.AddWithValue("@CurrentDateTime", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Отклоняет транзакцию с указанием причины
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <param name="rejectReason">Причина отклонения (не пустая строка)</param>
    /// <returns>true если удалось отклонить, иначе false</returns>
    /// <remarks>
    /// Отклонение возможно только если:
    /// <list type="bullet">
    /// <item><description>Транзакция не завершена</description></item>
    /// <item><description>Причина не пустая</description></item>
    /// <item><description>Количество проводок не менее 2</description></item>
    /// <item><description>Сумма всех проводок с учётом знака = 0</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> RejectAsync(Guid transactionId, string rejectReason)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Transactions.Reject;
        command.Parameters.AddWithValue("@Id", transactionId.ToString());
        command.Parameters.AddWithValue("@CurrentDateTime", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        command.Parameters.AddWithValue("@RejectReason", rejectReason);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static Dictionary<string, string> ParseMetadata(string json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}