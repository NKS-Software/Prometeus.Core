using System.Text;

namespace Prometeus.Core.Generators;

/// <summary>
/// Генератор номеров банковских карт по стандарту ISO/IEC 7812
/// </summary>
public static class CardNumberGenerator
{
    /// <summary>
    /// Минимальная длина BIN (6 цифр — стандарт ISO/IEC 7812)
    /// </summary>
    public const int MinBinLength = 6;

    /// <summary>
    /// Максимальная длина BIN (8 цифр — расширенный BIN)
    /// </summary>
    public const int MaxBinLength = 8;

    /// <summary>
    /// Минимальная длина номера карты
    /// </summary>
    public const int MinCardLength = 12;

    /// <summary>
    /// Максимальная длина номера карты
    /// </summary>
    public const int MaxCardLength = 19;

    /// <summary>
    /// Генерирует номер банковской карты с корректным контрольным числом (Luhn)
    /// </summary>
    /// <param name="bin">BIN (Bank Identification Number), 6-8 цифр</param>
    /// <param name="length">Длина номера карты (12-19), по умолчанию 16</param>
    /// <returns>Номер карты с контрольным числом</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string Generate(string bin, int length = 16)
    {
        if (string.IsNullOrEmpty(bin))
            throw new ArgumentException("BIN не может быть пустым", nameof(bin));
        if (!bin.All(char.IsDigit))
            throw new ArgumentException("BIN должен содержать только цифры", nameof(bin));
        if (bin.Length < MinBinLength || bin.Length > MaxBinLength)
            throw new ArgumentException(
                $"BIN должен содержать от {MinBinLength} до {MaxBinLength} цифр", nameof(bin));
        if (bin[0] == '0')
            throw new ArgumentException("BIN не может начинаться с 0", nameof(bin));

        if (length < MinCardLength || length > MaxCardLength)
            throw new ArgumentOutOfRangeException(nameof(length),
                $"Длина карты должна быть от {MinCardLength} до {MaxCardLength}");

        if (length <= bin.Length)
            throw new ArgumentOutOfRangeException(nameof(length),
                $"Длина карты ({length}) должна быть больше длины BIN ({bin.Length})");

        var remaining = length - bin.Length - 1;

        var builder = new StringBuilder(bin);
        for (int i = 0; i < remaining; i++)
            builder.Append(Random.Shared.Next(0, 10));

        var partial = builder.ToString();
        var checksum = CalculateLuhn(partial);
        return partial + checksum;
    }

    /// <summary>
    /// Проверяет номер карты по алгоритму Луна
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <returns>true если номер валиден, иначе false</returns>
    public static bool Validate(string number)
    {
        if (string.IsNullOrEmpty(number))
            return false;
        if (!number.All(char.IsDigit))
            return false;
        if (number.Length < MinCardLength || number.Length > MaxCardLength)
            return false;

        int sum = 0;
        bool doubleDigit = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int digit = number[i] - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleDigit = !doubleDigit;
        }
        return sum % 10 == 0;
    }

    /// <summary>
    /// Извлекает BIN из номера карты
    /// </summary>
    /// <param name="number">Номер карты</param>
    /// <param name="binLength">Длина BIN (6 или 8), по умолчанию 6</param>
    /// <returns>BIN или null, если номер некорректен</returns>
    public static string? ExtractBin(string number, int binLength = 6)
    {
        if (string.IsNullOrEmpty(number) || number.Length < binLength)
            return null;
        return number[..binLength];
    }

    /// <summary>
    /// Считает контрольное число по алгоритму Луна
    /// </summary>
    private static int CalculateLuhn(string partial)
    {
        int sum = 0;
        bool doubleDigit = true;

        for (int i = partial.Length - 1; i >= 0; i--)
        {
            int digit = partial[i] - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return (10 - (sum % 10)) % 10;
    }
}
