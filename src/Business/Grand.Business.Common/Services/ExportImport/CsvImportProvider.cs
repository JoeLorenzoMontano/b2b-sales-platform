using Grand.Business.Customers.Dto;
using Grand.Business.Core.Interfaces.ExportImport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.ExportImport
{
    public class CsvImportProvider : IImportDataProvider
    {
        public async Task<IEnumerable<T>> Convert<T>(Stream stream)
        {
            // For simplicity, we'll focus specifically on CustomerImportDto
            if (typeof(T) == typeof(CustomerImportDto))
            {
                return (IEnumerable<T>)(await ConvertCustomerCsv(stream));
            }
            
            throw new NotSupportedException($"Import of type {typeof(T).Name} is not supported by this provider");
        }
        
        private async Task<IEnumerable<CustomerImportDto>> ConvertCustomerCsv(Stream stream)
        {
            var result = new List<CustomerImportDto>();
            
            using (var reader = new StreamReader(stream))
            {
                // Read header line
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine))
                    return result;

                // Parse header columns
                var headers = ParseCsvLine(headerLine);
                
                // Create a mapping of header indexes
                var activeIndex = Array.IndexOf(headers, "Active");
                var companyIndex = Array.IndexOf(headers, "Company");
                var firstNameIndex = Array.IndexOf(headers, "FirstName");
                var lastNameIndex = Array.IndexOf(headers, "LastName");
                var emailIndex = Array.IndexOf(headers, "Email");
                var phoneIndex = Array.IndexOf(headers, "Phone");
                var streetAddressIndex = Array.IndexOf(headers, "StreetAddress");
                var cityIndex = Array.IndexOf(headers, "City");
                var stateIndex = Array.IndexOf(headers, "State");
                var zipIndex = Array.IndexOf(headers, "Zip");
                var passwordIndex = Array.IndexOf(headers, "Password");

                // Read data lines
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = ParseCsvLine(line);
                    
                    // Skip if not enough values
                    if (values.Length < 1)
                        continue;
                    
                    // Skip if email is missing (required field)
                    if (emailIndex == -1 || emailIndex >= values.Length || string.IsNullOrEmpty(values[emailIndex]))
                        continue;
                    
                    var customerDto = new CustomerImportDto
                    {
                        Email = emailIndex >= 0 && emailIndex < values.Length ? values[emailIndex] : "",
                        Active = activeIndex >= 0 && activeIndex < values.Length ? 
                            ConvertToBool(values[activeIndex]) : true,
                        Company = companyIndex >= 0 && companyIndex < values.Length ? values[companyIndex] : "",
                        FirstName = firstNameIndex >= 0 && firstNameIndex < values.Length ? values[firstNameIndex] : "",
                        LastName = lastNameIndex >= 0 && lastNameIndex < values.Length ? values[lastNameIndex] : "",
                        Phone = phoneIndex >= 0 && phoneIndex < values.Length ? values[phoneIndex] : "",
                        StreetAddress = streetAddressIndex >= 0 && streetAddressIndex < values.Length ? values[streetAddressIndex] : "",
                        City = cityIndex >= 0 && cityIndex < values.Length ? values[cityIndex] : "",
                        State = stateIndex >= 0 && stateIndex < values.Length ? values[stateIndex] : "",
                        Zip = zipIndex >= 0 && zipIndex < values.Length ? values[zipIndex] : "",
                        Password = passwordIndex >= 0 && passwordIndex < values.Length && !string.IsNullOrEmpty(values[passwordIndex]) ? 
                            values[passwordIndex] : "Password1!"
                    };
                    
                    result.Add(customerDto);
                }
            }

            return result;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line))
                return result.ToArray();
            
            // Pattern to match CSV values considering quoted fields (which may contain commas)
            var pattern = @"(?:^|,)(?=[^""]|(""|^)[^""]*(""|$))""|""|(?:(?:^|,)[^,]*)+";
            var matches = Regex.Matches(line, pattern);
            
            foreach (Match match in matches)
            {
                string value = match.Value.TrimStart(',');
                
                // Remove quotes if the value is quoted
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                    // Handle escaped quotes
                    value = value.Replace("\"\"", "\"");
                }
                
                result.Add(value);
            }
            
            return result.ToArray();
        }

        private bool ConvertToBool(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;  // Default to true
                
            if (bool.TryParse(value, out bool result))
                return result;
                
            if (value.Equals("1") || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || 
                value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("active", StringComparison.OrdinalIgnoreCase))
                return true;
                
            if (value.Equals("0") || value.Equals("no", StringComparison.OrdinalIgnoreCase) || 
                value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                return false;
                
            return true;  // Default to true if parsing fails
        }
    }
}