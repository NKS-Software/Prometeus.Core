using Prometeus.Core.Constants;

namespace Prometeus.Core.Models;

/// <summary>
/// Базовый тип ответа сервиса (маркерный интерфейс для паттерн-матчинга)
/// </summary>
public abstract record BaseResponse{}

/// <summary>
/// Успешный ответ с полезной нагрузкой
/// </summary>
/// <typeparam name="T">Тип полезной нагрузки</typeparam>
public record SuccessResponse<T> : BaseResponse
{
    /// <summary>
    /// Полезная нагрузка
    /// </summary>
    public required T Value { get; init; }

    /// <summary>
    /// Фабричный метод для создания успешного ответа с значением
    /// </summary>
    public static SuccessResponse<T> WithValue(T value) => new() { Value = value };
}

/// <summary>
/// Успешный ответ с текстовым сообщением (без полезной нагрузки)
/// </summary>
public record SuccessResponse : BaseResponse
{
    /// <summary>
    /// Текст сообщения
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Фабричный метод для создания успешного ответа с сообщением
    /// </summary>
    public static SuccessResponse WithMessage(string message) => new() { Message = message };
}

/// <summary>
/// Ответ с ошибкой
/// </summary>
public record ErrorResponse : BaseResponse
{
    /// <summary>
    /// Текст сообщения об ошибке
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Код ошибки (используется для маппинга в HTTP-статус на уровне API)
    /// </summary>
    public required ErrorCodes ErrorCode { get; init; }

    /// <summary>
    /// Ошибка: ресурс не найден (404)
    /// </summary>
    public static ErrorResponse NotFound(string message) => new()
    {
        ErrorCode = ErrorCodes.NotFound,
        Message = message
    };

    /// <summary>
    /// Ошибка: неверный запрос (400)
    /// </summary>
    public static ErrorResponse BadRequest(string message) => new()
    {
        ErrorCode = ErrorCodes.BadRequest,
        Message = message
    };

    /// <summary>
    /// Ошибка: доступ запрещён (403)
    /// </summary>
    public static ErrorResponse Forbidden(string message) => new()
    {
        ErrorCode = ErrorCodes.Forbidden,
        Message = message
    };

    /// <summary>
    /// Ошибка: внутренняя ошибка сервера (500)
    /// </summary>
    public static ErrorResponse InternalError() => new()
    {
        ErrorCode = ErrorCodes.InternalError,
        Message = "Internal server error"
    };
}
