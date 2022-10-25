var setting = {
    view: {
        dblClickExpand: false,
        showLine: true,
        fontCss: { 'color': 'black', 'font-weight': 'bold' },
        selectedMulti: true
    },
    async: {
        enable: true,
        url: "/GeneralSetting/AsyncSubSiteNode?SPHostUrl=" + getQueryString("SPHostUrl"),
        autoParam: ["id", "isParent"],
        dataType: "json"
    },
    check: {
        chkboxType: { "Y": "s", "N": "s" },
        chkStyle: "checkbox",
        enable: true
    },
    data: {
        simpleData: {
            enable: true,
            idKey: "id",
            pIdKey: "pId",
            rootPId: 0
        }
    },
    callback: {
        onAsyncSuccess: zTreeOnAsyncSuccess
    }
};

$(document).ready(function () {

    $.fn.zTree.init($("#regionZTree"), setting, data);
    var zTree = $.fn.zTree.getZTreeObj("regionZTree");
    var nodes = zTree.getNodes();
    if (nodes.length > 0) {
        for (var i = 0; i < nodes.length; i++) {
            zTree.expandNode(nodes[i], true, false, false);
        }
    }

    var rootNode = zTree.getNodesByFilter(function (node) { return node.level == 0 }, true);

    asyncSubNode(zTree, rootNode);

    var checkAll = true,
    selectNum = 0;

});

function asyncSubNode(zTree, rootNode) {
    var subsiteNode = zTree.getNodesByFilter(function (node) { return node.isParent == true }, false, rootNode);
    for (var i = 0; i < subsiteNode.length; i++) {
        zTree.reAsyncChildNodes(subsiteNode[i], "refresh", "", zTreeOnAsyncSuccess)
    }
}

function zTreeOnAsyncSuccess(event, treeId, treeNode, msg) {
    if (treeNode) {
        var zTree = $.fn.zTree.getZTreeObj("regionZTree");
        asyncSubNode(zTree, treeNode);
    }
}

function EnforceEntitySubmit() {
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
        jsondata.push(obj);
    }
    var data = { "znodeList": jsondata };
    $.ajax({
        type: "POST",
        data: data,
        dataType: "json",
        url: "/GeneralSetting/EnforceEntityFormSubmit?SPHostUrl=" + getQueryString("SPHostUrl"),
        success: function (data) { alert(data); ZENG.msgbox.hide(); },
        error: function () { alert("failed to save data!"); ZENG.msgbox.hide(); }
    });
    ZENG.msgbox.show("saving...please wait", 6);
}

function getQueryString(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)", "i");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}

$(".btns #btn-cancel").click(function () {
    window.history.back(-1);
});
