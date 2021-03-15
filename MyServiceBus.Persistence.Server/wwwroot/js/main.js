var Main = /** @class */ (function () {
    function Main() {
    }
    Main.tickTimer = function () {
        var _this = this;
        if (!this.bodyElement)
            this.bodyElement = document.getElementsByTagName("BODY")[0];
        if (!this.signalRConnection) {
            this.initSignalR();
        }
        if (this.signalRConnection.connection.connectionState != 1) {
            this.signalRConnection.start().then(function () {
                _this.connected = true;
            })
                .catch(function (err) { return console.error(err.toString()); });
        }
    };
    Main.readDictDifferenceContract = function (contract) {
        return {
            insert: new Dictionary(contract['i']),
            update: new Dictionary(contract['u']),
            delete: contract['d']
        };
    };
    Main.initSignalR = function () {
        var _this = this;
        this.signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl("/monitoringhub")
            .build();
        this.signalRConnection.on("init", function (data) {
            document.title = data.version;
        });
        this.signalRConnection.on("topics", function (data) {
            _this.bodyElement.innerHTML = HtmlRenderer.renderTopics(data);
        });
        this.signalRConnection.on("topics-info", function (contract) {
            var data = new Dictionary(contract);
            data.iterate(function (topicId, topicInfo) {
                var el = document.getElementById(topicId + '-msg-id');
                if (el)
                    el.innerHTML = topicInfo.messageId.toString();
                el = document.getElementById(topicId + '-write-pos');
                if (el)
                    el.innerHTML = topicInfo.writePosition.toString();
                el = document.getElementById(topicId + '-write-queue-size');
                if (el)
                    el.innerHTML = topicInfo.writePosition.toString();
            });
        });
    };
    Main.connected = true;
    return Main;
}());
window.setInterval(function () {
    Main.tickTimer();
}, 1000);
$('document').ready(function () {
    Main.tickTimer();
});
//# sourceMappingURL=main.js.map