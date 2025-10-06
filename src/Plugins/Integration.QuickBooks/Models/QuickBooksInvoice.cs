using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Integration.QuickBooks.Models
{
    public class QuickBooksInvoice
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("SyncToken")]
        public string SyncToken { get; set; }

        [JsonProperty("DocNumber")]
        public string DocNumber { get; set; }

        [JsonProperty("TxnDate")]
        public DateTime TxnDate { get; set; }

        [JsonProperty("DueDate")]
        public DateTime DueDate { get; set; }

        [JsonProperty("CustomerRef")]
        public ReferenceType CustomerRef { get; set; }

        [JsonProperty("TotalAmt")]
        public decimal TotalAmt { get; set; }

        [JsonProperty("Line")]
        public List<InvoiceLine> Line { get; set; } = new List<InvoiceLine>();

        [JsonProperty("TxnTaxDetail")]
        public TxnTaxDetail TxnTaxDetail { get; set; }

        [JsonProperty("CustomerMemo")]
        public MemoRef CustomerMemo { get; set; }

        [JsonProperty("BillEmail")]
        public EmailAddress BillEmail { get; set; }

        [JsonProperty("ShipAddr")]
        public PhysicalAddress ShipAddr { get; set; }

        [JsonProperty("BillAddr")]
        public PhysicalAddress BillAddr { get; set; }
    }

    public class ReferenceType
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class InvoiceLine
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("LineNum")]
        public int LineNum { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("DetailType")]
        public string DetailType { get; set; }

        [JsonProperty("SalesItemLineDetail")]
        public SalesItemLineDetail SalesItemLineDetail { get; set; }

        [JsonProperty("DiscountLineDetail")]
        public DiscountLineDetail DiscountLineDetail { get; set; }
    }

    public class SalesItemLineDetail
    {
        [JsonProperty("ItemRef")]
        public ReferenceType ItemRef { get; set; }

        [JsonProperty("Qty")]
        public decimal Qty { get; set; }

        [JsonProperty("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonProperty("TaxCodeRef")]
        public ReferenceType TaxCodeRef { get; set; }

        [JsonProperty("TaxInclusiveAmt")]
        public decimal TaxInclusiveAmt { get; set; }
    }

    public class DiscountLineDetail
    {
        [JsonProperty("PercentBased")]
        public bool PercentBased { get; set; }

        [JsonProperty("DiscountPercent")]
        public decimal DiscountPercent { get; set; }
    }

    public class TxnTaxDetail
    {
        [JsonProperty("TotalTax")]
        public decimal TotalTax { get; set; }

        [JsonProperty("TaxLine")]
        public List<TaxLine> TaxLine { get; set; }
    }

    public class TaxLine
    {
        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("DetailType")]
        public string DetailType { get; set; }

        [JsonProperty("TaxLineDetail")]
        public TaxLineDetail TaxLineDetail { get; set; }
    }

    public class TaxLineDetail
    {
        [JsonProperty("TaxRateRef")]
        public ReferenceType TaxRateRef { get; set; }

        [JsonProperty("PercentBased")]
        public bool PercentBased { get; set; }

        [JsonProperty("TaxPercent")]
        public decimal TaxPercent { get; set; }

        [JsonProperty("NetAmountTaxable")]
        public decimal NetAmountTaxable { get; set; }
    }

    public class MemoRef
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class QuickBooksInvoiceResponse
    {
        [JsonProperty("Invoice")]
        public QuickBooksInvoice Invoice { get; set; }
    }

    public class QuickBooksInvoiceQueryResponse
    {
        [JsonProperty("QueryResponse")]
        public InvoiceQueryResponse QueryResponse { get; set; }
    }

    public class InvoiceQueryResponse
    {
        [JsonProperty("Invoice")]
        public List<QuickBooksInvoice> Invoices { get; set; }

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }

        [JsonProperty("startPosition")]
        public int StartPosition { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }
}