var Dictionary = /** @class */ (function () {
    function Dictionary(initItems) {
        if (initItems)
            this.items = initItems;
        else
            this.items = {};
    }
    Dictionary.prototype.init = function (data) {
        this.items = data;
    };
    Dictionary.prototype.add = function (key, value) {
        this.items[key] = value;
    };
    Dictionary.prototype.addRange = function (values, getKey) {
        for (var i = 0; i < values.length; i++) {
            var value = values[i];
            var key = getKey(value);
            this.add(key, value);
        }
    };
    Dictionary.prototype.remove = function (key) {
        this.items[key] = undefined;
    };
    Dictionary.prototype.clear = function () {
        this.items = {};
    };
    Dictionary.prototype.getValue = function (key) {
        return this.items[key];
    };
    Dictionary.prototype.getValueOrDefault = function (key, getDefault) {
        var result = this.items[key];
        if (result)
            return result;
        return getDefault();
    };
    Dictionary.prototype.hasKey = function (key) {
        return this.items[key] != undefined;
    };
    Dictionary.prototype.iterate = function (callbask) {
        var keys = Object.keys(this.items);
        for (var i = 0; i < keys.length; i++) {
            var key = keys[i];
            callbask(key, this.items[key]);
        }
    };
    Dictionary.prototype.getValues = function () {
        var result = [];
        this.iterateValues(function (value) {
            result.push(value);
        });
        return result;
    };
    Dictionary.prototype.iterateValues = function (callbask) {
        var keys = Object.keys(this.items);
        for (var i = 0; i < keys.length; i++) {
            var key = keys[i];
            callbask(this.items[key]);
        }
    };
    Dictionary.prototype.getKeys = function () {
        return Object.keys(this.items);
    };
    Dictionary.prototype.getCount = function () {
        return this.getKeys().length;
    };
    return Dictionary;
}());
//# sourceMappingURL=dictionary.js.map