function FormatDate(value) {

    if (!value) {return "—";}

    var date = new Date(value);

    if (isNaN(date.getTime())) {return value;}

    return date.toLocaleDateString();
}

function EscapeHtml(value) {
    return $("<div>").text(value || "").html();
}

function ShowPageMessage(message, messageType) {

    var pageMessage = $("#pageMessage");

    pageMessage
        .removeClass("errorMessage successMessage")
        .text(message);

    if (messageType == "success") {
        pageMessage.addClass("successMessage");
    }
    else if (messageType == "error") {
        pageMessage.addClass("errorMessage");
    }
}


function ClearPageMessage() {

    $("#pageMessage")
        .removeClass("errorMessage successMessage")
        .text("");
}

function LogOut() {

    $.ajax({
        type: "POST",
        url: "ProjectServices.asmx/LogOut",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",

        success: function (msg) {

            if (msg.d == "Success") {
                window.location.href = "login.html";
            }
            else {
                ShowPageMessage("Unable to log out.", "error");
            }
        },

        error: function (xhr) {
            console.log(xhr.responseText);
            ShowPageMessage("Unable to connect to the logout service.", "error");
        }
    });
}

function LoadAccountInformation() {

    $.ajax({
        type: "POST",
        url: "ProjectServices.asmx/GetCurrentUserRole",
        data: "{}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",

        success: function (msg) {

            if (msg.d == "Not Logged In") {
                window.location.href = "home-page.html";
                return;
            }

            $("#welcomeMessage").text(msg.d);
            $("#signedInArea").show();
        },

        error: function () {
            ShowPageMessage(
                "Unable to load your account information.", "error"
            );
        }
    });
}

function GoBackOrDefault(defaultPage) {
    var parameters = new URLSearchParams(window.location.search);
    var source = parameters.get("source");

    if (source == "reminders") {
        window.location.href = "reminder-interviews.html";
        return;
    }

    if (source == "recruiters") {
        window.location.href = "view-recruiters.html";
        return;
    }

    if (source == "search") {
        window.location.href = "search.html";
        return;
    }

    if (source == "applications") {
        window.location.href = "view-applications.html";
        return;
    }

    window.location.href = defaultPage;
}

function GetNavigationSource() {
    var parameters = new URLSearchParams(window.location.search);
    var source = parameters.get("source");

    if (source == "reminders") {
        return {
            Page: "reminder-interviews.html",
            Label: "Reminders & Interviews"
        };
    }

    if (source == "search") {
        return {
            Page: "search.html",
            Label: "Search & Filter"
        };
    }

    if (source == "recruiters") {
        return {
            Page: "view-recruiters.html",
            Label: "Recruiters"
        };
    }

    if (source == "documents") {
        return {
            Page: "view-documents.html",
            Label: "Documents"
        };
    }

    if (source == "applications") {
        return {
            Page: "view-applications.html",
            Label: "Applications"
        };
    }

    if (source == "home") {
        return {
            Page: "home-page.html",
            Label: "Home"
        };
    }

    return null;
}

function UpdatePageNavigation() {
    var navigationSource = GetNavigationSource();

    if (!navigationSource) {
        return;
    }

    $("#sourceBreadcrumb")
        .attr("href", navigationSource.Page)
        .text(navigationSource.Label);
}