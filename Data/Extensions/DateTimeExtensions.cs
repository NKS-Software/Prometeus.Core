namespace Prometeus.Core.Data.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        public string ToFormattedString() => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    extension(DateOnly dateOnly)
    {
        public string ToFormattedString() => dateOnly.ToString("yyyy-MM-dd");
    }

    extension(TimeOnly timeOnly)
    {
        public string ToFormattedString() => timeOnly.ToString("HH:mm:ss");
    }
}
