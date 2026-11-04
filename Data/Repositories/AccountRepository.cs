using Microsoft.Data.Sqlite;
using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Extensions;
using Prometeus.Core.Data.Queries.DML;
using System.Data;
using TimeMachine.Core;

namespace Prometeus.Core.Data.Repositories;

public class AccountRepository(SqliteConnection connection) : RepositoryBase(connection)
{
    /// <summary>
    /// Проверяет счёт на существование по его номеру
    /// </summary>
    /// <param name="number">Номер счёта для проверки</param>
    /// <returns>true если счёт с таким номером существует, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsExistsAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.IsExists;
        command.Parameters.AddWithValue("@Number", number);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет можно ли пополнить счёт
    /// </summary>
    /// <param name="number">Номер счёта для проверки</param>
    /// <returns>true если счёт возможно пополнить, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> PossibleCreditAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.PossibleCredit;
        command.Parameters.AddWithValue("@Number", number);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет есть ли у клиента активные счета
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <returns>true если есть активные счета, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> HasActiveAccountsAsync(Guid clientId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.HasActiveAccounts;
        command.Parameters.AddWithValue("@ClientId", clientId.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет можно ли списать со счёта средства
    /// </summary>
    /// <param name="number">Номер счёта для проверки</param>
    /// <param name="amount">Сумма которую необходимо списать</param>
    /// <returns>true если с указанного счёта можно списать указанную сумму, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> PossibleDebitAsync(string number, decimal amount)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.PossibleDebit;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@Amount", amount);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет принадлежит ли указанный счёт клиенту
    /// </summary>
    /// <param name="number">Номер счёта для проверки</param>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <returns>true если указанный счёт принадлежит клиенту, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> BelongsToClientAsync(string number, Guid clientId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.BelongsToClient;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@ClientId", clientId.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Получает идентификатор клиента по номеру счёта
    /// </summary>
    /// <param name="number">Номер счёта</param>
    /// <returns>Guid клиента, если с таким номером существует, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Guid?> GetClientByNumberAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.GetClientByNumber;
        command.Parameters.AddWithValue("@Number", number);
        var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Guid.Parse(reader.GetString("ClientId")) : null;
    }

    /// <summary>
    /// Получает информацию о счёте по указанному номеру
    /// </summary>
    /// <param name="number">Номер счёта</param>
    /// <returns>Account если счёт с указанным номером существует, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Account?> GetByNumberAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.GetByNumber;
        command.Parameters.AddWithValue("@Number", number);
        var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new Account
        {
            ClientId = reader.GetGuid("ClientId"),
            Number = reader.GetString("Number"),
            Name = reader.GetString("Name"),
            OpenedAt = reader.GetDateTime("OpenedAt"),
            ClosedAt = null,
            Balance = reader.GetDecimal("Balance")
        } : null;
    }

    /// <summary>
    /// Получает список счётов принадлежащих клиенту
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <returns>Список счетов клиента</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<List<Account>> GetByClientIdAsync(Guid clientId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.GetByClientId;
        command.Parameters.AddWithValue("ClientId", clientId.ToString());
        var reader = await command.ExecuteReaderAsync();
        var list = new List<Account>();
        while (await reader.ReadAsync())
        {
            list.Add(new Account
            {
                ClientId = clientId,
                Number = reader.GetString("Number"),
                Name = reader.GetString("Name"),
                OpenedAt = reader.GetDateTime("OpenedAt"),
                ClosedAt = null,
                Balance = reader.GetDecimal("Balance")
            });
        }
        return list;
    }

    /// <summary>
    /// Открывает новый счёт
    /// </summary>
    /// <param name="account">Информация о счёте</param>
    /// <returns>Информацию о счёте в случае успешного открытия счёта, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Account?> OpenAsync(Account account)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.Open;
        command.Parameters.AddWithValue("@ClientId", account.ClientId.ToString());
        command.Parameters.AddWithValue("@Number", account.Number);
        command.Parameters.AddWithValue("@Name", account.Name);
        command.Parameters.AddWithValue("@OpenedAt", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());

        var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new Account
        {
            ClientId = reader.GetGuid("ClientId"),
            Number = reader.GetString("Number"),
            Name = reader.GetString("Name"),
            OpenedAt = reader.GetDateTime("OpenedAt"),
            ClosedAt = null,
            Balance = 0
        } : null;
    }

    /// <summary>
    /// Изменяет имя счёта
    /// </summary>
    /// <returns>Новое имя счёта в случае успеха, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<string?> RenameAsync(string number, string newName)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.Rename;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@Name", newName);

        var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? reader.GetString("Name") : null;
    }

    /// <summary>
    /// Закрывает счёт, с указанным номером
    /// </summary>
    /// <param name="number">Номер счёта</param>
    /// <returns>true если удалось закрыть счёт, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> CloseAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.Close;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@ClosedAt", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Списывает указанную сумму со счёта
    /// <para><b>Внимание: сумма должна быть БОЛЬШЕ нуля и МЕНЬШЕ / РАВНА балансу счёта, иначе списания не произойдёт</b></para>
    /// </summary>
    /// <param name="number">Номер счёта</param>
    /// <param name="amount">Сумма для списания</param>
    /// <returns>true если удалось списать средства иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> DebitAsync(string number, decimal amount)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.Debit;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@Amount", amount);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Зачисляет указанную сумму на счёт
    /// <para><b>Внимание: сумма должна быть БОЛЬШЕ нуля, иначе зачисление не произойдёт</b></para>
    /// </summary>
    /// <param name="number">Номер счёта</param>
    /// <param name="amount">Сумма для зачисления</param>
    /// <returns>true если удалось зачислить средства, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> CreditAsync(string number, decimal amount)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Accounts.Credit;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@Amount", amount);
        return await command.ExecuteNonQueryAsync() > 0;
    }
}
