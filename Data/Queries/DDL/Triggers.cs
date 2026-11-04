//strpsql-on
namespace Prometeus.Core.Data.Queries.DDL;

public static class Triggers
{
    public static string AccountOnAfterClosed => """
        CREATE TRIGGER IF NOT EXISTS Account_OnClosed 
        AFTER UPDATE OF ClosedAt ON Accounts
        WHEN OLD.ClosedAt IS NULL AND NEW.ClosedAt IS NOT NULL
        BEGIN
            UPDATE Cards SET 
            Status = 2 
            WHERE AccountNumber = NEW.Number 
            AND Status != 2;
        END;
        """;
}
