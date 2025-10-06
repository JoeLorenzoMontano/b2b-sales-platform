using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;
using System.Globalization;
using System.Text;

namespace Grand.Business.Common.Services.ExportImport;

public class CsvExportProvider : IExportProvider
{
    public byte[] ExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
    {
        var sb = new StringBuilder();
        
        // Add headers
        var headers = properties.Select(p => EscapeCsvField(p.PropertyName));
        sb.AppendLine(string.Join(",", headers));
        
        // Add data rows
        foreach (var item in itemsToExport)
        {
            var values = properties.Select(property =>
            {
                var value = property.GetProperty(item);
                return EscapeCsvField(value?.ToString() ?? string.Empty);
            });
            
            sb.AppendLine(string.Join(",", values));
        }
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public IExportProvider BuilderExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
    {
        // For CSV, we don't need a builder pattern, just return self
        return this;
    }

    public byte[] BuilderExportToByte()
    {
        // Not applicable for CSV export
        return Array.Empty<byte>();
    }
    
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
            
        // If field contains comma, newline, or double quote, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('\n') || field.Contains('\r') || field.Contains('"'))
        {
            return '"' + field.Replace("\"", "\"\"") + '"';
        }
        
        return field;
    }
}