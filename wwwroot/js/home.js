
    document.addEventListener("DOMContentLoaded", function () {
    const buttons = document.querySelectorAll(".btn");
    buttons.forEach(btn => {
    btn.addEventListener("click", function (e) {
    if (!isLoggedIn) {
    e.preventDefault(); // chặn chuyển trang
    document.getElementById("loginModal").style.display = "flex";
}
});
});
});
