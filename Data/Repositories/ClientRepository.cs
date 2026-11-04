using Microsoft.Data.Sqlite;
using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Extensions;
using Prometeus.Core.Data.Queries.DML;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Prometeus.Core.Data.Repositories;

public class ClientRepository(SqliteConnection connection) : RepositoryBase(connection)
{
    /// <summary>
    /// Проверяет существует клиент с указанным идентификатором
    /// </summary>
    /// <param name="id">Идентификатор клиента</param>
    /// <returns>true если существует, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> IsExistsAsync(Guid id)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.IsExists;
        command.Parameters.AddWithValue("@Id", id.ToString());
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет используется ли данный адрес электронной почты
    /// </summary>
    /// <param name="email">Адрес электронной почты</param>
    /// <returns>true если используется, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> EmailAlreadyUsedAsync(string email)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.EmailAlreadyUsed;
        command.Parameters.AddWithValue("@Email", email);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Проверяет используется ли данный номер телефона
    /// </summary>
    /// <param name="phone">Номер телефона</param>
    /// <returns>true если используется, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> PhoneAlreadyUsedAsync(string phone)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.PhoneAlreadyUsed;
        command.Parameters.AddWithValue("@Phone", phone);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    /// <summary>
    /// Получает идентификатор клиента по адресу электронной почты
    /// </summary>
    /// <param name="email">Адрес электронной почты клиентa</param>
    /// <returns>Guid если клиент найден, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Guid?> GetIdByEmailAsync(string email)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.GetIdByEmail;
        command.Parameters.AddWithValue("@Email", email);
        return Guid.TryParse(Convert.ToString(await command.ExecuteScalarAsync()), out var id) ? id : null;
    }

    /// <summary>
    /// Получает идентификатор клиента по номеру телефона
    /// </summary>
    /// <param name="phone">Номер телефона клиентa</param>
    /// <returns>Guid если клиент найден, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Guid?> GetIdByPhoneAsync(string phone)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.GetIdByPhone;
        command.Parameters.AddWithValue("@Phone", phone);
        return Guid.TryParse(Convert.ToString(await command.ExecuteScalarAsync()), out var id) ? id : null;
    }

    /// <summary>
    /// Получает информацию о клиенте по идентификатору
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Новый объект Client если удалось найти клиента, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Client?> GetByIdAsync(Guid id)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.GetById;
        command.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = await command.ExecuteReaderAsync();
        return (await reader.ReadAsync()) ? new Client
        {
            Id = reader.GetGuid("Id"),
            Name = reader.GetString("Name"),
            Surname = reader.GetString("Surname"),
            Patronimyc = !reader.IsDBNull("Patronimyc") ? reader.GetString("Patronimyc") : null,
            BirthDate = reader.GetDateOnly("BirthDate"),
            Email = reader.GetString("Email"),
            Phone = reader.GetString("Phone")
        } : null;
    }

    /// <summary>
    /// Добавляет нового клиента
    /// </summary>
    /// <param name="client">Информация о клиенте</param>
    /// <returns>Client с новым идентификатором если удалось добавить, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Client?> AddAsync(Client client)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.Add;
        command.Parameters.AddWithValue("@Id", Guid.CreateVersion7().ToString());
        command.Parameters.AddWithValue("@Name", client.Name);
        command.Parameters.AddWithValue("@Surname", client.Surname);
        command.Parameters.AddWithValue("@Patronimyc", !string.IsNullOrWhiteSpace(client.Patronimyc) ? client.Patronimyc : DBNull.Value);
        command.Parameters.AddWithValue("@BirthDate", client.BirthDate.ToFormattedString());
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Phone", client.Phone);

        using var reader = await command.ExecuteReaderAsync();
        return (await reader.ReadAsync()) ? new Client
        {
            Id = reader.GetGuid("Id"),
            Name = reader.GetString("Name"),
            Surname = reader.GetString("Surname"),
            Patronimyc = !reader.IsDBNull("Patronimyc") ? reader.GetString("Patronimyc") : null,
            BirthDate = reader.GetDateOnly("BirthDate"),
            Email = reader.GetString("Email"),
            Phone = reader.GetString("Phone")
        } : null;
    }

    /// <summary>
    /// Обновляет информацию о клиенте
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <param name="name">Новое имя клиента</param>
    /// <param name="surname">Новая фамилия клиента</param>
    /// <param name="patronimyc">Новое отчество клиента</param>
    /// <param name="birthDate">Новая дата рождения клиента</param>
    /// <returns>Client если удалось обновить, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Client?> UpdateAsync(Guid clientId, string? name = null, string? surname = null, string? patronimyc = null, DateOnly? birthDate = null)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.Update;
        command.Parameters.AddWithValue("@Id", clientId.ToString());
        command.Parameters.AddWithValue("@Name", !string.IsNullOrWhiteSpace(name) ? name : DBNull.Value);
        command.Parameters.AddWithValue("@Surname", !string.IsNullOrWhiteSpace(surname) ? surname : DBNull.Value);
        command.Parameters.AddWithValue("@Patronimyc", !string.IsNullOrWhiteSpace(patronimyc) ? patronimyc : DBNull.Value);
        command.Parameters.AddWithValue("@BirthDate", birthDate != null ? birthDate.Value.ToFormattedString() : DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        return (await reader.ReadAsync()) ? new Client
        {
            Id = reader.GetGuid("Id"),
            Name = reader.GetString("Name"),
            Surname = reader.GetString("Surname"),
            Patronimyc = !reader.IsDBNull("Patronimyc") ? reader.GetString("Patronimyc") : null,
            BirthDate = reader.GetDateOnly("BirthDate"),
            Email = reader.GetString("Email"),
            Phone = reader.GetString("Phone")
        } : null;
    }

    /// <summary>
    /// Изменяет адрес электронной почты клиента
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <param name="email">Новый адрес электронной почты</param>
    /// <returns>Client если удалось изменить адрес электронной почты, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Client?> ChangeEmailAsync(Guid clientId, string email)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.ChangeEmail;
        command.Parameters.AddWithValue("@Id", clientId.ToString());
        command.Parameters.AddWithValue("@Email", email);

        using var reader = await command.ExecuteReaderAsync();
        return (await reader.ReadAsync()) ? new Client
        {
            Id = reader.GetGuid("Id"),
            Name = reader.GetString("Name"),
            Surname = reader.GetString("Surname"),
            Patronimyc = !reader.IsDBNull("Patronimyc") ? reader.GetString("Patronimyc") : null,
            BirthDate = reader.GetDateOnly("BirthDate"),
            Email = reader.GetString("Email"),
            Phone = reader.GetString("Phone")
        } : null;
    }

    /// <summary>
    /// Изменяет номер телефона клиента
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <param name="phone">Новый номер телефона</param>
    /// <returns>Client если удалось изменить номер телефона, иначе null</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<Client?> ChangePhoneAsync(Guid clientId, string phone)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.ChangePhone;
        command.Parameters.AddWithValue("@Id", clientId.ToString());
        command.Parameters.AddWithValue("@Phone", phone);

        using var reader = await command.ExecuteReaderAsync();
        return (await reader.ReadAsync()) ? new Client
        {
            Id = reader.GetGuid("Id"),
            Name = reader.GetString("Name"),
            Surname = reader.GetString("Surname"),
            Patronimyc = !reader.IsDBNull("Patronimyc") ? reader.GetString("Patronimyc") : null,
            BirthDate = reader.GetDateOnly("BirthDate"),
            Email = reader.GetString("Email"),
            Phone = reader.GetString("Phone")
        } : null;
    }

    /// <summary>
    /// Удаляет клиента
    /// </summary>
    /// <param name="clientId">Идентификатор клиента</param>
    /// <returns>true если удалось удалить, иначе false</returns>
    /// <exception cref="SqliteException"></exception>
    public async Task<bool> DeleteAsync(Guid clientId)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = Clients.Delete;
        command.Parameters.AddWithValue("@Id", clientId.ToString());
        return await command.ExecuteNonQueryAsync() > 0;
    }
}
