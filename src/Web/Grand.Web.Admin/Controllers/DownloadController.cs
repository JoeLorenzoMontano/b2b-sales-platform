using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Permissions;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Files)]
public class DownloadController : BaseAdminController
{
    private readonly IDownloadService _downloadService;
    private readonly IContextAccessor _contextAccessor;
    private readonly MediaSettings _mediaSettings;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IDownloadService downloadService, IContextAccessor contextAccessor, MediaSettings mediaSettings, ILogger<DownloadController> logger)
    {
        _downloadService = downloadService;
        _contextAccessor = contextAccessor;
        _mediaSettings = mediaSettings;
        _logger = logger;
    }

    public async Task<IActionResult> DownloadFile(Guid downloadGuid)
    {
        var download = await _downloadService.GetDownloadByGuid(downloadGuid);
        if (download == null)
            return Content("No download record found with the specified id");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //use stored data
        if (download.DownloadBinary == null)
            return Content($"Download data is not available any more. Download GD={download.Id}");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType) {
            FileDownloadName = fileName + download.Extension
        };
    }

    [Route("PreviewFile/{downloadGuid:guid}")]
    public async Task<IActionResult> PreviewFile(Guid downloadGuid)
    {
        var download = await _downloadService.GetDownloadByGuid(downloadGuid);
        if (download == null)
            return Content("No download record found with the specified id");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //use stored data
        if (download.DownloadBinary == null)
            return Content($"Download data is not available any more. Download GD={download.Id}");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";

        // Set inline disposition for preview functionality
        var fullFileName = fileName + download.Extension;
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fullFileName}\"");
        
        return File(download.DownloadBinary, contentType);
    }

    [Route("PreviewFile/{downloadId}")]
    public async Task<IActionResult> PreviewFile(string downloadId)
    {
        var download = await _downloadService.GetDownloadById(downloadId);
        if (download == null)
            return Content("No download record found with the specified id");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //use stored data
        if (download.DownloadBinary == null)
            return Content($"Download data is not available any more. Download GD={download.Id}");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";

        // Set inline disposition for preview functionality
        var fullFileName = fileName + download.Extension;
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fullFileName}\"");
        
        return File(download.DownloadBinary, contentType);
    }
    [HttpPost]

    //do not validate request token (XSRF)
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveDownloadUrl(string downloadUrl, DownloadType downloadType = DownloadType.None,
        string referenceId = "")
    {
        if (string.IsNullOrEmpty(downloadUrl)) return Json(new { success = false, error = "URL can't be empty" });
        //insert
        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = true,
            DownloadUrl = downloadUrl,
            DownloadType = downloadType,
            ReferenceId = referenceId
        };
        await _downloadService.InsertDownload(download);

        return Json(new { downloadId = download.Id, success = true });
    }

    [HttpPost]
    //do not validate request token (XSRF)
    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> AsyncUpload(IFormFile file, DownloadType downloadType = DownloadType.None,
        string referenceId = "")
    {
        if (file == null)
            return Json(new {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });

        var fileExtension = Path.GetExtension(file.FileName);
        
        // Validate file type
        var allowedDocumentTypes = FileExtensions.GetAllowedDocumentFileTypes(_mediaSettings.AllowedDocumentFileTypes);
        if (!allowedDocumentTypes.IsAllowedDocumentFileType(fileExtension))
        {
            return Json(new {
                success = false,
                message = $"File type '{fileExtension}' is not allowed. Allowed types: {string.Join(", ", allowedDocumentTypes)}"
            });
        }

        // Check if it's a ZIP file
        if (FileExtensions.IsZipFile(fileExtension))
        {
            return await ProcessZipUpload(file, downloadType, referenceId);
        }

        // Process regular file upload
        return await ProcessSingleFileUpload(file, downloadType, referenceId);
    }

    private async Task<IActionResult> ProcessSingleFileUpload(IFormFile file, DownloadType downloadType, string referenceId)
    {
        var fileBinary = file.GetDownloadBits();

        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            CustomerId = _contextAccessor.WorkContext.CurrentCustomer.Id,
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = fileBinary,
            ContentType = file.ContentType,
            Filename = Path.GetFileNameWithoutExtension(file.FileName),
            Extension = Path.GetExtension(file.FileName),
            DownloadType = downloadType,
            ReferenceId = referenceId
        };
        await _downloadService.InsertDownload(download);

        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            downloadId = download.Id,
            downloadUrl = Url.Action("DownloadFile",
                new { downloadGuid = download.DownloadGuid, area = Constants.AreaAdmin })
        });
    }

    private async Task<IActionResult> ProcessZipUpload(IFormFile file, DownloadType downloadType, string referenceId)
    {
        const int maxZipSizeBytes = 52428800; // 50MB
        const int maxFilesInZip = 100;
        const int maxExtractedSizeBytes = 104857600; // 100MB
        
        _logger.LogInformation("Processing ZIP upload: {FileName}, Size: {FileSize} bytes", file.FileName, file.Length);
        
        // Validate ZIP file size
        if (file.Length > maxZipSizeBytes)
        {
            _logger.LogWarning("ZIP file too large: {FileSize} bytes", file.Length);
            return Json(new {
                success = false,
                message = $"ZIP file is too large. Maximum size: {maxZipSizeBytes / (1024 * 1024)}MB"
            });
        }

        var extractedFiles = new List<object>();
        var allowedDocumentTypes = FileExtensions.GetAllowedDocumentFileTypes(_mediaSettings.AllowedDocumentFileTypes);
        var zipFileName = Path.GetFileNameWithoutExtension(file.FileName);
        
        _logger.LogInformation("Allowed document types: {AllowedTypes}", string.Join(", ", allowedDocumentTypes));
        
        try
        {
            using var zipStream = new MemoryStream();
            await file.CopyToAsync(zipStream);
            zipStream.Position = 0;

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            
            // Validate number of files
            if (archive.Entries.Count > maxFilesInZip)
            {
                return Json(new {
                    success = false,
                    message = $"ZIP contains too many files. Maximum: {maxFilesInZip} files"
                });
            }

            var totalExtractedSize = 0L;
            var fileCounter = 0;

            foreach (var entry in archive.Entries)
            {
                _logger.LogInformation("Processing ZIP entry: {EntryName}, Size: {EntrySize}", entry.Name, entry.Length);
                
                // Skip directories
                if (string.IsNullOrEmpty(entry.Name) || entry.Name.EndsWith('/'))
                {
                    _logger.LogInformation("Skipping directory: {EntryName}", entry.Name);
                    continue;
                }

                // Validate extracted size
                totalExtractedSize += entry.Length;
                if (totalExtractedSize > maxExtractedSizeBytes)
                {
                    _logger.LogWarning("Total extracted size exceeds limit: {TotalSize} bytes", totalExtractedSize);
                    return Json(new {
                        success = false,
                        message = $"Total extracted size exceeds limit. Maximum: {maxExtractedSizeBytes / (1024 * 1024)}MB"
                    });
                }

                var entryExtension = Path.GetExtension(entry.Name);
                _logger.LogInformation("Entry extension: {Extension}", entryExtension);
                
                // Skip files with disallowed extensions (excluding ZIP to prevent recursion)
                if (string.Equals(entryExtension, ".zip", StringComparison.OrdinalIgnoreCase) ||
                    !allowedDocumentTypes.IsAllowedDocumentFileType(entryExtension))
                {
                    _logger.LogInformation("Skipping file with disallowed extension: {Extension}", entryExtension);
                    continue;
                }

                // Sanitize filename
                var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(entry.Name));
                if (string.IsNullOrEmpty(fileName))
                    continue;

                // Handle duplicate filenames
                var finalFileName = fileCounter > 0 ? $"{zipFileName}_{fileName}_{fileCounter}" : $"{zipFileName}_{fileName}";
                fileCounter++;

                // Extract file
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream);
                var fileBinary = memoryStream.ToArray();

                // Create download entity
                var download = new Download {
                    DownloadGuid = Guid.NewGuid(),
                    CustomerId = _contextAccessor.WorkContext.CurrentCustomer.Id,
                    UseDownloadUrl = false,
                    DownloadUrl = "",
                    DownloadBinary = fileBinary,
                    ContentType = GetContentTypeFromExtension(entryExtension),
                    Filename = finalFileName,
                    Extension = entryExtension,
                    DownloadType = downloadType,
                    ReferenceId = referenceId
                };

                await _downloadService.InsertDownload(download);

                extractedFiles.Add(new {
                    downloadId = download.Id,
                    filename = finalFileName + entryExtension,
                    downloadUrl = Url.Action("DownloadFile",
                        new { downloadGuid = download.DownloadGuid, area = Constants.AreaAdmin })
                });
            }

            _logger.LogInformation("ZIP processing complete. Extracted {FileCount} files", extractedFiles.Count);
            
            if (!extractedFiles.Any())
            {
                _logger.LogWarning("No valid files found in ZIP archive");
                return Json(new {
                    success = false,
                    message = "No valid files found in ZIP archive"
                });
            }

            return Json(new {
                success = true,
                message = $"Successfully extracted {extractedFiles.Count} files from ZIP",
                files = extractedFiles
            });
        }
        catch (Exception ex)
        {
            return Json(new {
                success = false,
                message = $"Error processing ZIP file: {ex.Message}"
            });
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
        
        // Limit length
        if (sanitized.Length > 100)
            sanitized = sanitized.Substring(0, 100);
            
        return sanitized.Trim();
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}