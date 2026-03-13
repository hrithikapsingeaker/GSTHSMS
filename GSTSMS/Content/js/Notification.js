

    let removedNotificationIds = [];
    let firstLoad = true;

    // Prevent dropdown from closing on clicks inside notification's dropdown-menu or on remove buttons
    $(document).on('click', '#notification-body .dropdown-menu, #notification-body .btn-remove-notification', function (e) {
        e.stopPropagation();
        e.preventDefault(); // Prevent Bootstrap dropdown closing only for notifications
    });

    function getQueryParam(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }

    // Parse .NET or ISO date safely → returns Date object
    function parseDotNetDate(dotNetDate) {
        if (!dotNetDate) return null;
        if (typeof dotNetDate === "string" && dotNetDate.includes("/Date(")) {
            const timestamp = parseInt(dotNetDate.replace(/[^0-9]/g, ""), 10);
            return new Date(timestamp);
        }
        return new Date(dotNetDate);
    }

    // Format parsed date to time string (hh:mm AM/PM)
    function formatDotNetDate(dotNetDate) {
        const date = parseDotNetDate(dotNetDate);
        if (!date || isNaN(date)) return "";
        return date.toLocaleTimeString("en-US", {
            hour: "2-digit",
            minute: "2-digit",
            hour12: true
        });
    }

    // Categorize notification types
    function categorizeNotification(item) {
        const text =
            ((item.Name ? item.Name : "") + " " + (item.Description ? item.Description : "")).toLowerCase();
        const maintenanceKeywords = ["maintenance", "maintenance fee", "maintenance amount"];
        const eventKeywords = ["event", "created", "budget approved"];
        const profileKeywords = ["password change", "security", "login", "request", "password"];
        if (maintenanceKeywords.some(k => text.includes(k))) return "maintenance";
        if (eventKeywords.some(k => text.includes(k))) return "event";
        if (profileKeywords.some(k => text.includes(k))) return "profile";
        return "other";
    }

    // Get URL for notification type
    function getNotificationUrl(item) {
        const category = categorizeNotification(item);
        switch (category) {
            case "maintenance":
                return "/accountmanager/MaintenanceManagementSY";
            case "event":
                return "/accountmanager/EventList";
            case "profile":
                return "/accountmanager/ChangePassword";
            default:
                return "/Notifications";
        }
    }

    // Render a single notification block
    function renderNotification(item) {
        const targetUrl = getNotificationUrl(item);
        return `
        <div class="notification-item"
             data-id="${item.NotificationId}"
             data-url="${targetUrl}">
            <div class="icon"><i class="fas fa-bell"></i></div>
            <div class="text">
                <b>${item.Name}</b>
                <div class="description">${item.Description || ""}</div>
                <div class="time text-muted small">${formatDotNetDate(item.CreatedDate)}</div>
            </div>
            <button class="btn-remove-notification" data-id="${item.NotificationId}">&times;</button>
        </div>`;
    }

    // Update notifications UI grouped and sorted
    function updateNotifications(data) {
        const $body = $("#notification-body");
        const $count = $("#notification-count");

        // Filter out removed locally
        data = data.filter(item => !removedNotificationIds.includes(item.NotificationId));

        if (data.length === 0) {
            $body.html('<div class="text-center text-muted py-3">No new notifications</div>');
            $count.text("0").show();   // Always show 0 instead of hiding
            toggleMarkAllReadButton();
            return;
        }

        // Categorize notifications
        let maintenance = [], events = [], profiles = [], others = [];
        data.forEach(item => {
            const category = categorizeNotification(item);
            if (category === "maintenance") maintenance.push(item);
            else if (category === "event") events.push(item);
            else if (category === "profile") profiles.push(item);
            else others.push(item);
        });

        // Sort descending by date
        const sortByDateDesc = arr => arr.sort(
            (a, b) => parseDotNetDate(b.CreatedDate) - parseDotNetDate(a.CreatedDate)
        );
        maintenance = sortByDateDesc(maintenance);
        events = sortByDateDesc(events);
        profiles = sortByDateDesc(profiles);
        others = sortByDateDesc(others);

        // Preserve scroll position
        const scrollPos = $body.scrollTop();

        // Build HTML
        let html = "";
        if (maintenance.length) {
            html += '<div class="notif-section-header">Maintenance</div>';
            maintenance.forEach(item => { html += renderNotification(item); });
        }
        if (events.length) {
            html += '<div class="notif-section-header">Events</div>';
            events.forEach(item => { html += renderNotification(item); });
        }
        if (profiles.length) {
            html += '<div class="notif-section-header">Profile & Security</div>';
            profiles.forEach(item => { html += renderNotification(item); });
        }
        if (others.length) {
            html += '<div class="notif-section-header">Others</div>';
            others.forEach(item => { html += renderNotification(item); });
        }

        $body.html(html);
        $body.scrollTop(scrollPos);

        // Update badge count
        $count.text(data.length).show();
        toggleMarkAllReadButton();
    }

    // Load notifications from server via AJAX
    function loadNotifications() {
        const $body = $("#notification-body");
        if (firstLoad) $body.html('<div class="text-center text-muted py-3">Loading...</div>');
        $.get("/AccountManager/GetNotificationsPDV", function (data) {
            firstLoad = false;
            updateNotifications(data);
        });
    }

    // Open notification link when clicking notification (excluding remove button)
    $(document).on('click', '.notification-item', function (e) {
        if ($(e.target).hasClass('btn-remove-notification')) return;
        const url = $(this).data("url");
        if (url && url !== "#") {
            window.location.href = url;
        }
    });

    // Remove notification button click handler with smooth slide up and reload count
    $(document).on('click', '.btn-remove-notification', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const id = $(this).data("id");
        const $item = $(`.notification-item[data-id="${id}"]`);
        $.post('/AccountManager/MarkNotificationReadPDV', { id: id }, function () {
            removedNotificationIds.push(id);
            $item.slideUp(300, function () {
                $item.remove();
                updateNotificationCount();
                if ($('.notification-item').length === 0) {
                    $('#notification-body').html('<div class="text-center text-muted py-3">No new notifications</div>');
                    $('#notification-count').text("0").show();   // show 0 when empty
                }
                // Reopen dropdown toggle after removal to keep panel open
                $('.notification-toggle').dropdown('show');
            });
        });
    });

    // Mark all notifications as read button click
    $('#markAllRead').click(function (e) {
        e.preventDefault();
        $.post('/AccountManager/MarkNotificationsPDV', function (response) {
            if (response.success) {
                removedNotificationIds = [];
                $('#notification-body').html('<div class="text-center text-muted py-3">No new notifications</div>');
                $('#notification-count').text("0").show();   // show 0 instead of hiding
            } else {
                alert("Failed to mark all as read. Please try again.");
            }
        }).fail(function () {
            alert("Error occurred while marking notifications as read.");
        });
    });

    // Update notification count badge
    function updateNotificationCount() {
        const total = $('.notification-item').length;
        const $count = $('#notification-count');
        $count.text(total).show();   // Always show, even if 0
        toggleMarkAllReadButton();
    }

    // Initialize notifications load and periodic refresh
    loadNotifications();
    setInterval(loadNotifications, 10000);

    // Sidebar toggle handler
    $('[data-toggle="sidebar"]').on('click', function (e) {
        e.preventDefault();
        $('body').toggleClass('sidebar-mini');
    });

    function toggleMarkAllReadButton() {
        const hasNotifications = $('.notification-item').length > 0;
        const $btn = $('#markAllRead');
        if (hasNotifications) {
            $btn.prop('disabled', false);
        } else {
            $btn.prop('disabled', true);
        }
    }



