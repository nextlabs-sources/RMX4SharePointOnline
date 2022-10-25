(function (window, undefined) {

    "use strict";

    var $ = window.jQuery;
    var document = window.document;

    // SPHostUrl 参数名称
    var SPHostUrlKey = "SPHostUrl";

    // 从当前 URL 获取 SPHostUrl，并将其作为查询字符串追加到指向页面中的当前域的链接。
    $(document).ready(function () {
        ensureSPHasRedirectedToSharePointRemoved();

        var spHostUrl = getSPHostUrlFromQueryString(window.location.search);
        var currentAuthority = getAuthorityFromUrl(window.location.href).toUpperCase();

        if (spHostUrl && currentAuthority) {
            appendSPHostUrlToLinks(spHostUrl, currentAuthority);
        }
    });

    // 将 SPHostUrl 作为查询字符串追加到指向当前域的所有链接。
    function appendSPHostUrlToLinks(spHostUrl, currentAuthority) {
        $("a")
            .filter(function () {
                var authority = getAuthorityFromUrl(this.href);
                if (!authority && /^#|:/.test(this.href)) {
                    // 筛选出具有其他不受支持的协议的定位点和 URL。
                    return false;
                }
                return authority.toUpperCase() == currentAuthority;
            })
            .each(function () {
                if (!getSPHostUrlFromQueryString(this.search)) {
                    if (this.search.length > 0) {
                        this.search += "&" + SPHostUrlKey + "=" + spHostUrl;
                    }
                    else {
                        this.search = "?" + SPHostUrlKey + "=" + spHostUrl;
                    }
                }
            });
    }

    // 从给定的查询字符串中获取 SPHostUrl。
    function getSPHostUrlFromQueryString(queryString) {
        if (queryString) {
            if (queryString[0] === "?") {
                queryString = queryString.substring(1);
            }

            var keyValuePairArray = queryString.split("&");

            for (var i = 0; i < keyValuePairArray.length; i++) {
                var currentKeyValuePair = keyValuePairArray[i].split("=");

                if (currentKeyValuePair.length > 1 && currentKeyValuePair[0] == SPHostUrlKey) {
                    return currentKeyValuePair[1];
                }
            }
        }

        return null;
    }

    // 如果给定 URL 是带 http/https 协议的绝对 URL 或协议相对 URL，则可从该 URL 获得授权。
    function getAuthorityFromUrl(url) {
        if (url) {
            var match = /^(?:https:\/\/|http:\/\/|\/\/)([^\/\?#]+)(?:\/|#|$|\?)/i.exec(url);
            if (match) {
                return match[1];
            }
        }
        return null;
    }

    // 如果查询字符串中存在 SPHasRedirectedToSharePoint，请将其删除。
    // 因此，当用户将 URL 设置为书签时，将不包括 SPHasRedirectedToSharePoint。
    // 请注意，修改 window.location.search 将导致向服务器发出额外的请求。
    function ensureSPHasRedirectedToSharePointRemoved() {
        var SPHasRedirectedToSharePointParam = "&SPHasRedirectedToSharePoint=1";

        var queryString = window.location.search;

        if (queryString.indexOf(SPHasRedirectedToSharePointParam) >= 0) {
            window.location.search = queryString.replace(SPHasRedirectedToSharePointParam, "");
        }
    }

})(window);
