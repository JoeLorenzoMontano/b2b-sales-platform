using Grand.Domain.Orders;
using Integration.QuickBooks.Models;
using System;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// QuickBooks invoice service interface
    /// </summary>
    public interface IQuickBooksInvoiceService
    {
        /// <summary>
        /// Create or update an invoice in QuickBooks
        /// </summary>
        /// <param name="order">GrandNode order</param>
        /// <returns>QuickBooks Invoice entity</returns>
        Task<QuickBooksInvoice> CreateOrUpdateInvoiceAsync(Order order);

        /// <summary>
        /// Find an invoice in QuickBooks by GrandNode order number
        /// </summary>
        /// <param name="orderNumber">Order number</param>
        /// <returns>QuickBooks Invoice entity if found, null otherwise</returns>
        Task<QuickBooksInvoice> FindInvoiceByOrderNumberAsync(string orderNumber);

        /// <summary>
        /// Find an invoice in QuickBooks by ID
        /// </summary>
        /// <param name="id">QuickBooks invoice ID</param>
        /// <returns>QuickBooks Invoice entity if found, null otherwise</returns>
        Task<QuickBooksInvoice> FindInvoiceByIdAsync(string id);

        /// <summary>
        /// Sync orders from GrandNode to QuickBooks
        /// </summary>
        /// <param name="startDate">Start date for sync</param>
        /// <returns>Number of invoices synced</returns>
        Task<int> SyncInvoicesAsync(DateTime? startDate = null);
    }
}