// Simple version - minimal JavaScript to avoid conflicts
document.addEventListener('DOMContentLoaded', function () {
    console.log('Admin Edit User JavaScript loaded');

    // Store original values for comparison only
    var originalValues = {};
    var isSubmitting = false;

    // Initialize original values
    function initOriginalValues() {
        const inputs = document.querySelectorAll('input:not([readonly]):not([type="hidden"]), select:not([readonly]), textarea:not([readonly])');
        inputs.forEach(function (element) {
            const name = element.getAttribute('name');
            if (name && name !== '__RequestVerificationToken') {
                if (element.type === 'checkbox') {
                    originalValues[name] = element.checked;
                } else {
                    originalValues[name] = element.value;
                }
            }
        });
        console.log('Original values stored:', originalValues);
    }

    // Simple form submission - let it submit naturally
    const editUserForm = document.getElementById('editUserForm');
    if (editUserForm) {
        editUserForm.addEventListener('submit', function (e) {
            console.log('Form submit triggered');

            if (isSubmitting) {
                console.log('Already submitting, preventing duplicate');
                e.preventDefault();
                return false;
            }

            // Basic validation only
            const fullName = document.getElementById('FullName');
            const gender = document.getElementById('Gender');
            const dateOfBirth = document.getElementById('DateOfBirth');
            const role = document.getElementById('Role');

            let hasErrors = false;

            if (fullName && !fullName.value.trim()) {
                alert('Please enter Full Name');
                fullName.focus();
                e.preventDefault();
                return false;
            }

            if (gender && !gender.value) {
                alert('Please select Gender');
                gender.focus();
                e.preventDefault();
                return false;
            }

            if (dateOfBirth && !dateOfBirth.value) {
                alert('Please enter Date of Birth');
                dateOfBirth.focus();
                e.preventDefault();
                return false;
            }

            if (role && !role.value) {
                alert('Please select Role');
                role.focus();
                e.preventDefault();
                return false;
            }

            // Show loading state
            isSubmitting = true;
            const saveBtn = document.getElementById('saveBtn');
            if (saveBtn) {
                saveBtn.disabled = true;
                saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Saving...';
            }

            console.log('Form validation passed, submitting...');
            // Let the form submit naturally
            return true;
        });
    }

    // Global functions for buttons (simplified)
    window.resetForm = function () {
        if (confirm('Reset all changes?')) {
            location.reload();
        }
    };

    window.previewChanges = function () {
        alert('Preview functionality - form will submit when you click Save All Changes');
    };

    window.confirmDelete = function () {
        const deleteModal = document.getElementById('deleteModal');
        if (deleteModal && typeof bootstrap !== 'undefined') {
            const modal = new bootstrap.Modal(deleteModal);
            modal.show();
        }
    };

    // Initialize
    initOriginalValues();
});