//strpsql-on
namespace Prometeus.Core.Data.Queries.DDL;

public static class Indexes
{
    public static string Accounts => """
        CREATE INDEX IF NOT EXISTS idx_Accounts_ClientId ON Accounts(ClientId);
        """;
}
