$(document).ready(function () {
    const $config = $('#fee-status-refresh');
    const statusUrl = $config.data('status-url');
    const fallbackUrl = $config.data('fallback-url');
    const intervalMs = parseInt($config.data('interval-ms'), 10) || 3000;
    const timeoutMs = parseInt($config.data('timeout-ms'), 10) || 60000;
    const startTime = Date.now();

    $('#global-spinner').css('display', 'flex');

    const intervalId = setInterval(checkFeeStatus, intervalMs);

    function checkFeeStatus() {
        if (Date.now() - startTime >= timeoutMs) {
            clearInterval(intervalId);
            window.location.href = fallbackUrl;
            return;
        }
        $.ajax({
            url: statusUrl,
            type: 'GET',
            success: handleSuccess,
            error: handleError
        });
    }

    function handleSuccess(result) {
        if (result.redirectUrl) {
            clearInterval(intervalId);
            window.location.href = result.redirectUrl;
        }
    }

    function handleError(xhr, status, error) {
        console.log(error);
    }
});
