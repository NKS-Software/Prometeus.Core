namespace Prometeus.Core.Data.Entities;

/// <summary>
/// Класс для хранения информации о транзакции
/// </summary>
public class Transaction
{
    /// <summary>
    /// Идентификатор транзакции
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Дата создания транзакции
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата завершшения транзакции
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Причина отклонения транзакции
    /// </summary>
    public string? RejectReason { get; set; }

    /// <summary>
    /// Статус транзакции
    /// <para>Вычисляет при запросе</para>
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Дополнительная информация о транзакции
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Список проводок связанных с этой транзакцией
    /// </summary>
    public List<TransactionEntry> Entries { get; set; } = [];
}

/// <summary>
/// Класс для хранения информации о проводках
/// </summary>
public class TransactionEntry
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Идентификатор транзакции с которой связана проводка
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// Номер счёта куда зачислить/списать средства
    /// </summary>
    public required string AccountNumber { get; init; }

    /// <summary>
    /// Сумма зачисления/списания
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Тип записи. Подробнее <see cref="EntryType"/>
    /// </summary>
    public required EntryType Type { get; init; }
}

/// <summary>
/// Enum для указания типа записи
/// </summary>
public enum EntryType
{
    /// <summary>
    /// Источник(откуда списываются средства)
    /// </summary>
    Source = -1,

    /// <summary>
    /// Получатель(куда зачисляются средства)
    /// </summary>
    Destination = 1,
}

/// <summary>
/// Enum для указания статуса транзакции
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// В ожидании (не завершена)
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Завершена
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Отклонена
    /// </summary>
    Rejected = 2
}