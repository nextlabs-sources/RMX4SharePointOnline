using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SkyDrmRestHelp;
using RmxForSPOWeb.Common;
using Microsoft.SharePoint.Client;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace RmxForSPOWeb.Controllers
{

        [DataContract]
        class TagInfo
        {
            [DataMember]
        public string tagname { get; set; }

            [DataMember]
        public string[] tagvalue { get; set; }
        }

      
        public class RightsManageController : Controller
    {
        protected static CLog theLog = CLog.GetLogger("RightsManageController");
        public static readonly string m_strActionEncrypt = "encrypt";
        public static readonly string m_strActionSecView = "secureview";
      //  private static readonly string m_strTipWhenSessionTimeout = "You are not authorization. Please try again by refresh this page or by launching the app installed on your site.";
        IniFiles listconfigFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "ListSeeting.ini");
        [ActionName("View")]
        public ActionResult ViewAction(string listId, string itemId, string siteUrl, string SPHostUrl)
        {
            theLog.Debug( string.Format("Enter RightsManageController ViewAction. listId:{0}, itemId:{1},  SiteUrl:{2}", listId, itemId, siteUrl));

            //call GetSharePointContext to load and save the SharePoint context inorder to get sharepoint context after we redirect to other pages 
            SharePointContext spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
            var commonMessageConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~")
               .GetSection("CommonMessageConfig") as RmxForSPOWeb.Common.ConfigFileUtility.CommonMessageConfig;
            ClientContext clientContextAppOnly = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext); // RMXUtility.GetSharePointApponlyClientContext(siteUrl);
            if (clientContextAppOnly != null)
            {
                List docLibrary = null;
                ListItem listItem = null;
                try
                {
                    clientContextAppOnly.Load(clientContextAppOnly.Web, oweb => oweb.Lists, oweb => oweb.Url,
                              oweb => oweb.CurrentUser);

                    docLibrary = clientContextAppOnly.Web.Lists.GetById(new Guid(listId));
                    clientContextAppOnly.Load(docLibrary, d => d.Id, d => d.Title, d => d.BaseType, d => d.BaseTemplate);

                    listItem = docLibrary.GetItemById(itemId);
                    clientContextAppOnly.Load(listItem, item => item.Id, item => item.File, item=>item.FileSystemObjectType);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextAppOnly);

                    //unsupport for list, folder
                    bool bUnSupportItem = false;
                    if (SPOEUtility.SupportedListTypes.Contains(docLibrary.BaseTemplate))
                    {
                        bUnSupportItem = true;
                    }
                    else if (SPOEUtility.SupportedLibraryTypes.Contains(docLibrary.BaseTemplate))
                    {
                        if (listItem.FileSystemObjectType!=FileSystemObjectType.File)
                        {
                            bUnSupportItem = true;
                        }
                    }
                    if (bUnSupportItem)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.SVNotSupportItem + "');history.go(-1);</script>");
                        theLog.Error("View file failed. not support item.");
                        return View("ViewAction");
                    }
                   
                    clientContextAppOnly.Load(listItem.File, d => d.ServerRelativeUrl);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextAppOnly);

                    //non nxl  file
                    if (!listItem.File.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase))
                    {
                        string strMsg = string.Format("View file: {0} failed. Only support (.nxl) file.", listItem.File.Name);
                        Response.Write("<script>alert('" + commonMessageConfig.SVNotNxlFile + "');history.go(-1);</script>");
                        theLog.Info(strMsg);
                        //"Response.Write()" need "return View()" function after
                        return View("ViewAction");
                    }
                    var domian = SPOEUtility.GetDomainFromWebUrl(clientContextAppOnly.Web.Url);
                    var spFile = listItem.File;

                    var finalIndex = spFile.ServerRelativeUrl.LastIndexOf("/");
                    var ServerRelativeUrl = spFile.ServerRelativeUrl.Substring(0, finalIndex);
                    ViewBag.BackSiteUrl = domian + ServerRelativeUrl;
                    
                }
                catch (Exception ex)
                {
                    Response.Write("<script>alert('" + commonMessageConfig.SVExceptionMessage + "');history.go(-1);</script>");
                    theLog.Error("Exception on get file property:" + ex.ToString());
                    return View("ViewAction");
                }
                //login to skydrm
                LoginData ld = SkyDrmSessionMgr.GetSkyDrmLoginData(Request.Cookies);
                if (ld == null)
                {
                    ld = SkyDrmSessionMgr.LoginSkyDrmByTrustApp(clientContextAppOnly,clientContextAppOnly.Web, Response);
                    if (ld == null)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.FailedLoginSkydrm + "');history.go(-1);</script>");
                        return View("ViewAction");
                    }
                }
               

                try
                {
                    ClientResult<Stream> clientResult = listItem.File.OpenBinaryStream();
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextAppOnly);

                    byte[] bytesFile = null;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        clientResult.Value.CopyTo(ms);
                        bytesFile = ms.ToArray();
                    }

                    if (bytesFile != null)
                    {
                        RemoteViewResult rvResult = SkyDrmSessionMgr.RemoteView(listItem.File.Name, bytesFile, ld);

                        if ((rvResult != null) && SkyDrmSessionMgr.IsUnAuth(rvResult.statusCode))
                        {
                            theLog.Info("SkyDrmSessionMgr.RemoteView return unAuth, login again");
                            ld = SkyDrmSessionMgr.LoginSkyDrmByTrustApp(clientContextAppOnly,clientContextAppOnly.Web, Response);
                            if (ld == null)
                            {
                                Response.Write("<script>alert('" + commonMessageConfig.FailedLoginSkydrm + "');history.go(-1);</script>");
                                return View("ViewAction");
                            }

                            rvResult = SkyDrmSessionMgr.RemoteView(listItem.File.Name, bytesFile, ld);
                        }

                        if (rvResult!=null)
                        {
                            if (rvResult.statusCode == (int)HttpStatusCode.OK && rvResult.remoteViewData != null)
                            {
                                theLog.Info(string.Format("SkyDrmSessionMgr.RemoteView success: {0}, {1}", rvResult.remoteViewData.Cookie.ToString(), rvResult.remoteViewData.viewUrl));
                                return RedirectToRemoteViewer(rvResult.remoteViewData);
                            }
                            else
                            {
                                string strMsg = string.Format("View file: {0} failed. message:{1}", listItem.File.Name, rvResult.message);
                                Response.Write("<script>alert('" + commonMessageConfig.SVRemoteViewFailed + rvResult.message + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                                theLog.Info(string.Format("SkyDrmSessionMgr.RemoteView failed, status:{0}, message:{1}", rvResult.statusCode, rvResult.message));
                                return View("ViewAction");
                            }
                        }
                        else
                        {
                            string strMsg = string.Format("View file: {0} failed. pleae try again later", listItem.File.Name);
                            Response.Write("<script>alert('" + commonMessageConfig.SVTryAgainLater + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                            theLog.Error("ViewAction SkyDrmSessionMgr.RemoteView return null.");
                            return View("ViewAction");
                        }
                    }
                    else
                    {
                        string strMsg = "View file failed. get file content failed.";
                        Response.Write("<script>alert('" + commonMessageConfig.SVGetFileContentFailed + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                        theLog.Error(strMsg);
                        return View("ViewAction");
                    }
                }
                catch (Exception ex)
                {
                    Response.Write("<script>alert('" + commonMessageConfig.SVExceptionMessage + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                    theLog.Error("Exception on secure view:" + ex.ToString());
                    return View("ViewAction");
                }
            }
            else
            {
                Response.Write("<script>alert('" + commonMessageConfig.SVExceptionMessage + "');history.go(-1);</script>");
                return View("ViewAction");
            }
        }

        public ActionResult Protect(string id, string listId, string itemId, string siteUrl, string SPHostUrl, string classifyResult)
        {
            theLog.Debug(string.Format("Enter RightsManageController Protect. id:{3}, listId:{0}, itemId:{1},  SiteUrl:{2}, SPHostUrl:{5}, classifyResult:{4}",
                listId, itemId, siteUrl, id, classifyResult, SPHostUrl));

            //call GetSharePointContext to load and save the SharePoint context in order to get SharePoint context after we redirect to other pages 
            SharePointContext spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
            
            //get file name
            ListItem listItem = null;
            List list = null;
            Microsoft.SharePoint.Client.File spFile = null;
            var commonMessageConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~")
                .GetSection("CommonMessageConfig") as RmxForSPOWeb.Common.ConfigFileUtility.CommonMessageConfig;
            ClientContext clientContextInUse = RMXUtility.GetSharePointCurrentUserClientContext(HttpContext);  // RMXUtility.GetSharePointApponlyClientContext(siteUrl);
            try
            {
                if (clientContextInUse != null)
                {
                    clientContextInUse.Load(clientContextInUse.Web, oweb => oweb.Lists, oweb => oweb.Url,
                              oweb => oweb.CurrentUser);

                    list = clientContextInUse.Web.Lists.GetById(new Guid(listId));
                    clientContextInUse.Load(list, d => d.Id, d => d.Title, d => d.BaseType, d => d.BaseTemplate,d=>d.DefaultViewUrl);

                    clientContextInUse.Load(clientContextInUse.Web.CurrentUser,d=>d.LoginName);

                    listItem = list.GetItemById(itemId);
                    clientContextInUse.Load(listItem, item=>item.FileSystemObjectType, item=>item.DisplayName, item => item.Id, item => item.File, item => item.ParentList);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                }
                else
                {
                    theLog.Error("Failed to create context for current user.");
                }
            }
            catch (System.Exception ex)
            {
                Response.Write("<script>alert('" + commonMessageConfig.RPExceptionMessage + "');history.go(-1);</script>");
                theLog.Error("Exception on get file property:" + ex.ToString());
                return View();
            }
            try
            {
                var domian = SPOEUtility.GetDomainFromWebUrl(clientContextInUse.Web.Url);
                //check file status
                if (SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate))
                {
                    if (listItem.FileSystemObjectType == FileSystemObjectType.File)
                    {
                        spFile = listItem.File;
                        clientContextInUse.Load(spFile, file => file.Name, file => file.CheckOutType, file => file.LockedByUser, file => file.ServerRelativeUrl);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        var finalIndex = spFile.ServerRelativeUrl.LastIndexOf("/");
                        var ServerRelativeUrl = spFile.ServerRelativeUrl.Substring(0, finalIndex);
                        ViewBag.BackSiteUrl = domian + ServerRelativeUrl;
                        clientContextInUse.Load(spFile.LockedByUser, p => p.UserPrincipalName);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        ViewBag.fileName = listItem.File.Name;

                        //not support onenote
                        if (spFile.Name.EndsWith(".one", StringComparison.OrdinalIgnoreCase))
                        {
                            Response.Write("<script>alert('" + commonMessageConfig.RPOneNoteFile + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                            theLog.Info("Manual protect,return for the file is .one file.");
                            return View();
                        }
                        //not support .nxl
                        if (spFile.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase))
                        {
                            Response.Write("<script>alert('" + commonMessageConfig.RPNxlFile + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                            theLog.Info("Manual protect,return for the file is .nxl file.");
                            return View();
                        }
                        //not support read permission
                        try
                        {
                            clientContextInUse.Load(listItem, olistItem => olistItem.EffectiveBasePermissions);
                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                            if (!listItem.EffectiveBasePermissions.Has(PermissionKind.EditListItems))
                            {
                                Response.Write("<script>alert('" + commonMessageConfig.RPUserPermission + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                                theLog.Info("Manual protect,return for User has no permission to protect this item.");
                                return View();
                            }
                        }
                        catch (Exception)
                        {
                            //when login user has only view access,ExecuteQuery function will throw exception
                            Response.Write("<script>alert('" + commonMessageConfig.RPUserPermission + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                            theLog.Info("Manual protect,return for User has no permission to protect this item.");
                            return View();
                        }
                       
                        //checkout status
                        if (spFile.CheckOutType != CheckOutType.None)
                        {
                            Response.Write("<script>alert('" + commonMessageConfig.RPFileCheckedOut + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                            theLog.Info("Manual protect,return for the file is checked out.");
                            return View();
                        }

                        //lock status
                        string strLockedByUser = RMXUtility.GetUserPrincipalName(spFile.LockedByUser);
                        if (!string.IsNullOrWhiteSpace(strLockedByUser))
                        {
                            Response.Write("<script>alert('" + commonMessageConfig.RPFileLocked + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                            theLog.Info("Rights Protection return for:this file is locked by:" + strLockedByUser);
                            return View();
                        }
                    }
                    else
                    {
                        clientContextInUse.Load(listItem.Folder, d => d.ServerRelativeUrl);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        var finalIndex = listItem.Folder.ServerRelativeUrl.LastIndexOf("/");
                        var ServerRelativeUrl = listItem.Folder.ServerRelativeUrl.Substring(0, finalIndex);
                        ViewBag.BackSiteUrl = domian + ServerRelativeUrl;
                        ViewBag.fileName = listItem.DisplayName;
                        Response.Write("<script>alert('" + commonMessageConfig.RPFolder + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                        theLog.Info("Rights Protection return for: folder not supported:" + listItem.DisplayName);
                        return View();
                    }

                }
                else if (SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate))
                {
                    ViewBag.BackSiteUrl = domian + list.DefaultViewUrl;
                    ViewBag.fileName = "all attachments in " + listItem.DisplayName;
                    AttachmentCollection attachmentCollection = listItem.AttachmentFiles;
                    clientContextInUse.Load(attachmentCollection);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);

                    //check permission,not support read permission
                    try
                    {
                        clientContextInUse.Load(listItem, olistItem=>olistItem.EffectiveBasePermissions);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        if (!listItem.EffectiveBasePermissions.Has(PermissionKind.EditListItems))
                        {
                            theLog.Info("Manual protect,return for User has no permission to protect this item.");
                            string strJs = "<script>alert('" + commonMessageConfig.RPUserPermission + "');location.href='" + ViewBag.BackSiteUrl + "';</script>";
                            Response.Write(strJs);
                            return View();
                        }
                    }
                    catch (Exception)
                    {
                        //when login user has only view access,ExecuteQuery function will throw exception
                        Response.Write("<script>alert('" + commonMessageConfig.RPUserPermission + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                        theLog.Info("Manual protect,return for User has no permission to protect this item. 2");
                        return View();
                    }

                    //check attachment count
                    if (attachmentCollection.Count == 0)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.RPNoAttachment + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                        return View();
                    }

                    //check if attachment contains nxl
                    bool isNxl = true;
                    foreach (Attachment attachment in attachmentCollection)
                    {
                        clientContextInUse.Load(attachment, d => d.ServerRelativeUrl);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        string serverRelativeUrl = attachment.ServerRelativeUrl;
                        Microsoft.SharePoint.Client.File attachfile = clientContextInUse.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
                        clientContextInUse.Load(attachfile, p => p.ServerRelativeUrl, p => p.Name, p => p.LockedByUser, p => p.CheckOutType);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        if (!attachfile.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase))
                        {
                            isNxl = false;
                            break;
                        }
                    }
                    if(isNxl)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.RPFullOfAttachment + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                        return View();
                    }
                }
                else
                {
                    ViewBag.BackSiteUrl = domian + list.DefaultViewUrl;
                    Response.Write("<script>alert('" + commonMessageConfig.RPOtherList + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                    return View();
                }
               
                //login to skydrm
                LoginData ld = SkyDrmSessionMgr.GetSkyDrmLoginData(Request.Cookies);
                if (ld == null)
                {
                    ld = SkyDrmSessionMgr.LoginSkyDrmByTrustApp(clientContextInUse,clientContextInUse.Web, Response);
                    if (ld == null)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.FailedLoginSkydrm + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                        return View();
                    }
                }

                {
                    DoRmxForProtect(clientContextInUse, list, listItem, id, listId, itemId, siteUrl, SPHostUrl, classifyResult, ld, commonMessageConfig);
                    return View();
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script>alert('" + commonMessageConfig.RPExceptionMessage + "');location.href='" + list.DefaultViewUrl + "';</script>");
                string strLog = string.Format("Exception on Protect,  listId:{0}, itemId:{1},  SiteUrl:{2}, Ex:{3}", listId, itemId, siteUrl, ex.ToString());
                theLog.Error(strLog);
                return View();
            }
        }

        private void DoRmxForProtect(ClientContext clientContextInUse, List list, ListItem listItem, string id,
            string listId, string itemId, string siteUrl, string SPHostUrl, string classifyResult, LoginData ld, RmxForSPOWeb.Common.ConfigFileUtility.CommonMessageConfig commonMessageConfig)
        {
            if (string.Equals(id, "submit", StringComparison.OrdinalIgnoreCase))
            {
                //get tags and format ti
                List<TagInfo> TagResult = JsonConvert.DeserializeObject<List<TagInfo>>(classifyResult);// JsonHelp.LoadFromJson<TagInfo[]>(classifyResult);
                Dictionary<string, string> dicTags = new Dictionary<string, string>();
                foreach (TagInfo tag in TagResult)
                {
                    string strValues = string.Join(SPOEUtility.TagSeparator, tag.tagvalue);
                    dicTags.Add(tag.tagname, strValues);
                }

                //encrypt
                bool bEncrypt = true;
                if (SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate))
                {
                    bEncrypt = RmxModule.EncryptItemVerstions(clientContextInUse, list, listItem, listItem.File, dicTags);
                    if (bEncrypt)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.RPSuccessed + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                    }
                    else
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.RPLibraryFailed + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                    }
                }
                else if (SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate))
                {
                    AttachmentCollection attachmentCollection = listItem.AttachmentFiles;
                    foreach (Attachment attachment in attachmentCollection)
                    {
                        clientContextInUse.Load(attachment, d => d.ServerRelativeUrl);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        string serverRelativeUrl = attachment.ServerRelativeUrl;
                        Microsoft.SharePoint.Client.File attachfile = clientContextInUse.Web.GetFileByServerRelativeUrl(serverRelativeUrl);

                        clientContextInUse.Load(attachfile, p => p.ServerRelativeUrl, p => p.Name, p => p.LockedByUser, p => p.CheckOutType);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextInUse);
                        if(attachfile.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
						string fileUrl = SPOEUtility.GetDomainFromWebUrl(clientContextInUse.Web.Url) + attachfile.ServerRelativeUrl;
                        theLog.Debug("fileUrl:" + fileUrl);                    
                        bool bRmx = RmxModule.EncryptItemVerstions(clientContextInUse, list, listItem, attachfile, dicTags);
                        if (!bRmx && bEncrypt)
                        {
                            bEncrypt = false;
                        }
                    }
                    if (bEncrypt)
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.RPSuccessed + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                    }
                    else
                    {
                        Response.Write("<script>alert('" + commonMessageConfig.RPListFailed + "');location.href='" + ViewBag.BackSiteUrl + "';</script>");
                    }
                }
                theLog.Info("RightsManageController::Protect encrypt result:" + bEncrypt.ToString());
            }
            else
            {
                ViewBag.listId = listId;
                ViewBag.itemId = itemId;
                ViewBag.siteUrl = siteUrl;
                ViewBag.SPHostUrl = SPHostUrl;

                var tenantName = listconfigFile.IniReadValue(listId, "TenantName");
                //get classification information
                ClassificationResult clsRest = SkyDrmSessionMgr.GetClassificationResult(ld, "");
                if ((clsRest != null) && SkyDrmSessionMgr.IsUnAuth(clsRest.statusCode))
                {
                    theLog.Info("SkyDrmSessionMgr.GetClassificationResult return unAuth, login again");
                    ld = SkyDrmSessionMgr.LoginSkyDrmByTrustApp(clientContextInUse,clientContextInUse.Web, Response);
                    if (ld == null)
                    {
                        theLog.Info("SkyDrmSessionMgr.GetClassificationResult,login again failed.");
                        return;
                    }

                    clsRest = SkyDrmSessionMgr.GetClassificationResult(ld, "");
                }
               
                if (clsRest == null)
                {
                    theLog.Error(string.Format("GetClassificationResult return null"));
                }
                else if ((clsRest.statusCode == (int)HttpStatusCode.OK))
                {
                    if (clsRest.data != null)
                        ViewBag.classifyData = clsRest.data;
                    else
                        theLog.Warn(string.Format("GetClassificationResult successed with no classfication data, user:{0}", ld.userName));
                }
                else
                {
                    theLog.Error(string.Format("GetClassificationResult failed. message:{0}", clsRest.message));
                }
            }
        }

        public ActionResult Login(string id, string nextAction, string listId, string itemId, string siteUrl, string SPHostUrl, string skyDrmUserName, string skyDrmPassword)
        {
            theLog.Debug( string.Format("Enter RightsManageController Login. id:{0}, nextAction:{1}, listId:{2}, itemId:{3}", id, nextAction, listId, itemId) );

            ViewBag.nextAction = nextAction;
            ViewBag.listId = listId;
            ViewBag.itemId = itemId;
            ViewBag.siteUrl = siteUrl;
            ViewBag.SPHostUrl = SPHostUrl;
            ViewBag.errorMsg = "";

            if (string.Equals(id, "submit", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.errorMsg = "Log in failed, please try again latter.";
                LoginResult loginRes =  SkyDrmSessionMgr.LoginToSkyDrm(skyDrmUserName, skyDrmPassword);
                if(loginRes!=null)
                {
                    theLog.Debug( string.Format("LoginToSkyDrm return not null, message:{0}", loginRes.message) );

                    if(loginRes.loginData!=null)
                    {
                        string strSessionGuid = Guid.NewGuid().ToString();
                        SkyDrmSessionMgr.AddedSkyDrmSessionInfo(strSessionGuid, loginRes.loginData);

                        //response cookie
                        HttpCookie ck = new HttpCookie(SkyDrmSessionMgr.m_strSkyDrmSessionKey);
                        ck.Value = strSessionGuid;
                        Response.Cookies.Add(ck);

                        //do action
                        if (string.Equals(nextAction, m_strActionEncrypt, StringComparison.OrdinalIgnoreCase))
                        {
                            string strListid = listId;
                            string strItemid = itemId;
                            string strSiteUrl = siteUrl;
                            string strSPHostUrl = SPHostUrl;
                            return RedirectToAction("Protect", new
                            {
                                listId = strListid,
                                itemId = strItemid,
                                siteUrl = strSiteUrl,
                                SPHostUrl = strSPHostUrl //in order to get sharepoint context, we must pass this paramater.
                            }
                            );
                        }
                        else if (string.Equals(nextAction, m_strActionSecView, StringComparison.OrdinalIgnoreCase))
                        {
                            string strListid = listId;
                            string strItemid = itemId;
                            string strSiteUrl = siteUrl;
                            string strSPHostUrl = SPHostUrl;
                            return RedirectToAction("View", new
                            {
                                listId = strListid,
                                itemId = strItemid,
                                siteUrl = strSiteUrl,
                                SPHostUrl = strSPHostUrl //inorder to get sharepoint context, we must pass this paramater.
                            }
                            );
                        }
                    }
                    else
                    {
                        theLog.Debug(string.Format("LoginToSkyDrm return loginData null"));     
                        ViewBag.errorMsg = loginRes.message;
                    }
                } 
                else
                {
                    theLog.Debug(string.Format("LoginToSkyDrm return  null"));
                }             
            }

            return View();
        }

    /*    protected ActionResult RedirectToLoginPage(string strAction, string strListId, string strItemId, string strSiteUrl, string strSPHostUrl)
        {
            return RedirectToAction("Login", new {nextAction=strAction, listId=strListId, itemId = strItemId, siteUrl=strSiteUrl, SPHostUrl=strSPHostUrl });
        }
        */

        protected ActionResult RedirectToRemoteViewer(RemoteViewData rvData)
        {
            //modify domain property of cookie.
            string strOutDomain;
            for (int i = 0; i < rvData.Cookie.Length; i++)
            {
                string strCookie = rvData.Cookie[i];
                string strNewCookie = ChangeCookieDomain(strCookie, ".edrm.cloudaz.com", out strOutDomain);
                HttpContext.Response.Headers.Add("Set-Cookie", strNewCookie);
            }

              return Redirect(rvData.viewUrl);
           // return Redirect("https://www.baidu.com");

        }

        public static string ChangeCookieDomain(string ckstr, string newdomain, out string olddomain)
        {
            olddomain = "";
            string ret = null;
            char[] sep = { ';' };
            string[] strs = ckstr.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            for (int idx = 0; idx < strs.Length; ++idx)
            {
                string str = strs[idx];
                if (str.Contains("="))
                {
                    char[] eq = { '=' };
                    string[] kv = str.Split(eq, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length >= 2)
                    {
                        if (kv[0].Trim().ToLower() == "Domain".ToLower())
                        {
                            olddomain = kv[1];
                            kv[1] = newdomain;
                            strs[idx] = string.Join("=", kv);
                        }
                    }
                }
                else if ("Secure".ToLower() == str.Trim().ToLower())
                {
                    // to do
                    strs[idx] = "";
                }
            }
            ret = string.Join(";", strs);
            return ret;
        }

        public ActionResult ListSettingView()
        {
            try
            {
                var listId = Request.QueryString["listId"];
                listId = listId.Substring(1, listId.Length - 2);
                var backUpEnable = listconfigFile.IniReadValue(listId, SPOEUtility.strBackUpEnable);
                var backUpPath = listconfigFile.IniReadValue(listId, SPOEUtility.strBackUpPath);
                var projectName = listconfigFile.IniReadValue(listId, SPOEUtility.strProjectName);
                var projectTenantName = listconfigFile.IniReadValue(listId, SPOEUtility.strProjectTenantName);
                ViewBag.backUpPath = backUpPath;
                ViewBag.projectName = projectName;
                ViewBag.projectTenantName = projectTenantName;
                ViewBag.backUpEnable = backUpEnable;
            }
            catch (Exception ex)
            {
                theLog.Debug("ListSettingView ex:"+ex.Message+ex.StackTrace);
            }
           
            return View();
        }
        public JsonResult ListSettingSubmit(string backUpPath,string projectName, string projectTenantName, string backUpEnable)
        {
            try
            {
                var listId = Request.QueryString["listId"];
                var hostUrl = Request.QueryString["SPHostUrl"];
                listId = listId.Substring(1, listId.Length - 2);
                //Set value
                string strSectionListID = listId;
                listconfigFile.IniWriteValue(listId, SPOEUtility.strBackUpEnable, backUpEnable);
                listconfigFile.IniWriteValue(listId, SPOEUtility.strBackUpPath, backUpPath);
                listconfigFile.IniWriteValue(listId, SPOEUtility.strProjectName, projectName);
                listconfigFile.IniWriteValue(listId, SPOEUtility.strProjectTenantName, projectTenantName);
                //check backUpPath
                Uri sharePointUrl = new Uri(hostUrl);
                string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
                    sharePointUrl.Authority, TokenHelper.GetRealmFromTargetUrl(sharePointUrl)).AccessToken;
                using (ClientContext clientcontext = TokenHelper.GetClientContextWithAccessToken(hostUrl, apponlyAccessToken))
                {
                    clientcontext.Load(clientcontext.Web,d=>d.Url);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientcontext);
                    var isCorrectPath = SPOEUtility.CheckBackUpPath(clientcontext,backUpPath);
                    if(!isCorrectPath)
                    {
                        return Json("This backUp path is unreachable!");
                    }
                }
                return Json("save successful");
            }
            catch (Exception ex)
            {
                theLog.Debug("ListSettingSubmit ex:"+ex.Message+ex.StackTrace);
                return Json("save failed");
            }
        }
    }
}