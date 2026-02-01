using Sodium;
using System.Text;

namespace ProSushiMsg.Client.Services;

/// <summary>
/// E2EE сервис с libsodium (NaCl).
/// Шифрует/расшифровывает сообщения перед отправкой.
/// </summary>
public class EncryptionService
{
    private byte[]? _publicKey;
    private byte[]? _secretKey;

    public EncryptionService()
    {
        Console.WriteLine("? EncryptionService создан (Sodium будет инициализирован при первом использовании)");
    }

    /// <summary>
    /// Инициализирует пару ключей для пользователя.
    /// </summary>
    public (string PublicKeyHex, string SecretKeyHex) GenerateKeyPair()
    {
        // Sodium.Core API: возвращает KeyPair объект
        var keyPair = PublicKeyAuth.GenerateKeyPair();

        _publicKey = keyPair.PublicKey;
        _secretKey = keyPair.PrivateKey;

        return (
            Convert.ToHexString(keyPair.PublicKey),
            Convert.ToHexString(keyPair.PrivateKey)
        );
    }

    /// <summary>
    /// Загружает приватный ключ пользователя из хранилища.
    /// </summary>
    public void LoadSecretKey(string secretKeyHex)
    {
        _secretKey = Convert.FromHexString(secretKeyHex);
    }

    /// <summary>
    /// Шифрует сообщение для другого пользователя (sealed box).
    /// </summary>
    public string EncryptMessage(string message, string recipientPublicKeyHex)
    {
        if (_secretKey == null)
            throw new InvalidOperationException("Секретный ключ не загружен");

        var recipientPublicKey = Convert.FromHexString(recipientPublicKeyHex);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        // Используем Sealed Box (проще — не нужна информация о отправителе в plaintext)
        var ciphertext = SealedPublicKeyBox.Create(messageBytes, recipientPublicKey);

        return Convert.ToHexString(ciphertext);
    }

    /// <summary>
    /// Расшифровывает полученное сообщение.
    /// </summary>
    public string DecryptMessage(string encryptedHex, string senderPublicKeyHex)
    {
        if (_secretKey == null)
            throw new InvalidOperationException("Секретный ключ не загружен");

        var ciphertext = Convert.FromHexString(encryptedHex);
        var senderPublicKey = Convert.FromHexString(senderPublicKeyHex);

        try
        {
            // Для Sealed Box используем только ciphertext
            var decrypted = SealedPublicKeyBox.Open(ciphertext, senderPublicKey, _secretKey);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Не удалось расшифровать сообщение: {ex.Message}");
        }
    }

    /// <summary>
    /// Подписывает сообщение (для верификации отправителя).
    /// </summary>
    public string SignMessage(string message)
    {
        if (_secretKey == null)
            throw new InvalidOperationException("Секретный ключ не загружен");

        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signed = PublicKeyAuth.SignDetached(messageBytes, _secretKey);

        return Convert.ToHexString(signed);
    }

    /// <summary>
    /// Проверяет подпись сообщения.
    /// </summary>
    public bool VerifySignature(string message, string signatureHex, string senderPublicKeyHex)
    {
        try
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var signature = Convert.FromHexString(signatureHex);
            var senderPublicKey = Convert.FromHexString(senderPublicKeyHex);

            return PublicKeyAuth.VerifyDetached(signature, messageBytes, senderPublicKey);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Шифрует файл перед загрузкой (secret box — симметричное).
    /// </summary>
    public byte[] EncryptFile(byte[] fileData, byte[] sharedKey)
    {
        var nonce = SodiumCore.GetRandomBytes(24); // SecretBox.NonceBytes = 24
        var encrypted = SecretBox.Create(fileData, nonce, sharedKey);

        // Комбинируем nonce + encrypted
        var result = new byte[nonce.Length + encrypted.Length];
        Array.Copy(nonce, 0, result, 0, nonce.Length);
        Array.Copy(encrypted, 0, result, nonce.Length, encrypted.Length);

        return result;
    }

    /// <summary>
    /// Расшифровывает файл после загрузки.
    /// </summary>
    public byte[] DecryptFile(byte[] encryptedData, byte[] sharedKey)
    {
        // Извлекаем nonce (24 байта)
        var nonce = new byte[24];
        Array.Copy(encryptedData, 0, nonce, 0, nonce.Length);

        // Извлекаем шифротекст
        var ciphertext = new byte[encryptedData.Length - nonce.Length];
        Array.Copy(encryptedData, nonce.Length, ciphertext, 0, ciphertext.Length);

        try
        {
            return SecretBox.Open(ciphertext, nonce, sharedKey);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Не удалось расшифровать файл: {ex.Message}");
        }
    }
}
