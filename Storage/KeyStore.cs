using Prometeus.Core.Utils;
using System.Text;

namespace Prometeus.Core.Storage;

/// <summary>
/// Эмуляция HSM (Hardware Security Module) для хранения чувствительных данных.
/// <para>
/// В реальной банковской системе ключи хранятся в аппаратном HSM и никогда не покидают его.
/// Данная реализация — программная эмуляция для DEV-окружения и пет-проектов.
/// </para>
/// <para>
/// Поддерживает два режима:
/// <list type="bullet">
/// <item><description>In-Memory — данные только в памяти (для тестов)</description></item>
/// <item><description>File — данные в зашифрованном файле (для DEV)</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class KeyStore : IDisposable
{
    private readonly string _masterKey;
    private readonly string _filePath;
    private readonly bool _inMemory;
    private Dictionary<Guid, string> memoryCache = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;
    private KeyStore(string key, string filePath)
    {
        _masterKey = key;
        _filePath = filePath;
        if(filePath.Equals(":memory:")) _inMemory = true;
    }

    /// <summary>
    /// Builder для создания экземпляра <see cref="KeyStore"/>
    /// </summary>
    public sealed class Builder
    {
        private string _masterKey;
        private string _filePath;

        /// <summary>
        /// Устанавливает мастер-ключ для шифрования (32 байта, AES-256)
        /// </summary>
        /// <param name="key">Мастер-ключ (32 символа)</param>
        /// <returns>Builder для цепочки вызовов</returns>
        public Builder SetMasterKey(string key)
        {
            _masterKey = key;
            return this;
        }

        /// <summary>
        /// Устанавливает путь к файлу для хранения данных
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Builder для цепочки вызовов</returns>
        public Builder SetFilePath(string filePath)
        {
            _filePath = filePath;
            return this;
        }

        /// <summary>
        /// Устанавливает режим In-Memory (данные только в памяти)
        /// </summary>
        /// <returns>Builder для цепочки вызовов</returns>
        public Builder InMemory()
        {
            _filePath = ":memory:";
            return this;
        }

        /// <summary>
        /// Создаёт экземпляр <see cref="KeyStore"/>
        /// </summary>
        /// <returns>Новый экземпляр KeyStore</returns>
        /// <exception cref="InvalidOperationException">
        /// Если не задан мастер-ключ, ключ не 32 байта, или не задан путь/режим
        /// </exception>
        public KeyStore Build()
        {
            if (string.IsNullOrEmpty(_masterKey))
                throw new InvalidOperationException("Ключ не задан. Вызовите SetKey()");
            if (Encoding.UTF8.GetByteCount(_masterKey) != 32)
                throw new InvalidOperationException("Ключ должен быть 32 байта (AES-256)");
            if (string.IsNullOrEmpty(_filePath))
                throw new InvalidOperationException("Путь к файлу не задан. Вызовите SetFilePath() или InMemory()");
            return new KeyStore(_masterKey, _filePath);
        }
    }

    /// <summary>
    /// Создаёт файл для хранения данных, если его нет
    /// </summary>
    /// <remarks>
    /// В режиме In-Memory ничего не делает.
    /// </remarks>
    public void EnsureCreate()
    {
        if (_inMemory) return;
        if (!File.Exists(_filePath))
        {
            using var s = File.Create(_filePath);
        }
    }

    /// <summary>
    /// Записывает значение в хранилище и возвращает уникальный идентификатор
    /// </summary>
    /// <param name="value">Значение для сохранения (будет зашифровано)</param>
    /// <param name="token">Токен отмены</param>
    /// <returns>Уникальный идентификатор (GUID) для последующего чтения</returns>
    /// <remarks>
    /// Значение шифруется мастер-ключом перед сохранением.
    /// В файловом режиме данные добавляются в конец файла.
    /// </remarks>
    public async Task<Guid> WriteAsync(string value, CancellationToken token = default)
    {
        await _lock.WaitAsync(token);
        try
        {
            var id = Guid.NewGuid();
            var encriptedValue = Encryptor.Encrypt(value, _masterKey);
            if (_inMemory)
            {
                startInMemory:
                id = Guid.NewGuid();
                if (memoryCache.TryAdd(id, encriptedValue)) return id;
                else goto startInMemory;
            }
            else
            {
                var data = await LoadFileAsync(token);
                start:
                id = Guid.NewGuid();
                if (data.TryAdd(id, encriptedValue) && memoryCache.TryAdd(id,encriptedValue))
                {
                    await File.AppendAllLinesAsync(_filePath, [$"{id}:{encriptedValue}"], token);
                    return id;
                }
                else goto start;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Читает значение по идентификатору и расшифровывает его
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="token">Токен отмены</param>
    /// <returns>Расшифрованное значение или null, если не найдено</returns>
    /// <remarks>
    /// Сначала проверяет кэш в памяти. Если нет — читает из файла (в файловом режиме).
    /// </remarks>
    public async Task<string?> ReadAsync(Guid id, CancellationToken token = default)
    {
        await _lock.WaitAsync(token);
        try
        {
            if (memoryCache.TryGetValue(id, out var encriptedValue))
            {
                return Encryptor.Decrypt(encriptedValue, _masterKey);
            }
            else
            {
                if (!_inMemory)
                {
                    var data = await LoadFileAsync(token);
                    if (data.TryGetValue(id, out var encriptedData))
                    {
                        return Encryptor.Decrypt(encriptedData, _masterKey);
                    }
                }
            }
            return null;
        }
        finally 
        { 
            _lock.Release(); 
        }
    }

    private async Task<Dictionary<Guid, string>> LoadFileAsync(CancellationToken token)
    {
        var result = new Dictionary<Guid, string>();
        var lines = await File.ReadAllLinesAsync(_filePath, token);
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2 && Guid.TryParse(parts[0], out var id))
                result[id] = parts[1];
        }
        return result;
    }

    /// <summary>
    /// Освобождает ресурсы
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _lock.Dispose();
        memoryCache.Clear();
        _disposed = true;
    }
}