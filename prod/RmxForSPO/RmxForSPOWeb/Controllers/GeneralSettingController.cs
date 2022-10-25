using Microsoft.SharePoint.Client;
using RmxForSPOWeb.Common;
using RmxForSPOWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;
using RmxForSPOWeb.Filters;

namespace RmxForSPOWeb.Controllers
{
    public class GeneralSettingController : Controller
    {
        protected static CLog theLog = CLog.GetLogger("GeneralSettingController");
        private string domainUrl = "";
        public static readonly string m_strTipWhenSessionTimeout = "You are not authorization. Please try again by refresh this page or by launching the app installed on your site.";
        // GET: GeneralSetting
        public static string m_strDumbItemUrl = null;
        public static string m_strEditListUrl = null;
        public static string m_strRightProtectUrl = null;
        public static string m_strSecurityViewUrl = null;

        [SharePointPermissionsAuthentication("GuideView")]
        public ActionResult GuideView()
        {
            theLog.Debug("GuideView enter");
            try
            {
               
                var targetView = Request.QueryString["TargetView"];
                if (!string.IsNullOrEmpty(targetView))
                {
                    return RedirectToAction(targetView, new
                    {
                        SPHostUrl = Request.QueryString["SPHostUrl"]
                    });
                }
            }
            catch (Exception ex)
            {
                theLog.Error("GuideView error:" + ex.Message + ex.StackTrace);
                return View("Error");
            }
            return View();
        }
        [SharePointPermissionsAuthentication("ActivateView")]
        public ActionResult ActivateView()
        {
            try
            {
                var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
                if (spContext == null)
                {
                    theLog.Debug("spContext is null");
                    return View("Error");
                }
                using (var clientContext = spContext.CreateUserClientContextForSPHost())
                {
                    if (clientContext != null)
                    {
                        Uri SharePointUrl = new Uri(Request.QueryString["SPHostUrl"]);
                        domainUrl = SharePointUrl.ToString();
                        Web web = clientContext.Web;
                        clientContext.Load(web, oweb => oweb.CurrentUser, oweb => oweb.Title, oweb => oweb.Url, oweb => oweb.CurrentUser.LoginName);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                        // Add nodes for setting
                        string pid = "0";
                        var znodeList = new List<ZNodeModel>();
                        AddSiteToZNode(clientContext, clientContext.Web, znodeList, pid);
                        ViewBag.data = JsonConvert.SerializeObject(znodeList);
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Debug("ActivateView error:"+ex.Message+ex.StackTrace);
                return View("Error");
            }
            return View("ActivateView");
        }
        [SharePointPermissionsAuthentication("GeneralSettingView")]
        public ActionResult GeneralSettingView()
        {
            try
            {
                // loading generalsetting info
                RMXConfig cfg = RMXConfig.Instance();
                ViewBag.JavaPcHost = cfg.JavaPcHost;
                ViewBag.OAUTHHost = cfg.OAUTHHost;
                ViewBag.ClientSecureID = cfg.ClientSecureID;
                ViewBag.ClientSecureKey = cfg.ClientSecureKey;
                ViewBag.SecureViewURL = cfg.SecureViewURL;
                ViewBag.RouterURL = cfg.RouterURL;
                ViewBag.AppId = cfg.AppID;
                ViewBag.AppKey = cfg.AppKey;
                ViewBag.CertContent = cfg.CertificateFileContentBase64;
                ViewBag.CertFileName = cfg.CertificateFileName;
                ViewBag.CertPwd = cfg.CertificatePassword;
            }
            catch (Exception ex)
            {
                theLog.Debug("GeneralSettingView error:" + ex.Message + ex.StackTrace);
                return View("Error");
            }
            return View("GeneralSettingView");
        }
        [SharePointPermissionsAuthentication("SiteSettingView")]
        public ActionResult SiteSettingView()
        {
            try
            {
                var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
                if (spContext == null)
                {
                    theLog.Debug("spContext is null");
                    return View("Error");
                }

                using (var clientContext = spContext.CreateUserClientContextForSPHost())
                {
                    if (clientContext != null)
                    {
                        Web web = clientContext.Web;
                        clientContext.Load(web, oweb => oweb.AllProperties, oweb => oweb.Title, oweb => oweb.Url);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        //get all sites && selected properties
                        IniFiles sitePropertyFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "SiteProperty.ini");
                        ViewBag.SitePropertyLevel = sitePropertyFile.IniReadValue(web.Url, SPOEUtility.SitePropertyLevel);
                        var siteJson = sitePropertyFile.IniReadValue(web.Url, SPOEUtility.SitePropertyList);
                        List<ZSiteNodeModel> sitesNodeList = JsonConvert.DeserializeObject<List<ZSiteNodeModel>>(siteJson);
                        if (sitesNodeList == null)
                        {
                            sitesNodeList = new List<ZSiteNodeModel>();
                        }
                        theLog.Debug("sitesNodeList.Count" + sitesNodeList.Count);

                        //check 
                        ZSiteNodeModel node = null;

                        List<ZSiteNodeModel> znodeList = new List<ZSiteNodeModel>();
                        ZSiteNodeModel rootNode = new ZSiteNodeModel();
                        rootNode.name = web.Title;
                        rootNode.id = web.Url;
                        rootNode.pId = "0";
                        rootNode.isLoaded = true;
                        rootNode.isParent = true;
                        node = sitesNodeList.Where(p => p.id == web.Url).FirstOrDefault();
                        rootNode.siteProperties = GetWebProperty(web, node);
                        znodeList.Add(rootNode);

                        WebCollection subwebs = clientContext.Web.GetSubwebsForCurrentUser(null);
                        clientContext.Load(subwebs, webs => webs.Include(p => p.Title, p => p.Url,p=>p.AllProperties));
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        foreach (Web cellWeb in subwebs)
                        {
                            ZSiteNodeModel subNode = new ZSiteNodeModel();
                            subNode.name = cellWeb.Title;
                            subNode.id = cellWeb.Url;
                            subNode.pId = web.Url;

                            subNode.isParent = true;
                            node = sitesNodeList.Where(p => p.id == cellWeb.Url).FirstOrDefault();
                            subNode.siteProperties = GetWebProperty(cellWeb, node);
                            znodeList.Add(subNode);
                        }
                        ViewBag.data = JsonConvert.SerializeObject(znodeList);
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Debug("SiteSettingView error:" + ex.Message + ex.StackTrace);
                return View("Error");
            }
            return View("SiteSettingView");
        }
        public JsonResult AsyncSubWebPropperty(string id)
        {
            var znodeList = new List<ZSiteNodeModel>();
            var result = "";
            try
            {
                var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
                if (spContext == null)
                {
                    theLog.Debug("spContext is null");
                }
                using (ClientContext clientContext = RMXUtility.GetSharePointApponlyClientContext(id))
                {
                    clientContext.Load(clientContext.Site,p=>p.Url);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                    GetSubWebProperty(clientContext, id, znodeList);
                    result = JsonConvert.SerializeObject(znodeList);
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                theLog.Error("AsyncSubSiteNode error:" + ex.Message + ex.StackTrace);
                znodeList = new List<ZSiteNodeModel>();
                var node = new ZSiteNodeModel();
                node.name = "failed,please try again";
                node.id = Guid.NewGuid().ToString();
                node.pId = id;
                znodeList.Add(node);
                result = JsonConvert.SerializeObject(znodeList);
                return Json(result);
            }
        }

        [SharePointPermissionsAuthentication("GeneralSetting-FormSubmit")]
        public JsonResult GeneralSettingFormSubmit(GeneralSettingModel info)
        {
            theLog.Debug("GeneralSettingFormSubmit enter");

            //check authorization
            ClientContext clientCtx = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext);
             if (clientCtx==null)
            {
                theLog.Error("GeneralSettingFormSubmit error: you are not authorization");
                return Json(m_strTipWhenSessionTimeout);
            }

            bool testConnectionResult =false;
            try
            {
                RMXConfig cfg = RMXConfig.Instance();
                cfg.JavaPcHost = info.JavaPcHost;
                cfg.OAUTHHost = info.OAUTHHost;
                cfg.ClientSecureID = info.ClientSecureID;
                cfg.ClientSecureKey = info.ClientSecureKey;

                cfg.SecureViewURL = info.SecureViewURL;
                if (cfg.SecureViewURL.EndsWith("/"))
                    cfg.SecureViewURL = cfg.SecureViewURL.TrimEnd('/');

                cfg.RouterURL = info.RouterURL;
                if (cfg.RouterURL.EndsWith("/"))
                    cfg.RouterURL = cfg.RouterURL.TrimEnd('/');

                cfg.AppID = int.Parse(info.AppId);
                cfg.AppKey = info.AppKey;

                cfg.CertificateFileContentBase64 = info.CertificatefileContent;
                cfg.CertificatePassword = info.CertificatefilePassword;
                cfg.CertificateFileName = info.CertificatefileName;

                cfg.WriteConfigToFile();
                CloudAZQuery.Instance.InitParams();
                testConnectionResult = CloudAZQuery.CheckConnection(cfg.JavaPcHost, cfg.OAUTHHost, cfg.ClientSecureID, cfg.ClientSecureKey);
            }
            catch (Exception ex)
            {
                theLog.Error("GeneralSettingFormSubmit error:" + ex.Message);
                return Json("Save failed!");
            }
            theLog.Debug("GeneralSettingFormSubmit end");
            if(!testConnectionResult)
            {
                return Json("test connection failed");
            }
            return Json("Save successfully!");
        }

        [SharePointPermissionsAuthentication("GeneralSetting-EnforceEntityFormSubmit")]
        public JsonResult EnforceEntityFormSubmit(List<ZNodeModel> znodeList)
        {
            theLog.Debug("EnforceEntityFormSubmit enter");
            bool bSuccess = true;
            //check authorization
            ClientContext clientCtx = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext);

            if (clientCtx == null)
            {
                theLog.Error("EnforceEntityFormSubmit error: you are not authorization");
                return Json(m_strTipWhenSessionTimeout);
            }
            List<string> listFailed = new List<string>();
            List<string> webFailed = new List<string>();
            try
            {
                var hostUrl = Request.QueryString["SPHostUrl"];
                if (znodeList == null)
                {
                    theLog.Debug("nodeList == null");
                }
                theLog.Debug("strNodeList:" + znodeList.Count);
                InitRemoteEventRecieverUrl();
                Uri sharePointUrl = new Uri(hostUrl);
                string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
                    sharePointUrl.Authority, TokenHelper.GetRealmFromTargetUrl(sharePointUrl)).AccessToken;
                using (ClientContext clientcontext = TokenHelper.GetClientContextWithAccessToken(hostUrl, apponlyAccessToken))
                {
                    clientcontext.Load(clientcontext.Web, web => web.Title, web => web.Url, web => web.Lists,
                            web => web.EventReceivers);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientcontext);

                    if (string.IsNullOrWhiteSpace(m_strDumbItemUrl))
                    {
                        InitlizeCustomActionUrl(clientcontext);
                    }
                   
                    ZNodeModel rootNode = znodeList[0];
                    UpdateEventStatus(znodeList, rootNode, clientcontext, listFailed, webFailed);
                }
            }
            catch (Exception ex)
            {
                theLog.Error("EnforceEntityFormSubmit error:" + ex.ToString());
                bSuccess = false;
            }
            string strFailed = "";
            if (listFailed.Count > 0)
            {
                strFailed += "\r\nFailed Lists: " + string.Join(", ", listFailed.ToArray());
                bSuccess = false;
            }
            if (webFailed.Count > 0)
            {
                strFailed += "\r\nFailed Webs: " + string.Join(", ", webFailed.ToArray());
                bSuccess = false;
            }
            if (bSuccess)
            {
                theLog.Debug("EnforceEntityFormSubmit end");
                return Json("Save successfully!");
            }
            else
            {
                if (!string.IsNullOrEmpty(strFailed))
                {
                    theLog.Error("EnforceEntityFormSubmit error: " + strFailed);
                    return Json("Save Failed at " + DateTime.Now.ToString() + strFailed);
                }
                else
                {
                    return Json("Save Failed at " + DateTime.Now.ToString());
                }
            }
        }
        [SharePointPermissionsAuthentication("SiteProperty-FormSubmit")]
        public JsonResult SitePropertySubmit(string SitePropertyLevel, List<ZSiteNodeModel> znodeList)
        {
            theLog.Debug("SitePropertySubmit enter");
            //check authorization
            ClientContext clientCtx = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext);
            if (clientCtx == null)
            {
                theLog.Error("SitePropertySubmit error: you are not authorization");
                return Json(m_strTipWhenSessionTimeout);
            }
            try
            {
                Web web = clientCtx.Web;
                clientCtx.Load(web, oweb => oweb.Url);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientCtx);
                string webUrl = web.Url;

                IniFiles sitePropertyFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "SiteProperty.ini");
                sitePropertyFile.IniWriteValue(webUrl, SPOEUtility.SitePropertyLevel, SitePropertyLevel);
                sitePropertyFile.IniWriteValue(webUrl, SPOEUtility.SitePropertyList, JsonConvert.SerializeObject(znodeList));
                return Json("Save successfully!");
            }
            catch (Exception ex)
            {
                theLog.Debug("LibSettingSubmit error:" + ex.Message + ex.StackTrace);
                return Json("Save failed!");
            }
        }
        [SharePointPermissionsAuthentication("GeneralSetting-LibSettingView")]
        public ActionResult LibSettingView()
        {
            theLog.Debug("LibSettingView enter");
            try
            {
                var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
                if (spContext == null)
                {
                    theLog.Debug("spContext is null");
                    return View("Error");
                }
                using (var clientContext = spContext.CreateUserClientContextForSPHost())
                {
                    if (clientContext != null)
                    {
                        string listId = Request.QueryString["listId"];
                        listId = listId.Substring(1, listId.Length - 2);
                        Web web = clientContext.Web;
                        clientContext.Load(web, oweb => oweb.CurrentUser,oweb=>oweb.Lists, oweb => oweb.Url, oweb => oweb.CurrentUser.LoginName);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        List list = web.Lists.GetById(new Guid(listId));
                        clientContext.Load(list,d=>d.BaseTemplate,d=>d.Fields);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        bool isList = false;
                        if(SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate))
                        {
                            isList = true;
                        }
                        //set batch mode status
                        ViewBag.BatchModeStatus = BatchModeWorker.GetBatchModeShowStatus(listId);
                        ViewBag.BatchModeRunning = BatchModeWorker.CheckBatchModeRunning(listId) ? "true" : "false";
                        //ViewBag.BatchModeRunning = BatchModeWorker.CheckBatchModeRunning(listId) ? "Status: In Progress" : "Status: Not Running";
                        ViewBag.BatchModeFailFilesCount = BatchModeWorker.GetBatchModeFailedFilesCount(listId);
                        IniFiles libSettingFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "LibSetting.ini");
                        ViewBag.deleteSourceFile = libSettingFile.IniReadValue(listId, SPOEUtility.strDeleteSourceFileEnable);
                        ViewBag.historyVersion = libSettingFile.IniReadValue(listId, SPOEUtility.strHistoryVersionEnable);
                        ViewBag.isList = isList ? "true" : "false";

                        //get selected columns
                        var strSelectedColumns = libSettingFile.IniReadValue(listId, SPOEUtility.strLibColumns);
                        Dictionary<string, string> selectedColumns = JsonConvert.DeserializeObject<Dictionary<string, string>>(strSelectedColumns);
                        if (selectedColumns == null)
                        {
                            selectedColumns = new Dictionary<string, string>();
                        }
                        List<ZNodeModel> znodeList = new List<ZNodeModel>();
                        foreach (Field field in list.Fields)
                        {
                            ZNodeModel node = new ZNodeModel();
                            node.id = field.InternalName;
                            node.name = field.Title + "("+field.InternalName+")";
                            if (selectedColumns.ContainsKey(field.InternalName))
                            {
                                node.@checked = true;
                            }
                            znodeList.Add(node);
                        }
                        znodeList = znodeList.OrderBy(p => p.name).ToList();
                        ViewBag.columns = JsonConvert.SerializeObject(znodeList);
                        //Schedule data
                        //ViewBag.ScheduleData = libSettingFile.IniReadValue(listId,SPOEUtility.strSchedultList);
                    }
                }
               
            }
            catch (Exception ex)
            {
                theLog.Error("Index error:" + ex.Message + ex.StackTrace);
                return View("Error");
            }
            theLog.Debug("LibSettingView end");
            return View();
        }
        [SharePointPermissionsAuthentication("GeneralSetting-LibSettingSubmit")]
        public JsonResult LibSettingSubmit(string batchModeStatus, string deleteSourceFile,string historyVersion, string selectedColumns)
        {
            theLog.Debug("LibSettingSubmit enter");
            theLog.Debug("deleteSourceFile:"+ deleteSourceFile);
            theLog.Debug("historyVersion:"+ historyVersion);
            theLog.Debug("batchModeStatus:"+ batchModeStatus);
            //check authorization
            ClientContext clientCtx = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext);
            if (clientCtx == null)
            {
                theLog.Error("LibSettingSubmit error: you are not authorization");
                return Json(m_strTipWhenSessionTimeout);
            }
            try
            {
                string listId = Request.QueryString["listId"];
                listId = listId.Substring(1, listId.Length - 2);
                theLog.Debug("listId:" + listId);
                IniFiles libSettingFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "LibSetting.ini");
                libSettingFile.IniWriteValue(listId, SPOEUtility.strDeleteSourceFileEnable, deleteSourceFile);
                libSettingFile.IniWriteValue(listId, SPOEUtility.strHistoryVersionEnable, historyVersion);
                libSettingFile.IniWriteValue(listId, SPOEUtility.strLibColumns, selectedColumns);
                if (batchModeStatus == "true")
                {
                    using (ClientContext clientContext = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext))
                    {
                        BatchModeWorker batchModeWorker = new BatchModeWorker(clientContext, listId);
                        Task.Run(() => batchModeWorker.RunBatchModeForList());
                    }
                }
                //string webUrl = Request.QueryString["SPHostUrl"];
                //SPOEUtility.SetBatchModeTimer(webUrl, listId, scheduleData);
                return Json("Save successfully!");
            }
            catch (Exception ex)
            {
                theLog.Debug("LibSettingSubmit error:" + ex.Message + ex.StackTrace);
                return Json("Save failed!");
            }
        }

        [SharePointPermissionsAuthentication("GeneralSetting-BatchModeFailedView")]
        public ActionResult BatchModeFailedView()
        {
            var listId = Request.QueryString["listId"];
            listId = listId.Substring(1, listId.Length - 2);
            ViewBag.BatchModeFailFilesCount = BatchModeWorker.GetBatchModeFailedFilesCount(listId);
            ViewBag.BatchModeFailFiles = BatchModeWorker.GetBatchModeFailedFiles(listId);
            return View();
        }
        public JsonResult AsyncSubSiteNode(string id,string isParent)
        {
            var znodeList = new List<ZNodeModel>();
            var result = "";
            try
            {
                if (isParent=="true")
                {
                    using (ClientContext clientContext = RMXUtility.GetSharePointApponlyClientContext(id))
                    {
                        domainUrl = Request.QueryString["SPHostUrl"];
                        GetZNodeFromSubsite(clientContext, id, znodeList);
                        result = JsonConvert.SerializeObject(znodeList);
                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Error("AsyncSubSiteNode error:" + ex.Message + ex.StackTrace);
            }
            znodeList = new List<ZNodeModel>();
            var node = new ZNodeModel();
            node.name = "failed,please try again";
            node.id = Guid.NewGuid().ToString();
            node.pId = id;
            znodeList.Add(node);
            result = JsonConvert.SerializeObject(znodeList);
            return Json(result);
        }
        private void UpdateEventStatus(List<ZNodeModel> znodeList,ZNodeModel rootNode,ClientContext clientcontext, List<string> listFailed, List<string> webFailed)
        {
            theLog.Debug("UpdateEventStatus enter");
            if(!rootNode.@checked)
            {
                //list
                List<List> ActiveListCollection = new List<List>();
                List<List> DeactiveListCollection = new List<List>();
                var listNode = znodeList.Where(p => p.isParent == false && p.pId == rootNode.id).ToList();
                foreach (var node in listNode)
                {
                    List list = clientcontext.Web.Lists.GetById(new Guid(node.id));
                    if (node.@checked)
                    {
                        ActiveListCollection.Add(list);
                    }
                    else
                    {
                        DeactiveListCollection.Add(list);
                    }
                }
                //subsite
                var siteNode = znodeList.Where(p => p.isParent == true && p.pId == rootNode.id).ToList();
                foreach (var node in siteNode)
                {
                    Uri subsiteUri = new Uri(node.id);
                    string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
                            subsiteUri.Authority, TokenHelper.GetRealmFromTargetUrl(subsiteUri)).AccessToken;
                    using (ClientContext subsiteClientcontext = TokenHelper.GetClientContextWithAccessToken(node.id, apponlyAccessToken))
                    {
                        subsiteClientcontext.Load(subsiteClientcontext.Web, web => web.Url, web => web.Title, web => web.Lists,
                            web => web.EventReceivers);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(subsiteClientcontext);

                        UpdateEventStatus(znodeList, node, subsiteClientcontext, listFailed, webFailed);
                    }
                }
                //add enforcer for selected list/library
                if (ActiveListCollection.Count > 0)
                {
                    SPOEUtility.AddEnforcerToLibaries(clientcontext, ActiveListCollection,
                                                      SPOEUtility.ListRemoteEventRevieverUrl, m_strRightProtectUrl,
                                                      m_strSecurityViewUrl, m_strEditListUrl,
                                                      false, listFailed);
                }

                //remove enforcer for unselected list/library
                if (DeactiveListCollection.Count > 0)
                {
                    SPOEUtility.RemoveEnforcerToLibaries(clientcontext, DeactiveListCollection,
                                                         false, listFailed);
                }
            }
            else
            {
                // activate for all subsite
                UpdateAllSubSiteEventStatus(clientcontext,clientcontext.Web, listFailed);
            }
        }
        private void UpdateAllSubSiteEventStatus(ClientContext clientContext,Web web, List<string> listFailed)
        {
            List<List> ActiveListCollection = new List<List>();
            clientContext.Load(web, d => d.Lists.Include(l => l.BaseTemplate));
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            foreach (List cellList in web.Lists)
            {
                if (SPOEUtility.SupportedLibraryTypes.Contains(cellList.BaseTemplate) || SPOEUtility.SupportedListTypes.Contains(cellList.BaseTemplate))
                {
                    ActiveListCollection.Add(cellList);
                }
            }
            if (ActiveListCollection.Count > 0)
            {
                SPOEUtility.AddEnforcerToLibaries(clientContext, ActiveListCollection,
                                                  SPOEUtility.ListRemoteEventRevieverUrl, m_strRightProtectUrl,
                                                  m_strSecurityViewUrl, m_strEditListUrl,
                                                  false, listFailed);
            }
            WebCollection subwebs = web.GetSubwebsForCurrentUser(null);
            clientContext.Load(subwebs,d=>d.Include(p=>p.Url));
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            foreach(Web cellWeb in subwebs)
            {
                UpdateAllSubSiteEventStatus(clientContext, cellWeb, listFailed);
            }
        }
        private void InitRemoteEventRecieverUrl()
        {
            theLog.Debug("InitRemoteEventRecieverUrl enter");
            string fullUrl = Request.Url.AbsoluteUri;
            string url1 = Request.Url.ToString();
            string pagesUrl = fullUrl.Substring(0, fullUrl.
                LastIndexOf("GeneralSetting/EnforceEntityFormSubmit", StringComparison.OrdinalIgnoreCase));
            if (pagesUrl.Contains("http://"))
            {
                pagesUrl = pagesUrl.Replace("http://", "https://");
            }
            SPOEUtility.ListRemoteEventRevieverUrl = pagesUrl + "Services/ListEventHandler.svc";
            SPOEUtility.WebRemoteEventRevieverUrl = pagesUrl + "Services/WebEventHander.svc";
            theLog.Debug("SPOEUtility.ListRemoteEventRevieverUrl:"+ SPOEUtility.ListRemoteEventRevieverUrl);
            theLog.Debug("SPOEUtility.WebRemoteEventRevieverUrl:" + SPOEUtility.WebRemoteEventRevieverUrl);
            theLog.Debug("InitRemoteEventRecieverUrl end");
        }
        private void GetZNodeFromSubsite(ClientContext clientContext, string id, List<ZNodeModel> znodeList)
        {
            clientContext.Load(clientContext.Web, d => d.Lists.Include(l => l.Title, l => l.Id, l => l.BaseTemplate, l => l.EventReceivers.Include(e => e.ReceiverName)), d => d.Title, d => d.Url);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            GetListNodes(clientContext, znodeList, id);
            GetSubSiteNodes(clientContext, znodeList, id);
        }
        private void GetListNodes(ClientContext clientContext, List<ZNodeModel> znodeList, string pid)
        {
            ListCollection lists = clientContext.Web.Lists;
            foreach(List cellList in lists)
            {
                if (SPOEUtility.SupportedLibraryTypes.Contains(cellList.BaseTemplate) || SPOEUtility.SupportedListTypes.Contains(cellList.BaseTemplate))
                {
                    AddListToZNode(clientContext, cellList, znodeList, pid);
                }
            }
        }

        private void GetSubSiteNodes(ClientContext clientContext, List<ZNodeModel> znodeList, string pid)
        {
            WebCollection subwebs = clientContext.Web.GetSubwebsForCurrentUser(null);
            clientContext.Load(subwebs);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            foreach (Web cellWeb in subwebs)
            {
                AddSubsiteToZNode(clientContext, cellWeb, znodeList, pid);
            }
        }

        private void AddSubsiteToZNode(ClientContext clientContext, Web web, List<ZNodeModel> znodeList, string pid)
        {
            clientContext.Load(web, oweb => oweb.Title, oweb => oweb.Url);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            string thiswebdomain = web.Url.Substring(0, web.Url.LastIndexOf(".com"));
            string tempdomain = domainUrl.Substring(0, domainUrl.LastIndexOf(".com"));
            if (thiswebdomain.Equals(tempdomain))
            {
                var node = new ZNodeModel();
                node.name = web.Title;
                node.id = web.Url;
                node.pId = pid;
                node.isParent = true;
                znodeList.Add(node);
            }
        }
        private void AddListToZNode(ClientContext clientContext, List list, List<ZNodeModel> znodeList,string pid)
        {
            var node = new ZNodeModel();
            node.name = list.Title;
            node.id = list.Id.ToString();
            node.pId = pid;
            node.@checked = SPOEUtility.CheckEvent(clientContext.Web, list);
            znodeList.Add(node);
        }

        private void AddSiteToZNode(ClientContext clientContext, Web web, List<ZNodeModel> znodeList, string pid)
        {
            clientContext.Load(web, oweb => oweb.Title, oweb => oweb.Url);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            string thiswebdomain = web.Url.Substring(0, web.Url.LastIndexOf(".com"));
            string tempdomain = domainUrl.Substring(0, domainUrl.LastIndexOf(".com"));
            if (thiswebdomain.Equals(tempdomain))
            {
                var node = new ZNodeModel();
                node.name = web.Title;
                node.id = web.Url;
                node.pId = pid;
                node.isParent = true;
                node.isLoaded = true;
                znodeList.Add(node);

                Uri site = new Uri(web.Url);
                string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
                        site.Authority, TokenHelper.GetRealmFromTargetUrl(site)).AccessToken;
                using (ClientContext subClientContext = TokenHelper.GetClientContextWithAccessToken(web.Url, apponlyAccessToken))
                {
                    subClientContext.Load(subClientContext.Web, subweb => subweb.Title, subweb => subweb.Url, 
                        subweb => subweb.Lists.Include(olist => olist.Title, olist => olist.Id, olist => olist.BaseTemplate,
                        olist => olist.EventReceivers.Include( eventReceiver => eventReceiver.ReceiverName)));
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(subClientContext);
                    pid = node.id;
                    GetListNodes(subClientContext, znodeList, pid);
                    GetSubSiteNodes(subClientContext, znodeList, pid);
                }
            }
        }       

        public static bool InitlizeCustomActionUrl(ClientContext clientcontext)
        {
            // query ECB Action url
            try
            {
                UserCustomActionCollection userCustomActionColl = clientcontext.Web.UserCustomActions;
                clientcontext.Load(userCustomActionColl);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientcontext);

                foreach (var action in userCustomActionColl)
                {
                    if (action.Title.Equals("RMX DUMB ITEM", StringComparison.OrdinalIgnoreCase))
                    {
                        m_strDumbItemUrl = action.Url;
                        theLog.Info("RMX DUMB ITEM Url:" + m_strDumbItemUrl);
                        break;
                    }
                }


                //create right protect url
                if (string.IsNullOrWhiteSpace(m_strRightProtectUrl))
                {
                    m_strRightProtectUrl = m_strDumbItemUrl.Replace("View", "Protect");
                    theLog.Info("Right Protect Url:" + m_strRightProtectUrl);
                }



                //create security view
                if (string.IsNullOrWhiteSpace(m_strSecurityViewUrl))
                {
                    m_strSecurityViewUrl = m_strDumbItemUrl;
                    theLog.Info("Security Url:" + m_strSecurityViewUrl);
                }


                //create editList url
                if (string.IsNullOrWhiteSpace(m_strEditListUrl))
                {
                    m_strEditListUrl = m_strDumbItemUrl.Replace("RightsManage", "GeneralSetting");
                    m_strEditListUrl = m_strEditListUrl.Replace("View", "LibSettingView");
                    theLog.Info("EditList Url:" + m_strEditListUrl);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on InitlizeCustomActionUrl:" + ex.ToString());
                return false;
            }
        }

        private List<SitePropertyModel> GetWebProperty(Web web,ZSiteNodeModel node)
        {
            List<SitePropertyModel> siteProperties = new List<SitePropertyModel>();
            try
            {
                foreach (KeyValuePair<string,object> dic in web.AllProperties.FieldValues)
                {
                    var prop = new SitePropertyModel();
                    prop.displayName = dic.Key;
                    if (prop.displayName == "")
                    {
                        continue;
                    }
                    if (node != null)
                    {
                        var property = node.siteProperties.Where(p => p.displayName == dic.Key).FirstOrDefault();
                        if (property != null)
                        {
                            prop.@checked = true;
                        }
                    }
                    siteProperties.Add(prop);
                }
                siteProperties = siteProperties.OrderBy(p => p.displayName).ToList();
            }
            catch (Exception ex)
            {
                var prop = new SitePropertyModel();
                prop.displayName = "failed,please try again";
                siteProperties.Add(prop);
                theLog.Debug("GetWebProperty error:"+ex.Message+ex.StackTrace);
            }
            return siteProperties;
        }
        private void GetSubWebProperty(ClientContext clientContext, string id, List<ZSiteNodeModel> znodeList)
        {
            WebCollection subwebs = clientContext.Web.GetSubwebsForCurrentUser(null);
            clientContext.Load(subwebs,webs=> webs.Include(p=>p.Title,p=>p.Url,p=>p.AllProperties));
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            //read site property from file
            IniFiles sitePropertyFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "SiteProperty.ini");
            var siteJson = sitePropertyFile.IniReadValue(clientContext.Site.Url, SPOEUtility.SitePropertyList);
            List<ZSiteNodeModel> sitesNodeList = JsonConvert.DeserializeObject<List<ZSiteNodeModel>>(siteJson);
            if (sitesNodeList == null)
            {
                sitesNodeList = new List<ZSiteNodeModel>();
            }
            ZSiteNodeModel node = null;
            foreach (Web cellWeb in subwebs)
            {
                ZSiteNodeModel subNode = new ZSiteNodeModel();
                subNode.name = cellWeb.Title;
                subNode.id = cellWeb.Url;
                subNode.pId = id;
                subNode.isParent = true;
                node = sitesNodeList.Where(p => p.id == cellWeb.Url).FirstOrDefault();
                subNode.siteProperties = GetWebProperty(cellWeb,node);
                znodeList.Add(subNode);
            }
        }
    }
}