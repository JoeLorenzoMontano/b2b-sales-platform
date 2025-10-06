using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Integration.QuickBooks.Models
{
    public class QuickBooksCustomer
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("SyncToken")]
        public string SyncToken { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("GivenName")]
        public string GivenName { get; set; }

        [JsonProperty("MiddleName")]
        public string MiddleName { get; set; }

        [JsonProperty("FamilyName")]
        public string FamilyName { get; set; }

        [JsonProperty("CompanyName")]
        public string CompanyName { get; set; }

        [JsonProperty("PrimaryEmailAddr")]
        public EmailAddress PrimaryEmailAddr { get; set; }

        [JsonProperty("PrimaryPhone")]
        public PhoneNumber PrimaryPhone { get; set; }

        [JsonProperty("Mobile")]
        public PhoneNumber Mobile { get; set; }

        [JsonProperty("BillAddr")]
        public PhysicalAddress BillAddr { get; set; }

        [JsonProperty("ShipAddr")]
        public PhysicalAddress ShipAddr { get; set; }

        [JsonProperty("Active")]
        public bool Active { get; set; } = true;

        [JsonProperty("Notes")]
        public string Notes { get; set; }

        [JsonProperty("Job")]
        public bool? Job { get; set; }

        [JsonProperty("BillWithParent")]
        public bool? BillWithParent { get; set; }

        [JsonProperty("CustomerTypeRef")]
        public ReferenceType CustomerTypeRef { get; set; }

        [JsonProperty("PreferredDeliveryMethod")]
        public string PreferredDeliveryMethod { get; set; } = "Email";

        [JsonProperty("Taxable")]
        public bool? Taxable { get; set; } = true;

        [JsonProperty("PrintOnCheckName")]
        public string PrintOnCheckName { get; set; }

        [JsonProperty("Balance")]
        public decimal? Balance { get; set; }

        [JsonProperty("BalanceWithJobs")]
        public decimal? BalanceWithJobs { get; set; }

        [JsonProperty("CurrencyRef")]
        public ReferenceType CurrencyRef { get; set; }

        [JsonProperty("TaxExemptionReasonId")]
        public int? TaxExemptionReasonId { get; set; }
    }

    public class EmailAddress
    {
        [JsonProperty("Address")]
        public string Address { get; set; }
    }

    public class PhoneNumber
    {
        [JsonProperty("FreeFormNumber")]
        public string FreeFormNumber { get; set; }
    }

    public class PhysicalAddress
    {
        [JsonProperty("Line1")]
        public string Line1 { get; set; }

        [JsonProperty("Line2")]
        public string Line2 { get; set; }

        [JsonProperty("City")]
        public string City { get; set; }

        [JsonProperty("CountrySubDivisionCode")]
        public string CountrySubDivisionCode { get; set; }

        [JsonProperty("PostalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("Country")]
        public string Country { get; set; }
    }

    public class QuickBooksCustomerResponse
    {
        [JsonProperty("Customer")]
        public QuickBooksCustomer Customer { get; set; }
    }

    public class QuickBooksCustomerQueryResponse
    {
        [JsonProperty("QueryResponse")]
        public QueryResponse QueryResponse { get; set; }
    }

    public class QueryResponse
    {
        [JsonProperty("Customer")]
        public List<QuickBooksCustomer> Customers { get; set; }

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }

        [JsonProperty("startPosition")]
        public int StartPosition { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

    // ReferenceType is already defined in QuickBooksInvoice.cs
}