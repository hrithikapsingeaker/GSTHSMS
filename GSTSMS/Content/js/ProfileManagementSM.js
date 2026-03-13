///////////////////////////////////////////////////////////////////// Profile Shruti ////////////////////
// Handle Change Password navigation
$(document).ready(function () {
$(document).on('click', '#btnChangePassword', function () {
    window.location.href = '@Url.Action("ChangePassword", "AccountManager")';
});



$(document).on('click', '#btnLogout', function (e) {
    e.preventDefault();

    swal({
        title: "Logout Confirmation",
        text: "Are you sure you want to log out?",
        icon: "warning",
        buttons: ["Cancel", "Yes, Logout"],
        dangerMode: true,
    }).then((willLogout) => {
        if (willLogout) {
            sessionStorage.clear();
            localStorage.clear();
            window.location.href = '@Url.Action("Logout", "Account")';
        }
    });
});



// Prevent back navigation after logout
(function () {
    if (window.history && window.history.pushState) {
        window.history.pushState(null, null, location.href);
        window.onpopstate = function () {
            location.replace('@Url.Action("LoginRK", "Account")');
        };
    }

    // Reload if returning via bfcache
    window.addEventListener('pageshow', function (event) {
        if (event.persisted || performance.getEntriesByType("navigation")[0]?.type === "back_forward") {
            location.reload();
        }
    });
})();

// Redirect to login if session expired

    var sessionEmail = '@(Session["Email"] ?? "")'.trim();
    if (!sessionEmail) {
        window.location.href = '@Url.Action("LoginRK", "Account")';
    }
});





