using System.Text;

namespace Prometeus.Core.Generators;

public static class AccountNumberGenerator
{
    /// <summary>
    /// Генерирует номер счёта по стандарту ЦБ РФ
    /// </summary>
    /// <param name="balanceFirst">Балансовый счёт первого порядка, от 100 до 999</param>
    /// <param name="balanceSecond">Балансовый счёт второго порядка, от 1 до 99</param>
    /// <param name="currencyCode">Цифровой код валюты по ОКВ, от 100 до 999</param>
    /// <param name="bik">БИК банка</param>
    /// <param name="branchCode">Номер отделения, опционально, от 0 до 9999</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static string Generate(int balanceFirst, int balanceSecond, int currencyCode, string bik, int branchCode = 0)
    {
        if (balanceFirst < 100 || balanceFirst > 999)
            throw new ArgumentOutOfRangeException(nameof(balanceFirst), "Некоректный балансовый счёт первого порядка");
        if (balanceSecond < 0 || balanceSecond > 99)
            throw new ArgumentOutOfRangeException(nameof(balanceSecond), "Некоректный балансовый счёт второго порядка");
        if (currencyCode < 100 || currencyCode > 999)
            throw new ArgumentOutOfRangeException(nameof(currencyCode), "Некорректный код валюты");
        if (branchCode < 0 || branchCode > 9999)
            throw new ArgumentOutOfRangeException(nameof(branchCode), "Номер отделения");

        if (bik.EndsWith("000") || bik.Length != 9)
            throw new InvalidDataException("Некорректный БИК");

        var bfstr = balanceFirst.ToString();
        var bsstr = balanceSecond.ToString().PadLeft(2, '0');
        var ccstr = currencyCode.ToString();
        var bcstr = branchCode.ToString().PadLeft(4, '0');

        var builder = new StringBuilder();
        builder.Append(bfstr);
        builder.Append(bsstr);
        builder.Append(ccstr);
        builder.Append('_');
        if (balanceFirst >= 400 && balanceFirst <= 499)
        {
            builder.Append(bcstr);
            for (int i = 0; i < 7; i++) { builder.Append(Random.Shared.Next(0, 10)); }
        }
        else if (balanceFirst >= 300 && balanceFirst <= 399)
        {
            builder.Append("0000");
            builder.Append(bik[6..].PadLeft(7, '0'));
        }
        else
        {
            builder.Append("00000000000");
        }
        var rawNumber = builder.ToString();
        var cs = GetChecksum(rawNumber, bik);
        return rawNumber.Replace("_", cs.ToString());
    }
    private static int GetChecksum(string rawNumber, string bik)
    {
        string account = rawNumber.Replace("_", "");

        string accountWithoutKey = account.Length >= 19 ? account[..19] : account.PadLeft(19, '0');

        string bankCode = bik.Substring(4, 3);
        string baseString = "0" + bankCode + accountWithoutKey;

        int[] weights = { 7, 1, 3 };
        int sum = 0;
        for (int i = 0; i < baseString.Length; i++)
        {
            int digit = int.Parse(baseString[i].ToString());
            int weight = weights[i % 3];
            int product = digit * weight;
            sum += product % 10;
        }

        int checksum = (sum % 10) * 3;
        checksum %= 10;

        return checksum;
    }

    /// <summary>
    /// Проверяет номер счёта на корректность
    /// </summary>
    /// <param name="number">Номер счёта для проверки</param>
    /// <param name="bik">БИК банка</param>
    /// <returns>true если счёт корректен, иначе false</returns>
    /// <exception cref="ArgumentException"></exception>
    public static bool Validate(string number, string bik)
    {
        if (string.IsNullOrEmpty(bik) || bik[6..] == "000" || bik.Length != 9 || !bik.All(char.IsDigit))
            throw new InvalidDataException("Некорректный БИК");

        if (string.IsNullOrEmpty(number) || number.Length != 20 || number.StartsWith("000") || !number.All(char.IsDigit))
            return false;

        var (BalanceFirst, BalanceSecond, CurrencyCode, CheckSum, BranchCode, UniquePart) = Parse(number);
        var builder = new StringBuilder();
        builder.Append(BalanceFirst);
        builder.Append(BalanceSecond.ToString().PadLeft(2, '0'));
        builder.Append(CurrencyCode);
        builder.Append('_');
        builder.Append(BranchCode.ToString().PadLeft(4, '0'));
        builder.Append(UniquePart);
        var cs = GetChecksum(builder.ToString(), bik);
        return cs == CheckSum;
    }

    /// <summary>
    /// Парсит номер счёта на части
    /// </summary>
    /// <param name="number">Номер счёта для парсинга</param>
    /// <returns>Кортеж частей счёта</returns>
    /// <exception cref="ArgumentException"></exception>
    public static (int BalanceFirst, int BalanceSecond, int CurrencyCode, int CheckSum, int BranchCode, string UniquePart) Parse(string number)
    {
        if (number.Length != 20)
            throw new ArgumentException("Номер счета должен содержать 20 цифр");
        var bf = int.Parse(number[..3]);
        var bs = int.Parse(number[3..5]);
        var cc = int.Parse(number[5..8]);
        var cs = int.Parse(number[8].ToString());
        var bc = int.Parse(number[9..13]);
        var up = number[13..];
        return (bf, bs, cc, cs, bc, up);
    }

    /// <summary>
    /// Безопасная версия метода 
    /// <seealso cref="Parse(string)"/>
    /// </summary>
    /// <param name="number"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParse(string number, out (int BalanceFirst, int BalanceSecond, int CurrencyCode, int CheckSum, int BranchCode, string UniquePart) result)
    {
        try
        {
            result = Parse(number);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Безопасная версия метода
    /// <seealso cref="Validate(string, string)"/>
    /// </summary>
    /// <param name="number"></param>
    /// <param name="bik"></param>
    /// <param name="isValid"></param>
    /// <returns></returns>
    public static bool TryValidate(string number, string bik, out bool isValid)
    {
        try
        {
            isValid = Validate(number, bik);
            return true;
        }
        catch
        {
            isValid = false;
            return false;
        }
    }
}
