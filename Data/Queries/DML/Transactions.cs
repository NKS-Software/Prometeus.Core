//strpsql-on

namespace Prometeus.Core.Data.Queries.DML;

/// <summary>
/// Запросы для работы с транзакциями и их проводками
/// </summary>
internal static class Transactions
{
    /// <summary>
    /// Проверяет существование транзакции по идентификатору
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    public static string IsExists => """
        SELECT EXISTS(SELECT TRUE FROM Transactions WHERE Id = @Id)
        """;

    /// <summary>
    /// Проверяет завершена ли транзакция
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    public static string IsCompleted => """
        SELECT EXISTS(SELECT TRUE FROM Transactions WHERE Id = @Id AND CompletedAt IS NOT NULL)
        """;

    /// <summary>
    /// Проверяет отклонена ли транзакция
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    public static string IsRejected => """
        SELECT EXISTS(
            SELECT TRUE FROM Transactions 
            WHERE Id = @Id 
            AND CompletedAt IS NOT NULL 
            AND RejectReason IS NOT NULL 
            AND RejectReason != ''
        )
        """;

    /// <summary>
    /// Проверяет возможность завершения транзакции
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    /// <remarks>
    /// Транзакцию можно завершить, если:
    /// <list type="bullet">
    /// <item><description>Она существует и не завершена</description></item>
    /// <item><description>Количество проводок не менее 2</description></item>
    /// <item><description>Сумма всех проводок с учётом знака = 0 (сбалансирована)</description></item>
    /// </list>
    /// </remarks>
    public static string CanComplete => """
        SELECT CASE 
            WHEN NOT EXISTS(SELECT TRUE FROM Transactions WHERE Id = @Id AND CompletedAt IS NULL) 
                THEN FALSE
            WHEN NOT EXISTS(SELECT TRUE FROM TransactionEntries WHERE TransactionId = @Id) 
                THEN FALSE
            WHEN (SELECT COUNT(Id) FROM TransactionEntries WHERE TransactionId = @Id) < 2
                THEN FALSE
            WHEN COALESCE((SELECT SUM(Type * Amount) FROM TransactionEntries WHERE TransactionId = @Id),  0) != 0 
                THEN FALSE
            ELSE TRUE
        END
        """;

    /// <summary>
    /// Создаёт новую транзакцию
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// <para>@CurrentDateTime - дата и время создания в формате: YYYY-MM-DD HH:MM:SS</para>
    /// </summary>
    public static string Create => """
        INSERT INTO Transactions(Id, CreatedAt)
        SELECT @Id, @CurrentDateTime 
        WHERE @CurrentDateTime GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'
        AND LENGTH(@Id) = 36
        AND NOT EXISTS(SELECT TRUE FROM Transactions WHERE Id = @Id)
        RETURNING Id, CreatedAt
        """;

    /// <summary>
    /// Создаёт новую проводку для транзакции
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор проводки (Guid в формате строки)</para>
    /// <para>@TransactionId - идентификатор транзакции (Guid в формате строки)</para>
    /// <para>@AccountNumber - номер счёта (20 цифр)</para>
    /// <para>@Amount - сумма проводки (должна быть больше 0)</para>
    /// <para>@Type - тип проводки: -1 (Source), 1 (Destination)</para>
    /// </summary>
    /// <remarks>
    /// Проводка создаётся только если:
    /// <list type="bullet">
    /// <item><description>Транзакция существует</description></item>
    /// <item><description>Счёт существует и не закрыт</description></item>
    /// <item><description>Сумма больше 0</description></item>
    /// <item><description>Тип -1 или 1</description></item>
    /// <item><description>Проводка с таким Id ещё не существует</description></item>
    /// </list>
    /// </remarks>
    public static string CreateEntry => """
        INSERT INTO TransactionEntries(Id, TransactionId, AccountNumber, Amount, Type)
        SELECT @Id, @TransactionId, @AccountNumber, @Amount, @Type
        WHERE EXISTS(SELECT TRUE FROM Transactions WHERE Id = @TransactionId)
        AND EXISTS(SELECT TRUE FROM Accounts WHERE Number = @AccountNumber AND ClosedAt IS NULL)
        AND CAST(@Amount AS DECIMAL) > 0
        AND @Type IN(-1, 1)
        AND LENGTH(@Id) = 36
        AND NOT EXISTS(SELECT TRUE FROM TransactionEntries WHERE Id = @Id)
        RETURNING Id
    """;

    /// <summary>
    /// Получает транзакцию по идентификатору со всеми проводками
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    /// <returns>Транзакция с проводками (LEFT JOIN — даже если проводок нет)</returns>
    public static string GetById => """
        SELECT
            t.Id AS Id,
            t.CreatedAt AS CreatedAt,
            t.CompletedAt AS CompletedAt,
            t.RejectReason AS RejectReason,
            t.Metadata AS Metadata,
            CASE
                WHEN(t.CompletedAt IS NULL)
                    THEN 0
                WHEN(t.CompletedAt IS NOT NULL AND (t.RejectReason IS NULL OR t.RejectReason = '')) 
                    THEN 1
                WHEN(t.CompletedAt IS NOT NULL AND t.RejectReason != '')
                    THEN 2
                ELSE 0
            END AS Status,
            te.Id AS TransactionEntryId,
            te.AccountNumber AS TransactionEntryAccountNumber,
            te.Amount AS TransactionEntryAmount,
            te.Type AS TransactionEntryType
        FROM Transactions AS t
        LEFT JOIN TransactionEntries AS te ON te.TransactionId = t.Id
        WHERE t.Id = @Id
        """;

    /// <summary>
    /// Получает все транзакции, в которых участвует указанный счёт
    /// <para>Параметры:</para>
    /// <para>@AccountNumber - номер счёта (20 цифр)</para>
    /// <para>@Count - количество записей на странице</para>
    /// <para>@Page - номер страницы (начиная с 1)</para>
    /// </summary>
    /// <returns>Транзакции со всеми проводками (LEFT JOIN)</returns>
    /// <remarks>
    /// Возвращает все проводки транзакций, где счёт участвует,
    /// а не только проводки по этому счёту.
    /// </remarks>
    public static string GetAllForAccount => """
        SELECT
            t.Id AS Id,
            t.CreatedAt AS CreatedAt,
            t.CompletedAt AS CompletedAt,
            t.RejectReason AS RejectReason,
            t.Metadata AS Metadata,
            CASE
                WHEN(t.CompletedAt IS NULL)
                    THEN 0
                WHEN(t.CompletedAt IS NOT NULL AND (t.RejectReason IS NULL OR t.RejectReason = '')) 
                    THEN 1
                WHEN(t.CompletedAt IS NOT NULL AND t.RejectReason != '')
                    THEN 2
                ELSE 0
            END AS Status,
            te.Id AS TransactionEntryId,
            te.AccountNumber AS TransactionEntryAccountNumber,
            te.Amount AS TransactionEntryAmount,
            te.Type AS TransactionEntryType
        FROM Transactions AS t
        LEFT JOIN TransactionEntries AS te ON te.TransactionId = t.Id
        WHERE t.Id IN (SELECT TransactionId FROM TransactionEntries WHERE AccountNumber = @AccountNumber)
        ORDER BY t.CreatedAt DESC
        LIMIT @Count
        OFFSET CAST(@Count AS INTEGER) * (CAST(@Page AS INTEGER) - 1)
        """;

    /// <summary>
    /// Получает все транзакции, в которых участвуют счета указанного клиента
    /// <para>Параметры:</para>
    /// <para>@ClientId - идентификатор клиента (Guid в формате строки)</para>
    /// <para>@Count - количество записей на странице</para>
    /// <para>@Page - номер страницы (начиная с 1)</para>
    /// </summary>
    /// <returns>Транзакции со всеми проводками (LEFT JOIN)</returns>
    /// <remarks>
    /// Возвращает все проводки транзакций, где участвуют счета клиента,
    /// а не только проводки по его счетам.
    /// </remarks>
    public static string GetAllForClient => """
        SELECT
            t.Id AS Id,
            t.CreatedAt AS CreatedAt,
            t.CompletedAt AS CompletedAt,
            t.RejectReason AS RejectReason,
            t.Metadata AS Metadata,
            CASE
                WHEN(t.CompletedAt IS NULL) THEN 0
                WHEN(t.CompletedAt IS NOT NULL AND (t.RejectReason IS NULL OR t.RejectReason = '')) THEN 1
                WHEN(t.CompletedAt IS NOT NULL AND t.RejectReason != '') THEN 2
                ELSE 0
            END AS Status,
            te.Id AS TransactionEntryId,
            te.AccountNumber AS TransactionEntryAccountNumber,
            te.Amount AS TransactionEntryAmount,
            te.Type AS TransactionEntryType
        FROM Transactions AS t
        LEFT JOIN TransactionEntries AS te ON te.TransactionId = t.Id
        WHERE t.Id IN (
            SELECT te2.TransactionId 
            FROM TransactionEntries te2
            JOIN Accounts acc ON acc.Number = te2.AccountNumber
            WHERE acc.ClientId = @ClientId
        )
        ORDER BY t.CreatedAt DESC
        LIMIT @Count
        OFFSET CAST(@Count AS INTEGER) * (CAST(@Page AS INTEGER) - 1)
    """;

    /// <summary>
    /// Получает метаданные транзакции по идентификатору
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    /// <returns>JSON-строка с метаданными</returns>
    public static string GetMetadataById => """
        SELECT Metadata FROM Transactions WHERE Id = @Id
        """;

    /// <summary>
    /// Получает статус транзакции по идентификатору
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// </summary>
    /// <returns>
    /// Статус транзакции:
    /// <list type="bullet">
    /// <item><description>0 - Pending (не завершена)</description></item>
    /// <item><description>1 - Completed (завершена)</description></item>
    /// <item><description>2 - Rejected (отклонена)</description></item>
    /// </list>
    /// </returns>
    public static string GetStatusById => """
        SELECT 
            CASE
                WHEN(CompletedAt IS NULL) THEN 0
                WHEN(CompletedAt IS NOT NULL AND (RejectReason IS NULL OR RejectReason = '')) THEN 1
                WHEN(CompletedAt IS NOT NULL AND RejectReason != '') THEN 2
                ELSE 0
            END AS Status
        FROM Transactions WHERE Id = @Id
        """;

    /// <summary>
    /// Обновляет метаданные транзакции
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// <para>@Metadata - JSON-строка с метаданными</para>
    /// </summary>
    /// <remarks>
    /// Обновление возможно только для незавершённой транзакции
    /// и если @Metadata является валидным JSON.
    /// </remarks>
    public static string UpdateMetadata => """
        UPDATE Transactions SET
        Metadata = @Metadata
        WHERE Id = @Id
        AND CompletedAt IS NULL
        AND json_valid(@Metadata)
        """;

    /// <summary>
    /// Завершает транзакцию
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// <para>@CurrentDateTime - дата и время завершения в формате: YYYY-MM-DD HH:MM:SS</para>
    /// </summary>
    /// <remarks>
    /// Завершение возможно только если:
    /// <list type="bullet">
    /// <item><description>Транзакция не завершена</description></item>
    /// <item><description>Количество проводок не менее 2</description></item>
    /// <item><description>Сумма всех проводок с учётом знака = 0</description></item>
    /// </list>
    /// </remarks>
    public static string Complete => """
        UPDATE Transactions SET
        CompletedAt = @CurrentDateTime
        WHERE Id = @Id
        AND @CurrentDateTime GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'
        AND CompletedAt IS NULL
        AND (SELECT COUNT(Id) FROM TransactionEntries WHERE TransactionId = @Id) >= 2
        AND COALESCE((SELECT SUM(Type * Amount) FROM TransactionEntries WHERE TransactionId = @Id), 0) = 0 
        """;

    /// <summary>
    /// Отклоняет транзакцию с указанием причины
    /// <para>Параметры:</para>
    /// <para>@Id - идентификатор транзакции (Guid в формате строки)</para>
    /// <para>@CurrentDateTime - дата и время отклонения в формате: YYYY-MM-DD HH:MM:SS</para>
    /// <para>@RejectReason - причина отклонения (не пустая строка)</para>
    /// </summary>
    /// <remarks>
    /// Отклонение возможно только если:
    /// <list type="bullet">
    /// <item><description>Транзакция не завершена</description></item>
    /// <item><description>Причина не пустая</description></item>
    /// <item><description>Количество проводок не менее 2</description></item>
    /// <item><description>Сумма всех проводок с учётом знака = 0</description></item>
    /// </list>
    /// </remarks>
    public static string Reject => """
        UPDATE Transactions SET
        CompletedAt = @CurrentDateTime,
        RejectReason = @RejectReason
        WHERE Id = @Id
        AND @RejectReason IS NOT NULL
        AND @RejectReason != ''
        AND @CurrentDateTime GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'
        AND CompletedAt IS NULL
        AND (SELECT COUNT(Id) FROM TransactionEntries WHERE TransactionId = @Id) >= 2
        AND COALESCE((SELECT SUM(Type * Amount) FROM TransactionEntries WHERE TransactionId = @Id), 0) = 0 
        """;
}
