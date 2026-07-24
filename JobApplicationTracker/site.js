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
                alert("Unable to log out.");
            }
        },

        error: function (xhr) {
            console.log(xhr.responseText);
            alert("Unable to connect to the logout service.");
        }
    });
}