var setting = {
    view: {
        dblClickExpand: true,
        showLine: false,
        fontCss: { 'color': 'black', 'font-weight': 'bold' },
        selectedMulti: true,
        showIcon: false
    },
    edit: {
        enable: false,
        editNameSelectAll: true,
        showRemoveBtn: false,
        showRenameBtn: false,
        removeTitle: "remove",
        renameTitle: "rename"
    },
    callback: {
        onClick: myOnClick
    },
    data: {
        simpleData: {
            enable: true,
            idKey: "id",
            pIdKey: "pId",
            rootPId: 0
        }
    },
    async: {
        enable: true,
        url: "/GeneralSetting/AsyncSubWebPropperty?SPHostUrl=" + getQueryString("SPHostUrl"),
        autoParam: ["id"],
        dataType: "json"
    }
};



$(document).ready(function () {

    $('#box .list').html('');

    //init zTree data and create object
    $.fn.zTree.init($("#regionZTree"), setting, data);
    var zTree = $.fn.zTree.getZTreeObj("regionZTree"),
        nodes = zTree.getNodes();

    //get the value of select-box
    var selectValue = sitePropertyselectAllStatus || "None";
    $('#select-box').val(selectValue);


    if (selectValue == "SiteCollection") {
        $('.configuration').css({ display: 'flex' });
        $('#box').css({ display: 'block' });
        $('#box > div.select-box span.drop-btn').css({ display: 'none' });
        $('#box .select-box').off('click');
    } else if (selectValue == "None") {
        $('.configuration').css({ display: 'none' });
        $('#box').css({ display: 'block' });
    }else if (selectValue == "") {
        $('.configuration').css({ display: 'none' });
    } else {
        $('.configuration').css({ display: 'flex' });
        $('#box').css({ display: 'block' });
        $('#box .select-box').off('click').on("click", function () {
            $('#box .zTree-box').slideToggle();
        });
    }

    if (nodes.length > 0) {
        for (var i = 0; i < nodes.length; i++) {
            zTree.expandNode(nodes[i], true, false, false);
        }
    }

    var rootNode = zTree.getNodesByFilter(function (node) {
        return node.level == 0;
    }, true);

    asyncSubNode(zTree, rootNode);

    var checkAll = true,
        selectNum = 0;

    if (selectValue == "Subsite") {
        checkAll = false;
        selectNum = '';
        $('#box .site-name').text(nodes[0].name);
    } else if (selectValue == "Both" || selectValue == "SiteCollection") {
        nodes[0].siteProperties.forEach(initList);
        $('.site-name').text(nodes[0].name);
    }

    $('#box .list li input').each(function (index, ele) {
        if (ele.checked == false) {
            checkAll = false;
        } else {
            ++selectNum;
        }
    });

    $('.select-all').unbind('change').change(function (e) {
        $('#box .list li input').each(function (index, ele) {
            ele.checked = e.currentTarget.checked;
            nodes[0].siteProperties[index].checked = ele.checked;
        });
        selectNum = e.currentTarget.checked ? $('#box .list li input').size() : 0;
        $('.select-num').text(selectNum);
    });


    $('#box .list li input').each(function (index, ele) {
        $(ele).change(function (e) {
            nodes[0].siteProperties[index].checked = e.currentTarget.checked;
            if (e.currentTarget.checked == false) {
                --selectNum;
                $('.select-all').prop('checked', false);
            } else {
                ++selectNum;
            }
            if (selectNum == $('#box .list li input').size()) {
                $('.select-all').prop('checked', true);
            }
            $('.select-num').text(selectNum);
        });
    });

    $('.select-all').prop('checked', checkAll);
    $('.select-num').text(selectNum);
});

function asyncSubNode(zTree, rootNode) {
    var subsiteNode = zTree.getNodesByFilter(function (node) {
        return node.isParent == true
    }, false, rootNode);
    for (var i = 0; i < subsiteNode.length; i++) {
        zTree.reAsyncChildNodes(subsiteNode[i], "refresh", "", zTreeOnAsyncSuccess)
    }
}

function zTreeOnAsyncSuccess(event, treeId, treeNode, msg) {
    if (treeNode) {
        var zTree = $.fn.zTree.getZTreeObj("regionZTree"),
            nodes = zTree.getNodes();

        asyncSubNode(zTree, treeNode);
    }
}

$('#select-box').on('change', function () {

    $('#box .list').html('');

    var zTree = $.fn.zTree.getZTreeObj("regionZTree"),
        nodes = zTree.getNodes();

    var selectValue = $(this).val();

    $('.site-name').text(nodes[0].name);

    var selectNum = 0,
        checkAll = true;

    if (selectValue == 'SiteCollection') {
        nodes[0].siteProperties.forEach(initList);
        $('#box .select-box').off('click');
        $('#box > div.select-box span.drop-btn').css({ display: 'none' });
        $('.configuration').css({ display: 'flex' });
        $('#box').css({ display: 'block' });
    } else if (selectValue == 'None') {
        $('.configuration').css({ display: 'none' });
    } else if (selectValue == 'Both') {
        nodes[0].siteProperties.forEach(initList);
        $('#box .select-box').off('click').on("click", function () {
            $('#box .zTree-box').slideToggle();
        });
        $('#box > div.select-box span.drop-btn').css({ display: 'inline-block' });
        $('.configuration').css({ display: 'flex' });
        $('#box').css({ display: 'block' });
    } else {
        checkAll = false;
        selectNum = '';
        $('#box').css({ display: 'block' });
        $('#box .select-box').off('click').on("click", function () {
            $('#box .zTree-box').slideToggle();
        });
        $('#box > div.select-box span.drop-btn').css({ display: 'inline-block' });
        $('.configuration').css({ display: 'flex' });
    }

    $('#box .list > li > input').each(function (index, ele) {
        $(ele).change(function (e) {
            nodes[0].siteProperties[index].checked = e.currentTarget.checked;
            if (e.currentTarget.checked == false) {
                --selectNum;
                checkAll = false;
            } else {
                ++selectNum;
            }
            $('.select-num').text(selectNum);
            if (selectNum == $('#box .list li input').size()) {
                checkAll = true;
            }
            $('.select-all').prop('checked', checkAll);
        });
    });

    $('.select-all').unbind('change').change(function (e) {
        var len = $('#box .list li input').size();
        for (var i = 0; i < len; i++) {
            $('#box .list li input').eq(i).prop('checked', e.currentTarget.checked);
            nodes[0].siteProperties[i].checked = e.currentTarget.checked;
        }
        selectNum = e.currentTarget.checked ? len : 0;
        $('.select-num').text(selectNum);
    });

    $('#box .zTree-box').slideUp();

    $('#box .list li input').each(function (index, ele) {
        if (ele.checked == false) {
            checkAll = false;
        } else {
            ++selectNum;
        }
    });
    $('.select-num').text(selectNum);
    $('.select-all').prop('checked', checkAll);
});

function getQueryString(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)", "i");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}


function myOnClick(event, treeId, treeNode, clickFlag) {

    var selectValue = $('#select-box').val();

    if (selectValue == "Subsite" && treeNode.pId == "0") {
        return false;
    } else {
        $('#box .list').html('');

        treeNode.siteProperties.forEach(initList);
        var len = $('#box .list li input').size();

        $('#box .select-box .site-name').html(treeNode.name);

        var selectNum = 0,
            checkAll = true;

        $('#box .list li input').each(function (index, ele) {
            if (ele.checked == false) {
                checkAll = false;
            } else {
                ++selectNum;
            }
        });

        $('#box .list > li > input').each(function (index, ele) {
            $(ele).change(function (e) {
                treeNode.siteProperties[index].checked = e.currentTarget.checked;
                if (e.currentTarget.checked == false) {
                    --selectNum;
                    checkAll = false;
                    $('.select-all').prop('checked', checkAll);
                } else {
                    ++selectNum;
                }
                $('.select-num').text(selectNum);
                if (selectNum == len) {
                    checkAll = true;
                    $('.select-all').prop('checked', checkAll);
                }
            });
        });

        $('.select-all').unbind('change').change(function (e) {

            for (var i = 0; i < len; i++) {
                $('#box .list li input').eq(i).prop('checked', e.currentTarget.checked);
                treeNode.siteProperties[i].checked = e.currentTarget.checked;
            }
            if (e.currentTarget.checked) {
                selectNum = len;
                $('.select-num').html(selectNum);
            } else {
                selectNum = 0;
                $('.select-num').html(selectNum);
            }
        });

        $('.select-num').html(selectNum);
        $('.select-all').prop('checked', checkAll);

        $('#box .zTree-box').slideUp();
    }
}

$(document).on('click', function (e) {
    var e = e || window.event;
    if (e.target.className == "select-box" || e.target.id.indexOf('regionZTree') != -1 || e.target.className == "site-name") {
        return false;
    } else {
        $('#box .zTree-box').slideUp();
    }
});

function initList(item, index) {
    var oLi = document.createElement('li'),
        oInp = document.createElement('input'),
        oLabel = document.createElement('label');
    oInp.setAttribute('type', 'checkbox');
    oInp.setAttribute('name', index);
    oInp.setAttribute('id', index);
    oInp.checked = item.checked;
    oLabel.innerText = item.displayName;
    oLabel.setAttribute('for', index);
    oLi.appendChild(oInp);
    oLi.appendChild(oLabel);
    $('#box .list').append(oLi);
}

function updateData() {
    var jsondata = [];
    var zTree = $.fn.zTree.getZTreeObj("regionZTree");
    var node = zTree.getNodes();
    var nodes = zTree.transformToArray(node);
    for (var i = 0; i < nodes.length; i++) {
        var obj = {};
        obj.id = nodes[i].id;
        obj.pId = nodes[i].pId;
        obj.name = nodes[i].name;
        obj.checked = nodes[i].checked;
        obj.isParent = nodes[i].isParent;
        obj.isLoaded = nodes[i].isLoaded;
        obj.siteProperties = nodes[i].siteProperties;
        jsondata.push(obj);
    }
    return jsondata;
}

function trimData(target) {
    var len = target.length;
    for (var i = 0; i < len; i++) {
        var tempArr = [];
        for (var j = 0; j < target[i]["siteProperties"].length; j++) {

            if (target[i]["siteProperties"][j].checked == true) {
                tempArr.push(target[i]["siteProperties"][j]);
            }
        }
        target[i]["siteProperties"] = tempArr;
    }

    return target;
}

$('#btns .save').click(onSaveClick);
$('#btns .cancel').click(onCancelClick);

function onSaveClick() {
    var newData = trimData(updateData());
    var SitePropertyLevel = $('#select-box').val();
    $.ajax({
        type: "POST",
        data: { "SitePropertyLevel": SitePropertyLevel, "znodeList": newData},
        url: "/GeneralSetting/SitePropertySubmit?SPHostUrl=" + getQueryString("SPHostUrl"),
        success: function (data) {
            alert(data);
            ZENG.msgbox.hide();
        },
        error: function () {
            alert("failed to save data!");
        }
    });
}

function onCancelClick() {
    window.history.back(-1);
}