$(document).ready(function () {
    const CHECK_INTERVAL = 5000; // 5 seconds
    let intervalId = setInterval(checkUploadStatus, CHECK_INTERVAL);

    function checkUploadStatus() {
        $.ajax({
            url: '/report-data/subsidiaries-check-upload-status',
            type: 'GET',
            success: handleSuccess,
            error: handleError
        });
    }

    function handleSuccess(result) {
        if (result.redirectUrl) {
            clearInterval(intervalId);
            window.location.href = result.redirectUrl;
        } else if (!result.isFileUploadInProgress) {
            clearInterval(intervalId);
        }
    }
    function handleError(xhr, status, error) {  
        console.log(error);
    }
});
