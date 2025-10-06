using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Integration.QuickBooks.Models;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// Interface for mapping between GrandNode and QuickBooks entities
    /// </summary>
    public interface IQuickBooksMapper
    {
        /// <summary>
        /// Map a GrandNode customer to a QuickBooks customer
        /// </summary>
        /// <param name="customer">GrandNode customer</param>
        /// <returns>QuickBooks customer</returns>
        Task<QuickBooksCustomer> ToQuickBooksCustomerAsync(Customer customer);

        /// <summary>
        /// Map a GrandNode order to a QuickBooks invoice
        /// </summary>
        /// <param name="order">GrandNode order</param>
        /// <returns>QuickBooks invoice</returns>
        Task<QuickBooksInvoice> ToQuickBooksInvoiceAsync(Order order);

        /// <summary>
        /// Get QuickBooks customer ID for GrandNode customer
        /// </summary>
        /// <param name="customer">GrandNode customer</param>
        /// <returns>QuickBooks customer ID if exists, null otherwise</returns>
        Task<string> GetQuickBooksCustomerIdAsync(Customer customer);

        /// <summary>
        /// Store QuickBooks customer ID for GrandNode customer
        /// </summary>
        /// <param name="customer">GrandNode customer</param>
        /// <param name="quickBooksCustomerId">QuickBooks customer ID</param>
        /// <returns>Task</returns>
        Task StoreQuickBooksCustomerIdAsync(Customer customer, string quickBooksCustomerId);

        /// <summary>
        /// Get QuickBooks invoice ID for GrandNode order
        /// </summary>
        /// <param name="order">GrandNode order</param>
        /// <returns>QuickBooks invoice ID if exists, null otherwise</returns>
        Task<string> GetQuickBooksInvoiceIdAsync(Order order);

        /// <summary>
        /// Store QuickBooks invoice ID for GrandNode order
        /// </summary>
        /// <param name="order">GrandNode order</param>
        /// <param name="quickBooksInvoiceId">QuickBooks invoice ID</param>
        /// <returns>Task</returns>
        Task StoreQuickBooksInvoiceIdAsync(Order order, string quickBooksInvoiceId);
    }
}