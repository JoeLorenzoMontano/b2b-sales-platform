using System.ComponentModel.DataAnnotations;

namespace Grand.Business.Customers.Services.ExportImport
{
    public class CustomerImportDto
    {
        public bool Active { get; set; } = true;
        public string Company { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        
        [Required]
        public string Email { get; set; }
        
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        
        // Optional password (defaults to "Password1!" if not provided)
        public string Password { get; set; } = "Password1!";
    }
}