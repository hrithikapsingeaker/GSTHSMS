$(document).ready(function () {

    // ===================== Shruti Mane =====================

    let selectedFiles = [];

    $('#openModalBtn').click(function () {
        console.log("Button clicked!");
        $('#loadingSpinner').show();

        $.get('/AccountManager/CashTransactionPage', function (data) {

            console.log("Data loaded!");
            $('#modalBody').html(data);   // Load partial view HTML first

            // Show modal
            const modal = new bootstrap.Modal(document.getElementById('CashModal'), {
                backdrop: 'static',
                keyboard: false
            });
            modal.show();

            // ================== Apply Select2 (IMPORTANT FIX) ==================
            // Apply Select2 AFTER partial is injected!
            $('#TransactionIdDropdown, #MaintenanceDropdown, #MemberDropdown, #month, #TransactionTypeDropdown, #ReceiverCode, #BankNameDD, #MaintenanceTypeDropdown')
                .select2({
                    width: '100%',
                    dropdownParent: $('#CashModal'),   // 🔥 prevents search box from being hidden
                    minimumResultsForSearch: 0         // 🔥 search ON even if few options
                });

            // Reset Receiver dropdown
            $('#ReceiverCode').val('').trigger('change');

            // Flatpickr month select
            flatpickr("#MonthDropdown", {
                plugins: [
                    new monthSelectPlugin({
                        shorthand: true,
                        dateFormat: "m/Y",
                        altFormat: "F Y"
                    })
                ],
                allowInput: false,
                defaultDate: new Date()
            });

            // Hide Bank Balance initially
            $('#bankBalanceWrapper').addClass('d-none').hide();
            $('#NetBankBalance').text("₹ 0.00");

            // Call dependent functions
            toggleFields();
            bindPaymentModeChangeHandler();

        })
            .fail(function () {
                console.error("Failed to load form.");
                Swal.fire('Error', 'Error loading the form.', 'error');
            })
            .always(function () {
                $('#loadingSpinner').hide();
            });
    });



    $(document).on('hidden.bs.modal', '.modal', function () {
        if ($('.modal:visible').length) {
            $('body').addClass('modal-open');
        }
    });

    // ✅ Center modal (this is good — keep it)
    $('#CashModal').on('shown.bs.modal', function () {
        $(this).find('.modal-dialog').css({
            'margin-top': 'auto',
            'margin-bottom': 'auto'
        });
    });

    $(document).on('click', '.custom-modal-close', function () {
        console.log("Close button clicked! Hiding modal...");
        $('#CashModal').modal('hide');
        console.log("Modal should now be hidden.");
    });

    $('#CashModal').on('shown.bs.modal', function () {

        var monthVal = $('#MonthDropdown').val();

        // ✅ If Flatpickr auto-selected current month
        if (monthVal) {
            $('#MonthDropdown').trigger('change');  // ✅ Backend ला auto call होईल
        }
    });




    $('#openDebitedModalBtn').click(function () {
        $.get('@Url.Action("AddDebitedCashTransaction", "AccountManager")', function (data) {
            $('#modalBody').html(data);
            const modal = new bootstrap.Modal(document.getElementById('CashModal'), {
                backdrop: 'static',
                keyboard: false
            });
            modal.show();

            setTimeout(() => {
                toggleFields();
                bindPaymentModeChangeHandler();
                initializeDebitTransactionScript();
            }, 100);
        }).fail(function () {
            Swal.fire('Error', 'Error loading the debited transaction form.', 'error');
        });
    });

    function toggleFields() {
        var transactionVal = $('#TransactionIdDropdown').val(); // 26 = Credit, 27 = Debit
        var paymentMode = $('input[name="PaymentMode"]:checked').val(); // 32 = Cash, 34 = Cheque
        var receiverType = $('#TransactionTypeDropdown').val();
        var maintenanceType = $('#MaintenanceTypeDropdown').val();

        // Hide all sections first
        $('.cheque-fields').hide();
        $('#TypeDropdownDiv, #ReceiverDropdownDiv, #ReceiverTextboxDiv, #CreditSection, #PaymentPurposeDiv').hide();
        $('#MonthDiv, #MaintenanceDiv, #MemberDiv, #MaintenanceTypeDiv').hide();

        // Reset purpose
        $('#PaymentPurpose').val('');

        // ==== CREDIT ====
        if (transactionVal === "26") {
            $('#CreditSection').show();
            $('#PaymentPurposeDiv').hide();
            $('#MaintenanceTypeDiv').show();

            if (maintenanceType === "59") { // Monthly
                $('#MonthDiv').hide();
                $('#MaintenanceDiv').show();
                $('#MemberDiv').show();
                $('#PaymentPurpose').val('Monthly Maintainance');
            }
            else if (maintenanceType === "58") { // Occasional
                $('#MonthDiv').show();
                $('#MaintenanceDiv').show();
                $('#MemberDiv').show();
                $('#PaymentPurpose').val('Occational Maintainance');
            }
        }

        // ==== DEBIT ====
        else if (transactionVal === "27") {
            $('#PaymentPurposeDiv').show();
            $('#TypeDropdownDiv').show();

            if (receiverType === "Other") {
                $('#ReceiverTextboxDiv').show();
            }
            else if (receiverType) {
                $('#ReceiverDropdownDiv').show();
            }
        }

        // ==== Cheque fields independent ====
        // ==== Cheque fields independent ====
        if (paymentMode === "34") {
            console.log("✅ PaymentMode = Cheque (34) → Showing cheque fields");
            $('.cheque-fields').show();
        } else {
            console.log("ℹ PaymentMode = " + paymentMode + " → Hiding cheque fields");
            $('.cheque-fields').hide();
        }


    }

    // 🔹 Event bindings (trigger toggle immediately)
    $('#TransactionIdDropdown').change(toggleFields);
    $('#TransactionTypeDropdown').change(toggleFields);
    $('#MaintenanceTypeDropdown').change(toggleFields);
    $('input[name="PaymentMode"]').change(toggleFields);
    $('#MaintenanceTypeDropdown').change(toggleFields);
    $(document).on('change', 'input[name="PaymentMode"]', toggleFields);

    toggleFields(); // Run once on page load




    function bindPaymentModeChangeHandler() {
        $(document).on('change', 'input[name="PaymentMode"]', toggleFields);
    }

    $(document).on('change', '#TransactionIdDropdown, #TransactionTypeDropdown', toggleFields);

    // ===================== Receiver Binding =====================
    $(document).on('change', '#TransactionTypeDropdown', function () {
        var selectedType = $(this).val();
        var $rc = $('#ReceiverCode');

        // Show dropdown, hide textbox
        $('#ReceiverTextboxDiv').hide();
        $('#ReceiverDropdownDiv').show();

        // Always reset Receiver dropdown
        $rc.empty().append($('<option>', { value: '', text: '-- Select Receiver --', selected: true }));

        // Reset other fields
        $('#Amount').val('');
        $('#ExpenseCode, #EventCode').val('');

        if (!selectedType) return;

        $.get('/AccountManager/GetReceiversByType', { type: selectedType }, function (data) {
            console.log("Response from GetReceiversByType:", data);

            // If no data
            if (data.empty || (Array.isArray(data) && data.length === 0)) {
                iziToast.info({
                    title: 'No Data',
                    message: 'No receivers found for the selected type.',
                    position: 'topCenter',
                    timeout: 3000
                });
                return;
            }

            const receiverList = data.data || data;

            // Add options dynamically
            receiverList.forEach(function (item) {
                $rc.append($('<option>', {
                    value: item.Value,
                    text: item.Text,
                    'data-entity': item.EntityCode,
                    'data-amount': item.Amount || 0,
                    'data-paymentpurpose': item.PaymentPurpose || '' // <--- हे जोड
                }));
            });

            // Always reset selection to default
            $rc.prop('selectedIndex', 0);

        }).fail(function () {
            iziToast.error({
                title: 'Error',
                message: 'Failed to fetch receivers.',
                position: 'topCenter'
            });

            $rc.empty().append($('<option>', { value: '', text: '-- Failed to load --', selected: true }));
        });
    });



    // On receiver change



    $(document).on('change', '#ReceiverCode', function () {
        var selected = $('option:selected', this);
        var selectedType = $('#TransactionTypeDropdown').val();
        var amount = selected.data('amount');
        var paymentPurpose = selected.data('paymentpurpose'); // <--- ADD केले
        var entity = selected.data('entity') || '';

        // Amount सेट करणं
        if (amount !== undefined && amount !== null) {
            amount = amount.toString().replace(/,/g, '');
            if (!isNaN(amount) && amount !== '') {
                amount = Number(amount).toFixed(2);
            } else {
                amount = '';
            }
        } else {
            amount = '';
        }
        $('#Amount').val(amount);

        // Payment Purpose सेट करणं
        if (paymentPurpose !== undefined && paymentPurpose !== null) {
            $('#PaymentPurpose').val(paymentPurpose);
        } else {
            $('#PaymentPurpose').val('');
        }

        $('#ExpenseCode, #EventCode').val('');
        if (selectedType === "Vendor") {
            $('#ExpenseCode').val(entity);
        } else if (selectedType === "EventHandler") {
            $('#EventCode').val(entity);
        }

        if ($(this).val()) {
            $(this).removeClass('is-invalid').addClass('is-valid');
        } else {
            $(this).removeClass('is-valid').addClass('is-invalid');
        }
    });

    // ===================== Maintenance Dropdown =====================



    let selectedMember = { MonthsPending: 0, PenaltyAmount: 0, FinalAmount: 0 };

    $(document).on('change', '#MaintenanceDropdown', function () {
        var $selectedOption = $(this).find(':selected');
        var code = $selectedOption.val();

        // Set PaymentPurpose into the textbox
        var paymentPurpose = $selectedOption.data('paymentpurpose') || '';
        $('#PaymentPurpose').val(paymentPurpose);

        if (!code) return;

        $.get('/AccountManager/GetMembersByMaintenance', { maintenanceCode: code }, function (data) {
            var $receiver = $('#MemberDropdown');
            $receiver.empty();

            console.log("🔍 API Response =>", data);

            if (!data.members || data.members.length === 0) {
                $receiver.append($('<option>', { value: '', text: '-- No Members --' }));

                iziToast.info({
                    title: 'Info',
                    message: 'All members have already paid this maintenance.',
                    position: 'topCenter',
                    timeout: 3000
                });
                return;
            }

            // Default option
            $receiver.append($('<option>', { value: '', text: '-- Select Member --' }));

            // Bind members with pending + penalty + months + amount
            $.each(data.members, function (i, item) {
                $receiver.append(
                    $('<option>', {
                        value: item.Value,
                        text: item.Text,              // ✅ ONLY MEMBER NAME
                        'data-pending': item.PendingAmount,
                        'data-penalty': item.PenaltyAmount,
                        'data-months': item.MonthsPending,
                        'data-amount': item.Amount   // ✅ hidden amount
                    })
                );
            });

            // After binding, check dropdown HTML
            console.log("🔍 Dropdown HTML =>", $receiver.html());

        }).fail(function () {
            iziToast.error({
                title: 'Error',
                message: 'Failed to fetch members.',
                position: 'topCenter',
                timeout: 3000
            });
        });
    });


    $(document).on('change', '#MemberDropdown', function () {
        var $selected = $(this).find(':selected');

        var pending = parseFloat($selected.attr('data-pending')) || 0;   // Pending maintenance
        var penalty = parseFloat($selected.attr('data-penalty')) || 0;   // Penalty
        var months = parseInt($selected.attr('data-months')) || 0;
        var amount = parseFloat($selected.attr('data-amount')) || 0;    // Newly bound Amount

        var finalAmount = pending;   // ✅ Still using pending for textbox + object
        var displayTotal = amount + penalty; // ✅ Only for message

        // ✅ Set textbox value
        $('#Amount').val(finalAmount.toFixed(2));

        // ✅ Show message using Amount + Penalty
        if (penalty > 0) {
            $('#AmountValidation')
                .text(
                    " (Maintenance ₹" + amount.toFixed(2) +
                    " + Penalty ₹" + penalty.toFixed(2) + ")")
                .css({ "color": "red", "font-weight": "bold" })
                .show();
        } else {
            $('#AmountValidation')
                .text("₹" + amount.toFixed(2) + " (Maintenance ₹" + amount.toFixed(2) + ")")
                .css({ "color": "green", "font-weight": "bold" })
                .show();
        }

        // ✅ Keep object based on pending
        selectedMember = {
            MonthsPending: months,
            PenaltyAmount: penalty,
            FinalAmount: amount
        };

        console.log("✅ Selected Member =>", selectedMember);
    });


    $(document).on('change', '#MonthDropdown', function () {

        var selectedValue = $.trim($(this).val()); // e.g. "12/2025"

        var $maintenanceDropdown = $('#MaintenanceDropdown');
        var $memberDropdown = $('#MemberDropdown');
        var $amount = $('#Amount');

        $('#AmountValidation').text('').hide();

        // ✅ Reset fields
        $maintenanceDropdown.empty().append($('<option>', { value: '', text: '-- Select Maintenance --' }));
        $memberDropdown.empty().append($('<option>', { value: '', text: '-- Select Member --' }));
        $amount.val('');

        if (!selectedValue) return;

        // ✅ Split Month & Year
        var parts = selectedValue.split('/');
        var month = parts[0]; // "12"
        var year = parts[1];  // "2025"

        // ✅ API Call with Separate Params
        $.get('/AccountManager/GetMaintenanceListByMonth', {
            month: month,
            year: year
        })
            .done(function (data) {

                if (data && data.length > 0) {
                    $.each(data, function (i, item) {
                        $maintenanceDropdown.append(
                            $('<option>', {
                                value: item.Value,
                                text: item.Text,
                                'data-paymentpurpose': item.PaymentPurpose
                            })
                        );
                    });
                } else {
                    iziToast.warning({
                        title: 'No Data',
                        message: 'No maintenance found for selected month.',
                        position: 'topCenter',
                        timeout: 3000
                    });
                }
            })
            .fail(function () {
                iziToast.error({
                    title: 'Error',
                    message: 'Failed to fetch maintenance list.',
                    position: 'topCenter',
                    timeout: 3000
                });
            });

        toggleFields();
    });




    // ===================== File Upload Handling =====================

    $(document).on('change', '#AttachmentInput', function (e) {
        const files = Array.from(e.target.files);

        if (selectedFiles.length + files.length > 3) {
            iziToast.warning({
                title: 'Limit Exceeded',
                message: 'You can upload a maximum of 3 attachments only.',
                position: 'topCenter',
                timeout: 3000
            });

            this.value = ''; // reset input
            return;
        }

        files.forEach(file => {
            const isDuplicate = selectedFiles.some(f => f.name === file.name && f.size === file.size);
            if (!isDuplicate) {
                selectedFiles.push(file);
            }
        });



        if (selectedFiles.length >= 3) {
            $('#AttachmentInput')
                .attr('readonly', true)                // style apply hoil
                .css('pointer-events', 'none');        // click block hoil
            $('.custom-file-label').text('Maximum 3 files uploaded');
        }


        updateFileListUI();
        this.value = ''; // reset input for same file re-selection
    });



    $(document).on('click', '.remove-file', function () {
        const index = $(this).data('index');
        selectedFiles.splice(index, 1);
        updateFileListUI();

        // ✅ Re-enable the file input if files are fewer than 3
        if (selectedFiles.length < 3) {
            $('#AttachmentInput').prop('disabled', false);
            $('.custom-file-label').text('Choose files');
        }
    });



    function updateFileListUI() {
        const fileList = $('#filePreviewList');
        fileList.empty();

        selectedFiles.forEach((file, index) => {
            const fileURL = URL.createObjectURL(file); // Create temporary blob URL

            const item = $(`
            <div class="d-inline-flex align-items-center me-3 mb-2">
                <a href="${fileURL}" target="_blank" class="text-primary text-decoration-underline small" title="${file.name}">
                    ${file.name}
                </a>
                <span class="text-danger ms-1 fw-bold remove-file" style="cursor:pointer;" data-index="${index}">&times;</span>
            </div>
        `);
            fileList.append(item);
        });

        // ✅ Enable/disable file input based on file count
        if (selectedFiles.length >= 3) {
            $('#AttachmentInput')
                .attr('readonly', true)           // readonly set
                .prop('disabled', true)           // disable set
                .css('pointer-events', 'none');   // click block
            $('.custom-file-label').text('Maximum 3 files uploaded');
        } else {
            $('#AttachmentInput')
                .removeAttr('readonly')           // readonly remove
                .prop('disabled', false)          // enable
                .css('pointer-events', 'auto');   // click allow
            $('.custom-file-label').text('Choose files');
        }
    }




    // ===================== Form Submission =====================
    function markInvalid(selector, message) {
        const element = $(selector);

        if (!element.hasClass('is-invalid')) {
            element.addClass('is-invalid');
        }

        // Only add message if it doesn’t already exist
        if (element.next('.validation-message').length === 0) {
            const msg = $('<span class="text-danger validation-message">' + message + '</span>');
            element.after(msg);

            // Remove only the message after 3 seconds
            setTimeout(() => {
                msg.fadeOut(300, () => msg.remove());
            }, 3000);
        }

        // global form flag
        isValid = false;
    }

    $(document).on('submit', '#cashTransactionForm', function (e) {
        e.preventDefault();
        let isValid = true;

        function markInvalid(selector, message) {
            const element = $(selector);
            let validationDiv;
            let targetElement = element;

            if (element.is(':radio')) {
                validationDiv = $("#PaymentModeValidationContainer");
                targetElement = null;
            } else {
                switch (element.attr("id")) {
                    case "TransactionIdDropdown":
                        validationDiv = $("#TransactionIdDropdownValidation");
                        break;
                    case "TransactionTypeDropdown":
                        validationDiv = $("#TransactionTypeDropdownValidation");
                        break;
                    case "ReceiverCode":
                        validationDiv = $("#ReceiverCodeValidation");
                        break;
                    case "ReceiverNameTextbox": // Receiver textbox if "Other"
                        validationDiv = $("#ReceiverTextboxValidation");
                        break;
                    //case "MaintenanceTypeDropdown":
                    //    validationDiv = $("#MaintenanceTypeDropdownValidation");
                    //    break;
                    case "MaintenanceDropdown":
                        validationDiv = $("#MaintenanceDropdownValidation");
                        break;
                    case "MemberDropdown":
                        validationDiv = $("#MemberDropdownValidation");
                        break;
                    case "ChecqueNo":
                        validationDiv = $("#ChequeNoTxtValidation");
                        break;
                    case "BankNameDD":
                        validationDiv = $("#BankNameDDValidation");
                        break;
                    case "MonthDropdown":
                        validationDiv = $("#MonthDropdownValidation");
                        break;
                    default:
                        validationDiv = $("#" + element.attr("id") + "Validation");
                }

                // जर Select2 असेल तर wrapper select कर
                if (element.hasClass("select2-hidden-accessible")) {
                    targetElement = element.next(".select2-container").find(".select2-selection");
                }
            }

            // आधीचे messages clear
            validationDiv.empty();

            // error दाखवा
            const msgElement = $('<span class="text-danger validation-message d-block mt-1">' + message + '</span>');
            validationDiv.append(msgElement);

            if (targetElement) {
                targetElement.removeClass("is-valid").addClass("is-invalid");
            }

            isValid = false;

            // auto remove message
            setTimeout(() => {
                msgElement.fadeOut(400, function () { $(this).remove(); });
            }, 3000);
        }

        // === Clear previous ===
        $('.validation-message').remove();
        $('.is-invalid').removeClass('is-invalid');
        $('#PaymentModeValidationContainer').empty();

        // === Disable browser tooltip ===
        $(this).attr('novalidate', 'novalidate');
        $('input[name="PaymentMode"]').removeAttr('required');

        // === Validate ===
        const transactionType = $('#TransactionIdDropdown').val();
        const paymentMode = $('input[name="PaymentMode"]:checked').val();
        const receiverType = $('#TransactionTypeDropdown').val();
        const maintenanceType = $('#MonthDropdown').val();

        if (!transactionType) markInvalid('#TransactionIdDropdown', 'Select Transaction Type');
        if (!paymentMode) markInvalid('input[name="PaymentMode"]:first', 'Select Payment Mode');

        // ==== CREDIT ====
        if (transactionType === "26") {
            if (!maintenanceType) markInvalid('#MonthDropdown', 'Select Month ');



            if (!$('#MaintenanceDropdown').val()) markInvalid('#MaintenanceDropdown', 'Select Maintenance');
            if (!$('#MemberDropdown').val()) markInvalid('#MemberDropdown', 'Select Member');

            if (paymentMode === "34") {
                if (!$('#ChecqueNo').val()) markInvalid('#ChecqueNo', 'Enter Cheque Number');
                if (!$('#BankNameDD').val()) markInvalid('#BankNameDD', 'Select Bank Name');
                // if (!$('#ChequeDate').val()) markInvalid('#ChequeDate', 'Select Cheque Date'); // optional
                // if (!$('#ifscCode').val()) markInvalid('#ifscCode', 'Enter IFSC Code'); // optional
            }
        }

        // ==== DEBIT ====
        if (transactionType === "27") {
            if (!receiverType) markInvalid('#TransactionTypeDropdown', 'Select Receiver Type');

            if (receiverType === "Other") {
                if (!$('#ReceiverNameTextbox').val()) markInvalid('#ReceiverNameTextbox', 'Enter Receiver Name');
            } else {
                if (!$('#ReceiverCode').val()) markInvalid('#ReceiverCode', 'Select Receiver');
            }

            if (paymentMode === "34") {
                if (!$('#ChecqueNo').val()) markInvalid('#ChecqueNo', 'Enter Cheque Number');
                if (!$('#BankNameDD').val()) markInvalid('#BankNameDD', 'Select Bank Name');
            }
        }

        if (!isValid) {
            $('.is-invalid').first().focus();
            return;
        }

        const form = $(this);
        const formData = new FormData(this);

        // Append manually selected files (max 3) from selectedFiles array
        selectedFiles.forEach((file) => {
            formData.append('Attachments', file); // 'Attachments' should match controller param
        });



        $.ajax({
            url: $(form).attr('action'),
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            beforeSend: function () {

                $('#cashTransactionForm button[type="submit"]').prop('disabled', true).text('Saving...');
            },
            success: function (result) {
                if (result.success) {
                    let message = 'Transaction saved successfully!';
                    let icon = 'success';

                    if (result.transactionId == 26) {
                        // PDF generation
                        $.post('/AccountManager/GenerateReceiptPdf', {
                            transactionCode: result.transactionCode,
                            monthsPending: selectedMember.MonthsPending,
                            penaltyAmount: selectedMember.PenaltyAmount,
                            finalAmount: selectedMember.FinalAmount
                        }, function (res) {
                            if (res.success) {
                                message = 'Transaction saved and receipt PDF generated successfully!';
                            } else {
                                icon = 'warning';
                                message = 'Transaction saved but PDF generation failed.';
                            }

                            swalWithAutoClose('Success', message, icon);
                            $('#CashModal').modal('hide');
                            resetCashForm();

                            $('#refreshBtn').trigger('click');

                        }).fail(function () {
                            swalWithAutoClose('Error', 'Error during receipt PDF generation.', 'error');
                            $('#CashModal').modal('hide');
                            resetCashForm();
                        });

                    } else if (result.transactionId == 27 && result.receiverType === "Worker" && result.receiverCode) {
                        const pdfWindow = window.open('', '_blank');

                        $.post('/AccountManager/GenerateSalarySlip', { transactionCode: result.transactionCode }, function (res) {
                            let icon = 'success';
                            let message = '';

                            if (res.success && res.filePath) {
                                message = 'Transaction saved and salary slip generated!';
                                pdfWindow.location.href = res.filePath;
                            } else {
                                pdfWindow.close();
                                icon = 'warning';
                                message = 'Transaction saved, but salary slip generation failed: ' + (res.message || 'Unknown error.');
                            }

                            swalWithAutoClose('Success', message, icon);
                            $('#CashModal').modal('hide');
                            resetCashForm();
                            $('#refreshBtn').trigger('click');

                        }).fail(function () {
                            pdfWindow.close();
                            swalWithAutoClose('Error', 'Error during salary slip generation.', 'error');
                            $('#CashModal').modal('hide');
                            resetCashForm();
                        });

                    } else {
                        swalWithAutoClose('Success', message, 'success');
                        $('#CashModal').modal('hide');
                        resetCashForm();
                        $('#refreshBtn').trigger('click');

                    }
                } else {
                    $('#modalBody').html(result);

                }
            },
            error: function () {
                swalWithAutoClose('Error', 'An error occurred while saving the transaction.', 'error');


            },
            complete: function () {

                $('#cashTransactionForm button[type="submit"]').prop('disabled', false).text('Save');
            }
        });
    });


    function swalWithAutoClose(title, text, icon) {
        swal({
            title: title,
            text: text,
            icon: icon,
            buttons: false, // hide OK button
            timer: 3000     // auto close after 3 seconds
        });
    }



    $('#CashModal .select2').on('change', function () {
        $(this).trigger('input'); // वरच्या logic ला चालू करेल
    });

    $(document).on('input change', '#CashModal input:visible, #CashModal select:visible, #CashModal textarea:visible', function () {
        const $field = $(this);

        // Radio


        // Select2
        if ($field.hasClass('select2-hidden-accessible')) {
            const $select2Box = $field.next('.select2-container').find('.select2-selection');
            if ($field.val() && $field.val().length > 0) {
                $select2Box.removeClass('is-invalid').addClass('is-valid');
                $("#" + $field.attr("id") + "Validation").empty();
            }
            return;
        }

        // Normal fields
        if ($field.val().trim() !== '') {
            $field.removeClass('is-invalid').addClass('is-valid');
            $("#" + $field.attr("id") + "Validation").empty();
        }
    });


    $(document).on('input', '#CashModal input:visible, #CashModal textarea:visible', function () {
        const $field = $(this);
        if ($field.val().trim() !== '') {
            $field.removeClass('is-invalid').addClass('is-valid');
            $("#" + $field.attr("id") + "Validation").empty();
        } else {
            $field.removeClass('is-valid').addClass('is-invalid');
        }
    });

    function resetCashForm() {
        let form = $('#cashTransactionForm');

        form[0].reset();

        // Clear custom file input
        let oldInput = $('#AttachmentInput');
        let newInput = oldInput.clone().val('');
        oldInput.replaceWith(newInput);

        // Reset file label and preview
        $('.custom-file-label').text('Choose files');
        $('#fileListPreview, #SelectedFilesContainer, #filePreviewList').empty();

        // Clear validation
        form.find('.is-invalid').removeClass('is-invalid');
        form.find('.text-danger').remove();

        // Reset selects
        form.find('select').each(function () {
            $(this).val($(this).find('option:first').val()).trigger('change');
        });

        // Hide conditional fields
        $('#ChequeNoDiv, #BankNameDiv, #FlatNoDiv, #ReceiverTypeDiv, #ReceiverDiv, #MaintenanceDiv, #PaymentPurposeDiv').addClass('d-none');

        // Clear selected files array
        selectedFiles = [];

        // Rebind file input listener
        $('#AttachmentInput').on('change', function (e) {
            selectedFiles = Array.from(e.target.files);
            $('.custom-file-label').text(selectedFiles.map(f => f.name).join(', '));
        });
    }




    // ===================== Reset Modal on Close =====================
    $(document).on('click', '#closeModalBtn', function () {
        const $form = $('#cashTransactionForm');
        if ($form.length) {
            $form[0].reset();
        }
        $form.find('select').each(function () {
            $(this).val($(this).find('option:first').val());
        });

        $('#CreditSection, #TypeDropdownDiv, #ReceiverDropdownDiv, #ReceiverTextboxDiv, .cheque-fields').hide();
        $('.text-danger').remove();
        $('.is-invalid').removeClass('is-invalid');
        $('#fileList').empty();
        $('#AttachmentPath').val('');
        selectedFiles = [];
    });

    $('#TransactionIdDropdown').on('change', function () {
        // trigger reflow/repaint if needed
        $('.modal').trigger('resize'); // optional
    });




    function resetModalForm() {
        const $form = $('#cashTransactionForm');

        // Mark we are resetting so toggleFields doesn't run
        let isResetting = true;

        // Reset form inputs
        $form[0].reset();
        $form.find('select').val('').trigger('change');

        // Clear dropdowns manually
        $('#TransactionId').val('').trigger('change');
        $('#PaymentMode').val('').trigger('change');
        $('#ReceiverCode').val('');

        // Clear file input and custom logic
        selectedFiles = [];
        $('#AttachmentInput').val('');
        renderFileList();

        // Clear validation errors
        $('.text-danger').remove();
        $('.is-invalid').removeClass('is-invalid');

        // Hide conditional sections manually
        $('#CreditSection').hide();
        $('#ReceiverDropdownDiv').hide();
        //  $('#ReceiverDropdownDiv .required-star').hide();
        $('#TypeDropdownDiv').hide();
        $('.cheque-fields').hide();
        $('#selectedFileName').hide();

        isResetting = false;

        // Now re-trigger logic safely
        toggleFields();
        $('.required-star').show(); // <- This line brings back the red asterisks
    }


    $(document).on('click', '#clearModalBtnSM', function () {
        // ✅ फक्त cashTransactionForm reset
        $('#cashTransactionForm')[0].reset();
        console.log("Clear button clicked");

        const $form = $('#cashTransactionForm');

        // Reset dropdowns
        $form.find('select').val('').trigger('change');

        // Uncheck radios/checkboxes
        $form.find('input[type="radio"], input[type="checkbox"]').prop('checked', false);

        // Remove validation
        $form.find('.is-invalid').removeClass('is-invalid');
        $form.find('span.field-validation-error')
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .empty();
        $form.find('.text-danger').text(''); // div remove नाही, फक्त text clear

        // Clear files
        selectedFiles.length = 0;
        updateFileListUI();
        $('#AttachmentInput').val('').prop('disabled', false);
        $('.custom-file-label').text('Choose files');

        // Hide sections
        $('#CreditSection, #ChequeSection, #AttachFileDiv, #PaymentPurposeDiv, #ReceiverDropdownDiv, #ReceiverTextboxDiv, #TypeDropdownDiv, .cheque-fields').hide();

        // Reset dropdown placeholders
        $('#MemberDropdown').empty().append($('<option>', {
            value: '',
            text: '-- Select Member --'
        })).val('').trigger('change');

        $('#MaintenanceDropdown').empty().append($('<option>', {
            value: '',
            text: '-- Select Maintenance --'
        })).val('').trigger('change');

        toggleFields();
    });

    $('#AttachmentInput').on('change', function () {
        if (this.files.length > 0) {
            $(this).removeClass('is-invalid').addClass('is-valid');
        } else {
            $(this).removeClass('is-valid is-invalid');
        }
    });



    //$('#BankNameDD').on('change', function () {
    //    var selectedText = $("#BankNameDD option:selected").text();
    //    $('#BankName').val(selectedText);
    //});

    $(document).on('change', '#TransactionIdDropdown', function () {
        clearAmountValidation();   // ✅ Credit → Debit switch वर red text clear
    });

    $(document).on('click', '#clearModalBtnSM', function () {
        clearAmountValidation();

        $('#bankBalanceWrapper').addClass('force-hide').hide();

        $('#NetBankBalance').text("₹ 0.00");
        $('#BankNameDD').prop('selectedIndex', 0).trigger('change.select2');

    });

    function clearAmountValidation() {
        $('#AmountValidation').text('').hide();   // ✅ Red text hide
        $('#Amount').removeClass('is-invalid is-valid'); // ✅ Borders clear
        $('#Amount').val('0.00');  // ✅ Amount 0.00 set
    }




    $(document).on('input', '#ifscCode', function () {
        var $ifscInput = $(this);
        var ifsc = $ifscInput.val().trim().toUpperCase();

        $ifscInput.val(ifsc);
        var isValidFormat = /^[A-Z]{4}0[A-Z0-9]{6}$/.test(ifsc);

        if (ifsc.length === 11 && isValidFormat) {
            $.ajax({
                url: 'https://ifsc.razorpay.com/' + ifsc,
                method: 'GET',
                success: function (response) {
                    var bankNameFromApi = response.BANK || '';

                    if (bankNameFromApi) {
                        // आपल्या dropdown मध्ये match शोधणे
                        var matchFound = false;
                        $('#BankNameDD option').each(function () {
                            if ($(this).text().toLowerCase().includes(bankNameFromApi.toLowerCase())) {
                                $('#BankName').val($(this).val());
                                matchFound = true;
                                return false; // break
                            }
                        });

                        if (!matchFound) {
                            Swal.fire({
                                icon: 'warning',
                                title: 'Bank Not Found',
                                text: 'The bank from IFSC code is not in the dropdown list.'
                            });
                        }

                        $ifscInput.removeClass('is-invalid').addClass('is-valid');
                    }
                },
                error: function () {
                    $ifscInput.removeClass('is-valid').addClass('is-invalid');
                    Swal.fire({
                        icon: 'error',
                        title: 'Invalid IFSC',
                        text: 'Could not fetch bank details. Please check the IFSC code.'
                    });
                }
            });
        } else {
            $ifscInput.removeClass('is-valid').addClass('is-invalid');
        }
    });

    const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === 1) {
                    const $node = $(node);

                    if ($node.hasClass("field-validation-error") || $node.hasClass("text-danger")) {
                        setTimeout(function () {
                            $node.fadeOut('slow', function () { $(this).remove(); });
                            // $('.is-invalid').removeClass('is-invalid'); ❌ Don't remove red border
                        }, 3000);
                    }
                }
            });
        });


        const modalBody = document.querySelector('.modal-body');
        if (modalBody) {
            observer.observe(modalBody, { childList: true, subtree: true });
        }

        // Initial fade-out for server-side messages
        setTimeout(function () {
            $('.field-validation-error, .text-danger').fadeOut('slow', function () {
                $(this).remove();
            });
            // $('.is-invalid').removeClass('is-invalid'); ❌ Don't remove red border
        }, 3000);
    });



    $(document).on('input', '#ChecqueNo', function () {

        var value = $(this).val();

        // ✅ Only Alphabets + Numbers allowed
        var cleanValue = value.replace(/[^a-zA-Z0-9]/g, '');

        if (value !== cleanValue) {
            $(this).val(cleanValue);

            $('#ChequeNoTxtValidation')
                .text('Only letters and numbers are allowed. No symbols.')
                .show();
        }
        else {
            $('#ChequeNoTxtValidation').text('').hide();
        }
    });




    $("#BankNameDD option").each(function () {

        var text = $(this).text();

        // ✅ Amount वेगळा काढून data-balance मध्ये टाक
        var match = text.match(/₹\s?([\d,]+\.\d{2})/);

        if (match && match[1]) {
            var balance = match[1].replace(/,/g, '');

            // ✅ data attribute मध्ये balance store
            $(this).attr("data-balance", balance);

            // ✅ Dropdown text मधून amount काढ
            var cleanText = text.replace(/\(₹.*?\)/g, "").trim();
            $(this).text(cleanText);
        }
    });



    // ✅ 2) Bank select केल्यावर NetBankBalance अपडेट
    // ✅ Remove duplicate bindings (VERY IMPORTANT)
    $(document).off('change', '#BankNameDD');

    $(document).on('change', '#BankNameDD', function () {

        var bankCode = $(this).val();
        var bankName = $("#BankNameDD option:selected").text();

        console.log("🔹 BankCode:", bankCode);

        $('input[name="BankName"]').val(bankName);

        // ✅ ✅ STRICT HIDE CONDITION (0 index + empty + Select2 ghost value)
        if (
            $(this).prop('selectedIndex') === 0 ||
            bankCode === "" ||
            bankCode === null ||
            bankCode === undefined
        ) {


            $('#bankBalanceWrapper').hide().addClass('d-none');
            $('#NetBankBalance').text("₹ 0.00");

            console.log("✅ Visible after hide:", $('#bankBalanceWrapper').is(':visible'));
            return;
        }

        // ✅ ✅ STRICT SHOW CONDITION
        if (window.bankBalances && window.bankBalances.hasOwnProperty(bankCode)) {

            var balance = window.bankBalances[bankCode];

            var formatted = parseFloat(balance).toLocaleString('en-IN', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });

            $('#NetBankBalance').text("₹ " + formatted);

            $('#bankBalanceWrapper')
                .removeClass('d-none')
                .css('display', 'flex')
                .show();


        }
        else {


            $('#bankBalanceWrapper').hide().addClass('d-none');
            $('#NetBankBalance').text("₹ 0.00");
        }
    });




});