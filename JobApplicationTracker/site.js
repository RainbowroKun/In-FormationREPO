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
                window.location.href = "home-page.html";
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