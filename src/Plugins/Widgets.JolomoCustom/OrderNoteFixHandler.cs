using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Widgets.JolomoCustom
{
    public class OrderNoteFixHandler : INotificationHandler<EntityInsertedEvent<OrderNote>>
    {
        private readonly ILogger<OrderNoteFixHandler> _logger;

        public OrderNoteFixHandler(ILogger<OrderNoteFixHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles the event when an OrderNote is inserted
        /// This is a diagnostic handler that writes to the console when an order note is inserted
        /// to help troubleshoot why customer-created notes aren't showing up
        /// </summary>
        public Task Handle(EntityInsertedEvent<OrderNote> notification, CancellationToken cancellationToken)
        {
            var orderNote = notification.Entity;
            
            // Log information about the inserted order note with increased logging level
            _logger.LogWarning($"ORDERNOTE_DEBUG: OrderNote inserted - ID: {orderNote.Id}, OrderId: {orderNote.OrderId}");
            _logger.LogWarning($"ORDERNOTE_DEBUG: CreatedByCustomer: {orderNote.CreatedByCustomer}, DisplayToCustomer: {orderNote.DisplayToCustomer}");
            _logger.LogWarning($"ORDERNOTE_DEBUG: Note Content: {orderNote.Note}");
            _logger.LogWarning($"ORDERNOTE_DEBUG: CreatedOnUtc: {orderNote.CreatedOnUtc}");
            
            // Also log to console for direct visibility
            Console.WriteLine($"ORDERNOTE_DEBUG: OrderNote inserted - ID: {orderNote.Id}, OrderId: {orderNote.OrderId}");
            Console.WriteLine($"ORDERNOTE_DEBUG: CreatedByCustomer: {orderNote.CreatedByCustomer}, DisplayToCustomer: {orderNote.DisplayToCustomer}");
            Console.WriteLine($"ORDERNOTE_DEBUG: Note Content: {orderNote.Note}");
            Console.WriteLine($"ORDERNOTE_DEBUG: CreatedOnUtc: {orderNote.CreatedOnUtc}");
            
            return Task.CompletedTask;
        }
    }
}