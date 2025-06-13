using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Integration.QuickBooks.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// Implementation of QuickBooks mapper
    /// </summary>
    public class QuickBooksMapper : IQuickBooksMapper
    {
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly ILogger<QuickBooksMapper> _logger;

        // Field names for storing QuickBooks IDs in GrandNode entities
        private const string QB_CUSTOMER_ID_FIELD = "QuickBooksCustomerId";
        private const string QB_INVOICE_ID_FIELD = "QuickBooksInvoiceId";

        public QuickBooksMapper(
            ICustomerService customerService,
            IOrderService orderService,
            ILogger<QuickBooksMapper> logger)
        {
            _customerService = customerService;
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Map a GrandNode customer to a QuickBooks customer
        /// </summary>
        public async Task<QuickBooksCustomer> ToQuickBooksCustomerAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            // Extract customer attributes
            string firstName = string.Empty;
            string lastName = string.Empty;
            string company = string.Empty;
            string phone = string.Empty;

            // Look for attributes in CustomAttributes
            foreach (var attr in customer.Attributes)
            {
                if (attr.Key == "FirstName")
                    firstName = attr.Value?.ToString() ?? string.Empty;
                else if (attr.Key == "LastName")
                    lastName = attr.Value?.ToString() ?? string.Empty;
                else if (attr.Key == "Company")
                    company = attr.Value?.ToString() ?? string.Empty;
                else if (attr.Key == "Phone")
                    phone = attr.Value?.ToString() ?? string.Empty;
            }

            var qbCustomer = new QuickBooksCustomer
            {
                GivenName = firstName,
                FamilyName = lastName,
                DisplayName = $"{firstName} {lastName}".Trim(),
                CompanyName = company,
                Active = true,
                PrintOnCheckName = $"{firstName} {lastName}".Trim(),
                Taxable = true,
                PreferredDeliveryMethod = "Email"
            };

            // If display name is empty, use username or email
            if (string.IsNullOrWhiteSpace(qbCustomer.DisplayName))
            {
                qbCustomer.DisplayName = !string.IsNullOrEmpty(customer.Username) 
                    ? customer.Username 
                    : (customer.Email ?? "Customer");
            }

            // Set email if available
            if (!string.IsNullOrEmpty(customer.Email))
            {
                qbCustomer.PrimaryEmailAddr = new EmailAddress { Address = customer.Email };
            }

            // Set phone if available
            if (!string.IsNullOrEmpty(phone))
            {
                qbCustomer.PrimaryPhone = new PhoneNumber { FreeFormNumber = phone };
            }

            // Set billing address if available
            var billingAddress = customer.Addresses?.FirstOrDefault(a => a.AddressType == AddressType.Billing);
            if (billingAddress != null)
            {
                qbCustomer.BillAddr = new PhysicalAddress
                {
                    Line1 = billingAddress.Address1 ?? string.Empty,
                    Line2 = billingAddress.Address2 ?? string.Empty,
                    City = billingAddress.City ?? string.Empty,
                    CountrySubDivisionCode = billingAddress.StateProvinceId ?? string.Empty,
                    PostalCode = billingAddress.ZipPostalCode ?? string.Empty,
                    Country = billingAddress.CountryId ?? string.Empty
                };
            }

            // Set shipping address if available
            var shippingAddress = customer.Addresses?.FirstOrDefault(a => a.AddressType == AddressType.Shipping);
            if (shippingAddress != null)
            {
                qbCustomer.ShipAddr = new PhysicalAddress
                {
                    Line1 = shippingAddress.Address1 ?? string.Empty,
                    Line2 = shippingAddress.Address2 ?? string.Empty,
                    City = shippingAddress.City ?? string.Empty,
                    CountrySubDivisionCode = shippingAddress.StateProvinceId ?? string.Empty,
                    PostalCode = shippingAddress.ZipPostalCode ?? string.Empty,
                    Country = shippingAddress.CountryId ?? string.Empty
                };
            }

            // Add the customer ID if it exists
            var quickBooksCustomerId = await GetQuickBooksCustomerIdAsync(customer);
            if (!string.IsNullOrEmpty(quickBooksCustomerId))
            {
                qbCustomer.Id = quickBooksCustomerId;
            }

            return qbCustomer;
        }

        /// <summary>
        /// Map a GrandNode order to a QuickBooks invoice
        /// </summary>
        public async Task<QuickBooksInvoice> ToQuickBooksInvoiceAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // Get the customer from GrandNode
            var customer = await _customerService.GetCustomerById(order.CustomerId);
            if (customer == null)
            {
                throw new Exception($"Customer with ID {order.CustomerId} not found");
            }

            // Get the customer ID from QuickBooks
            var quickBooksCustomerId = await GetQuickBooksCustomerIdAsync(customer);
            if (string.IsNullOrEmpty(quickBooksCustomerId))
            {
                throw new Exception($"Customer {order.CustomerId} does not have a QuickBooks ID. Please sync customers first.");
            }

            var qbInvoice = new QuickBooksInvoice
            {
                DocNumber = order.OrderNumber.ToString(),
                TxnDate = order.CreatedOnUtc.Date,
                DueDate = order.CreatedOnUtc.Date.AddDays(30), // Set due date to 30 days from order date
                CustomerRef = new ReferenceType { Value = quickBooksCustomerId },
                Line = new System.Collections.Generic.List<InvoiceLine>(),
                BillEmail = new EmailAddress { Address = customer.Email }
            };

            // Set billing address
            if (order.BillingAddress != null)
            {
                qbInvoice.BillAddr = new PhysicalAddress
                {
                    Line1 = order.BillingAddress.Address1 ?? string.Empty,
                    Line2 = order.BillingAddress.Address2 ?? string.Empty,
                    City = order.BillingAddress.City ?? string.Empty,
                    CountrySubDivisionCode = order.BillingAddress.StateProvinceId ?? string.Empty,
                    PostalCode = order.BillingAddress.ZipPostalCode ?? string.Empty,
                    Country = order.BillingAddress.CountryId ?? string.Empty
                };
            }

            // Set shipping address
            if (order.ShippingAddress != null)
            {
                qbInvoice.ShipAddr = new PhysicalAddress
                {
                    Line1 = order.ShippingAddress.Address1 ?? string.Empty,
                    Line2 = order.ShippingAddress.Address2 ?? string.Empty,
                    City = order.ShippingAddress.City ?? string.Empty,
                    CountrySubDivisionCode = order.ShippingAddress.StateProvinceId ?? string.Empty,
                    PostalCode = order.ShippingAddress.ZipPostalCode ?? string.Empty,
                    Country = order.ShippingAddress.CountryId ?? string.Empty
                };
            }

            // Add line items
            int lineNumber = 1;
            foreach (var item in order.OrderItems)
            {
                // Use whatever description is available
                string description = item.AttributeDescription;
                if (string.IsNullOrEmpty(description))
                {
                    description = $"Product {item.ProductId}";
                    if (!string.IsNullOrEmpty(item.Sku))
                        description += $" (SKU: {item.Sku})";
                }

                var line = new InvoiceLine
                {
                    LineNum = lineNumber++,
                    Description = description,
                    Amount = (decimal)item.UnitPriceInclTax * (decimal)item.Quantity,
                    DetailType = "SalesItemLineDetail",
                    SalesItemLineDetail = new SalesItemLineDetail
                    {
                        Qty = (decimal)item.Quantity,
                        UnitPrice = (decimal)item.UnitPriceInclTax,
                        TaxInclusiveAmt = (decimal)item.UnitPriceInclTax * (decimal)item.Quantity
                    }
                };
                
                qbInvoice.Line.Add(line);
            }

            // Add shipping if applicable
            if (order.OrderShippingInclTax > 0)
            {
                var shippingLine = new InvoiceLine
                {
                    LineNum = lineNumber++,
                    Description = "Shipping",
                    Amount = (decimal)order.OrderShippingInclTax,
                    DetailType = "SalesItemLineDetail",
                    SalesItemLineDetail = new SalesItemLineDetail
                    {
                        Qty = 1,
                        UnitPrice = (decimal)order.OrderShippingInclTax,
                        TaxInclusiveAmt = (decimal)order.OrderShippingInclTax
                    }
                };
                
                qbInvoice.Line.Add(shippingLine);
            }

            // Add payment method additional fee if applicable
            if (order.PaymentMethodAdditionalFeeInclTax > 0)
            {
                var feeLine = new InvoiceLine
                {
                    LineNum = lineNumber++,
                    Description = "Payment Fee",
                    Amount = (decimal)order.PaymentMethodAdditionalFeeInclTax,
                    DetailType = "SalesItemLineDetail",
                    SalesItemLineDetail = new SalesItemLineDetail
                    {
                        Qty = 1,
                        UnitPrice = (decimal)order.PaymentMethodAdditionalFeeInclTax,
                        TaxInclusiveAmt = (decimal)order.PaymentMethodAdditionalFeeInclTax
                    }
                };
                
                qbInvoice.Line.Add(feeLine);
            }

            // Add discount if applicable
            if (order.OrderDiscount > 0)
            {
                var discountLine = new InvoiceLine
                {
                    LineNum = lineNumber++,
                    Description = "Discount",
                    Amount = -1 * (decimal)order.OrderDiscount, // Negative amount for discount
                    DetailType = "DiscountLineDetail",
                    DiscountLineDetail = new DiscountLineDetail
                    {
                        DiscountPercent = 0,
                        PercentBased = false
                    }
                };
                
                qbInvoice.Line.Add(discountLine);
            }

            // Add the invoice ID if it exists
            var quickBooksInvoiceId = await GetQuickBooksInvoiceIdAsync(order);
            if (!string.IsNullOrEmpty(quickBooksInvoiceId))
            {
                qbInvoice.Id = quickBooksInvoiceId;
            }

            return qbInvoice;
        }

        /// <summary>
        /// Get QuickBooks customer ID for GrandNode customer
        /// </summary>
        public async Task<string> GetQuickBooksCustomerIdAsync(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            // Check if QuickBooks ID is stored in custom attributes
            var quickBooksCustomerId = customer.Attributes
                .FirstOrDefault(a => a.Key == QB_CUSTOMER_ID_FIELD)?.Value as string;
            
            return quickBooksCustomerId;
        }

        /// <summary>
        /// Store QuickBooks customer ID for GrandNode customer
        /// </summary>
        public async Task StoreQuickBooksCustomerIdAsync(Customer customer, string quickBooksCustomerId)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (string.IsNullOrEmpty(quickBooksCustomerId))
                throw new ArgumentNullException(nameof(quickBooksCustomerId));

            // Remove existing attribute if any
            var existingAttr = customer.Attributes.FirstOrDefault(a => a.Key == QB_CUSTOMER_ID_FIELD);
            if (existingAttr != null)
                customer.Attributes.Remove(existingAttr);

            // Add new attribute
            customer.Attributes.Add(new CustomAttribute 
            { 
                Key = QB_CUSTOMER_ID_FIELD, 
                Value = quickBooksCustomerId 
            });
            
            // Save customer changes
            await _customerService.UpdateCustomer(customer);
        }

        /// <summary>
        /// Get QuickBooks invoice ID for GrandNode order
        /// </summary>
        public async Task<string> GetQuickBooksInvoiceIdAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // GrandNode stores order attributes in CheckoutAttributes
            var quickBooksInvoiceId = order.CheckoutAttributes
                .FirstOrDefault(a => a.Key == QB_INVOICE_ID_FIELD)?.Value as string;
            
            return quickBooksInvoiceId;
        }

        /// <summary>
        /// Store QuickBooks invoice ID for GrandNode order
        /// </summary>
        public async Task StoreQuickBooksInvoiceIdAsync(Order order, string quickBooksInvoiceId)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrEmpty(quickBooksInvoiceId))
                throw new ArgumentNullException(nameof(quickBooksInvoiceId));

            // Remove existing attribute if any
            var existingAttr = order.CheckoutAttributes.FirstOrDefault(a => a.Key == QB_INVOICE_ID_FIELD);
            if (existingAttr != null)
                order.CheckoutAttributes.Remove(existingAttr);

            // Add new attribute
            order.CheckoutAttributes.Add(new CustomAttribute 
            { 
                Key = QB_INVOICE_ID_FIELD, 
                Value = quickBooksInvoiceId 
            });
            
            // Save order changes
            await _orderService.UpdateOrder(order);
        }
    }
}