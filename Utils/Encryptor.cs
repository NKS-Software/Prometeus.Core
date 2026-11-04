using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Prometeus.Core.Utils;

public static class Encryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    /// Шифрует данные. Nonce генерируется автоматически и встраивается в результат.
    /// Формат: base64(nonce + ciphertext + tag)
    /// </summary>
    public static string Encrypt(string data, string masterKey)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Данные не могут быть пустыми", nameof(data));
        if (string.IsNullOrEmpty(masterKey))
            throw new ArgumentException("Мастер-ключ не может быть пустым", nameof(masterKey));

        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);

        if (masterKeyBytes.Length != 32)
            throw new ArgumentException("Мастер-ключ должен быть 32 байта (AES-256)", nameof(masterKey));

        // Генерируем уникальный nonce
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] cipherData = new byte[dataBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aesGcm = new AesGcm(masterKeyBytes, TagSize);
        aesGcm.Encrypt(nonce, dataBytes, cipherData, tag);

        // Формат: nonce + ciphertext + tag
        byte[] result = new byte[NonceSize + cipherData.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(cipherData, 0, result, NonceSize, cipherData.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + cipherData.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Расшифровывает данные. Nonce извлекается из шифротекста.
    /// </summary>
    public static string Decrypt(string encryptedBase64, string masterKey)
    {
        if (string.IsNullOrEmpty(encryptedBase64))
            throw new ArgumentException("Зашифрованные данные не могут быть пустыми", nameof(encryptedBase64));
        if (string.IsNullOrEmpty(masterKey))
            throw new ArgumentException("Мастер-ключ не может быть пустым", nameof(masterKey));

        byte[] masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);
        if (masterKeyBytes.Length != 32)
            throw new ArgumentException("Мастер-ключ должен быть 32 байта (AES-256)", nameof(masterKey));

        byte[] encrypted = Convert.FromBase64String(encryptedBase64);

        if (encrypted.Length < NonceSize + TagSize)
            throw new ArgumentException("Некорректные зашифрованные данные", nameof(encryptedBase64));

        // Извлекаем nonce, ciphertext, tag
        byte[] nonce = new byte[NonceSize];
        int cipherLength = encrypted.Length - NonceSize - TagSize;
        byte[] ciphertext = new byte[cipherLength];
        byte[] tag = new byte[TagSize];

        Buffer.BlockCopy(encrypted, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encrypted, NonceSize, ciphertext, 0, cipherLength);
        Buffer.BlockCopy(encrypted, NonceSize + cipherLength, tag, 0, TagSize);

        byte[] plaintext = new byte[cipherLength];

        using var aesGcm = new AesGcm(masterKeyBytes, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
