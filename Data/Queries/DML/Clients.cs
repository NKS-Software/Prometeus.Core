//strpsql-on
namespace Prometeus.Core.Data.Queries.DML;

internal static class Clients
{
    /// <summary>
    /// Запрос для проверки существования клиента по идентификатору
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// </summary>
    public static string IsExists => """
        SELECT EXISTS(SELECT TRUE FROM Clients WHERE Id = @Id)
        """;

    /// <summary>
    /// Запрос для проверки занят ли адрес электронной почты
    /// <para>Параметры:</para>
    /// <para>@Email - адрес электронной почты (длина не более 256 символов)</para>
    /// </summary>
    public static string EmailAlreadyUsed => """
        SELECT EXISTS(SELECT TRUE FROM Clients WHERE Email = @Email)
        """;

    /// <summary>
    /// Запрос для проверки занят ли номер телефона
    /// <para>Параметры:</para>
    /// <para>@Phone - номер телефона (длина не более 16 символов, начинается с "+")</para>
    /// </summary>
    public static string PhoneAlreadyUsed => """
        SELECT EXISTS(SELECT TRUE FROM Clients WHERE Phone = @Phone)
        """;

    /// <summary>
    /// Запрос для получения идентфикатора клиента по адресу электронной почты
    /// <para>Параметры:</para>
    /// <para>@Email - адрес электронной почты (длина не более 256 символов)</para>
    /// </summary>
    public static string GetIdByEmail => """
        SELECT Id FROM Clients WHERE Email = @Email
        """;

    /// <summary>
    /// Запрос для получения идентфикатора клиента по номеру телефона
    /// <para>Параметры:</para>
    /// <para>@Phone - номер телефона (длина не более 16 символов, начинается с "+")</para>
    /// </summary>
    public static string GetIdByPhone => """
        SELECT Id FROM Clients WHERE Phone = @Phone
        """;

    /// <summary>
    /// Запрос для получения информации о клиенте по идентификатору
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// </summary>
    public static string GetById => """
        SELECT * FROM Clients WHERE Id = @Id
        """;

    /// <summary>
    /// Запрос для добавления клиента
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// <para>@Name - имя клиента(длина не более 128 символов)</para>
    /// <para>@Surname - фамилия клиента(длина не более 128 символов)</para>
    /// <para>@Patronimyc - отчество клиента(длина не более 128 символов, опционально)</para>
    /// <para>@BirthDate - дата рождения клиента(DateOnly в формате строки "yyyy-MM-dd")</para>
    /// <para>@Email - адрес электронной почты (длина не более 256 символов)</para>
    /// <para>@Phone - номер телефона (длина не более 16 символов, начинается с "+")</para>
    /// </summary>
    public static string Add => """
        INSERT INTO Clients(Id, Name, Surname, Patronimyc, BirthDate, Email, Phone)
        SELECT @Id, @Name, @Surname, @Patronimyc, @BirthDate, @Email, @Phone
        WHERE TRUE
            AND NOT EXISTS(SELECT TRUE FROM Clients WHERE Email = @Email)
            AND NOT EXISTS(SELECT TRUE FROM Clients WHERE Phone = @Phone)
            AND @BirthDate GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
            AND LENGTH(@BirthDate) = 10
            AND @Email LIKE '%@%.%'
            AND LENGTH(@Email) <= 256
            AND @Phone LIKE '+%'
            AND LENGTH(@Phone) <= 16 AND LENGTH(@Phone) > 10
        RETURNING *
        """;

    /// <summary>
    /// Запрос для обновления данных клиента
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// <para>@Name - новое имя клиента(длина не более 128 символов, опционально)</para>
    /// <para>@Surname - новая фамилия клиента(длина не более 128 символов, опционально)</para>
    /// <para>@Patronimyc - новое отчество клиента(длина не более 128 символов, опционально)</para>
    /// <para>@BirthDate - новая дата рождения клиента(DateOnly в формате строки "yyyy-MM-dd")</para>
    /// </summary>
    public static string Update => """
        UPDATE Clients SET
            Name = CASE WHEN(Name != @Name AND @Name IS NOT NULL AND @Name != '' AND LENGTH(@Name) <= 128) THEN @Name ELSE Name END,
            Surname = CASE WHEN(Surname != @Surname AND @Surname IS NOT NULL AND @Surname != '' AND LENGTH(@Surname) <= 128) THEN @Surname ELSE Surname END,
            Patronimyc = CASE WHEN(Patronimyc != @Patronimyc AND LENGTH(@Patronimyc) <= 128) THEN @Patronimyc ELSE Patronimyc END,
            BirthDate = CASE WHEN(BirthDate != @BirthDate AND LENGTH(@BirthDate) = 10 AND @BirthDate GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]') THEN @BirthDate ELSE BirthDate END
        WHERE Id = @Id
        RETURNING *
        """;

    /// <summary>
    /// Запрос для смены адреса электронной почты клиента
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// <para>@Email - новый адрес электронной почты (длина не более 256 символов)</para>
    /// </summary>
    public static string ChangeEmail => """
        UPDATE Clients SET Email = CASE WHEN(Email != @Email AND LENGTH(Email) <= 256 AND Email LIKE '%@%.%' ) THEN @Email ELSE Email END
        WHERE Id = @Id 
            AND NOT EXISTS(SELECT TRUE FROM Clients WHERE Email = @Email AND Id != @Id)
            AND @Email LIKE '%@%.%'
            AND LENGTH(@Email) <= 256
        RETURNING *
        """;

    /// <summary>
    /// Запрос для смены номера телефона клиента
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// <para>@Phone - новый номер телефона (длина не более 16 символов, начинается с "+")</para>
    /// </summary>
    public static string ChangePhone => """
        UPDATE Clients SET Phone = CASE WHEN(Phone != @Phone AND LENGTH(Phone) <= 16 AND Phone LIKE '+%') THEN @Phone ELSE Phone END
        WHERE Id = @Id 
            AND NOT EXISTS(SELECT TRUE FROM Clients WHERE Phone = @Phone AND Id != @Id)
            AND @Phone LIKE '+%'
            AND LENGTH(@Phone) <= 16 AND LENGTH(@Phone) > 10
        RETURNING *
        """;

    /// <summary>
    /// Запрос для удаления клиента
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор клиента (Guid в формате строки)</para>
    /// </summary>
    public static string Delete => """
        DELETE FROM Clients WHERE Id = @Id AND NOT EXISTS(SELECT TRUE FROM Accounts WHERE ClientId = @Id AND ClosedAt IS NULL)
        """;
}
