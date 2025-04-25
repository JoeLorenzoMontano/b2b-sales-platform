// Add inventory journal tab to product tabs
$(document).ready(function() {
    // Check if we're on the product edit page
    if ($('#product-edit').length > 0) {
        // Add inventory journal tab
        var tabStrip = $('#product-edit').data('kendoTabStrip');
        if (tabStrip) {
            // Find the inventory tab
            var inventoryTab = $('#tab-inventory');
            if (inventoryTab.length > 0) {
                // Add inventory journal tab after inventory tab
                var inventoryTabIndex = $('#product-edit>ul>li').index($('li:has(a[href="#tab-inventory"])'));
                if (inventoryTabIndex >= 0) {
                    // Create tab
                    tabStrip.insertAfter({
                        text: 'Inventory Journal',
                        content: '<div id="tab-inventoryjournal"></div>'
                    }, inventoryTabIndex);
                    
                    // Load inventory journal content
                    $.ajax({
                        cache: false,
                        url: '/Admin/Product/InventoryJournalTab?productId=' + $('#Id').val(),
                        type: 'GET',
                        success: function(data) {
                            $('#tab-inventoryjournal').html(data);
                        }
                    });
                }
            }
        }
    }
});