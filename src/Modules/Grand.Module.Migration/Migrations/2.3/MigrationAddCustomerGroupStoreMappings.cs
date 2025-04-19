using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Migration.Migrations._2._3;

public class MigrationAddCustomerGroupStoreMappings : IMigration
{
    public int Priority => 100;

    public DbVersion Version => new(2, 3);

    public Guid Identity => new("7D65F48E-1E84-47B1-B0D4-5E95E842DCAA");

    public string Name => "Add store mappings to customer groups";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();

        // Update existing customer groups to not be limited to stores
        var customerGroups = repository.Table.ToList();
        foreach (var customerGroup in customerGroups)
        {
            // Initialize properties for IStoreLinkEntity
            customerGroup.LimitedToStores = false;
            if (customerGroup.Stores == null)
                customerGroup.Stores = new List<string>();
                
            repository.Update(customerGroup);
        }

        return true;
    }
}