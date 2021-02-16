var Main = /** @class */ (function () {
    function Main() {
    }
    Main.request = function () {
        var _this = this;
        if (this.requesting)
            return;
        $.ajax({ url: '/api/status' })
            .then(function (r) {
            if (!_this.bodyElement)
                _this.bodyElement = document.getElementsByTagName("BODY")[0];
            _this.requesting = false;
            _this.bodyElement.innerHTML = HtmlRenderer.renderMainContent(r);
        })
            .fail(function () {
            _this.requesting = false;
        });
    };
    Main.requesting = false;
    return Main;
}());
window.setInterval(function () {
    Main.request();
}, 1000);
Main.request();
//# sourceMappingURL=main.js.map