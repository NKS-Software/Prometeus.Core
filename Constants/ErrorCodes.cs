namespace Prometeus.Core.Constants;

/// <summary>
/// Enum для указания кода ошибки
/// </summary>
public enum ErrorCodes
{
    /// <summary>
    /// Не найдено
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// Не верный запрос
    /// </summary>
    BadRequest = 400,

    /// <summary>
    /// Внутренняя(серверная) ошибка
    /// </summary>
    InternalError = 500,

    /// <summary>
    /// Отказано в доступе
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// Неизвестная ошибка
    /// </summary>
    Unknown = 0
}