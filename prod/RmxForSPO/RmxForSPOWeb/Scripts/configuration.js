function GeneralSettingSubmit() {
    var obj = {
        JavaPcHost: $("#JavaPcHost")[0].value,
        OAUTHHost: $("#OAUTHHost")[0].value,
        ClientSecureID: $("#ClientSecureID")[0].value,
        ClientSecureKey: $("#ClientSecureKey")[0].value,
        SecureViewURL: $("#SecureViewURL")[0].value,
        RouterURL: $("#RouterURL")[0].value,
        AppId: $("#AppId")[0].value,
        AppKey: $("#AppKey")[0].value,
        CertificatefileContent: $("#CertificateFileFileContent")[0].value,
        CertificatefileName: $("#CertificateFileFileName")[0].value,
        CertificatefilePassword: $("#CertificatePassword")[0].value
    };

    if (obj.JavaPcHost == "" || obj.OAUTHHost == "" || obj.ClientSecureID == "" || obj.ClientSecureKey == "" || obj.RouterURL == "" || obj.AppId == "" || obj.AppKey == "") {
        alert("Please complete the information");
        return;
    }

    $.ajax({
        type: "POST",
        data: obj,
        url: "/GeneralSetting/GeneralSettingFormSubmit?SPHostUrl=" + getQueryString("SPHostUrl"),
        success: function (data) {
            if (data == "test connection failed") {
                $("#TestConnectionResult")[0].classList.remove("hide");
            }
            else {
                $("#TestConnectionResult")[0].classList.add("hide");
                alert(data);
            }
            ZENG.msgbox.hide();
        },
        error: function () { alert("failed to save data!"); ZENG.msgbox.hide(); }
    });

    ZENG.msgbox.show("saving...please wait", 6);
}

function getQueryString(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)", "i");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}

function OnCertificateFileChange() {
    var certFileInput = document.getElementById('CertificateFile');
    var certFile = certFileInput.files[0];
    var fr = new FileReader();
    fr.readAsArrayBuffer(certFile);
    fr.onload = function () {
        var binary = '';
        var buffer = this.result;
        var bytes = new Uint8Array(buffer);
        var len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        var certFileInput = document.getElementById('CertificateFile');
        document.getElementById('CertificateFileFileContent').value = window.btoa(binary);
        document.getElementById('CertificateFileFileName').value = certFileInput.files[0].name;
    }
}

function onCancelClick() {
    window.history.back(-1);
}