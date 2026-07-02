(function () {
    function initFeePolling() {
        var form = document.getElementById('declaration-fullname-form');
        if (!form) return;

        var statusUrl = form.getAttribute('data-status-url');
        var fallbackUrl = form.getAttribute('data-fallback-url');
        var intervalMs = parseInt(form.getAttribute('data-interval-ms'), 10) || 3000;
        var timeoutMs = parseInt(form.getAttribute('data-timeout-ms'), 10) || 60000;

        if (!statusUrl || !fallbackUrl) return;

        var spinner = document.getElementById('global-spinner');
        var formWrapper = document.getElementById('declaration-fullname-wrapper');
        var pollingIntervalId = null;
        var submitInFlight = false;

        function showSpinnerNow() {
            if (spinner) spinner.style.display = 'flex';
            if (formWrapper) formWrapper.style.display = 'none';
        }

        function navigate(url) {
            if (pollingIntervalId !== null) {
                clearInterval(pollingIntervalId);
                pollingIntervalId = null;
            }
            window.location.href = url;
        }

        function fallbackToNativeSubmit() {
            // Detach our handler so the second submit call actually posts.
            form.removeEventListener('submit', onSubmit, true);
            form.submit();
        }

        function pollStatus() {
            var startTime = Date.now();
            pollingIntervalId = setInterval(function () {
                if (Date.now() - startTime >= timeoutMs) {
                    navigate(fallbackUrl);
                    return;
                }
                var xhr = new XMLHttpRequest();
                xhr.open('GET', statusUrl, true);
                xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
                xhr.onload = function () {
                    if (xhr.status < 200 || xhr.status >= 300) return; // keep polling
                    try {
                        var result = JSON.parse(xhr.responseText);
                        if (result && result.redirectUrl) {
                            navigate(result.redirectUrl);
                        }
                    } catch (e) {
                        // Ignore parse errors, keep polling
                    }
                };
                xhr.onerror = function () { /* keep polling */ };
                xhr.send();
            }, intervalMs);
        }

        function onSubmit(event) {
            if (submitInFlight) return;
            submitInFlight = true;
            event.preventDefault();

            // Reveal the spinner immediately so there is never a visual gap while
            // the AJAX submit is in flight.
            showSpinnerNow();

            var formData = new FormData(form);
            var xhr = new XMLHttpRequest();
            xhr.open('POST', form.action, true);
            xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
            xhr.onload = function () {
                if (xhr.status < 200 || xhr.status >= 300) {
                    submitInFlight = false;
                    fallbackToNativeSubmit();
                    return;
                }
                try {
                    var result = JSON.parse(xhr.responseText);
                    if (result && result.redirectUrl) {
                        navigate(result.redirectUrl);
                    } else {
                        pollStatus();
                    }
                } catch (e) {
                    submitInFlight = false;
                    fallbackToNativeSubmit();
                }
            };
            xhr.onerror = function () {
                submitInFlight = false;
                fallbackToNativeSubmit();
            };
            xhr.send(formData);
        }

        // Use capture phase so we run before any other listener that might read
        // form state after submission is initiated. The other listener
        // (loadingspinner.js) is only concerned with showing the spinner, which
        // we also do explicitly.
        form.addEventListener('submit', onSubmit, true);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initFeePolling);
    } else {
        initFeePolling();
    }
})();
