// =============================================
// CALENDAR SYSTEM
// =============================================

// Initialize calendar when modal is shown
$('#calendarModal').on('shown.bs.modal', function () {
    initCalendar();
});

// Calendar close button handler
$('#closeCalendarModal').click(function () {
    $('#calendarModal').modal('hide');
}).css({
    'background': 'transparent',
    'border': 'none',
    'outline': 'none',
    'box-shadow': 'none'
});






$('#closeEventDetails').click(function () {
    $('#eventDetailsModal').modal('hide');
}).css({
    'background': 'transparent',
    'border': 'none',
    'outline': 'none',
    'box-shadow': 'none'
});






// Initialize FullCalendar
function initCalendar() {
    $('#myCalendar').fullCalendar({
        header: {
            left: 'prev,next today',
            center: 'title',
            right: 'month' // Only show month view for better mobile experience
        },
        defaultView: 'month',
        fixedWeekCount: false,
        height: 'parent',
        contentHeight: 'auto',
        aspectRatio: 1.35, // Slightly taller aspect ratio
        events: function (start, end, timezone, callback) {
            // Load both events and meetings
            $.when(
                $.get('@Url.Action("GetCalendarEvents", "AccountManager")'),
                $.get('@Url.Action("GetCalendarMeetings", "AccountManager")')
            ).then(function (eventsResponse, meetingsResponse) {
                var events = eventsResponse[0] || [];
                var meetings = meetingsResponse[0] || [];

                // Process meetings data
                var processedMeetings = meetings.map(function (m) {
                    return {
                        title: m.title,
                        start: m.start,
                        MeetingCode: m.MeetingCode,
                        allDay: true,
                        className: 'fc-event-meeting'
                    };
                });

                // Process events data
                var processedEvents = events.map(function (e) {
                    return {
                        title: e.title,
                        start: e.start,
                        EventCode: e.EventCode,
                        allDay: true,
                        className: 'fc-event-event'
                    };
                });

                // Combine and display events
                callback(processedEvents.concat(processedMeetings));
            }).fail(function (error) {
                console.error("Error loading events:", error);
                callback([]);
            });
        },

        eventRender: function (event, element) {
            // Style calendar events
            element.css({
                'font-size': '0.75em',
                'padding': '1px 3px',
                'white-space': 'nowrap',
                'overflow': 'hidden',
                'text-overflow': 'ellipsis',
                'border-radius': '2px',
                'margin-bottom': '1px',
                'cursor': 'pointer'
            });

            // Different styles for meetings vs events
            if (event.MeetingCode) {
                element.css({
                    'background': 'linear-gradient(135deg, #3498db, #2980b9)',
                    'border-left': '3px solid #1d6fa5',
                    'color': 'white'
                });
            } else {
                element.css({
                    'background': 'linear-gradient(135deg, #2ecc71, #27ae60)',
                    'border-left': '3px solid #1e8449',
                    'color': 'white'
                });
            }
        },
        eventClick: function (event) {
            showEventDetails(event);
        },
        dayRender: function (date, cell) {
            cell.empty();
            var dayNumber = $('<a class="fc-day-number"></a>').text(date.date());
            cell.append(dayNumber);

            if (date.isSame(moment(), 'day')) {
                cell.css('background-color', 'rgba(0, 123, 255, 0.1)');
                dayNumber.css({
                    'font-weight': 'bold',
                    'color': '#fff',
                    'background': '#007bff',
                    'border-radius': '50%',
                    'width': '22px',
                    'height': '22px',
                    'text-align': 'center',
                    'line-height': '22px',
                    'display': 'inline-block'
                });
            }
        }
    });
}

// Show event details in modal
function showEventDetails(event) {
    var url = event.EventCode ?
        '@Url.Action("GetEventDetailsByCode", "AccountManager")' :
        '@Url.Action("GetMeetingDetailsByCode", "AccountManager")';
    var data = event.EventCode ?
        { eventCode: event.EventCode } :
        { meetingCode: event.MeetingCode };

    $.get(url, data, function (html) {
        $('#eventDetailsBody').html(html);
        // Show modal with strict close behavior
        $('#eventDetailsModal').modal({
            backdrop: 'static',
            keyboard: false
        }).modal('show');
    });
}