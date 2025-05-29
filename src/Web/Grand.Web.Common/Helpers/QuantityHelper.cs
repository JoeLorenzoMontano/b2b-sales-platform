namespace Grand.Web.Common.Helpers;

public static class QuantityHelper
{
    public static int MaxQuantity(this int quantity) => Math.Min(quantity, 100);
    
    public static double MaxQuantity(this double quantity) => Math.Min(quantity, 100);
}