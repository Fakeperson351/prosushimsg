using Microsoft.AspNetCore.SignalR.Client;
using ProSushiMsg.Client.Services;

namespace ProSushiMsg.Client.Pages;

/// <summary>
/// Расширение для SignalRService с шифрованием.
/// Все сообщения автоматически шифруются перед отправкой.
/// </summary>
public static class EncryptedSignalRExtensions
{
    /// <summary>
    /// Отправляет зашифрованное сообщение.
    /// </summary>
    public static async Task SendEncryptedMessageAsync(
        this SignalRService service,
        HubConnection connection,
        string message,
        EncryptionService encryption,
        string recipientPublicKeyHex,
        int? groupId = null)
    {
        // Шифруем сообщение
        var encrypted = encryption.EncryptMessage(message, recipientPublicKeyHex);

        // Подписываем для верификации отправителя
        var signature = encryption.SignMessage(message);

        // Отправляем через SignalR
        if (groupId.HasValue)
            await connection.SendAsync("SendEncryptedGroupMessage", groupId.Value, encrypted, signature);
        else
            await connection.SendAsync("SendEncryptedMessage", encrypted, signature);
    }

    /// <summary>
    /// Обработчик получения зашифрованного сообщения.
    /// </summary>
    public static void OnEncryptedMessageReceived(
        this HubConnection connection,
        EncryptionService encryption,
        Func<int, string, string, Task> handler)
    {
        connection.On<int, string, string>("ReceiveEncryptedMessage", async (userId, encryptedMsg, signature) =>
        {
            try
            {
                // Расшифровываем
                var decrypted = encryption.DecryptMessage(encryptedMsg, "sender_public_key_hex");

                // Проверяем подпись (опционально)
                // var isValid = encryption.VerifySignature(decrypted, signature, "sender_public_key_hex");

                await handler(userId, decrypted, DateTime.Now.ToString("HH:mm"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка расшифровки: {ex.Message}");
            }
        });
    }
}
