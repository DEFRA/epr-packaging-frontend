document.addEventListener("DOMContentLoaded", () => {
    const downloadButton = document.getElementById("downloadPDFButton");
    if (!downloadButton) return;

    downloadButton.addEventListener("click", () => {
        downloadPDF().catch((error) => console.error("Error in downloadPDF:", error));
    });
});

async function downloadPDF() {
    const pathSegments = window.location.pathname.split('/');
    const prnType = pathSegments[pathSegments.length - 2];  // Example: 'accepted-prn', 'selected-prn', or 'rejected-prn'
    const prnGuid = pathSegments[pathSegments.length - 1];

    const downloadUrl = `/report-data/download-${prnType}-pdf/${prnGuid}`;

    const response = await fetch(downloadUrl, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' }
    });

    const { fileName, htmlContent } = await response.json();

    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = htmlContent;

    const options = {
        margin: [0, 10, 0, 10],
        filename: `${fileName}.pdf`,
        html2canvas: { scale: 2, width: 794, dpi: 300, letterRendering: true, useCORS: true },
        jsPDF: { unit: 'pt', format: 'a4', orientation: 'portrait' }
    };
    
    await html2pdf().from(tempDiv).set(options).save();
    tempDiv.remove();
}
