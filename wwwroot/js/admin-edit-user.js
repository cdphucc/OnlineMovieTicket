$(document).ready(function () {
    // Store original values for reset functionality
    var originalValues = {};
    var originalRole = window.userEditData.originalRole;

    // Initialize original values
    $('input, select, textarea').each(function () {
        var element = $(this);
        if (!element.prop('readonly') && !element.is(':hidden')) {
            var name = element.attr('name');
            if (name) {
                if (element.is(':checkbox')) {
                    originalValues[name] = element.prop('checked');
                } else {
                    originalValues[name] = element.val();
                }
            }
        }
    });

    // Role change warning
    $('#roleSelect').on('change', function () {
        var currentRole = $(this).val();
        if (originalRole !== currentRole) {
            $('#roleWarning').removeClass('d-none');
        } else {
            $('#roleWarning').addClass('d-none');
        }
    });

    // Email confirmed status text update
    $('#emailConfirmedSwitch').on('change', function () {
        var isChecked = $(this).prop('checked');
        $('#emailStatusText').text(isChecked ? 'Email Verified' : 'Email Not Verified');
    });

    // Real-time change detection and highlighting
    $('input, select, textarea').not(':hidden, [readonly]').on('change input', function () {
        updateChangeHighlighting();
        updateChangesSummary();
    });

    function updateChangeHighlighting() {
        $('input, select, textarea').not(':hidden, [readonly]').each(function () {
            var element = $(this);
            var name = element.attr('name');
            if (!name) return;

            var currentValue, originalValue;

            if (element.is(':checkbox')) {
                currentValue = element.prop('checked');
                originalValue = originalValues[name];
            } else {
                currentValue = element.val();
                originalValue = originalValues[name];
            }

            if (originalValue !== undefined && originalValue !== currentValue) {
                element.addClass('border-warning border-2');
                element.closest('.form-group').find('label').addClass('text-warning fw-bold');
            } else {
                element.removeClass('border-warning border-2 border-danger');
                element.closest('.form-group').find('label').removeClass('text-warning fw-bold');
            }
        });
    }

    function updateChangesSummary() {
        var changedFields = [];

        $('input, select, textarea').not(':hidden, [readonly]').each(function () {
            var element = $(this);
            var name = element.attr('name');
            if (!name) return;

            var currentValue, originalValue;

            if (element.is(':checkbox')) {
                currentValue = element.prop('checked');
                originalValue = originalValues[name];
            } else {
                currentValue = element.val();
                originalValue = originalValues[name];
            }

            if (originalValue !== undefined && originalValue !== currentValue) {
                var fieldLabel = element.closest('.form-group').find('label').first().text()
                    .replace('*', '').replace(/[\u{1F300}-\u{1F9FF}]/gu, '').trim();

                var changeText = fieldLabel;
                if (element.is(':checkbox')) {
                    changeText += ': ' + (currentValue ? 'Enabled' : 'Disabled');
                } else if (element.is('select')) {
                    changeText += ': ' + currentValue;
                } else {
                    changeText += ': Modified';
                }

                changedFields.push(changeText);
            }
        });

        if (changedFields.length > 0) {
            $('#changedFieldsList').html(changedFields.map(function (field) {
                return '<li>' + field + '</li>';
            }).join(''));
            $('#changesSummary').removeClass('d-none');
        } else {
            $('#changesSummary').addClass('d-none');
        }
    }

    // Form submission confirmation
    $('#editUserForm').on('submit', function (e) {
        var changedFields = [];

        $('input, select, textarea').not(':hidden, [readonly]').each(function () {
            var element = $(this);
            var name = element.attr('name');
            if (!name) return;

            var currentValue, originalValue;

            if (element.is(':checkbox')) {
                currentValue = element.prop('checked');
                originalValue = originalValues[name];
            } else {
                currentValue = element.val();
                originalValue = originalValues[name];
            }

            if (originalValue !== undefined && originalValue !== currentValue) {
                var fieldLabel = element.closest('.form-group').find('label').first().text()
                    .replace('*', '').replace(/[\u{1F300}-\u{1F9FF}]/gu, '').trim();
                changedFields.push(fieldLabel);
            }
        });

        if (changedFields.length > 0) {
            var message = 'The following fields will be updated:\n\n' +
                changedFields.join('\n') + '\n\nDo you want to continue?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        }
    });

    // Reset form function
    window.resetForm = function () {
        if (confirm('Are you sure you want to reset all changes? All unsaved modifications will be lost.')) {
            for (var name in originalValues) {
                var element = $('[name="' + name + '"]');
                if (element.is(':checkbox')) {
                    element.prop('checked', originalValues[name]);
                } else {
                    element.val(originalValues[name]);
                }
            }

            // Reset visual indicators
            $('input, select, textarea').removeClass('border-warning border-2 border-danger');
            $('.form-group label').removeClass('text-warning fw-bold');
            $('#roleWarning').addClass('d-none');
            $('#changesSummary').addClass('d-none');
            $('#emailStatusText').text(window.userEditData.emailConfirmedText);
        }
    };

    // Preview changes function
    window.previewChanges = function () {
        var changedFields = [];

        $('input, select, textarea').not(':hidden, [readonly]').each(function () {
            var element = $(this);
            var name = element.attr('name');
            if (!name) return;

            var currentValue, originalValue;

            if (element.is(':checkbox')) {
                currentValue = element.prop('checked');
                originalValue = originalValues[name];
            } else {
                currentValue = element.val();
                originalValue = originalValues[name];
            }

            if (originalValue !== undefined && originalValue !== currentValue) {
                var fieldLabel = element.closest('.form-group').find('label').first().text()
                    .replace('*', '').replace(/[\u{1F300}-\u{1F9FF}]/gu, '').trim();

                changedFields.push({
                    field: fieldLabel,
                    original: element.is(':checkbox') ?
                        (originalValue ? 'Enabled' : 'Disabled') : originalValue,
                    current: element.is(':checkbox') ?
                        (currentValue ? 'Enabled' : 'Disabled') : currentValue
                });
            }
        });

        var previewHtml = '';
        if (changedFields.length > 0) {
            previewHtml = '<div class="table-responsive"><table class="table table-striped">' +
                '<thead><tr><th>Field</th><th>Original Value</th><th>New Value</th></tr></thead><tbody>';

            changedFields.forEach(function (change) {
                previewHtml += '<tr>' +
                    '<td><strong>' + change.field + '</strong></td>' +
                    '<td class="text-muted">' + (change.original || 'Empty') + '</td>' +
                    '<td class="text-success"><strong>' + (change.current || 'Empty') + '</strong></td>' +
                    '</tr>';
            });

            previewHtml += '</tbody></table></div>';
        } else {
            previewHtml = '<div class="alert alert-info"><i class="fas fa-info-circle me-2"></i>No changes detected.</div>';
        }

        $('#previewContent').html(previewHtml);
        $('#previewModal').modal('show');
    };

    // Delete confirmation function
    window.confirmDelete = function () {
        $('#deleteModal').modal('show');
    };

    // Real-time email validation
    $('#Email').on('input blur', function () {
        var email = $(this).val();
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (email && !emailPattern.test(email)) {
            $(this).addClass('border-danger').removeClass('border-warning');
            $(this).next('.text-danger').text('Please enter a valid email address.');
        } else {
            $(this).removeClass('border-danger');
            var errorSpan = $(this).next('.text-danger');
            if (errorSpan.text() === 'Please enter a valid email address.') {
                errorSpan.text('');
            }
        }
    });

    // Phone number formatting
    $('#PhoneNumber').on('input', function () {
        var value = $(this).val().replace(/\D/g, '');
        if (value.length >= 10) {
            var formatted = value.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
            $(this).val(formatted);
        }
    });

    // Date validation
    $('#DateOfBirth').on('change', function () {
        var birthDate = new Date($(this).val());
        var today = new Date();
        var age = today.getFullYear() - birthDate.getFullYear();
        var monthDiff = today.getMonth() - birthDate.getMonth();

        if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
            age--;
        }

        if (age < 13 || age > 120) {
            $(this).addClass('border-danger');
            $(this).next('.text-danger').text('Age must be between 13 and 120 years.');
        } else {
            $(this).removeClass('border-danger');
            var errorSpan = $(this).next('.text-danger');
            if (errorSpan.text().includes('Age must be')) {
                errorSpan.text('');
            }
        }
    });

    // Auto-save notification
    var autoSaveTimeout;
    $('input, select, textarea').not(':hidden, [readonly]').on('change input', function () {
        clearTimeout(autoSaveTimeout);
        autoSaveTimeout = setTimeout(function () {
            var notification = $('<div class="alert alert-info alert-dismissible fade show position-fixed" ' +
                'style="top: 20px; right: 20px; z-index: 9999; max-width: 300px;">' +
                '<i class="fas fa-save me-2"></i>Changes detected. Don\'t forget to save!' +
                '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
                '</div>');

            $('body').append(notification);
            setTimeout(function () {
                notification.alert('close');
            }, 5000);
        }, 10000);
    });

    // Initialize on page load
    updateChangeHighlighting();
    updateChangesSummary();
});