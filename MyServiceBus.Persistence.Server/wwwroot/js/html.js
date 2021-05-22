var HtmlRenderer = /** @class */ (function () {
    function HtmlRenderer() {
    }
    HtmlRenderer.renderQueuesTableContent = function (queues) {
        var result = '';
        for (var _i = 0, queues_1 = queues; _i < queues_1.length; _i++) {
            var queue = queues_1[_i];
            result += '<div><b>' + queue.queueId + '</b></div>';
            for (var _a = 0, _b = queue.ranges; _a < _b.length; _a++) {
                var range = _b[_a];
                result += '<div style="margin-left: 10px">' + range.fromId + ' - ' + range.toId + '</div>';
            }
            result += '<hr/>';
        }
        return result;
    };
    HtmlRenderer.renderLoadedPagesContent = function (topics) {
        var result = '';
        for (var _i = 0, topics_1 = topics; _i < topics_1.length; _i++) {
            var topic = topics_1[_i];
            var badges = '';
            for (var _a = 0, _b = topic.loadedPages; _a < _b.length; _a++) {
                var loadedPage = _b[_a];
                if (loadedPage.hasSkipped) {
                    badges += '<span class="badge badge-danger" style="margin-left: 5px">' + loadedPage.pageId + '</span>';
                }
                else {
                    badges += '<span class="badge badge-success" style="margin-left: 5px">' + loadedPage.pageId + '</span>';
                }
                badges += '<div><div class="progress">' +
                    '<div class="progress-bar" role="progressbar" style="width: ' + loadedPage.percent + '%;" aria-valuenow="' + loadedPage.percent + '" aria-valuemin="0" aria-valuemax="100">' + loadedPage.percent + '%</div>';
                '</div>`</div>';
            }
            var activePagesBadges = '';
            for (var _c = 0, _d = topic.activePages; _c < _d.length; _c++) {
                var activePage = _d[_c];
                activePagesBadges += '<span class="badge badge-warning" style="margin-left: 5px">' + activePage + '</span>';
            }
            var queuesContent = this.renderQueuesTableContent(topic.queues);
            result += '<tr style="font-size: 12px">' +
                '<td>' + topic.topicId + '<div>WritePos: ' + topic.writePosition + '</div>' +
                '<div>Active:</div>' + activePagesBadges + '<hr/><div>Loaded:</div>' + badges + '</td>' +
                '<td>' + queuesContent + '</td>' +
                '<td><div>Current Id:' + topic.messageId + '</div><div>Last Saved:' + topic.savedMessageId + '</div></td>' +
                '</tr>';
        }
        return result;
    };
    HtmlRenderer.renderMainTable = function (topics) {
        var content = this.renderLoadedPagesContent(topics);
        return '<table class="table table-striped"><tr><th>Topic</th><th>Queues</th><th>MessageId</th></tr>' + content + '</table>';
    };
    HtmlRenderer.renderAdditionalFields = function (r) {
        return '<div>Queue SnapshotId: ' + r.queuesSnapshotId + '</div>';
    };
    HtmlRenderer.renderActiveOperations = function (header, activeOperations) {
        var result = '<h1>' + header + '</h1><table class="table table-striped"><tr><th>Topic</th><th>Action</th></tr>';
        for (var _i = 0, activeOperations_1 = activeOperations; _i < activeOperations_1.length; _i++) {
            var op = activeOperations_1[_i];
            result += '<tr><td style="font-size:10px">' + op.topicId + '<div>' + op.pageId + '</div></td><td style="font-size:10px">' + op.name + '<div>' + op.dur + '</div></td></tr>';
        }
        return result + "</table>";
    };
    HtmlRenderer.splitPage = function (leftPart, rightPart) {
        return '<table style="width: 100%"><tr>' +
            '<td style="vertical-align: top">' + leftPart + '</td>' +
            '<td style="vertical-align: top">' + rightPart + '</td></tr></table>';
    };
    HtmlRenderer.renderMainContent = function (r) {
        var leftPart = this.renderMainTable(r.topics);
        var rightPart = this.renderActiveOperations("Active operations", r.activeOperations) +
            this.renderAdditionalFields(r) +
            this.renderActiveOperations("Awaiting operations", r.awaitingOperations);
        return this.splitPage(leftPart, rightPart);
    };
    return HtmlRenderer;
}());
//# sourceMappingURL=html.js.map