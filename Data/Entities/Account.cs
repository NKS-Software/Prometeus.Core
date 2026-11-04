namespace Prometeus.Core.Data.Entities;

public class Account
{
    /// <summary>
    /// Идентификатор владельца счёта
    /// </summary>
    public required Guid ClientId { get; set; }

    /// <summary>
    /// Уникальный номер счёта
    /// </summary>
    public required string Number { get; set; }

    /// <summary>
    /// Наименование счёта
    /// </summary>
    public string Name { get; set; } = "Payment";

    /// <summary>
    /// Дата и время открытия счёта
    /// </summary>
    public required DateTime OpenedAt { get; set; }

    /// <summary>
    /// Дата и время закрытия счёта
    /// </summary>
    public DateTime? ClosedAt { get; set; } = null;

    /// <summary>
    /// Остаток средств на счету
    /// </summary>
    public decimal Balance { get; set; } = 0;

    /// <summary>
    /// Список привязанных карт
    /// </summary>
    public List<Card> Cards { get; set; } = [];
}
