/**
 * Order Item Inline Edit with Auto-Save
 * Handles inline editing of order item price and quantity with automatic saving on blur
 */

$(document).ready(function() {
    // Initialize inline editing for all existing fields
    initializeInlineEditing();
});

function initializeInlineEditing() {
    // Handle blur event for auto-save
    $(document).on('blur', '.inline-edit-field', function() {
        const $field = $(this);
        const orderItemId = $field.data('order-item-id');
        const fieldType = $field.data('field-type');
        const newValue = $field.val().trim();
        const originalValue = $field.data('original-value').toString();
        
        // Only save if value changed and is valid
        if (newValue !== originalValue && isValidValue(newValue, fieldType)) {
            autoSaveField(orderItemId, fieldType, newValue, $field);
        } else if (newValue === originalValue) {
            // Reset any error states if reverting to original value
            clearFieldError($field);
        } else if (!isValidValue(newValue, fieldType)) {
            // Show validation error
            showFieldError($field, getValidationMessage(fieldType));
        }
    });

    // Handle focus event - clear any existing errors
    $(document).on('focus', '.inline-edit-field', function() {
        clearFieldError($(this));
    });

    // Prevent form submission when pressing Enter in inline edit fields
    $(document).on('keypress', '.inline-edit-field', function(e) {
        if (e.which === 13) { // Enter key
            $(this).blur(); // Trigger save
            return false;
        }
    });
}

function isValidValue(value, fieldType) {
    if (!value || value === '') return false;
    
    if (fieldType === 'quantity') {
        const num = parseInt(value);
        return /^\d+$/.test(value) && num > 0;
    }
    
    if (fieldType === 'price') {
        const num = parseFloat(value);
        return /^\d+(\.\d{1,2})?$/.test(value) && num >= 0;
    }
    
    return false;
}

function getValidationMessage(fieldType) {
    if (fieldType === 'quantity') {
        return 'Quantity must be a positive whole number';
    }
    if (fieldType === 'price') {
        return 'Price must be a valid decimal number (e.g., 12.34)';
    }
    return 'Invalid value';
}

function autoSaveField(orderItemId, fieldType, newValue, $field) {
    // Show loading state
    showLoadingState(orderItemId);
    
    // Disable the field during save
    $field.prop('disabled', true);
    
    // Get the order ID from the current URL or form
    const orderId = getOrderId();
    
    $.ajax({
        url: '/Admin/Order/UpdateOrderItemField',
        type: 'POST',
        data: {
            orderId: orderId,
            orderItemId: orderItemId,
            fieldType: fieldType,
            value: newValue
        },
        success: function(response) {
            if (response.success) {
                // Update the original value data attribute
                $field.data('original-value', newValue);
                
                // Show success state briefly
                showSuccessState(orderItemId);
                
                // Update any calculated totals if provided
                if (response.newSubTotal) {
                    updateSubTotal(orderItemId, response.newSubTotal);
                }
                
                // Update display value if provided
                if (response.displayValue) {
                    updateDisplayValue(orderItemId, fieldType, response.displayValue);
                }
                
                // Clear any existing errors
                clearFieldError($field);
            } else {
                // Show error message
                showFieldError($field, response.message || 'Save failed');
                
                // Revert to original value
                $field.val($field.data('original-value'));
            }
        },
        error: function(xhr, status, error) {
            // Show generic error
            showFieldError($field, 'Network error - please try again');
            
            // Revert to original value
            $field.val($field.data('original-value'));
        },
        complete: function() {
            // Re-enable field and hide loading
            $field.prop('disabled', false);
            hideLoadingState(orderItemId);
        }
    });
}

function showLoadingState(orderItemId) {
    const $statusDiv = $('#save-status-' + orderItemId);
    $statusDiv.html('<small class="text-muted"><i class="fa fa-spinner fa-spin"></i> Saving...</small>').show();
}

function showSuccessState(orderItemId) {
    const $statusDiv = $('#save-status-' + orderItemId);
    $statusDiv.html('<small class="text-success"><i class="fa fa-check"></i> Saved</small>').show();
    
    // Hide success message after 2 seconds
    setTimeout(function() {
        hideLoadingState(orderItemId);
    }, 2000);
}

function hideLoadingState(orderItemId) {
    $('#save-status-' + orderItemId).hide();
}

function showFieldError($field, message) {
    // Remove any existing error styling
    clearFieldError($field);
    
    // Add error styling
    $field.addClass('is-invalid border-danger');
    
    // Add error message
    const errorHtml = '<div class="invalid-feedback d-block">' + message + '</div>';
    $field.after(errorHtml);
}

function clearFieldError($field) {
    $field.removeClass('is-invalid border-danger');
    $field.next('.invalid-feedback').remove();
}

function updateSubTotal(orderItemId, newSubTotal) {
    // Find and update the subtotal display for this item
    // This selector might need adjustment based on the actual HTML structure
    const $row = $('[data-order-item-id="' + orderItemId + '"]').closest('tr');
    $row.find('.subtotal-display').text(newSubTotal);
}

function updateDisplayValue(orderItemId, fieldType, displayValue) {
    // Update the display value shown below the input field
    const $field = $('[data-order-item-id="' + orderItemId + '"][data-field-type="' + fieldType + '"]');
    $field.closest('td').find('.text-info span').text('Display: ' + displayValue);
}

function getOrderId() {
    // Extract order ID from URL or form data
    const urlParts = window.location.pathname.split('/');
    const editIndex = urlParts.indexOf('Edit');
    if (editIndex !== -1 && editIndex + 1 < urlParts.length) {
        return urlParts[editIndex + 1];
    }
    
    // Fallback: try to get from a hidden input or form
    const $hiddenOrderId = $('input[name="Id"]');
    if ($hiddenOrderId.length) {
        return $hiddenOrderId.val();
    }
    
    // Log error if we can't find order ID
    console.error('Could not determine order ID for auto-save');
    return null;
}

// Utility function to debounce rapid changes (optional enhancement)
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}