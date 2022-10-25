$(document).ready(function () {
    
    if (batchModeRunning == "true") {
        $("#batchModeCheckBox").prop("disabled", true);
        $("#btns .save").prop("disabled", true);
        $("#BatchModeInfo").text("Batch mode is performing rights protection for all items.");
    } else {
        $("#btns .save").prop("disabled", false);
        $("#batchModeCheckBox").prop("disabled", false);
        $("#BatchModeInfo").text(BatchModeStatus);
    }

    var listId = getQueryString("listId");
    var spHostUrl = getQueryString("SPHostUrl");

    $("#failedFileLink").attr("href", "BatchModeFailedView?listId=" + listId + "&SPHostUrl=" + spHostUrl);

    if (failedFileCount == "0" || failedFileCount == "") {
        $("#failedFileLink").hide();
    } else {
        $("#failedFileLink").show();
    }


    if (isList == "true") {
        $("#historyVersionCheckBox").prop("disabled", true);
        $(".header > label").text("list setting");
        $(".header > h2").text("list setting");
    }

    if (historyVersion == "true") {
        $("#historyVersionCheckBox").prop("checked", true);
    } else {
        $("#historyVersionCheckBox").prop("checked", false);
    }

    if (deleteSourceFile == "true") {
        $("#historyVersionCheckBox").prop("checked", true).prop("disabled", true);
        $("#sourceFileCheckBox").prop("checked", true);
    } else {
        $("#sourceFileCheckBox").prop("checked", false);
    }

    var data = JSON.parse(jsonData),
    len = data.length,
    checkAll = true,
    selectNum = 0;

    data.forEach(function (item, index) {
        if (!item.checked) {
            checkAll = false;
        } else {
            selectNum++;
        }
    });

    $("#box .select-all").prop('checked', checkAll);

    function initList() {
        for (let i = 0; i < len; i++) {
            var oLi = document.createElement('li'),
                oInp = document.createElement('input'),
                oLabel = document.createElement('label');
            oInp.setAttribute('type', 'checkbox');
            oInp.setAttribute('id', i);
            oInp.setAttribute('name', i);
            oInp.checked = data[i].checked;
            oLabel.innerText = data[i].name;
            oLabel.setAttribute('for', i);
            oLi.appendChild(oInp);
            oLi.appendChild(oLabel);
            $('#box .list').append(oLi);
        }
    }

    initList();

    $('#box .select-all').change(function (e) {
        $('#box .list li input').each(function (index, ele) {
            ele.checked = e.currentTarget.checked;
            data[index].checked = e.currentTarget.checked;
        });
        if (!e.currentTarget.checked) {
            checkAll = false;
        } else {
            selectNum = $('#box .list li input').size();
        }
    });


    $('#box .list li input').each(function (index, ele) {
        $(ele).change(function (e) {
            data[index].checked = e.currentTarget.checked;
            if (!e.currentTarget.checked) {
                selectNum--;
                checkAll = false;
                $('.select-all').prop('checked', checkAll);
            } else {
                selectNum++;
                if (selectNum == $('#box .list li input').size()) {
                    checkAll = true;
                    $('.select-all').prop('checked', checkAll);
                }
            }
        });

    });

    $("#sourceFileCheckBox").on('change', function (e) {
        if (e.currentTarget.checked) {
            $("#historyVersionCheckBox").prop("checked", true).prop("disabled", true);
        } else {
            $("#historyVersionCheckBox").prop("disabled", false);
        }
    });

    function getQueryString(name) {
        var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)", "i");
        var r = window.location.search.substr(1).match(reg);
        if (r != null) return unescape(r[2]); return null;
    }

    $("#btns .save").click(function (e) {
        $("#batchModeCheckBox").prop("disabled", true);
        $("#submitBtn").prop("disabled", true);
        var batchModeStatus = $("#batchModeCheckBox").is(':checked');
        var deleteSourceFile = $("#sourceFileCheckBox").is(':checked');
        var historyVersion = $("#historyVersionCheckBox").is(':checked');
        var newData = data.filter(function (item, index) {
            return item.checked;
        });
        var selectedColumns = {};
        for (let i = 0; i < newData.length; i++) {
            selectedColumns[newData[i]["id"]] = newData[i]["name"]
        }
        selectedColumns = JSON.stringify(selectedColumns);
        $.ajax({
            type: "POST",
            data: { "batchModeStatus": batchModeStatus, "deleteSourceFile": deleteSourceFile, "historyVersion": historyVersion, "selectedColumns": selectedColumns },
            url: "/GeneralSetting/LibSettingSubmit?listId=" + getQueryString("listId") + "&SPHostUrl=" + getQueryString("SPHostUrl"),
            success: function (data) {
                alert(data);
                var batchModeStatus = $("#batchModeCheckBox").is(':checked');
                if (data == "Save successfully!" && batchModeStatus) {
                    $("#BatchModeInfo").text("Batch mode is performing rights protection for all items.");
                }
                else {
                    $("#btns .save").prop('disabled', false);
                    $("#batchModeCheckBox").prop("disabled", false);
                }
            },
            error: function () {
                $("#batchModeCheckBox").prop("disabled", false);
                $("#btns .save").prop('disabled', false);
                alert("failed to save data!");
            }
        });
    });

    $("#btns .cancel").click(function () {
        window.history.back(-1);
    });


});



