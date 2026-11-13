using Microsoft.AspNetCore.Http;

namespace JuanApp.Services.Implements;

public sealed record ImageValidationResult(string? Extension, string? Error)
{
    public bool IsValid => Error is null;
}

public static class ImageUploadValidator
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    public static async Task<ImageValidationResult> ValidateAsync(IFormFile file)
    {
        if (file.Length == 0)
            return new ImageValidationResult(null, null);
        if (file.Length > MaxFileSize)
            return new ImageValidationResult(null, "Each image must be 5 MB or smaller.");

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory());
        var extension = DetectExtension(header, bytesRead);
        if (extension is null)
            return new ImageValidationResult(null, "Choose a valid JPG, PNG, GIF, WEBP, or BMP image.");

        var suppliedExtension = Path.GetExtension(file.FileName);
        if (!string.Equals(suppliedExtension, extension, StringComparison.OrdinalIgnoreCase)
            && !(extension == ".jpg" && string.Equals(suppliedExtension, ".jpeg", StringComparison.OrdinalIgnoreCase)))
            return new ImageValidationResult(null, "The file extension does not match the image format.");

        return new ImageValidationResult(extension, null);
    }

    private static string? DetectExtension(byte[] header, int length)
    {
        if (length >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
        if (length >= 6 && System.Text.Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a") return ".gif";
        if (length >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP") return ".webp";
        if (length >= 2 && header[0] == (byte)'B' && header[1] == (byte)'M') return ".bmp";
        return null;
    }
}
