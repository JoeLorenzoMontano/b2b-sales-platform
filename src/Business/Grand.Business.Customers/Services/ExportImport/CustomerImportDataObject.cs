using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.Customers;
using Grand.Business.Customers.Dto;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Services.ExportImport
{
    public class CustomerImportDataObject : IImportDataObject<CustomerImportDto>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly ICountryService _countryService;
        private readonly CustomerSettings _customerSettings;

        public CustomerImportDataObject(
            ICustomerService customerService,
            ICustomerManagerService customerManagerService,
            ICountryService countryService,
            CustomerSettings customerSettings)
        {
            _customerService = customerService;
            _customerManagerService = customerManagerService;
            _countryService = countryService;
            _customerSettings = customerSettings;
        }

        public async Task Execute(IEnumerable<CustomerImportDto> customers)
        {
            foreach (var customerDto in customers)
            {
                // Skip if no email provided (required field)
                if (string.IsNullOrEmpty(customerDto.Email))
                    continue;

                // Check if customer already exists
                var existingCustomer = await _customerService.GetCustomerByEmail(customerDto.Email);
                
                if (existingCustomer != null)
                {
                    // Update existing customer
                    existingCustomer.Active = customerDto.Active;
                    
                    // Update user fields
                    await UpdateUserFields(existingCustomer, customerDto);
                    
                    // Update address if provided
                    if (!string.IsNullOrEmpty(customerDto.StreetAddress) || 
                        !string.IsNullOrEmpty(customerDto.City) ||
                        !string.IsNullOrEmpty(customerDto.Zip))
                    {
                        // Update primary address or add new one
                        await UpdateCustomerAddress(existingCustomer, customerDto);
                    }

                    await _customerService.UpdateCustomer(existingCustomer);
                }
                else
                {
                    // Create new customer
                    var customer = new Customer
                    {
                        CustomerGuid = Guid.NewGuid(),
                        Email = customerDto.Email,
                        Active = customerDto.Active,
                        CreatedOnUtc = DateTime.UtcNow,
                        LastActivityDateUtc = DateTime.UtcNow
                    };

                    // Add user fields
                    await UpdateUserFields(customer, customerDto);

                    // Add customer to Registered role
                    customer.Groups.Add(SystemCustomerGroupNames.Registered);
                    
                    await _customerService.InsertCustomer(customer);

                    // Add address if provided
                    if (!string.IsNullOrEmpty(customerDto.StreetAddress) || 
                        !string.IsNullOrEmpty(customerDto.City) ||
                        !string.IsNullOrEmpty(customerDto.Zip))
                    {
                        await AddCustomerAddress(customer, customerDto);
                    }

                    // Set password
                    var changePassRequest = new ChangePasswordRequest(
                        customerDto.Email,
                        _customerSettings.DefaultPasswordFormat,
                        customerDto.Password);
                    await _customerManagerService.ChangePassword(changePassRequest);
                }
            }
        }

        private Task UpdateUserFields(Customer customer, CustomerImportDto customerDto)
        {
            // Helper to update user fields
            void UpdateUserField(string key, string value)
            {
                if (string.IsNullOrEmpty(value))
                    return;
                
                var existingField = customer.UserFields.FirstOrDefault(x => x.Key == key);
                if (existingField != null)
                {
                    existingField.Value = value;
                }
                else
                {
                    customer.UserFields.Add(new UserField { Key = key, Value = value, StoreId = "" });
                }
            }
            
            // Set user fields for name, company, phone
            UpdateUserField(SystemCustomerFieldNames.FirstName, customerDto.FirstName);
            UpdateUserField(SystemCustomerFieldNames.LastName, customerDto.LastName);
            UpdateUserField(SystemCustomerFieldNames.Company, customerDto.Company);
            UpdateUserField(SystemCustomerFieldNames.Phone, customerDto.Phone);
            
            return Task.CompletedTask;
        }

        private async Task UpdateCustomerAddress(Customer customer, CustomerImportDto customerDto)
        {
            // Always add a new address for existing customers to preserve existing addresses
            await AddCustomerAddress(customer, customerDto);
        }

        private async Task AddCustomerAddress(Customer customer, CustomerImportDto customerDto)
        {
            var address = new Address
            {
                FirstName = customerDto.FirstName,
                LastName = customerDto.LastName,
                Email = customer.Email,
                Company = customerDto.Company,
                Address1 = customerDto.StreetAddress,
                City = customerDto.City,
                ZipPostalCode = customerDto.Zip,
                PhoneNumber = customerDto.Phone
            };

            // Set country and state
            if (!string.IsNullOrEmpty(customerDto.State))
            {
                // Find country for US states
                var country = await _countryService.GetCountryByTwoLetterIsoCode("US");
                if (country != null)
                {
                    address.CountryId = country.Id;
                    
                    // Try to match state/province
                    var states = await _countryService.GetStateProvincesByCountryId(country.Id);
                    var state = states.FirstOrDefault(s => 
                        s.Abbreviation.Equals(customerDto.State, StringComparison.OrdinalIgnoreCase));
                    
                    if (state != null)
                    {
                        address.StateProvinceId = state.Id;
                    }
                }
            }

            await _customerService.InsertAddress(address, customer.Id);
            customer.Addresses.Add(address);
        }
    }
}