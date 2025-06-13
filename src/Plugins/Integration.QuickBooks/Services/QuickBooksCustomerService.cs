using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Integration.QuickBooks.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// QuickBooks customer service implementation
    /// </summary>
    public class QuickBooksCustomerService : IQuickBooksCustomerService
    {
        private readonly IQuickBooksService _quickBooksService;
        private readonly IQuickBooksMapper _quickBooksMapper;
        private readonly ICustomerService _customerService;
        private readonly ILogger<QuickBooksCustomerService> _logger;

        public QuickBooksCustomerService(
            IQuickBooksService quickBooksService,
            IQuickBooksMapper quickBooksMapper,
            ICustomerService customerService,
            ILogger<QuickBooksCustomerService> logger)
        {
            _quickBooksService = quickBooksService;
            _quickBooksMapper = quickBooksMapper;
            _customerService = customerService;
            _logger = logger;
        }

        /// <summary>
        /// Create or update a customer in QuickBooks
        /// </summary>
        public async Task<QuickBooksCustomer> CreateOrUpdateCustomerAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                // Convert GrandNode customer to QuickBooks customer
                var qbCustomer = await _quickBooksMapper.ToQuickBooksCustomerAsync(customer);
                
                // Check if customer already exists in QuickBooks
                var quickBooksCustomerId = await _quickBooksMapper.GetQuickBooksCustomerIdAsync(customer);
                
                if (!string.IsNullOrEmpty(quickBooksCustomerId))
                {
                    // Update existing customer
                    qbCustomer.Id = quickBooksCustomerId;
                    qbCustomer.SyncToken = await GetCustomerSyncTokenAsync(quickBooksCustomerId);
                    
                    var response = await _quickBooksService.PostAsync<QuickBooksCustomer, QuickBooksCustomerResponse>("customer", qbCustomer);
                    return response.Customer;
                }
                else
                {
                    // Try to find by email first
                    if (!string.IsNullOrEmpty(customer.Email))
                    {
                        var existingCustomer = await FindCustomerByEmailAsync(customer.Email);
                        if (existingCustomer != null)
                        {
                            // Store the ID for future use
                            await _quickBooksMapper.StoreQuickBooksCustomerIdAsync(customer, existingCustomer.Id);
                            
                            // Update the existing customer
                            qbCustomer.Id = existingCustomer.Id;
                            qbCustomer.SyncToken = existingCustomer.SyncToken;
                            
                            var updateResponse = await _quickBooksService.PostAsync<QuickBooksCustomer, QuickBooksCustomerResponse>("customer", qbCustomer);
                            return updateResponse.Customer;
                        }
                    }
                    
                    // Create new customer
                    var response = await _quickBooksService.PostAsync<QuickBooksCustomer, QuickBooksCustomerResponse>("customer", qbCustomer);
                    
                    // Store QuickBooks customer ID in GrandNode
                    await _quickBooksMapper.StoreQuickBooksCustomerIdAsync(customer, response.Customer.Id);
                    
                    return response.Customer;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating customer in QuickBooks. Customer ID: {CustomerId}", customer.Id);
                throw;
            }
        }

        /// <summary>
        /// Find a customer in QuickBooks by email
        /// </summary>
        public async Task<QuickBooksCustomer> FindCustomerByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                // Query customers by email
                var encodedEmail = Uri.EscapeDataString(email);
                var query = $"query?query=select * from Customer where PrimaryEmailAddr.Address = '{encodedEmail}' MAXRESULTS 1";
                
                var response = await _quickBooksService.GetAsync<QuickBooksCustomerQueryResponse>(query);
                
                if (response.QueryResponse != null && 
                    response.QueryResponse.Customers != null && 
                    response.QueryResponse.Customers.Any())
                {
                    return response.QueryResponse.Customers.First();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding customer in QuickBooks by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Find a customer in QuickBooks by ID
        /// </summary>
        public async Task<QuickBooksCustomer> FindCustomerByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                var response = await _quickBooksService.GetAsync<QuickBooksCustomerResponse>($"customer/{id}");
                return response.Customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding customer in QuickBooks by ID: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Sync all customers from GrandNode to QuickBooks
        /// </summary>
        public async Task<int> SyncCustomersAsync(DateTime? startDate = null)
        {
            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                var settings = _quickBooksService.GetSettings();
                startDate ??= settings.LastCustomerSyncDate;
                
                // Get customers updated since last sync
                // Note: Adjust this call based on the actual API of your ICustomerService implementation
                var customers = await _customerService.GetAllCustomers(
                    pageIndex: 0,
                    pageSize: int.MaxValue);

                int syncCount = 0;
                
                foreach (var customer in customers)
                {
                    // Skip guests and system accounts
                    if (customer.IsSystemAccount() || customer.Groups.Contains(SystemCustomerGroupNames.Guests))
                        continue;
                        
                    try
                    {
                        await CreateOrUpdateCustomerAsync(customer);
                        syncCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing customer to QuickBooks. Customer ID: {CustomerId}", customer.Id);
                    }
                }
                
                // Update last sync date
                settings.LastCustomerSyncDate = DateTime.UtcNow;
                await _quickBooksService.SaveSettingsAsync(settings);
                
                return syncCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing customers to QuickBooks");
                throw;
            }
        }

        /// <summary>
        /// Get customer sync token for updates
        /// </summary>
        private async Task<string> GetCustomerSyncTokenAsync(string customerId)
        {
            try
            {
                var customer = await FindCustomerByIdAsync(customerId);
                return customer?.SyncToken ?? "0";
            }
            catch
            {
                return "0";
            }
        }
    }
}