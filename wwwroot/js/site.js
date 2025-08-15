// DigiDoc - Main JavaScript File

// Global utility functions
window.DigiDoc = {
    // Show notification
    showNotification: function(message, type = 'info') {
        const alertClass = `alert-${type}`;
        const iconClass = this.getNotificationIcon(type);
        
        const alertHtml = `
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                <i class="fas ${iconClass} me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        
        // Insert at the top of the main container
        const container = document.querySelector('main .container');
        if (container) {
            container.insertAdjacentHTML('afterbegin', alertHtml);
        }
    },
    
    // Get notification icon based on type
    getNotificationIcon: function(type) {
        switch (type) {
            case 'success': return 'fa-check-circle';
            case 'error': return 'fa-exclamation-circle';
            case 'warning': return 'fa-exclamation-triangle';
            case 'info': 
            default: return 'fa-info-circle';
        }
    },
    
    // Format date
    formatDate: function(date) {
        return new Date(date).toLocaleDateString();
    },
    
    // Format datetime
    formatDateTime: function(date) {
        return new Date(date).toLocaleString();
    },
    
    // Show loading spinner
    showLoading: function(element) {
        if (element) {
            element.innerHTML = `
                <div class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-2">Loading...</p>
                </div>
            `;
        }
    },
    
    // Hide loading spinner
    hideLoading: function(element) {
        if (element) {
            element.innerHTML = '';
        }
    },
    
    // Make API request
    apiRequest: async function(url, options = {}) {
        try {
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
    
    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        var alerts = document.querySelectorAll('.alert');
        alerts.forEach(function(alert) {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);
    
    // Add fade-in animation to cards
    var cards = document.querySelectorAll('.card');
    cards.forEach(function(card, index) {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        setTimeout(function() {
            card.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });
});

// Form validation helpers
window.FormValidation = {
    // Validate email
    isValidEmail: function(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    },
    
    // Validate phone number
    isValidPhone: function(phone) {
        const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
        return phoneRegex.test(phone.replace(/[\s\-\(\)]/g, ''));
    },
    
    // Validate required fields
    validateRequired: function(form) {
        const requiredFields = form.querySelectorAll('[required]');
        let isValid = true;
        
        requiredFields.forEach(function(field) {
            if (!field.value.trim()) {
                field.classList.add('is-invalid');
                isValid = false;
            } else {
                field.classList.remove('is-invalid');
            }
        });
        
        return isValid;
    },
    
    // Clear validation errors
    clearValidation: function(form) {
        const invalidFields = form.querySelectorAll('.is-invalid');
        invalidFields.forEach(function(field) {
            field.classList.remove('is-invalid');
        });
    }
};

// Table helpers
window.TableHelpers = {
    // Sort table by column
    sortTable: function(table, columnIndex, type = 'string') {
        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        
        rows.sort(function(a, b) {
            const aValue = a.cells[columnIndex].textContent.trim();
            const bValue = b.cells[columnIndex].textContent.trim();
            
            if (type === 'number') {
                return parseFloat(aValue) - parseFloat(bValue);
            } else if (type === 'date') {
                return new Date(aValue) - new Date(bValue);
            } else {
                return aValue.localeCompare(bValue);
            }
        });
        
        // Reorder rows
        rows.forEach(function(row) {
            tbody.appendChild(row);
        });
    },
    
    // Filter table
    filterTable: function(table, searchTerm) {
        const tbody = table.querySelector('tbody');
        const rows = tbody.querySelectorAll('tr');
        
        rows.forEach(function(row) {
            const text = row.textContent.toLowerCase();
            const matches = text.includes(searchTerm.toLowerCase());
            row.style.display = matches ? '' : 'none';
        });
    }
};
