function InitSpinner(formWrapperId, formId) {
    const spinner = document.getElementById('global-spinner');
    const form = document.getElementById(formId);
    const formWrapper = document.getElementById(formWrapperId);
    let spinnerTimeout;
    if (spinner) {
        spinner.style.display = 'none';
    }

    function showSpinnerAndHideFormWithDelay() {
        spinnerTimeout = setTimeout(() => {
            if (spinner) {
                spinner.style.display = 'flex';
            }
            if (formWrapper) {
                formWrapper.style.display = 'none';
            }
        }, 500);
    }
    if (formWrapper) {
        if (form) {
            form.addEventListener("submit", function (event) {
                clearTimeout(spinnerTimeout);
                showSpinnerAndHideFormWithDelay();
            });
        }
    }
}