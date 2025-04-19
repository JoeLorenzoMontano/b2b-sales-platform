# Store-Specific Customer Groups for GrandNode 2

This feature enables you to limit customer groups to specific stores, ideal for SaaS or multi-store scenarios. It ensures that customer groups can be isolated by store, preventing cross-store visibility and management.

## Core Features

- Customer groups can be limited to specific stores
- System groups (Administrators, Registered, Guests, etc.) remain available to all stores
- Store staff can only see and manage customer groups relevant to their store
- Permissions and discounts tied to customer groups respect store boundaries
- Migration tool automatically initializes existing customer groups with empty store mappings

## Implementation Details

### Entity and Service Changes

1. `CustomerGroup` entity now implements `IStoreLinkEntity`
   - Added `LimitedToStores` (bool) property
   - Added `Stores` (IList<string>) property

2. `GroupService` updated to include store filtering
   - Updated `GetAllCustomerGroups()` to filter by store ID
   - Enhanced `IsInCustomerGroup()` to respect store-specific customer groups

3. Added validation to prevent limiting system groups to specific stores

### Admin UI Changes

1. Added Store Mapping tab to Customer Group edit page
   - Allows limiting the customer group to specific stores
   - Shows a list of available stores with checkboxes

2. Updated customer group listing to filter by current store for staff users

### Migration

A database migration `MigrationAddCustomerGroupStoreMappings` initializes existing customer groups to not be limited to stores.

## Usage

1. Navigate to Admin > Customers > Customer Groups
2. Edit a customer group
3. Go to the "Stores" tab
4. Check "Limited to stores" and select the stores this group should be available in

## Technical Notes

- System groups (IsSystem = true) cannot be limited to specific stores
- Store filtering is automatically applied for staff users
- The `IsInCustomerGroup()` method automatically checks store-specific access

This feature follows GrandNode 2's existing patterns for store-specific entities, ensuring consistency with the application's architecture.