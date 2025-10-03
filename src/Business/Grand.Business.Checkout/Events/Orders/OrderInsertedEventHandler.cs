using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
/// Event handler that automatically creates a pending payment transaction when a new order is inserted
/// </summary>
public class OrderInsertedEventHandler : INotificationHandler<EntityInserted<Order>>
{
    private readonly IPaymentTransactionService _paymentTransactionService;

    public OrderInsertedEventHandler(IPaymentTransactionService paymentTransactionService)
    {
        _paymentTransactionService = paymentTransactionService;
    }

    public async Task Handle(EntityInserted<Order> notification, CancellationToken cancellationToken)
    {
        var order = notification.Entity;

        // Check if payment transaction already exists for this order
        var existingTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (existingTransaction != null)
            return; // Payment transaction already exists, skip

        // Create new payment transaction with Pending status
        var paymentTransaction = new PaymentTransaction
        {
            OrderCode = order.Code,
            OrderGuid = order.OrderGuid,
            CustomerEmail = order.CustomerEmail,
            CustomerId = order.CustomerId,
            CurrencyCode = order.CustomerCurrencyCode,
            CurrencyRate = order.CurrencyRate,
            TransactionAmount = order.OrderTotal,
            PaidAmount = 0,
            RefundedAmount = 0,
            PaymentMethodSystemName = order.PaymentMethodSystemName ?? "Manual",
            TransactionStatus = TransactionStatus.Pending,
            StoreId = order.StoreId,
            IPAddress = string.Empty,
            CreatedOnUtc = DateTime.UtcNow
        };

        await _paymentTransactionService.InsertPaymentTransaction(paymentTransaction);
    }
}
