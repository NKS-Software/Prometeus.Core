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

    public static string TransactionsOnAfterComplete => """
        CREATE TRIGGER IF NOT EXISTS Transactions_OnAfterComplete
        AFTER UPDATE OF CompletedAt ON Transactions
        WHEN OLD.CompletedAt IS NULL AND NEW.CompletedAt IS NOT NULL AND NEW.RejectReason IS NULL
        BEGIN
            UPDATE Accounts
            SET Balance = Balance + (
                SELECT SUM(te.Amount * te.Type)
                FROM TransactionEntries te
                WHERE te.TransactionId = NEW.Id AND te.AccountNumber = Accounts.Number
            )
        WHERE Number IN (SELECT AccountNumber FROM TransactionEntries WHERE TransactionId = NEW.Id);
        END;
        """;
}
