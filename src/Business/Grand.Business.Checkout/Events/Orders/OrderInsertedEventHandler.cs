using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
/// Event handler that automatically creates a pending payment transaction when a new order is inserted
/// </summary>
public class OrderInsertedEventHandler : INotificationHandler<EntityInserted<Order>>
{
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly ILogger<OrderInsertedEventHandler> _logger;

    public OrderInsertedEventHandler(
        IPaymentTransactionService paymentTransactionService,
        ILogger<OrderInsertedEventHandler> logger)
    {
        _paymentTransactionService = paymentTransactionService;
        _logger = logger;
    }

    public async Task Handle(EntityInserted<Order> notification, CancellationToken cancellationToken)
    {
        try
        {
            var order = notification.Entity;

            _logger.LogInformation($"OrderInsertedEventHandler triggered for Order ID: {order.Id}, OrderGuid: {order.OrderGuid}");

            // Check if payment transaction already exists for this order
            var existingTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
            if (existingTransaction != null)
            {
                _logger.LogInformation($"Payment transaction already exists for Order {order.Id}, skipping auto-creation");
                return; // Payment transaction already exists, skip
            }

            _logger.LogInformation($"Creating payment transaction for Order {order.Id}. OrderTotal: {order.OrderTotal}, Currency: {order.CustomerCurrencyCode}");

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

            _logger.LogInformation($"Successfully created payment transaction for Order {order.Id}, Transaction ID: {paymentTransaction.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error auto-creating payment transaction for Order {notification.Entity?.Id}: {ex.Message}");
            // Don't rethrow - we don't want to block order creation if payment transaction fails
        }
    }
}
