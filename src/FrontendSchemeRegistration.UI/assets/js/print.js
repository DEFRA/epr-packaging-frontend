const printBtn = document.getElementById('printButton');

if (printBtn) {
    printBtn.addEventListener("click", printPage);
}

function printPage() {
    window.print();
}