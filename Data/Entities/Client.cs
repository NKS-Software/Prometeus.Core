namespace Prometeus.Core.Data.Entities;

/// <summary>
/// Класс для хранения данных о клиенте
/// </summary>
public class Client
{
    /// <summary>
    /// Идентификатор клиента (UUID v7)
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Имя клиента. Обязательное поле. Максимальная длина 128 символов
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Фамилия клиента. Обязательное поле. Максимальная длина 128 символов
    /// </summary>
    public required string Surname { get; set; }

    /// <summary>
    /// Отчество клиента. Опционально. Максимальная длина 128 символов
    /// </summary>
    public string? Patronimyc { get; set; } = null;

    /// <summary>
    /// Дата рождения клиента. Формат: yyyy-MM-dd
    /// </summary>
    public required DateOnly BirthDate { get; set; }

    /// <summary>
    /// Адрес электронной почты. Уникальное поле. Максимальная длина 256 символов
    /// </summary>
    public required string Email { get; set; } = string.Empty;

    /// <summary>
    /// Номер телефона. Уникальное поле. Максимальная длина 16 символов.
    /// Должен начинаться с '+'
    /// </summary>
    public required string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Список счетов пользователя
    /// </summary>
    public List<Account> Accounts { get; set; } = [];
}
