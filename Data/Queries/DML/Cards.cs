//strpsql-on
namespace Prometeus.Core.Data.Queries.DML;

internal static class Cards
{
    /// <summary>
    /// Запрос для проверки существования карты по номеру
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты для проверки</para>
    /// </summary>
    public static string IsExists => """
        SELECT EXISTS(SELECT TRUE FROM Cards WHERE Number = @Number)
        """;

    /// <summary>
    /// Запрос для проверки активна ли карта
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты для проверки</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string IsActive => """
        SELECT EXISTS(
            SELECT TRUE FROM Cards
            WHERE Number = @Number 
                AND ValidUntil >= @CurrentDate 
                AND Status = 0
        )
        """;

    /// <summary>
    /// Запрос для получения базовой информации о карте по её номеру
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string GetByNumber => """
        SELECT Number, AccountNumber, ValidUntil, CardHolderName, Status
        WHERE Number = @Number 
            AND ValidUntil >= @CurrentDate
        """;

    /// <summary>
    /// Запрос для открытия новой карты
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты</para>
    /// <para>@AccountNumber - номер счёта к которому привязана карта</para>
    /// <para>@PinHash - Хешированный пин-код</para>
    /// <para>@CvcKeyId - Guid ключ, по которому храниться CVC в KeyStore</para>
    /// <para>@CardHolderName - Имя держателя карты, по умолчанию Virtual, длина до 256 символов</para>
    /// <para>@ValidUntil - срок действия карты</para>
    /// </summary>
    public static string Open => """
        INSERT INTO Cards(Number, AccountNumber, PinHash, CvcKeyId, CardHolderName, ValidUntil)
        SELECT
            @Number,
            @AccountNumber,
            @PinHash,
            @CvcKeyId,
            CASE WHEN(@CardHolderName IS NOT NULL AND @CardHolderName != '' AND LENGTH(@CardHolderName) <= 256)
                THEN @CardHolderName
                ELSE 'Virtual'
            END,
            @ValidUntil
        WHERE EXISTS(SELECT TRUE FROM Accounts WHERE Number = @AccountNumber AND ClosedAt IS NULL)
            AND NOT EXISTS(SELECT TRUE FROM Cards WHERE Number = @Number)
            AND LENGTH(@Number) BETWEEN 12 AND 19
            AND @Number NOT GLOB '0*'
            AND @ValidUntil GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
        RETURNING Number, AccountNumber, PinHash, CvcKeyId, CardHolderName, ValidUntil
        """;

    /// <summary>
    /// Запрос для получения номера счёта к которому привязана карта
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты для проверки</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string GetAccountNumber => """
        SELECT AccountNumber FROM Cards 
        WHERE Number = @Number 
            AND ValidUntil >= @CurrentDate 
            AND Status = 0
        """;

    /// <summary>
    /// Запрос для получения хешированного пин-кода карты
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты для проверки</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string GetPinHash => """
        SELECT PinHash FROM Cards 
        WHERE Number = @Number 
            AND ValidUntil >= @CurrentDate 
            AND Status = 0
        """;

    /// <summary>
    /// Запрос для изменения хеша пин-кода карты
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты для проверки</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// <para>@PinHash - новый хеш пин-кода</para>
    /// </summary>
    public static string ChangePinHash => """
        UPDATE Cards
            SET PinHash = @PinHash
        WHERE Number = @Number
            AND ValidUntil >= @CurrentDate
            AND Status = 0
            AND PinHash != @PinHash
        """;

    /// <summary>
    /// Запрос для получения ключа в KeyStore по которому храниться CVC
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты для проверки</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string GetCvcKeyId => """
        SELECT CvcKeyId FROM Cards 
        WHERE Number = @Number 
            AND ValidUntil >= @CurrentDate 
            AND Status = 0
        """;

    /// <summary>
    /// Запрос для всех активных карт привязанные к указанному счёту
    /// <para>Параметры:</para>
    /// <para>@AccountNumber - номер счёта</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string GetAllByAccountNumber => """
        SELECT Number, CardHolderName, ValidUntil, Status FROM Cards 
        WHERE AccountNumber = @AccountNumber 
            AND ValidUntil >= @CurrentDate 
            AND Status != 2
        """;

    /// <summary>
    /// Запрос для заморозки карты
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string Freeze => """
        UPDATE Cards
            SET Status = 1
        WHERE Number = @Number 
            AND ValidUntil >= @CurrentDate
            AND Status = 0
        """;

    /// <summary>
    /// Запрос для разморозки карты
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string Unfreeze => """
        UPDATE Cards
            SET Status = 0
        WHERE Number = @Number 
            AND ValidUntil >= @CurrentDate
            AND Status = 1
        """;

    /// <summary>
    /// Запрос для перманентной блокировки карты
    /// <para>Параметры:</para>
    /// <para>@Number - номер карты</para>
    /// <para>@CurrentDate - текущая дата в формате: YYYY-MM-DD</para>
    /// </summary>
    public static string Lock => """
        UPDATE Cards
            SET Status = 2
        WHERE Number = @Number
            AND ValidUntil >= @CurrentDate 
            AND Status != 2
        """;
}
