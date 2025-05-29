using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;

namespace Widgets.JolomoCustom
{
    public class OrderNoteFixHandler : INotificationHandler<EntityInsertedEvent<OrderNote>>
    {
        /// <summary>
        /// Handles the event when an OrderNote is inserted
        /// This is a diagnostic handler that writes to the console when an order note is inserted
        /// to help troubleshoot why customer-created notes aren't showing up
        /// </summary>
        public Task Handle(EntityInsertedEvent<OrderNote> notification, CancellationToken cancellationToken)
        {
            var orderNote = notification.Entity;
            
            // Log information about the inserted order note
            Console.WriteLine($"OrderNote inserted - ID: {orderNote.Id}, OrderId: {orderNote.OrderId}");
            Console.WriteLine($"CreatedByCustomer: {orderNote.CreatedByCustomer}, DisplayToCustomer: {orderNote.DisplayToCustomer}");
            Console.WriteLine($"Note Content: {orderNote.Note}");
            Console.WriteLine($"CreatedOnUtc: {orderNote.CreatedOnUtc}");
            
            return Task.CompletedTask;
        }
    }
}