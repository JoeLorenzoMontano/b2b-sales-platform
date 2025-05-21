# Weight-Based Conversion Attribute Implementation Plan

This document outlines the implementation plan for adding a new attribute value type to GrandNode that supports weight-based conversion ratios for inventory management.

## Goal

Allow products to be sold in different measurement units (pound, ounce, etc.) that automatically decrement the main inventory by the appropriate conversion ratio.

## Overview

We'll add a new `WeightBasedConversion` attribute value type to GrandNode that:
1. Works with the existing product attribute system
2. Allows setting a conversion ratio in the admin UI
3. Automatically applies this ratio when decrementing inventory
4. Preserves all other product and attribute functionality

## Implementation Steps

### 1. Add New Attribute Value Type

**File**: `src/Core/Grand.Domain/Catalog/AttributeValueType.cs`

Add a new enum value:
```csharp
/// <summary>
///     Weight-based attribute with conversion ratio
/// </summary>
WeightBasedConversion = 20
```

### 2. Update Inventory Management Service

**File**: `src/Business/Grand.Business.Catalog/Services/Products/InventoryManageService.cs`

Modify the `AdjustReserved` method to recognize and handle the new attribute type:

```csharp
public virtual async Task AdjustReserved(Product product, int quantityToChange,
    IList<CustomAttribute> attributes = null, string warehouseId = "")
{
    ArgumentNullException.ThrowIfNull(product);

    if (quantityToChange == 0)
        return;

    // Handle the new WeightBasedConversion attribute type
    if (attributes != null && product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
    {
        var attributeValues = product.ParseProductAttributeValues(attributes);
        foreach (var attributeValue in attributeValues)
        {
            if (attributeValue.AttributeValueTypeId == AttributeValueType.WeightBasedConversion)
            {
                // Apply the conversion ratio from the Quantity field
                quantityToChange = quantityToChange * attributeValue.Quantity;
                break; // Only apply one conversion
            }
        }
    }

    // Original method continues...
```

### 3. Update Admin UI - Attribute Value Editor

**File**: `src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/Partials/CreateOrUpdateProductAttributeValue.cshtml`

Update the JavaScript function that toggles fields based on attribute type:

```javascript
function toggleProductType() {
    var selectedProductTypeId = $("#@Html.IdFor(model => model.AttributeValueTypeId)").val();
    if (selectedProductTypeId == @(((int)AttributeValueType.Simple).ToString())) {
        $('#group-associated-product').hide();
        $('#group-quantity').hide();
        $('#group-weight-adjustment').show();
        $('#group-cost').show();
    } else if (selectedProductTypeId == @(((int)AttributeValueType.AssociatedToProduct).ToString())) {
        $('#group-associated-product').show();
        $('#group-quantity').show();
        $('#group-weight-adjustment').hide();
        $('#group-cost').hide();
    } else if (selectedProductTypeId == @(((int)AttributeValueType.WeightBasedConversion).ToString())) {
        // For the new type, show quantity but hide associated product
        $('#group-associated-product').hide();
        $('#group-quantity').show();
        $('#group-weight-adjustment').show();
        $('#group-cost').hide();
        // Update label for conversion ratio
        $("label[for='@Html.IdFor(model => model.Quantity)']").text("Conversion Ratio:");
    }
}
```

Add a descriptive note for the quantity field:

```html
<div class="form-group" id="group-quantity">
    <admin-label asp-for="Quantity" class="col-sm-3 control-label"/>
    <div class="col-md-9 col-sm-9">
        <admin-input asp-for="Quantity"/>
        <span asp-validation-for="Quantity"></span>
        <div id="quantity-hint" class="small mt-1" style="display:none;">
            Enter the conversion ratio (e.g., 453 for pounds to grams).
            When a customer orders 1 unit, inventory will be decremented by this amount.
        </div>
    </div>
</div>

<script>
    $(document).ready(function() {
        $("#@Html.IdFor(model => model.AttributeValueTypeId)").change(function() {
            if ($(this).val() == @(((int)AttributeValueType.WeightBasedConversion).ToString())) {
                $("#quantity-hint").show();
            } else {
                $("#quantity-hint").hide();
            }
        });
        
        // Initial state
        if ($("#@Html.IdFor(model => model.AttributeValueTypeId)").val() == @(((int)AttributeValueType.WeightBasedConversion).ToString())) {
            $("#quantity-hint").show();
        }
    });
</script>
```

### 4. Update Translations

**File**: `src/Web/Grand.Web.Common/Localization/EnumTranslationProvider.cs` 

Ensure the new enum value has a friendly display name:

```csharp
private static readonly Dictionary<string, Dictionary<int, string>> _enumTranslations = new()
{
    // Other enums...
    ["AttributeValueType"] = new Dictionary<int, string>
    {
        [(int)AttributeValueType.Simple] = "Simple",
        [(int)AttributeValueType.AssociatedToProduct] = "Associated to product",
        [(int)AttributeValueType.WeightBasedConversion] = "Weight-based conversion" // Add this line
    },
    // More enums...
};
```

### 5. Testing Plan

1. **Build and Deploy**:
   - Compile the GrandNode solution
   - Deploy to the testing environment

2. **Configure Test Product**:
   - Create or use an existing product with inventory tracking
   - Add product attributes for different units (Pound, Ounce, etc.)
   - Set each attribute value to use the new `WeightBasedConversion` type
   - Set appropriate conversion ratios (453 for Pound, 28 for Ounce, etc.)

3. **Test Orders**:
   - Place test orders selecting different attribute values
   - Verify that inventory is decremented by the correct amount
   - Example: Ordering 2 pounds should decrement inventory by 906 (2 × 453)

4. **Verify Admin UI**:
   - Check that the attribute value editor displays correctly
   - Verify that the hint text appears for the new attribute type
   - Ensure all existing functionality still works

### 6. Rollout Plan

1. **Development Environment**:
   - Implement and test all changes
   - Verify functionality with test orders

2. **Staging Environment**:
   - Deploy to staging
   - Perform thorough testing
   - Check for any performance impacts

3. **Production Environment**:
   - Schedule deployment during low-traffic period
   - Prepare rollback plan
   - Deploy changes
   - Monitor orders and inventory adjustments

## Affected Files Summary

1. `src/Core/Grand.Domain/Catalog/AttributeValueType.cs`
2. `src/Business/Grand.Business.Catalog/Services/Products/InventoryManageService.cs`
3. `src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/Partials/CreateOrUpdateProductAttributeValue.cshtml`
4. `src/Web/Grand.Web.Common/Localization/EnumTranslationProvider.cs`

## Notes

- This implementation preserves backward compatibility
- Existing attribute values will continue to work as before
- The solution is maintainable and follows GrandNode's architecture
- Future updates should be minimal and straightforward