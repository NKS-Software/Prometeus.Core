//strpsql-on
namespace Prometeus.Core.Data.Queries.DDL;

public static class Tables
{
    /// <summary>
    /// Запрос для создания таблицы клиентов
    /// </summary>
    public static string Clients => """
        CREATE TABLE IF NOT EXISTS Clients(
            Id VARCHAR(36) PRIMARY KEY NOT NULL,
            Name VARCHAR(128) NOT NULL,
            Surname VARCHAR(128) NOT NULL,
            Patronimyc VARCHAR(128) NULL,
            BirthDate VARCHAR(10) NOT NULL CHECK(LENGTH(BirthDate) = 10 AND BirthDate GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'),
            Email VARCHAR(256) NOT NULL UNIQUE CHECK(Email LIKE '%@%.%'),
            Phone VARCHAR(16) NOT NULL UNIQUE CHECK(Phone LIKE '+%')
        );
        """;

    /// <summary>
    /// Запрос для создания таблицы счетов
    /// </summary>
    public static string Accounts => """
        CREATE TABLE IF NOT EXISTS Accounts(
            ClientId VARCHAR(36) NULL REFERENCES Clients(Id) ON DELETE SET NULL,
            Number VARCHAR(20) PRIMARY KEY NOT NULL CHECK(LENGTH(Number) = 20 AND Number GLOB '[1-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
            Name VARCHAR(128) NOT NULL DEFAULT 'Payment account',
            OpenedAt VARCHAR(19) NOT NULL CHECK(LENGTH(OpenedAt) = 19 AND OpenedAt GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'),
            ClosedAt VARCHAR(19) NULL CHECK(ClosedAt IS NULL OR(LENGTH(ClosedAt) = 19 AND ClosedAt GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]')),
            Balance DECIMAL(19,2) NOT NULL DEFAULT 0 CHECK(Balance >= 0)
        );
        """;

    /// <summary>
    /// Запрос для создания таблицы карт
    /// </summary>
    public static string Cards => """
        CREATE TABLE IF NOT EXISTS Cards(
            Number VARCHAR(19) PRIMARY KEY NOT NULL CHECK((LENGTH(Number) BETWEEN 12 AND 19)),
            AccountNumber VARCHAR(20) NOT NULL REFERENCES Accounts(Number) ON DELETE NO ACTION,
            PinHash TEXT NOT NULL,
            CvcKeyId TEXT NOT NULL,
            ValidUntil VARCHAR(10) NOT NULL CHECK(LENGTH(ValidUntil) = 10 AND ValidUntil GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'),
            CardHolderName VARCHAR(256) NOT NULL,
            Status INT DEFAULT 0 CHECK(Status IN(0,1,2))
        )
        """;

    /// <summary>
    /// Запрос для создания таблиц транзакций и их проводок
    /// </summary>
    public static string Transactions => """
        CREATE TABLE IF NOT EXISTS Transactions(
            Id VARCHAR(36) PRIMARY KEY NOT NULL,
            CreatedAt VARCHAR(19) NOT NULL CHECK(LENGTH(CreatedAt) = 19 AND CreatedAt GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'),
            CompletedAt VARCHAR(19) CHECK(CompletedAt IS NULL OR LENGTH(CompletedAt) = 19 AND CompletedAt GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]'),
            RejectReason TEXT,
            Metadata TEXT NOT NULL DEFAULT '{}'
        );

        CREATE TABLE IF NOT EXISTS TransactionEntries(
            Id VARCHAR(36) PRIMARY KEY NOT NULL,
            TransactionId VARCHAR(36) NOT NULL REFERENCES Transactions(Id),
            AccountNumber VARCHAR(20) NOT NULL REFERENCES Accounts(Number) ON DELETE NO ACTION,
            Amount DECIMAL NOT NULL,
            Type INTEGER NOT NULL DEFAULT(0) CHECK(Type IN(-1,1))
        );
        """;
}
