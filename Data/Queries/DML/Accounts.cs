//strpsql-on
namespace Prometeus.Core.Data.Queries.DML;

internal static class Accounts
{
    /// <summary>
    /// Запрос для проверки существования счёта по номеру
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта для проверки</para>
    /// </summary>
    public static string IsExists => """
        SELECT EXISTS(SELECT TRUE FROM Accounts WHERE Number = @Number)
        """;

    /// <summary>
    /// Запрос для проверки возможности зачисления средств на счёт
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@Amount - сумма зачисления (должна быть больше 0)</para>
    /// </summary>
    public static string PossibleCredit => """
        SELECT EXISTS(SELECT TRUE FROM Accounts WHERE Number = @Number AND ClosedAt IS NULL)
        """;

    /// <summary>
    /// Запрос для проверки наличия активных счетов у клиента
    /// <para>Параметры:</para>
    /// <para>@ClientId - идентификатор клиента</para>
    /// </summary>
    public static string HasActiveAccounts => """
        SELECT EXISTS(SELECT TRUE FROM Accounts WHERE ClientId = @ClientId AND ClosedAt IS NULL)
        """;

    /// <summary>
    /// Запрос для проверки возможности списания средств со счёта
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@Amount - сумма списания (должна быть больше 0 и не превышать баланс)</para>
    /// </summary>
    public static string PossibleDebit => """
        SELECT EXISTS(
            SELECT TRUE FROM Accounts 
            WHERE Number = @Number
                AND ClosedAt IS NULL 
                AND Balance - CAST(@Amount AS NUMERIC) > 0
                AND CAST(@Amount AS NUMERIC) > 0
        )
        """;

    /// <summary>
    /// Запрос для проверки принадлежности счёта указанному клиенту
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@ClientId - идентификатор клиента (Guid в формате строки)</para>
    /// </summary>
    public static string BelongsToClient => """
        SELECT EXISTS(
            SELECT TRUE FROM Accounts 
            WHERE Number = @Number 
                AND ClientId = @ClientId 
                AND ClosedAt IS NULL
        )
        """;

    /// <summary>
    /// Запрос для получения идентификатора клиента по номеру счёта
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// </summary>
    public static string GetClientByNumber => """
        SELECT ClientId FROM Accounts WHERE Number = @Number AND ClosedAt IS NULL
        """;

    /// <summary>
    /// Запрос для получения информации о счёте по номеру
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// </summary>
    public static string GetByNumber => """
        SELECT ClientId, Number, Name, OpenedAt, Balance 
        FROM Accounts 
        WHERE Number = @Number AND ClosedAt IS NULL
        """;

    /// <summary>
    /// Запрос для получения всех открытых счетов клиента
    /// <para>Параметры:</para>
    /// <para>@ClientId - идентификатор клиента (Guid в формате строки)</para>
    /// </summary>
    public static string GetByClientId => """
        SELECT Number, Name, OpenedAt, Balance 
        FROM Accounts 
        WHERE ClientId = @ClientId AND ClosedAt IS NULL
        """;

    /// <summary>
    /// Запрос для открытия нового счёта
    /// <para>Параметры:</para>
    /// <para>@ClientId - идентификатор клиента (Guid в формате строки)</para>
    /// <para>@Number - номер счёта (20 цифр)</para>
    /// <para>@Name - название счёта (опционально, по умолчанию 'Payment', до 128 символов)</para>
    /// <para>@OpenedAt - дата открытия в формате: YYYY-MM-DD HH:MM:SS</para>
    /// </summary>
    public static string Open => """
        INSERT INTO Accounts(ClientId, Number, Name, OpenedAt)
        SELECT 
            @ClientId, 
            @Number,
            CASE WHEN(@Name IS NOT NULL AND @Name != '' AND LENGTH(@Name) <= 128) 
                THEN @Name 
                ELSE 'Payment'
            END, 
            @OpenedAt
        WHERE EXISTS(SELECT TRUE FROM Clients WHERE Id = @ClientId)
            AND NOT EXISTS(SELECT TRUE FROM Accounts WHERE Number = @Number)
            AND @OpenedAt GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'
            AND LENGTH(@OpenedAt) = 19
            AND @Number GLOB '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
            AND LENGTH(@Number) = 20
        RETURNING ClientId, Number, Name, OpenedAt
        """;

    /// <summary>
    /// Запрос для обновления названия счёта
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@Name - новое название счёта (до 128 символов)</para>
    /// </summary>
    public static string Rename => """
        UPDATE Accounts SET
        Name = @Name
        WHERE Number = @Number
            AND ClosedAt IS NULL
        RETURNING Name
        """;

    /// <summary>
    /// Запрос для закрытия счёта
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@ClosedAt - дата закрытия в формате: YYYY-MM-DD HH:MM:SS</para>
    /// </summary>
    public static string Close => """
        UPDATE Accounts SET
            ClosedAt = @ClosedAt
        WHERE Number = @Number
            AND ClosedAt IS NULL
            AND Balance = 0
            AND NOT EXISTS(
                SELECT TRUE FROM Transactions AS t
                WHERE t.Id IN (SELECT TransactionId FROM TransactionEntries WHERE AccountNumber = @Number)
                AND t.CompletedAt IS NULL
            )
        """;

    /// <summary>
    /// Запрос для списания средств со счёта
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@Amount - сумма списания (должна быть больше 0 и не превышать баланс)</para>
    /// </summary>
    public static string Debit => """
        UPDATE Accounts SET
            Balance = Balance - CAST(@Amount AS NUMERIC)
        WHERE Number = @Number
            AND CAST(@Amount AS NUMERIC) > 0
            AND (Balance - CAST(@Amount AS NUMERIC)) >= 0
            AND ClosedAt IS NULL
        """;

    /// <summary>
    /// Запрос для зачисления средств на счёт
    /// <para>Параметры:</para>
    /// <para>@Number - номер счёта</para>
    /// <para>@Amount - сумма зачисления (должна быть больше 0)</para>
    /// </summary>
    public static string Credit => """
        UPDATE Accounts SET
            Balance = Balance + CAST(@Amount AS NUMERIC)
        WHERE Number = @Number
            AND CAST(@Amount AS NUMERIC) > 0
            AND ClosedAt IS NULL
        """;
}