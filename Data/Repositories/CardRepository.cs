using Microsoft.Data.Sqlite;
using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Extensions;
using Prometeus.Core.Data.Queries.DML;
using System.Data;
using System.Reflection.PortableExecutable;
using TimeMachine.Core;

namespace Prometeus.Core.Data.Repositories;

/// <summary>
/// Репозиторий для работы с банковскими картами
/// </summary>
/// <param name="connection">Соединение с базой данных SQLite</param>
public class CardRepository(SqliteConnection connection) : RepositoryBase(connection)
{
    /// <summary>
    /// Проверяет существование карты по номеру
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>true если карта существует, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsExistsAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.IsExists;
        command.Parameters.AddWithValue("@Number", number);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет активна ли карта
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>true если карта активна, иначе false</returns>
    /// <remarks>
    /// Карта считается активной, если:
    /// <list type="bullet">
    /// <item><description>Срок действия не истёк (ValidUntil >= текущая дата)</description></item>
    /// <item><description>Статус = 0 (Active)</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsActiveAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.IsActive;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Получает базовую информацию о карте по её номеру
    /// <para>В сущности поля PinHash и CvcKeyId будут установлены в дефолтные значения</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns>Card с базовой информацией если удалось получить, иначе null</returns>
    public async Task<Card?> GetByNumberAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.GetByNumber;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new Card()
        {
            Number = reader.GetString("Number"),
            AccountNumber = reader.GetString("AccountNumber"),
            PinHash = string.Empty,
            CvcKeyId = Guid.Empty,
            CardHolderName = reader.GetString("CardHolderName"),
            ValidUntil = reader.GetDateOnly("ValidUntil"),
            Status = (CardStatus)reader.GetInt32("Status")
        } : null;
    }

    /// <summary>
    /// Открывает новую карту
    /// </summary>
    /// <param name="card">Данные карты</param>
    /// <returns>Card с заполненными данными если удалось открыть, иначе null</returns>
    /// <remarks>
    /// Проверки перед открытием:
    /// <list type="bullet">
    /// <item><description>Счёт существует и не закрыт</description></item>
    /// <item><description>Карта с таким номером ещё не существует</description></item>
    /// <item><description>Длина номера 12-19 цифр</description></item>
    /// <item><description>Номер не начинается с 0</description></item>
    /// <item><description>Дата действия в формате YYYY-MM-DD</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<Card?> OpenAsync(Card card)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.Open;
        command.Parameters.AddWithValue("@Number", card.Number);
        command.Parameters.AddWithValue("@AccountNumber", card.AccountNumber);
        command.Parameters.AddWithValue("@PinHash", card.PinHash);
        command.Parameters.AddWithValue("@CvcKeyId", card.CvcKeyId.ToString());
        command.Parameters.AddWithValue("@CardHolderName", card.CardHolderName);
        command.Parameters.AddWithValue("@ValidUntil", card.ValidUntil.ToFormattedString());
        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new Card()
        {
            Number = reader.GetString("Number"),
            AccountNumber = reader.GetString("AccountNumber"),
            PinHash = reader.GetString("PinHash"),
            CvcKeyId = reader.GetGuid("CvcKeyId"),
            CardHolderName = reader.GetString("CardHolderName"),
            ValidUntil = reader.GetDateOnly("ValidUntil"),
            Status = 0
        } : null;
    }

    /// <summary>
    /// Получает номер счёта, к которому привязана карта
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>Номер счёта если карта активна, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<string?> GetAccountNumberAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.GetAccountNumber;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        var res = await command.ExecuteScalarAsync();
        return res?.ToString();
    }

    /// <summary>
    /// Получает хешированный PIN-код карты
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>Хеш PIN-кода если карта активна, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<string?> GetPinHashAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.GetPinHash;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        var res = await command.ExecuteScalarAsync();
        return res?.ToString();
    }

    /// <summary>
    /// Изменяет хеш PIN-кода карты
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <param name="newPinHash">Новый хеш PIN-кода</param>
    /// <returns>true если удалось изменить, иначе false</returns>
    /// <remarks>
    /// Изменение возможно только для активной карты и если новый хеш отличается от старого
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> ChangePinHashAsync(string number, string newPinHash)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.ChangePinHash;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@PinHash", newPinHash);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Получает идентификатор ключа в KeyStore, по которому хранится CVC
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>Guid ключа если карта активна, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Guid?> GetCvcKeyIdAsync(string number)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.GetCvcKeyId;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        var res = await command.ExecuteScalarAsync();
        return Guid.TryParse(res?.ToString(), out var keyId) ? keyId : null;
    }

    /// <summary>
    /// Получает все активные карты, привязанные к указанному счёту
    /// </summary>
    /// <param name="accountNumber">Номер счёта</param>
    /// <returns>Список карт (без заблокированных)</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<List<Card>> GetAllByAccountNumberAsync(string accountNumber)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Cards.GetAllByAccountNumber;
        command.Parameters.AddWithValue("@AccountNumber", accountNumber);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        var list = new List<Card>();
        var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            list.Add(new Card()
            {
                AccountNumber = accountNumber,
                Number = reader.GetString("Number"),
                CardHolderName = reader.GetString("CardHolderName"),
                ValidUntil = reader.GetDateOnly("ValidUntil"),
                Status = (CardStatus)reader.GetInt32("Status")
            });
        }
        return list;
    }

    /// <summary>
    /// Замораживает карту (временная блокировка)
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>true если удалось заморозить, иначе false</returns>
    /// <remarks>
    /// Заморозить можно только активную карту с неистёкшим сроком действия
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> FreezeAsync(string number)
    {
        var command = Connection.CreateCommand();
        command.CommandText = Cards.Freeze;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Размораживает карту
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>true если удалось разморозить, иначе false</returns>
    /// <remarks>
    /// Разморозить можно только замороженную карту (Status = 1) с неистёкшим сроком действия
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> UnfreezeAsync(string number)
    {
        var command = Connection.CreateCommand();
        command.CommandText = Cards.Unfreeze;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return await command.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Блокирует карту (перманентно)
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>true если удалось заблокировать, иначе false</returns>
    /// <remarks>
    /// Заблокировать можно любую карту с неистёкшим сроком действия, кроме уже заблокированной
    /// </remarks>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> LockAsync(string number)
    {
        var command = Connection.CreateCommand();
        command.CommandText = Cards.Lock;
        command.Parameters.AddWithValue("@Number", number);
        command.Parameters.AddWithValue("@CurrentDate", SharedTimeProvider.Read(DateTime.UtcNow).ToFormattedString());
        return await command.ExecuteNonQueryAsync() > 0;
    }
}