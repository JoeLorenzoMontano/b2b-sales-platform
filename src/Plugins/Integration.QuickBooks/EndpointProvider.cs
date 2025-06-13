using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Integration.QuickBooks
{
    public class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Integration.QuickBooks.Configure",
                pattern: "Admin/QuickBooks/Configure",
                defaults: new { controller = "QuickBooks", action = "Configure", area = "Admin" }
            );

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Integration.QuickBooks.ConnectToQuickBooks",
                pattern: "Admin/QuickBooks/ConnectToQuickBooks",
                defaults: new { controller = "QuickBooks", action = "ConnectToQuickBooks", area = "Admin" }
            );

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Integration.QuickBooks.Callback",
                pattern: "Admin/QuickBooks/Callback",
                defaults: new { controller = "QuickBooks", action = "Callback", area = "Admin" }
            );
        }

        public int Priority => 0;
    }
}