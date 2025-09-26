using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Payments.Consignment;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute("Plugin.PaymentConsignment",
            "Plugins/PaymentConsignment/{action}",
            new { controller = "PaymentConsignment" });
    }

    public int Priority => 0;
}