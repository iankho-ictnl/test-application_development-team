// Makes the browser-side number checks of jQuery Validation follow the page language.
// Dutch input uses a decimal comma ("12,50") and an optional dot for thousands ("1.250,50").
// The API stays the authority for validation; this only avoids refusing valid Dutch input.
(function (window, $) {
    'use strict';

    const dutchNumber = /^-?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/;
    const invariantNumber = /^-?(\d{1,3}(,\d{3})+|\d+)(\.\d+)?$/;

    function isDutch(lang) {
        return (lang || '').toLowerCase().startsWith('nl');
    }

    // Returns the number, or NaN when the text is not a number in the given language.
    function parseNumber(text, lang) {
        const value = String(text).trim();
        if (isDutch(lang)) {
            return dutchNumber.test(value) ? Number(value.replace(/\./g, '').replace(',', '.')) : NaN;
        }
        return invariantNumber.test(value) ? Number(value.replace(/,/g, '')) : NaN;
    }

    window.validationCulture = { parseNumber: parseNumber };

    if (!$ || !$.validator) {
        return;
    }

    const lang = () => document.documentElement.lang;
    const methods = $.validator.methods;

    methods.number = function (value, element) {
        return this.optional(element) || !isNaN(parseNumber(value, lang()));
    };
    methods.range = function (value, element, param) {
        const n = parseNumber(value, lang());
        return this.optional(element) || (n >= param[0] && n <= param[1]);
    };
    methods.min = function (value, element, param) {
        return this.optional(element) || parseNumber(value, lang()) >= param;
    };
    methods.max = function (value, element, param) {
        return this.optional(element) || parseNumber(value, lang()) <= param;
    };
})(window, window.jQuery);
