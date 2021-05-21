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
    HtmlRenderer.renderLoadedPagesContent = function (pages) {
        var result = '';
        for (var _i = 0, pages_1 = pages; _i < pages_1.length; _i++) {
            var page = pages_1[_i];
            var badges = '';
            for (var _a = 0, _b = page.pages; _a < _b.length; _a++) {
                var badge = _b[_a];
                badges += '<span class="badge badge-success" style="margin-left: 5px">' + badge + '</span>';
            }
            var activePagesBadges = '';
            for (var _c = 0, _d = page.activePages; _c < _d.length; _c++) {
                var activePage = _d[_c];
                activePagesBadges += '<span class="badge badge-warning" style="margin-left: 5px">' + activePage + '</span>';
            }
            var queuesContent = this.renderQueuesTableContent(page.queues);
            result += '<tr><td>' + page.topicId + '<div>WritePos: ' + page.writePosition + '</div></td><td>' + queuesContent + '</td><td><div>Max:' + page.messageId + '</div><div>Saved:' + page.savedMessageId + '</div></td><td><div>Active:</div>' + activePagesBadges + '<hr/><div>Loaded:</div>' + badges + '</td></tr>';
        }
        return result;
    };
    HtmlRenderer.renderMainTable = function (pages) {
        var content = this.renderLoadedPagesContent(pages);
        return '<table class="table table-striped"><tr><th>Topic</th><th>Queues</th><th>MessageId</th><th>Pages</th></tr>' + content + '</table>';
    };
    HtmlRenderer.renderAdditionalFields = function (r) {
        return '<div>Queue SnapshotId: ' + r.queuesSnapshotId + '</div>';
    };
    HtmlRenderer.renderActiveOperations = function (header, activeOperations) {
        var result = '<h1>' + header + '</h1><table class="table table-striped"><tr><th>Name</th><th>Topic</th><th>PageId</th><th>Reason</th></tr>';
        for (var _i = 0, activeOperations_1 = activeOperations; _i < activeOperations_1.length; _i++) {
            var op = activeOperations_1[_i];
            result += '<tr><td>' + op.name + '<div>' + op.id + '</div></td><td>' + op.topicId + '</td><td>' + op.pageId + '</td><td>' + op.reason + '</td></tr>';
        }
        return result + "</table>";
    };
    HtmlRenderer.splitPage = function (leftPart, rightPart) {
        return '<table style="width: 100%"><tr>' +
            '<td style="vertical-align: top">' + leftPart + '</td>' +
            '<td style="vertical-align: top">' + rightPart + '</td></tr></table>';
    };
    HtmlRenderer.renderMainContent = function (r) {
        var leftPart = this.renderMainTable(r.loadedPages);
        var rightPart = this.renderActiveOperations("Active operations", r.activeOperations) +
            this.renderAdditionalFields(r) +
            this.renderActiveOperations("Awaiting operations", r.awaitingOperations);
        return this.splitPage(leftPart, rightPart);
    };
    return HtmlRenderer;
}());
//# sourceMappingURL=html.js.map