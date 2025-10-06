# Inventory Management with Conversion Ratios in GrandNode2

This guide explains how to configure a product inventory system where you have a base unit of measure (grams) and offer various packaging sizes that decrement the base inventory at different rates (e.g., ounces, pounds).

## Setting Up Inventory for Products with Variable Packaging Options

### 1. Create the Base Product

First, set up your main product (bulk cannabis):

1. Go to **Catalog > Products > Add new**
2. Create a product with essential details (name, description, etc.)
3. For the Inventory settings:
   - Set **Inventory method** to "Manage stock by attributes"
   - Enable **Use multiple warehouses** if you store inventory across different locations
   - You can track the total grams available, but don't set specific stock quantities yet

### 2. Set Up Product Attributes

Create a product attribute for the packaging size:

1. Go to **Catalog > Attributes > Product attributes** and add a new attribute like "Package Size"
2. Go back to your product and add this attribute
3. In the product details, go to the **Attributes** tab and add "Package Size" with the following values:
   - "Half Ounce" (14 grams)
   - "Ounce" (28 grams)
   - "Half Pound" (226 grams)
   - "Pound" (453 grams)
   - Any other packaging sizes you need

### 3. Create Attribute Combinations

The key to your solution is in the attribute combinations:

1. Go to the **Attribute Combinations** tab for your product
2. Create combinations for each packaging size
3. For each combination:
   - Set the specific attributes (e.g., "Package Size: Ounce")
   - Define a unique SKU for tracking
   - Specify pricing
   - **Most importantly**: Do NOT set stock quantities here

### 4. Create Associated Products for Conversion

This is where the conversion magic happens:

1. Create "virtual" products for each packaging size (you can hide these from catalog)
2. Set each product's inventory method to "Manage stock"
3. For each product:
   - Set a specific conversion ratio in the description or name for reference
   
### 5. Link the Main Product Attributes to Associated Products

This is the crucial step:

1. Go back to your main product's attributes
2. Edit each attribute value (half ounce, ounce, etc.)
3. Change the attribute value type to "Associated to product"
4. Select the corresponding virtual product you created
5. Set the "Quantity" field to the conversion ratio:
   - For "Half Ounce" → Quantity = 14 (decrements 14 grams from total)
   - For "Ounce" → Quantity = 28 (decrements 28 grams from total)
   - For "Half Pound" → Quantity = 226 (decrements 226 grams from total)
   - For "Pound" → Quantity = 453 (decrements 453 grams from total)

### 6. How the System Will Work

With this setup:

1. When a customer adds "1 Ounce" to cart, the system will:
   - Find the attribute combination ("Package Size: Ounce")
   - See it's associated with a product with quantity 28
   - Decrement 28 grams from your total inventory

2. If you have 5000 grams total, the system will:
   - Allow up to 178 ounces to be sold (5000/28 ≈ 178)
   - Or 11 pounds (5000/453 ≈ 11)
   - Or any combination of packages that doesn't exceed 5000 grams

### 7. Warehouse-Specific Inventory

If you're using multiple warehouses:

1. In your product settings, enable "Use multiple warehouses"
2. Go to the "Inventory" tab and set quantities per warehouse
3. When customers order, they can select a warehouse (or the system will do it automatically)
4. The conversion will be applied to that specific warehouse's inventory

## Code Implementation Details

This system leverages two key aspects of GrandNode2:

1. **`AttributeValueType.AssociatedToProduct`**: Associates attribute values with actual products
2. **The `Quantity` field**: Controls how many units of the associated product are used

When an order is placed, GrandNode2's inventory system will automatically convert package sizes to grams using the attribute's quantity property, and decrement the raw material inventory accordingly.

The key code handling this is in the `InventoryManageService` class:

```csharp
// This section handles associated products in attributes
var attributeValues = product.ParseProductAttributeValues(attributes);
foreach (var attributeValue in attributeValues)
{
    if (attributeValue.AttributeValueTypeId != AttributeValueType.AssociatedToProduct) continue;
    //associated product
    var associatedProduct = await _productRepository.GetByIdAsync(attributeValue.AssociatedProductId);
    if (associatedProduct == null) continue;
    
    // This line applies the conversion ratio (attributeValue.Quantity)
    await AdjustReserved(associatedProduct, quantityToChange * attributeValue.Quantity, null, warehouseId);
}
```

With this setup, your inventory system will correctly track raw grams and allow you to sell in various package sizes without having to pre-define how many of each package you'll create.