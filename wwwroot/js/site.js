$(document).ready(function () {
    $(".nav-link").click(function () {
        $(".nav-link").removeClass("active");
        $(this).addClass("active");

        var tab = $(this).text().trim();
        switchTab(tab);
    });
});

function switchTab(tab) {
    $(".tab-content").hide();
    $("#" + tab + "Content").show();
}
