using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace AddressFormatter
{
    public class AddressAttribute
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class Address
    {
        public string? _id { get; set; }
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? VatNumber { get; set; }
        public string? CountryId { get; set; }
        public string? StateProvinceId { get; set; }
        public string? City { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? ZipPostalCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Note { get; set; }
        public int AddressType { get; set; }
        public List<AddressAttribute>? Attributes { get; set; }
    }

    public class Customer
    {
        public string? _id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public List<Address>? Addresses { get; set; }
        public Address? BillingAddress { get; set; }
        public Address? ShippingAddress { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Read the JSON file
                string jsonPath = "../customer_addresses.json";
                string jsonContent = await File.ReadAllTextAsync(jsonPath);
                var customers = JsonSerializer.Deserialize<List<Customer>>(jsonContent);

                if (customers == null)
                {
                    Console.WriteLine("Failed to deserialize customer data");
                    return;
                }

                Console.WriteLine("CUSTOMER_NAME\tAddress\tCity\tState\tZip");

                foreach (var customer in customers)
                {
                    var customerName = GetCustomerName(customer);
                    if (string.IsNullOrEmpty(customerName) || customerName == "Unknown") continue;

                    var addresses = new List<Address>();
                    
                    // Add billing address if it exists and has address data
                    if (customer.BillingAddress != null && !string.IsNullOrEmpty(customer.BillingAddress.Address1))
                        addresses.Add(customer.BillingAddress);
                    
                    // Add shipping address if it exists and has address data (and is different from billing)
                    if (customer.ShippingAddress != null && !string.IsNullOrEmpty(customer.ShippingAddress.Address1))
                    {
                        if (customer.BillingAddress == null || 
                            customer.BillingAddress.Address1 != customer.ShippingAddress.Address1 ||
                            customer.BillingAddress.City != customer.ShippingAddress.City)
                        {
                            addresses.Add(customer.ShippingAddress);
                        }
                    }
                    
                    // Add additional addresses
                    if (customer.Addresses != null)
                    {
                        foreach (var addr in customer.Addresses)
                        {
                            if (!string.IsNullOrEmpty(addr.Address1))
                            {
                                // Check if this address is different from already added ones
                                bool isDuplicate = addresses.Any(a => 
                                    a.Address1 == addr.Address1 && a.City == addr.City);
                                
                                if (!isDuplicate)
                                    addresses.Add(addr);
                            }
                        }
                    }

                    // Output each unique address
                    foreach (var address in addresses)
                    {
                        var addressLine = FormatAddress(address.Address1, address.Address2);
                        var city = address.City ?? "";
                        var zip = address.ZipPostalCode ?? "";
                        
                        // Skip if essential address components are missing
                        if (string.IsNullOrEmpty(addressLine) || string.IsNullOrEmpty(city))
                            continue;

                        Console.WriteLine($"{customerName}\t{addressLine}\t{city}\tOR\t{zip}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static string GetCustomerName(Customer customer)
        {
            // Try to get company name from billing address first
            if (customer.BillingAddress?.Company != null && !string.IsNullOrEmpty(customer.BillingAddress.Company))
                return customer.BillingAddress.Company;
            
            // Try shipping address company
            if (customer.ShippingAddress?.Company != null && !string.IsNullOrEmpty(customer.ShippingAddress.Company))
                return customer.ShippingAddress.Company;
            
            // Try first address company
            if (customer.Addresses?.FirstOrDefault()?.Company != null && !string.IsNullOrEmpty(customer.Addresses.First().Company))
                return customer.Addresses.First().Company;
            
            // Fall back to name from billing address
            if (customer.BillingAddress != null)
            {
                var firstName = customer.BillingAddress.FirstName ?? "";
                var lastName = customer.BillingAddress.LastName ?? "";
                if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                    return $"{firstName} {lastName}".Trim();
            }
            
            // Fall back to name from shipping address
            if (customer.ShippingAddress != null)
            {
                var firstName = customer.ShippingAddress.FirstName ?? "";
                var lastName = customer.ShippingAddress.LastName ?? "";
                if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                    return $"{firstName} {lastName}".Trim();
            }
            
            // Fall back to first address name
            if (customer.Addresses?.Any() == true)
            {
                var addr = customer.Addresses.First();
                var firstName = addr.FirstName ?? "";
                var lastName = addr.LastName ?? "";
                if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                    return $"{firstName} {lastName}".Trim();
            }
            
            // Last resort - use email
            return customer.Email ?? "Unknown";
        }

        static string FormatAddress(string? address1, string? address2)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(address1)) parts.Add(address1);
            if (!string.IsNullOrEmpty(address2)) parts.Add(address2);
            return string.Join(", ", parts);
        }
    }
}