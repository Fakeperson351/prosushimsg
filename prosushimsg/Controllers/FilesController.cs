using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSushiMsg.Models;

namespace ProSushiMsg.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private const string UploadFolder = "uploads"; // Папка для файлов
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 МБ (важно для 512 МБ RAM!)

    // Загрузка файла (фото, голосовое, документ)
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] MessageType type)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл пустой");

        // Проверка размера (доп защита)
        if (file.Length > MaxFileSize)
            return BadRequest($"Файл слишком большой (макс {MaxFileSize / 1024 / 1024} МБ)");

        // Создать папку, если её нет
        if (!Directory.Exists(UploadFolder))
            Directory.CreateDirectory(UploadFolder);

        // Генерируем уникальное имя (GUID + расширение)
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(UploadFolder, fileName);

        // ВАЖНО: Используем Stream для экономии памяти!
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
        {
            await file.CopyToAsync(stream);
        }

        // Возвращаем URL для клиента
        var fileUrl = $"/api/files/{fileName}";
        return Ok(new { fileUrl, fileName, size = file.Length, type });
    }

    // Скачивание файла
    [HttpGet("{fileName}")]
    [AllowAnonymous] // Можно сделать Authorize, если нужна защита
    public IActionResult DownloadFile(string fileName)
    {
        var filePath = Path.Combine(UploadFolder, fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Определяем MIME тип
        var contentType = GetContentType(fileName);

        // Потоковая отдача (не загружаем весь файл в память!)
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
        return File(fileStream, contentType, enableRangeProcessing: true);
    }

    // Удаление файла (только владелец)
    [HttpDelete("{fileName}")]
    public IActionResult DeleteFile(string fileName)
    {
        // TODO: Проверить, что файл принадлежит текущему пользователю (нужна таблица Files)
        var filePath = Path.Combine(UploadFolder, fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        System.IO.File.Delete(filePath);
        return Ok();
    }

    // Определение MIME типа по расширению
    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
