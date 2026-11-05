namespace Prometeus.Core.Data.Entities;

public record Card
{
    /// <summary>
    /// Уникальный номер карты
    /// </summary>
    public required string Number { get; set; }

    /// <summary>
    /// Номер счёта к которому привязана карта
    /// </summary>
    public required string AccountNumber { get; set; }

    /// <summary>
    /// Хешированный пин-код
    /// </summary>
    public string PinHash { get; set; } = "not specified";

    /// <summary>
    /// Guid ключ в KeyStore хранящий CVC
    /// </summary>
    public Guid CvcKeyId { get; set; } = Guid.Empty;

    /// <summary>
    /// Имя держателя карты, по умолчанию Virtual
    /// </summary>
    public string CardHolderName { get; set; } = "Virtual";

    /// <summary>
    /// Срок действия карты
    /// </summary>
    public required DateOnly ValidUntil { get; set; }

    /// <summary>
    /// Текущий статус карты
    /// <para>Подробнее: <see cref="CardStatus"/></para>
    /// </summary>
    public CardStatus Status { get; set; } = CardStatus.Active;
}

/// <summary>
/// enum для обозначения статуса карты
/// </summary>
public enum CardStatus
{
    /// <summary>
    /// Карта активна
    /// </summary>
    Active = 0,

    /// <summary>
    /// Карта заморожена
    /// </summary>
    Frozen = 1,

    /// <summary>
    /// Карта заблокирована
    /// </summary>
    Locked = 2
}
