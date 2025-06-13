using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Integration.QuickBooks.Services;

namespace Integration.QuickBooks
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IQuickBooksService, QuickBooksService>();
            services.AddScoped<IQuickBooksMapper, QuickBooksMapper>();
            services.AddScoped<IQuickBooksCustomerService, QuickBooksCustomerService>();
            services.AddScoped<IQuickBooksInvoiceService, QuickBooksInvoiceService>();
        }

        public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
        {
        }

        public int Priority => 10;
        public bool BeforeConfigure => false;
    }
}