#nullable enable
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Core.Interfaces.Catalog.Products;

public interface IStockQuantityService
{
    double GetTotalStockQuantity(Product product,
        bool useReservedQuantity = true,
        string warehouseId = "", bool total = false);

    double GetTotalStockQuantityForCombination(Product product, ProductAttributeCombination combination,
        bool useReservedQuantity = true, string warehouseId = "");

    (string resource, object? arg0) FormatStockMessage(Product product, string warehouseId,
        IList<CustomAttribute> attributes);
}