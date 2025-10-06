using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Integration.QuickBooks.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// QuickBooks invoice service implementation
    /// </summary>
    public class QuickBooksInvoiceService : IQuickBooksInvoiceService
    {
        private readonly IQuickBooksService _quickBooksService;
        private readonly IQuickBooksMapper _quickBooksMapper;
        private readonly IOrderService _orderService;
        private readonly ILogger<QuickBooksInvoiceService> _logger;

        public QuickBooksInvoiceService(
            IQuickBooksService quickBooksService,
            IQuickBooksMapper quickBooksMapper,
            IOrderService orderService,
            ILogger<QuickBooksInvoiceService> logger)
        {
            _quickBooksService = quickBooksService;
            _quickBooksMapper = quickBooksMapper;
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Create or update an invoice in QuickBooks
        /// </summary>
        public async Task<QuickBooksInvoice> CreateOrUpdateInvoiceAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                // Convert GrandNode order to QuickBooks invoice
                var qbInvoice = await _quickBooksMapper.ToQuickBooksInvoiceAsync(order);
                
                // Check if invoice already exists in QuickBooks
                var quickBooksInvoiceId = await _quickBooksMapper.GetQuickBooksInvoiceIdAsync(order);
                
                if (!string.IsNullOrEmpty(quickBooksInvoiceId))
                {
                    // Update existing invoice
                    qbInvoice.Id = quickBooksInvoiceId;
                    qbInvoice.SyncToken = await GetInvoiceSyncTokenAsync(quickBooksInvoiceId);
                    
                    var response = await _quickBooksService.PostAsync<QuickBooksInvoice, QuickBooksInvoiceResponse>("invoice", qbInvoice);
                    return response.Invoice;
                }
                else
                {
                    // Try to find by order number first
                    var existingInvoice = await FindInvoiceByOrderNumberAsync(order.OrderNumber.ToString());
                    if (existingInvoice != null)
                    {
                        // Store the ID for future use
                        await _quickBooksMapper.StoreQuickBooksInvoiceIdAsync(order, existingInvoice.Id);
                        
                        // Update the existing invoice
                        qbInvoice.Id = existingInvoice.Id;
                        qbInvoice.SyncToken = existingInvoice.SyncToken;
                        
                        var updateResponse = await _quickBooksService.PostAsync<QuickBooksInvoice, QuickBooksInvoiceResponse>("invoice", qbInvoice);
                        return updateResponse.Invoice;
                    }
                    
                    // Create new invoice
                    var response = await _quickBooksService.PostAsync<QuickBooksInvoice, QuickBooksInvoiceResponse>("invoice", qbInvoice);
                    
                    // Store QuickBooks invoice ID in GrandNode
                    await _quickBooksMapper.StoreQuickBooksInvoiceIdAsync(order, response.Invoice.Id);
                    
                    return response.Invoice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating invoice in QuickBooks. Order ID: {OrderId}", order.Id);
                throw;
            }
        }

        /// <summary>
        /// Find an invoice in QuickBooks by GrandNode order number
        /// </summary>
        public async Task<QuickBooksInvoice> FindInvoiceByOrderNumberAsync(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
                return null;

            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                // Query invoices by DocNumber (order number)
                var query = $"query?query=select * from Invoice where DocNumber = '{orderNumber}' MAXRESULTS 1";
                
                var response = await _quickBooksService.GetAsync<QuickBooksInvoiceQueryResponse>(query);
                
                if (response.QueryResponse != null && 
                    response.QueryResponse.Invoices != null && 
                    response.QueryResponse.Invoices.Any())
                {
                    return response.QueryResponse.Invoices.First();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding invoice in QuickBooks by order number: {OrderNumber}", orderNumber);
                throw;
            }
        }

        /// <summary>
        /// Find an invoice in QuickBooks by ID
        /// </summary>
        public async Task<QuickBooksInvoice> FindInvoiceByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                var response = await _quickBooksService.GetAsync<QuickBooksInvoiceResponse>($"invoice/{id}");
                return response.Invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding invoice in QuickBooks by ID: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Sync orders from GrandNode to QuickBooks
        /// </summary>
        public async Task<int> SyncInvoicesAsync(DateTime? startDate = null)
        {
            if (!_quickBooksService.IsConnected())
                throw new Exception("QuickBooks is not connected");

            try
            {
                var settings = _quickBooksService.GetSettings();
                startDate ??= settings.LastInvoiceSyncDate;
                
                // Get orders created or updated since last sync
                // Note: Adjust this call based on the actual API of your IOrderService implementation
                var orders = await _orderService.SearchOrders(
                    createdFromUtc: startDate,
                    pageIndex: 0,
                    pageSize: int.MaxValue);

                int syncCount = 0;
                
                foreach (var order in orders)
                {
                    // Only sync completed, processing, or pending orders
                    if (order.OrderStatusId != (int)OrderStatusSystem.Complete &&
                        order.OrderStatusId != (int)OrderStatusSystem.Processing &&
                        order.OrderStatusId != (int)OrderStatusSystem.Pending)
                    {
                        continue;
                    }
                    
                    try
                    {
                        await CreateOrUpdateInvoiceAsync(order);
                        syncCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing order to QuickBooks. Order ID: {OrderId}", order.Id);
                    }
                }
                
                // Update last sync date
                settings.LastInvoiceSyncDate = DateTime.UtcNow;
                await _quickBooksService.SaveSettingsAsync(settings);
                
                return syncCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing invoices to QuickBooks");
                throw;
            }
        }

        /// <summary>
        /// Get invoice sync token for updates
        /// </summary>
        private async Task<string> GetInvoiceSyncTokenAsync(string invoiceId)
        {
            try
            {
                var invoice = await FindInvoiceByIdAsync(invoiceId);
                return invoice?.SyncToken ?? "0";
            }
            catch
            {
                return "0";
            }
        }
    }
}