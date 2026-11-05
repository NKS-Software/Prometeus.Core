# Prometeus.Core

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-blue)](https://learn.microsoft.com/dotnet/csharp/)
[![SQLite](https://img.shields.io/badge/SQLite-3-blue)](https://www.sqlite.org/)
[![Tests](https://img.shields.io/badge/tests-170%2B-green)](https://github.com/NKS-Software/Prometeus.Core.Tests)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

Ядро автоматизированной банковской системы (АБС) на C#. Реализует клиентов, счета, карты и транзакции с **двойной записью** (дебет = кредит), строгой валидацией на уровне SQL и безопасным хранением чувствительных данных.

---

## 🚀 Возможности

### 🏦 Банковские сущности
- **Клиенты** — ФИО, дата рождения, email, телефон (с проверкой на дубликаты).
- **Счета** — расчётные, корреспондентские, системные (касса, капитал, доходы).
- **Карты** — привязка к счёту, статусы (`Active`, `Frozen`, `Locked`), срок действия.
- **Транзакции** — с проводками и **двойной записью** (сумма дебетов = сумме кредитов).

### 🔐 Безопасность
- **PIN** — PBKDF2 (HMACSHA256, 100 000 итераций, соль).
- **CVC** — AES-256-GCM (nonce встраивается в шифротекст).
- **KeyStore** — эмуляция HSM: ключи хранятся отдельно от БД, в БД только GUID-ссылка.
- **Разделение данных и ключей** — утечка одной БД бесполезна без другой.

### ⚙️ Генераторы
- **Номера счетов** — расчётные (`408...`), корреспондентские (`301...`), системные (`202...`, `100...`, `700...`) с контрольным ключом по алгоритму ЦБ РФ (MOD 10).
- **Номера карт** — по ISO/IEC 7812 с проверкой Luhn и внешним BIN.

### 🗄️ База данных
- SQLite с **CHECK-ограничениями**, **GLOB-валидацией** форматов и **внешними ключами**.
- Триггеры (например, блокировка карт при закрытии счёта).
- Индексы для производительности.

### 🧪 Тесты
- **170+ тестов** (xUnit) на репозитории.
- Покрытие всех статусов, негативных сценариев и граничных случаев.
- Тесты — в [отдельном репозитории](https://github.com/NKS-Software/Prometeus.Core.Tests).

---

## 📦 Стек

| Компонент | Версия |
|-----------|--------|
| .NET | 10.0 |
| C# | 13 |
| SQLite | 3 |
| Microsoft.Data.Sqlite | 9.x |
| xUnit | 2.9+ |

---

## 🏗️ Архитектура

├──Data/ </br>
│ ├──Entities/ # Client, Account, Card, Transaction </br>
│ ├──Repositories/ # ClientRepository, AccountRepository, ... </br>
│ ├──Queries/ # DDL (схема) и DML (запросы) </br>
│ ├──Extensions/ </br>
│ ├──Context/ # DatabaseContext (управление соединениями) </br>
├──Storage/ # KeyStore (эмуляция HSM) </br>
├──Generators/ # AccountNumberGenerator, CardNumberGenerator </br>
└──Utils/ # Hasher (PBKDF2), Encryptor (AES-256-GCM) </br>

---

## 🛠️ Пример использования

### Создание контекста БД

```csharp
var context = new DatabaseContext.Builder()
    .UseFile("prometeus.db")
    .AddInitializationQuery(Tables.Clients)
    .AddInitializationQuery(Tables.Accounts)
    .AddInitializationQuery(Tables.Cards)
    .AddInitializationQuery(Tables.Transactions)
    .Add(conn => new ClientRepository(conn))
    .Add(conn => new AccountRepository(conn))
    .Add(conn => new CardRepository(conn))
    .Add(conn => new TransactionRepository(conn))
    .Build();

await context.EnsureCreateAsync();
```
### Работа с клиентом
```csharp
var clientRepo = context.Get<ClientRepository>();
var client = new Client
{
    Id = Guid.CreateVersion7(),
    Name = "Иван",
    Surname = "Иванов",
    BirthDate = new DateOnly(2000, 1, 1),
    Email = "ivan@example.com",
    Phone = "+79061571234"
};

var added = await clientRepo.AddAsync(client);
```
### Генерация номера счёта
```csharp
var accountNumber = AccountNumberGenerator.Generate(
    balanceFirst: 408,
    balanceSecond: 02,
    currencyCode: 643,
    bik: "045655635",
    branchCode: 0
);
// 40802643000000000000
```
### Добавление карты
```csharp
var keyStore = new KeyStore.Builder()
    .SetMasterKey("0123456789abcdef0123456789abcdef")
    .SetFilePath("keys.db")
    .Build();

var cvc = "123";
var cvcKeyId = await keyStore.WriteAsync(cvc);

var card = new Card
{
    Number = CardNumberGenerator.Generate("220204"),
    AccountNumber = accountNumber,
    PinHash = Hasher.Hash("1234"),
    CvcKeyId = cvcKeyId,
    CardHolderName = "IVANOV IVAN",
    ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(3)
};

var addedCard = await cardRepo.OpenAsync(card);
```

## 🧠 Принципы
| Принцип | Реализация |
|-----------|--------|
| Безопасность | PIN — хеш, CVC — шифрование, ключи — отдельно |
| Консистентность | CHECK, GLOB, FOREIGN KEY, триггеры на уровне БД |
| Прагматизм | Без лишних интерфейсов; абстракции только там, где нужны |
| Тестируемость | `SharedTimeProvider`, in-memory БД, 170+ тестов |

## 📜 Стандарты
| Наименование | Где применяется |
|-----------|--------|
| ЦБ РФ | структура номеров счетов, контрольный ключ (MOD 10) |
| ISO/IEC 7812 | номера карт (Luhn) |
| PCI DSS | CVC не хранится в открытом виде, ключи отдельно от данных |
