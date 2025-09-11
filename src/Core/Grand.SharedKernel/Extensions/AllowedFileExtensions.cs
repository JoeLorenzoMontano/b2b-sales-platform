namespace Grand.SharedKernel.Extensions;

public static class FileExtensions
{
    public static IList<string> GetAllowedMediaFileTypes(string allowedFileTypes)
    {
        if (string.IsNullOrEmpty(allowedFileTypes))
            return new List<string> { ".gif", ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        return allowedFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
    }

    public static IList<string> GetAllowedDocumentFileTypes(string allowedDocumentFileTypes)
    {
        if (string.IsNullOrEmpty(allowedDocumentFileTypes))
            return new List<string> { ".pdf", ".doc", ".docx", ".txt", ".zip" };
        return allowedDocumentFileTypes.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
    }

    public static bool IsAllowedMediaFileType(this IEnumerable<string> allowedFileTypes, string fileExtension)
    {
        return allowedFileTypes.Any(ft => ft.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsAllowedDocumentFileType(this IEnumerable<string> allowedFileTypes, string fileExtension)
    {
        return allowedFileTypes.Any(ft => ft.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsZipFile(string fileExtension)
    {
        return string.Equals(fileExtension, ".zip", StringComparison.OrdinalIgnoreCase);
    }
}