using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace CustomerAddressExtractor
{
    // Define the Address model based on GrandNode2 schema
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

    public class AddressAttribute
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    // Define the Customer model with addresses
    public class Customer
    {
        public string? _id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public List<Address>? Addresses { get; set; }
        public Address? BillingAddress { get; set; }
        public Address? ShippingAddress { get; set; }
    }

    public class CustomerAddressExtractor
    {
        private readonly IMongoDatabase _database;

        public CustomerAddressExtractor(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public async Task<List<Customer>> ExtractCustomerAddressesAsync()
        {
            try
            {
                var collection = _database.GetCollection<Customer>("Customer");
                
                // Define projection to only get relevant fields
                var projection = Builders<Customer>.Projection
                    .Include(c => c._id)
                    .Include(c => c.Email)
                    .Include(c => c.Username)
                    .Include(c => c.Addresses)
                    .Include(c => c.BillingAddress)
                    .Include(c => c.ShippingAddress);

                var customers = await collection
                    .Find(FilterDefinition<Customer>.Empty)
                    .Project<Customer>(projection)
                    .ToListAsync();

                return customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting customer addresses: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Customer>> ExtractCustomersWithAddressesAsync()
        {
            try
            {
                var collection = _database.GetCollection<Customer>("Customer");
                
                // Filter to only get customers that have addresses
                var filter = Builders<Customer>.Filter.Or(
                    Builders<Customer>.Filter.Ne(c => c.Addresses, null),
                    Builders<Customer>.Filter.Ne(c => c.BillingAddress, null),
                    Builders<Customer>.Filter.Ne(c => c.ShippingAddress, null)
                );

                var projection = Builders<Customer>.Projection
                    .Include(c => c._id)
                    .Include(c => c.Email)
                    .Include(c => c.Username)
                    .Include(c => c.Addresses)
                    .Include(c => c.BillingAddress)
                    .Include(c => c.ShippingAddress);

                var customers = await collection
                    .Find(filter)
                    .Project<Customer>(projection)
                    .ToListAsync();

                return customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting customers with addresses: {ex.Message}");
                throw;
            }
        }

        public void PrintCustomerAddresses(List<Customer> customers)
        {
            Console.WriteLine($"Found {customers.Count} customers with address data");
            Console.WriteLine(new string('-', 80));

            foreach (var customer in customers)
            {
                Console.WriteLine($"Customer ID: {customer._id}");
                Console.WriteLine($"Email: {customer.Email}");
                Console.WriteLine($"Username: {customer.Username}");
                
                // Print billing address
                if (customer.BillingAddress != null)
                {
                    Console.WriteLine("Billing Address:");
                    PrintAddress(customer.BillingAddress, "  ");
                }

                // Print shipping address
                if (customer.ShippingAddress != null)
                {
                    Console.WriteLine("Shipping Address:");
                    PrintAddress(customer.ShippingAddress, "  ");
                }

                // Print additional addresses
                if (customer.Addresses != null && customer.Addresses.Any())
                {
                    Console.WriteLine($"Additional Addresses ({customer.Addresses.Count}):");
                    for (int i = 0; i < customer.Addresses.Count; i++)
                    {
                        Console.WriteLine($"  Address {i + 1}:");
                        PrintAddress(customer.Addresses[i], "    ");
                    }
                }

                Console.WriteLine(new string('-', 80));
            }
        }

        private void PrintAddress(Address address, string indent = "")
        {
            if (address == null) return;

            Console.WriteLine($"{indent}Name: {address.Name}");
            Console.WriteLine($"{indent}First Name: {address.FirstName}");
            Console.WriteLine($"{indent}Last Name: {address.LastName}");
            Console.WriteLine($"{indent}Email: {address.Email}");
            Console.WriteLine($"{indent}Company: {address.Company}");
            Console.WriteLine($"{indent}Address 1: {address.Address1}");
            Console.WriteLine($"{indent}Address 2: {address.Address2}");
            Console.WriteLine($"{indent}City: {address.City}");
            Console.WriteLine($"{indent}Zip/Postal Code: {address.ZipPostalCode}");
            Console.WriteLine($"{indent}Phone: {address.PhoneNumber}");
            Console.WriteLine($"{indent}Country ID: {address.CountryId}");
            Console.WriteLine($"{indent}State/Province ID: {address.StateProvinceId}");
            Console.WriteLine($"{indent}Address Type: {address.AddressType}");
        }

        public async Task ExportToJsonAsync(List<Customer> customers, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(customers, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(filePath, json);
                Console.WriteLine($"Customer address data exported to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting to JSON: {ex.Message}");
                throw;
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Connection string from appsettings.json
                string connectionString = "mongodb://localhost:27017/GrandNode";
                string databaseName = "GrandNode";

                // You can override connection string via command line arguments
                if (args.Length > 0)
                {
                    connectionString = args[0];
                }
                if (args.Length > 1)
                {
                    databaseName = args[1];
                }

                Console.WriteLine($"Connecting to MongoDB: {connectionString}");
                Console.WriteLine($"Database: {databaseName}");
                Console.WriteLine();

                var extractor = new CustomerAddressExtractor(connectionString, databaseName);

                // Extract customers with addresses
                var customers = await extractor.ExtractCustomersWithAddressesAsync();
                
                // Print to console
                extractor.PrintCustomerAddresses(customers);

                // Export to JSON file
                string exportPath = Path.Combine(Directory.GetCurrentDirectory(), "customer_addresses.json");
                await extractor.ExportToJsonAsync(customers, exportPath);

                Console.WriteLine();
                Console.WriteLine($"Extraction completed. Found {customers.Count} customers with address information.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
            }
        }
    }
}