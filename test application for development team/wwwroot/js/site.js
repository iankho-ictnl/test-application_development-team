// Page behaviour without inline script (security finding S-25: the content security policy blocks
// inline script and inline event handlers). Pages opt in with data attributes.
(function () {
    'use strict';

    // <form data-confirm="Are you sure?">: ask before the form is sent.
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) { return; }
        var message = form.getAttribute('data-confirm');
        if (message !== null && !window.confirm(message)) {
            event.preventDefault();
        }
    });

    // <select data-autosubmit>: send the form when the choice changes.
    document.addEventListener('change', function (event) {
        var select = event.target;
        if (select instanceof HTMLSelectElement && select.hasAttribute('data-autosubmit') && select.form) {
            select.form.submit();
        }
    });

    // <select data-pricing-toggle data-hourly="#hourly" data-fixed="#fixed">: show the hourly or the
    // fixed-price fields for the chosen pricing type (activities).
    function togglePricing(select) {
        var hourly = select.value === 'Hourly';
        var hourlyFields = document.querySelector(select.getAttribute('data-hourly'));
        var fixedFields = document.querySelector(select.getAttribute('data-fixed'));
        if (hourlyFields) { hourlyFields.hidden = !hourly; }
        if (fixedFields) { fixedFields.hidden = hourly; }
    }

    document.addEventListener('change', function (event) {
        var select = event.target;
        if (select instanceof HTMLSelectElement && select.hasAttribute('data-pricing-toggle')) {
            togglePricing(select);
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('select[data-pricing-toggle]').forEach(togglePricing);
    });
})();
