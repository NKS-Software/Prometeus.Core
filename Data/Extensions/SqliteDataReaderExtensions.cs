using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;

namespace Prometeus.Core.Data.Extensions;

public static class SqliteDataReaderExtensions
{
    extension(SqliteDataReader reader)
    {
        public DateTime GetDateTime(string columnName) =>
            DateTime.ParseExact(reader.GetString(columnName), "yyyy-MM-dd HH:mm:ss", null);

        public DateOnly GetDateOnly(string columnName) =>
            DateOnly.ParseExact(reader.GetString(columnName), "yyyy-MM-dd");

        public TimeOnly GetTimeOnly(string columnName) =>
            TimeOnly.ParseExact(reader.GetString(columnName), "HH:mm:ss");
    }
}
