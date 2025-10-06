using Grand.Domain.Customers;
using Integration.QuickBooks.Models;
using System;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// QuickBooks customer service interface
    /// </summary>
    public interface IQuickBooksCustomerService
    {
        /// <summary>
        /// Create or update a customer in QuickBooks
        /// </summary>
        /// <param name="customer">GrandNode customer</param>
        /// <returns>QuickBooks Customer entity</returns>
        Task<QuickBooksCustomer> CreateOrUpdateCustomerAsync(Customer customer);

        /// <summary>
        /// Find a customer in QuickBooks by email
        /// </summary>
        /// <param name="email">Customer email</param>
        /// <returns>QuickBooks Customer entity if found, null otherwise</returns>
        Task<QuickBooksCustomer> FindCustomerByEmailAsync(string email);

        /// <summary>
        /// Find a customer in QuickBooks by ID
        /// </summary>
        /// <param name="id">QuickBooks customer ID</param>
        /// <returns>QuickBooks Customer entity if found, null otherwise</returns>
        Task<QuickBooksCustomer> FindCustomerByIdAsync(string id);

        /// <summary>
        /// Sync all customers from GrandNode to QuickBooks
        /// </summary>
        /// <param name="startDate">Optional start date for sync</param>
        /// <returns>Number of customers synced</returns>
        Task<int> SyncCustomersAsync(DateTime? startDate = null);
    }
}