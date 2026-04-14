// wwwroot/js/traveler.js
function generateQRCode(text) {
    new QRCode(document.getElementById("qrcode"), {
        text: text,
        width: 128,
        height: 128,
        colorDark: "#000000",
        colorLight: "#ffffff",
        correctLevel: QRCode.CorrectLevel.H
    });
}