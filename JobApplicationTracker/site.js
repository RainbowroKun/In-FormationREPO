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
                alert("Unable to log out.");
            }
        },

        error: function (xhr) {
            console.log(xhr.responseText);
            alert("Unable to connect to the logout service.");
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
            ShowError(
                "Unable to load your account information."
            );
        }
    });
}

function ShowError(message) {

    $("#formMessage")
        .removeClass("successMessage")
        .addClass("errorMessage")
        .text(message);
}

function ShowSuccess(message) {

    $("#formMessage")
        .removeClass("errorMessage")
        .addClass("successMessage")
        .text(message);
}