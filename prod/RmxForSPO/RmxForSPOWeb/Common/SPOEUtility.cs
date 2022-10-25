using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using QueryCloudAZSDK;
using QueryCloudAZSDK.CEModel;
using RmxForSPOWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RmxForSPOWeb.Controllers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RmxForSPOWeb.Common
{
    public class SPOEUtility
    {
        protected static CLog theLog = CLog.GetLogger("SPOEUtility");
        public static List<int> SupportedLibraryTypes = new List<int> { 101, 109, 115, 119, 700, 850, 851, 1302 };
        public static List<int> SupportedListTypes = new List<int> { 100, 102, 103, 104, 105, 106, 107, 108, 120, 170, 171, 1100 };
        public static int MaxRequestCount = 20;
        public const string StrRmxAction = "EDIT";
        public const string StrRmxObName = "RMX";
        public static string ListRemoteEventRevieverUrl = "";
        public static string WebRemoteEventRevieverUrl = "";
        public static readonly string WebRemoteRecieverName = "RmxForSPOWebEventHander";
        public static readonly string ListRemoteRecieverName = "RmxForSPOListEventHandler";
        public static readonly string ListEditItemName = "RmxForSPOEditListItem";
        public static readonly string RightProtectItemName = "RmxForSPORightProtectItem";
        public static readonly string SecurityViewItemName = "RmxForSPOSecurityViewItem";
        public static readonly string BatchModeStatus = "BatchModeStatusForRmx";
        public static readonly string strBackUpPath = "BackUpPath";
        public static readonly string strProjectName = "ProjectName";
        public static readonly string strProjectTenantName = "ProjectTenantName";
        public static readonly string strBackUpEnable = "BackUpEnable";
        public static readonly string strDeleteSourceFileEnable = "DeleteSourceFileEnable"; 
        public static readonly string strHistoryVersionEnable = "HistoryVersionEnable"; 
        public static readonly string strLibColumns = "LibColumns";
        public static readonly string SitePropertyLevel = "SitePropertyLevel"; 
        public static readonly string SitePropertyList = "SitePropertyList";
        public static readonly string strSchedultList = "Schedule";
        public const string ColumnSeparator = "\r\n";
        public const string TagSeparator = "\n";
        public static char[] TrimFlag = new char[] { '/' };

        public enum SitePropLevel
        {
            None,
            Subsite,
            SiteCollection,
            Both
        }

        // Schedule timmer
        private static Dictionary<string, List<Timer>> DicScheduleTimmers = new Dictionary<string, List<Timer>>();
        private static object ScheduleLock = new object();

        public static string GetDomainFromWebUrl(string webUrl)
        {
            int pos = -1;
            pos = webUrl.IndexOf("/", 8);
            return webUrl.Substring(0, pos);
        }
        //public static void DoBatchMode(ClientContext clientContext, List<BatchModeFailedModel> listFailedItem)
        //{
        //    clientContext.Load(clientContext.Web, p => p.Lists, p => p.Url);
        //    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //    //batch mode for list
        //    BatchModeForListCollection(clientContext, clientContext.Web.Lists,listFailedItem);
        //    //batch mode for subsite
        //    WebCollection webList = clientContext.Web.GetSubwebsForCurrentUser(null);
        //    clientContext.Load(webList);
        //    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //    foreach (Web subWeb in webList)
        //    {
        //        clientContext.Load(subWeb, p => p.Url);
        //        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //        Uri sharePointUrl = new Uri(subWeb.Url);
        //        string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
        //                sharePointUrl.Authority, TokenHelper.GetRealmFromTargetUrl(sharePointUrl)).AccessToken;
        //        using (ClientContext subClientContext = TokenHelper.GetClientContextWithAccessToken(subWeb.Url, apponlyAccessToken))
        //        {
        //            theLog.Debug("run batch mode for site:" + subWeb.Url);
        //            DoBatchMode(subClientContext, listFailedItem);
        //        }
        //    }
        //}
        //public static void BatchModeForListCollection(ClientContext clientContext, ListCollection listCollection, List<BatchModeFailedModel> listFailedItem)
        //{
        //    List<List> lists = GetListsFromListCollection(clientContext, listCollection);
        //    foreach (List list in lists)
        //    {
        //        int count = list.EventReceivers.Where(p => p.ReceiverName == SPOEUtility.ListRemoteRecieverName).ToList().Count;
        //        if (count > 0)
        //        {
        //            //do batch mode for this list
        //            theLog.Debug("run batchmode for list:" + list.Title);

        //            CamlQuery camlQuery = new CamlQuery();
        //            // camlQuery.ViewXml = "<Query></Query>";
        //            camlQuery.ViewXml = "<View Scope='RecursiveAll'><Query></Query></View>";
        //            ListItemCollection listItemCollection = list.GetItems(camlQuery);
        //            clientContext.Load(listItemCollection);
        //            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

        //            theLog.Debug("listItemCollection count:" + listItemCollection.Count);
        //            //library
        //            if (SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate))
        //            {
        //                foreach (ListItem listItem in listItemCollection)
        //                {
        //                    try
        //                    {
        //                        BatchModeWorker.SetRunningDateTime(clientContext.Web.Url);
        //                        clientContext.Load(listItem, p => p.DisplayName, p => p.Versions, p => p.File, p => p.Folder,
        //                            p => p.FileSystemObjectType);
        //                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

        //                        if (listItem.FileSystemObjectType == FileSystemObjectType.File)
        //                        {
        //                            File file = listItem.File;
        //                            clientContext.Load(file, p => p.ServerRelativeUrl, p => p.Name, p => p.LockedByUser);
        //                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //                            string fileUrl = SPOEUtility.GetDomainFromWebUrl(clientContext.Web.Url) + file.ServerRelativeUrl;
        //                            theLog.Debug("BatchModeForList fileUrl:" + fileUrl);
        //                            string failedInfo = "";
        //                            bool ret = SPOEUtility.DoItemEnforcer(clientContext, list, listItem, file, fileUrl,ref failedInfo);
        //                            if (ret)
        //                            {
        //                                BatchModeFailedModel item = new BatchModeFailedModel(listItem.DisplayName, file.ServerRelativeUrl);
        //                                listFailedItem.Add(item);
        //                            }
        //                        }
        //                    }
        //                    catch (System.Exception ex)
        //                    {
        //                        theLog.Error("Exception on batch mode for listItem:" + ex.ToString());
        //                    }

        //                }
        //                //theLog.Debug("count:"+listItemCollection.Count);
        //            }
        //            else if (SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate))
        //            {
        //                theLog.Debug("list");
        //                foreach (ListItem listItem in listItemCollection)
        //                {
        //                    clientContext.Load(listItem, p => p.DisplayName, p => p.Versions);
        //                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //                    AttachmentCollection attachmentCollection = listItem.AttachmentFiles;
        //                    clientContext.Load(attachmentCollection);
        //                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //                    foreach (Attachment attachment in attachmentCollection)
        //                    {
        //                        try
        //                        {
        //                            BatchModeWorker.SetRunningDateTime(clientContext.Web.Url);
        //                            clientContext.Load(attachment, d => d.ServerRelativeUrl);
        //                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //                            string serverRelativeUrl = attachment.ServerRelativeUrl;
        //                            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
        //                            clientContext.Load(file, p => p.ServerRelativeUrl, p => p.Name, p => p.LockedByUser);
        //                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        //                            string fileUrl = SPOEUtility.GetDomainFromWebUrl(clientContext.Web.Url) + file.ServerRelativeUrl;
        //                            theLog.Debug("fileUrl:" + fileUrl);
        //                            string failedInfo = "";
        //                            bool ret = SPOEUtility.DoItemEnforcer(clientContext, list, listItem, file, fileUrl,ref failedInfo);
        //                            if (ret)
        //                            {
        //                                BatchModeFailedModel item = new BatchModeFailedModel(listItem.DisplayName, file.ServerRelativeUrl);
        //                                listFailedItem.Add(item);
        //                            }
        //                        }
        //                        catch (System.Exception ex)
        //                        {
        //                            theLog.Error("Exception on batch mode for Item attach:" + ex.ToString());
        //                        }

        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        public static void BatchModeForList(ClientContext clientContext, List list, List<BatchModeFailedModel> listFailedItem)
        {
            int count = list.EventReceivers.Where(p => p.ReceiverName == SPOEUtility.ListRemoteRecieverName).ToList().Count;
            if (count > 0)
            {
                //do batch mode for this list
                theLog.Debug("run batchmode for list:" + list.Title);

                CamlQuery camlQuery = new CamlQuery();
                // camlQuery.ViewXml = "<Query></Query>";
                camlQuery.ViewXml = "<View Scope='RecursiveAll'><Query></Query></View>";
                ListItemCollection listItemCollection = list.GetItems(camlQuery);
                clientContext.Load(listItemCollection);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                theLog.Debug("listItemCollection count:" + listItemCollection.Count);
                //library
                if (SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate))
                {
                    foreach (ListItem listItem in listItemCollection)
                    {
                        try
                        {
                            BatchModeWorker.SetRunningDateTime(list.Id.ToString());
                            theLog.Debug("list.Id.ToString():"+ list.Id.ToString());
                            clientContext.Load(listItem, p => p.DisplayName, p => p.Versions, p => p.File, p => p.Folder,
                                p => p.FileSystemObjectType,p=>p.FieldValuesAsText);
                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                            if (listItem.FileSystemObjectType == FileSystemObjectType.File)
                            {
                                File file = listItem.File;
                                clientContext.Load(file, p => p.ServerRelativeUrl, p => p.Name, p => p.LockedByUser, p => p.TimeLastModified);
                                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                                if (!file.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase) && CheckFileInBatch(clientContext, file))
                                {
                                    string fileUrl = SPOEUtility.GetDomainFromWebUrl(clientContext.Web.Url) + file.ServerRelativeUrl;
                                    theLog.Debug("BatchModeForList fileUrl:" + fileUrl);
                                    string failedInfo = "";
                                    bool ret = SPOEUtility.DoItemEnforcer(clientContext, list, listItem, file, fileUrl, ref failedInfo);
                                    if (!ret)
                                    {
                                        BatchModeFailedModel item = new BatchModeFailedModel(listItem.DisplayName, file.ServerRelativeUrl, failedInfo);
                                        listFailedItem.Add(item);
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            theLog.Error("Exception on batch mode for listItem:" + ex.ToString());
                        }

                    }
                    //theLog.Debug("count:"+listItemCollection.Count);
                }
                else if (SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate))
                {
                    theLog.Debug("list");
                    foreach (ListItem listItem in listItemCollection)
                    {
                        clientContext.Load(listItem, p => p.DisplayName, p => p.Versions);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        AttachmentCollection attachmentCollection = listItem.AttachmentFiles;
                        clientContext.Load(attachmentCollection);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        foreach (Attachment attachment in attachmentCollection)
                        {
                            try
                            {
                                BatchModeWorker.SetRunningDateTime(list.Id.ToString());
                                clientContext.Load(attachment, d => d.ServerRelativeUrl,d=>d.FileName);
                                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                                string serverRelativeUrl = attachment.ServerRelativeUrl;
                                var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
                                clientContext.Load(file, p => p.ServerRelativeUrl, p => p.Name, p => p.LockedByUser, p => p.TimeLastModified);
                                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                                if (!file.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase) && CheckFileInBatch(clientContext, file))
                                {
                                    string fileUrl = SPOEUtility.GetDomainFromWebUrl(clientContext.Web.Url) + file.ServerRelativeUrl;
                                    theLog.Debug("fileUrl:" + fileUrl);
                                    string failedInfo = "";
                                    bool ret = SPOEUtility.DoItemEnforcer(clientContext, list, listItem, file, fileUrl, ref failedInfo);
                                    if (!ret)
                                    {
                                        BatchModeFailedModel item = new BatchModeFailedModel(listItem.DisplayName, listItem.DisplayName + "/" + attachment.FileName, failedInfo);
                                        listFailedItem.Add(item);
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                theLog.Error("Exception on batch mode for Item attach:" + ex.ToString());
                            }

                        }
                    }
                }
            }
        }

        // Compare last modified time with "NXL" file and devide if item need to do RMX in Batch Mode.  
        public static bool CheckFileInBatch(ClientContext clientContext, File file)
        {
            bool bRet = true;
            try
            {
                File nxlFile = clientContext.Web.GetFileByServerRelativeUrl(file.ServerRelativeUrl + ".nxl");
                clientContext.Load(nxlFile, d => d.Exists, d => d.TimeLastModified);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                if (nxlFile != null && nxlFile.Exists && DateTime.Compare(nxlFile.TimeLastModified, file.TimeLastModified) > 0)
                {
                    bRet = false;
                }
            }
            catch
            {
            }
            return bRet;
        }

        public static void DoRMXEnforcer(SPRemoteEventProperties properties,ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, d => d.Url);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

            string afterUrl = properties.ItemEventProperties.AfterUrl.TrimStart(TrimFlag);
            string fileUrl = clientContext.Web.Url.TrimEnd(TrimFlag) + "/" + afterUrl;
            theLog.Debug("DoRMXEnforcer fileUrl:" + fileUrl);
            List list = SPOEUtility.LoadContextList(clientContext, clientContext.Web, properties);
            ListItem listItem = SPOEUtility.LoadContextListItem(clientContext, properties, list);
            if (listItem.FileSystemObjectType==FileSystemObjectType.Folder)
            {
                theLog.Debug("DoRMXEnforcer return for folder.");
                return;
            }
                
            int templateId = list.BaseTemplate;
            File file = null;
            if (SupportedLibraryTypes.Contains(templateId))
            {
                 file = SPOEUtility.LoadContextFile(clientContext, listItem);
            }
            else if(SupportedListTypes.Contains(templateId))
            {
                //get attachment
                string fileName = afterUrl.Substring(afterUrl.LastIndexOf("/"));
                file = SPOEUtility.LoadAttachment(clientContext, listItem, fileName);
            }
            else
            {
                theLog.Info("DoRMXEnforcer, return for: The type of library/list is not support:" + fileUrl);
                return;
            }
            string failedInfo = "";
            DoItemEnforcer(clientContext,list,listItem,file,fileUrl, ref failedInfo, true);
        }
        public static bool DoItemEnforcer(ClientContext clientContext,List list,ListItem listItem,
            File file,string fileUrl, ref string failedInfo, bool bEventMode = false)
        {
            try
            {
                bool bDelayProcess = false;
                //check avaiable
                if (listItem == null || file == null)
                {
                    theLog.Info("DoItemEnforcer return for listItem or file is empty.");
                    return true;
                }

                if (file.Name.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase))
                {
                    theLog.Info("DoItemEnforcer return for:this is .nxl file, we didn't support to protect it.");
                    return true;
                }

                //is new document
                if (RMXUtility.IsNewDocument(file.Name))
                {
                    theLog.Info("DoItemEnforcer return for:this file name no need to process.");
                    failedInfo = "Ignore,unsupported Type";
                    return false;
                }

                //load file property
                try
                {
                    clientContext.Load(clientContext.Web, p => p.CurrentUser);
                    clientContext.Load(clientContext.Site, p => p.Url);
                    clientContext.Load(clientContext.Web.CurrentUser, p => p.LoginName);
                    clientContext.Load(file, p => p.CheckOutType, p => p.LockedByUser);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                    clientContext.Load(file.LockedByUser, p => p.UserPrincipalName);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
                catch (System.Exception ex)
                {
                    theLog.Error("DoItemEnforcer failed to load file property." + ex.ToString());
                    failedInfo = "Failed,error occurred when load property";
                    return false;
                }

                //Check permission
                clientContext.Load(listItem, olistItem => olistItem.EffectiveBasePermissions);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                if (!listItem.EffectiveBasePermissions.Has(PermissionKind.EditListItems))
                {
                    theLog.Info("DoItemEnforcer return for:current user has no permission for this file.");
                    failedInfo = "Ignore,Current user has on permission for this file";
                    return false;
                }
                //check checkout status
                if (file.CheckOutType != CheckOutType.None)
                {
                    theLog.Info("DoItemEnforcer return for:this file is checked out.");
                    failedInfo = "Ignore,Checked out file";
                    return false;
                }

                //check file lock status
                string strLockedByUser = RMXUtility.GetUserPrincipalName(file.LockedByUser);
                if (!string.IsNullOrWhiteSpace(strLockedByUser))
                {
                    if (bEventMode)
                    {
                        bDelayProcess = true;
                    }
                    else
                    {
                        theLog.Info("DoItemEnforcer return for:this file is locked by:" + strLockedByUser);
                        failedInfo = "Ignore, file is locked";
                        return false;
                    }
                }

                int oldCount = listItem.Versions.Count;
                int templateId = list.BaseTemplate;
                User currentUser = SPOEUtility.LoadContextUser(clientContext, clientContext.Web);//load user information
                clientContext.Load(file, d => d.Name, d => d.ServerRelativeUrl, d => d.Properties);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                List<CEObligation> listObligation = new List<CEObligation>();
                if (!SPOEUtility.CheckListItemCloudAZ(list, listItem, file, fileUrl, clientContext, currentUser, ref listObligation))
                {
                    theLog.Debug("CheckListItemCloudAZ failed");
					failedInfo = "Failed,error occurred when query policy";
                    return false;
                }
                if (listObligation != null && listObligation.Count > 0)
                {
                    theLog.Debug("listObligation enter");
                    Dictionary<string, string> dicTags = SPOEUtility.GetTagsFromObligations(clientContext, listObligation, listItem, file);

                    if (bDelayProcess)
                    {
                        DelayedItemMgr.AddedDelayedItem(clientContext, list, listItem, file, fileUrl, dicTags);
                    }
                    else
                    {
                        bool bEncrypt = RmxModule.EncryptItemVerstions(clientContext, list, listItem, file, dicTags);
                        theLog.Debug("bEncrypt:" + bEncrypt);
                        if (!bEncrypt)
                        {
                            theLog.Debug("EncryptItemVerstions Failed");
                            failedInfo = "Failed,error occurred when do encrypt";
                            return false;
                        }
                    }
                }
                else
                {
                    failedInfo = "Ignore,no match policy";
                    return false;
                }
                if (SupportedListTypes.Contains(templateId))
                {
                    clientContext.Load(listItem, d => d.Versions);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                    RemoveListItemNewVersions(listItem, oldCount);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
            }
            catch (Exception ex)
            {
                theLog.Error("DoItemEnforcer error:"+ex.Message+ex.StackTrace);
                failedInfo = "Failed,error occurred";
                return false;
            }
            return true;
        }
        
        public static void RemoveListItemNewVersions(ListItem listItem,int oldCount)
        {
            theLog.Debug("RemoveListItemNewVersions enter");
            theLog.Debug("oldCount:"+ oldCount);
            ListItemVersionCollection listVersionCollection = listItem.Versions;
            int count = listVersionCollection.Count;
            theLog.Debug("count:"+ count);
            if(count > oldCount+1)
            {
                for(int i = count - oldCount - 1;i>0;i--)
                {
                    ListItemVersion version = listVersionCollection[i];
                    version.DeleteObject();
                }
            }
        }
        public static void MergeObligationsTags(Dictionary<string, string> dicTags, string name, string value, string mode, string overwriteLevel, Dictionary<string, string> listOverwriteLevel)
        {
            if (dicTags.ContainsKey(name))
            {
                if (mode.Equals("append", StringComparison.OrdinalIgnoreCase))
                {
                    if (!listOverwriteLevel.ContainsKey(name))
                    {
                        dicTags[name] = dicTags[name] + TagSeparator + value;
                    }
                }
                else if (mode.Equals("overwrite", StringComparison.OrdinalIgnoreCase))
                {
                    bool bOver = false;
                    if (listOverwriteLevel.ContainsKey(name))
                    {
                        string curLevel = listOverwriteLevel[name];
                        if (curLevel.Equals("Low", StringComparison.OrdinalIgnoreCase))
                        {
                            if (overwriteLevel.Equals("Middle", StringComparison.OrdinalIgnoreCase)
                                || overwriteLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
                            {
                                bOver = true;
                            }
                        }
                        else if (curLevel.Equals("Middle", StringComparison.OrdinalIgnoreCase)
                            && overwriteLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
                        {
                            bOver = true;
                        }
                    }
                    else
                    {
                        bOver = true;
                    }
                    if (bOver)
                    {
                        listOverwriteLevel[name] = overwriteLevel;
                        dicTags[name] = value;
                    }
                }
            }
            else
            {
                dicTags.Add(name, value);
                if (mode.Equals("overwrite", StringComparison.OrdinalIgnoreCase))
                {
                    listOverwriteLevel[name] = overwriteLevel;
                }
            }
        }
        public static Dictionary<string, string> GetTagsFromObligations(ClientContext clientContext, List<CEObligation> listObligation, ListItem item, File itemFile)
        {
            Dictionary<string, string> dicTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> fileTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool bGetFileTag = false;
            Dictionary<string, string> listOverwriteLevel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (CEObligation obligation in listObligation)
            {
                if (obligation.GetName().Equals(StrRmxObName, StringComparison.OrdinalIgnoreCase))
                {
                    CEAttres attrs = obligation.GetCEAttres();
                    string tagging = "";
                    string obName = "";
                    string obValue = "";
                    string mode = "";
                    string overwriteLevel = "";
                    for (int i = 0; i < attrs.Count; i++)
                    {
                        CEAttribute ceAttr = attrs[i];
                        if (ceAttr.Name.Equals("tagging", StringComparison.OrdinalIgnoreCase))
                        {
                            tagging = ceAttr.Value;
                        }
                        else if (ceAttr.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                        {
                            obName = ceAttr.Value.ToLower();
                        }
                        else if (ceAttr.Name.Equals("value", StringComparison.OrdinalIgnoreCase))
                        {
                            obValue = ceAttr.Value;
                        }
                        else if (ceAttr.Name.Equals("mode", StringComparison.OrdinalIgnoreCase))
                        {
                            mode = ceAttr.Value;
                        }
                        if (ceAttr.Name.Equals("overwriteLevel", StringComparison.OrdinalIgnoreCase))
                        {
                            overwriteLevel = ceAttr.Value;
                        }
                    }
                    // Get tags from file
                    if (!bGetFileTag && tagging.Equals("file-tag", StringComparison.OrdinalIgnoreCase))
                    {
                        if (itemFile.ServerRelativeUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            var clientResultStream = itemFile.OpenBinaryStream();
                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                            System.IO.Stream stream = clientResultStream.Value;
                            var serverRelativeUrl = itemFile.ServerRelativeUrl;
                            string sourcePath = RmxModule.SaveFileToTemp(stream, serverRelativeUrl);
                            RmxModule.GetNormalFileTags(sourcePath, fileTags, TagSeparator);
                            if (!string.IsNullOrEmpty(sourcePath) && System.IO.File.Exists(sourcePath))
                            {
                                System.IO.File.Delete(sourcePath);
                            }
                            bGetFileTag = true;
                        }
                        else
                        {
                            GetFileProperties(itemFile, fileTags);
                        }
                    }

                    List<string> listName = null;
                    List<string> listValue = null;
                    if (!string.IsNullOrEmpty(obName))
                    {
                        string[] arrName = obName.Split(new string[] { TagSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        listName = new List<string>(arrName);
                    }
                    if (listName == null) listName = new List<string>();

                    // Different modes (user-defined, specific-column, file-tag)
                    if (tagging.Equals("user-defined", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(obValue))
                        {
                            string[] values = obValue.Split(new string[] { TagSeparator }, StringSplitOptions.RemoveEmptyEntries);
                            listValue = new List<string>(values);
                        }
                        else continue;
                    }
                    else if (tagging.Equals("specific-column", StringComparison.OrdinalIgnoreCase))
                    {
                        listValue = GetItemColumnValues(clientContext, item, listName);
                    }
                    else if (tagging.Equals("file-tag", StringComparison.OrdinalIgnoreCase))
                    {
                        if (listValue == null) listValue = new List<string>();
                        if (!string.IsNullOrEmpty(obName))
                        {
                            foreach (string name in listName)
                            {
                                if (fileTags.ContainsKey(name))
                                {
                                    listValue.Add(fileTags[name]);
                                }
                                else
                                {
                                    listValue.Add("");
                                }
                            }
                        }
                        else
                        {
                            // Add all file tags
                            foreach (KeyValuePair<string, string> keyValue in fileTags)
                            {
                                listName.Add(keyValue.Key);
                                listValue.Add(keyValue.Value);
                            }
                        }
                    }

                    // Merge Obligations Tags.
                    for (int i = 0; i < listName.Count; i++)
                    {
                        string name = listName[i];
                        if (i >= listValue.Count) break;
                        string value = listValue[i];
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        {
                            MergeObligationsTags(dicTags, name, value, mode, overwriteLevel, listOverwriteLevel);
                        }
                    }
                }
            }
            return dicTags;
        }

        private static void GetFileProperties(File itemFile, Dictionary<string, string> fileTags)
        {
            Dictionary<string, object> dicProps = itemFile.Properties.FieldValues;
            foreach (string strkey in dicProps.Keys)
            {
                if (!strkey.StartsWith("vti_", StringComparison.OrdinalIgnoreCase) && !strkey.StartsWith("_")
                    && !strkey.StartsWith("Keywords", StringComparison.OrdinalIgnoreCase) &&
                    !strkey.StartsWith("ContentType", StringComparison.OrdinalIgnoreCase) &&
                    !strkey.StartsWith("TaxCatchAll", StringComparison.OrdinalIgnoreCase))
                {
                    object value = dicProps[strkey];
                    if (value != null && value is string && !string.IsNullOrEmpty(value.ToString()))
                    {
                        fileTags.Add(strkey, value.ToString());
                    }
                }
            }
        }

        public static bool CheckListItemCloudAZ(List list,ListItem listItem, File file,string strFileUrl, ClientContext clientContext, User currentUser,ref List<CEObligation> listObligation)
        {
            theLog.Debug("CheckListItemCloudAZ enter");
            try
            {
                if (SupportedListTypes.Contains(list.BaseTemplate))
                {
                    strFileUrl = strFileUrl.Substring(0, strFileUrl.LastIndexOf("/"));
                    strFileUrl = strFileUrl.Substring(0, strFileUrl.LastIndexOf("/"));
                    strFileUrl = strFileUrl.Substring(0, strFileUrl.LastIndexOf("/")) + "/" + listItem.DisplayName; ;
                    theLog.Debug("list, strFileUrl:"+ strFileUrl);
                }

                //  Convert "http://" or "https://" to "sharepoint://"
                int indx = strFileUrl.IndexOf("://");
                strFileUrl = "sharepoint" + strFileUrl.Substring(indx);
                theLog.Debug("replace https strFileUrl:"+ strFileUrl);

                //get selected columns
                IniFiles libSettingFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "LibSetting.ini");
                var strSelectedColumns = libSettingFile.IniReadValue(list.Id.ToString(), SPOEUtility.strLibColumns);
                Dictionary<string, string> selectedColumns = JsonConvert.DeserializeObject<Dictionary<string, string>>(strSelectedColumns);
                if (selectedColumns == null)
                {
                    selectedColumns = new Dictionary<string, string>();
                }
                FieldCollection filedCollection = list.Fields;
                CEAttres ceSrcAttrs = GetItemAttrs(listItem, filedCollection, selectedColumns, clientContext.Web.Url, clientContext.Site.Url);

                string strUserName = "";
                string strSid = "";
                CEAttres userAttrs = new CEAttres();
                GetSPUserAttrs(clientContext,currentUser, ref strUserName, ref strSid, userAttrs);
                CERequest obReq = CloudAZQuery.CreateQueryReq(StrRmxAction, "", strFileUrl, ceSrcAttrs, strSid, strUserName, userAttrs);
                PolicyResult emPolicyResult = PolicyResult.DontCare;
                QueryStatus emQueryRes = CloudAZQuery.Instance.QueryColuAZPC(obReq, ref listObligation, ref emPolicyResult);
                theLog.Debug("emQueryRes:"+ emQueryRes);
                theLog.Debug("emPolicyResult:"+ emPolicyResult);
                theLog.Debug("listObligation count:"+ listObligation.Count);
                if (emQueryRes == QueryStatus.S_OK)
                {
                    if (emPolicyResult == PolicyResult.Deny)
                    {
                        listObligation = null; // Ignore obligations when policy result is deny.
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                theLog.Error("CheckListItemCloudAZ Error: "+ex.Message+ex.StackTrace);
            }
            return false;
        }
        public static void GetSPUserAttrs(ClientContext clientContext, User user, ref string userName, ref string userSid, CEAttres userAttrs)
        {
            userName = user.LoginName;
            userSid = user.Id.ToString();
            userAttrs.AddAttribute(new CEAttribute("emailaddress", user.Email, CEAttributeType.XacmlString));
            userAttrs.AddAttribute(new CEAttribute("username", user.LoginName, CEAttributeType.XacmlString));

            //get user profile
            Dictionary<string, string[]> dicUserAttr = RMXUtility.GetUserAttributeFromProfile(clientContext);
            foreach (var varUserAttr in dicUserAttr)
            {
                foreach (string strValue in varUserAttr.Value)
                {
                    userAttrs.AddAttribute(new CEAttribute(varUserAttr.Key, strValue, CEAttributeType.XacmlString));
                }
            }
        }

        public static CEAttres GetItemAttrs(ListItem listItem, FieldCollection filedCollection, Dictionary<string, string> selectedColumn, string webUrl, string rootWebUrl)
        {
            theLog.Debug("GetItemAttrs enter");
            CEAttres ceAttrs = new CEAttres();

            FieldStringValues fieldStringValues = listItem.FieldValuesAsText;
            foreach (Field field in filedCollection)
            {
                try
                {
                    string key = field.Title;
                    if (!string.IsNullOrEmpty(key) && selectedColumn.ContainsKey(field.InternalName))
                    {
                        if (fieldStringValues.FieldValues.ContainsKey(field.InternalName))
                        {
                            string strValue = fieldStringValues.FieldValues[field.InternalName];
                            theLog.Debug("key:"+key+",value:"+ strValue);
                            if (!string.IsNullOrEmpty(strValue))
                            {
                                ceAttrs.AddAttribute(new CEAttribute(key, strValue, CEAttributeType.XacmlString));
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    theLog.Error("Exception on GetItemAttrs:" + ex.ToString());
                }
            }

            //get all sites && selected properties
            IniFiles sitePropertyFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "SiteProperty.ini");
            var  SitePropertyLevel = sitePropertyFile.IniReadValue(rootWebUrl, SPOEUtility.SitePropertyLevel);
            var siteJson = sitePropertyFile.IniReadValue(rootWebUrl, SPOEUtility.SitePropertyList);
            List<ZSiteNodeModel> sitesNodeList = JsonConvert.DeserializeObject<List<ZSiteNodeModel>>(siteJson);
            if (sitesNodeList == null)
            {
                sitesNodeList = new List<ZSiteNodeModel>();
            }
            if(SitePropertyLevel== SitePropLevel.Subsite.ToString())
            {
                if (webUrl != rootWebUrl)
                {
                    GetAttrFromSiteProperty(webUrl, sitesNodeList, ceAttrs, false);
                }
            }
            else if (SitePropertyLevel == SitePropLevel.SiteCollection.ToString())
            {
                GetAttrFromSiteProperty(rootWebUrl, sitesNodeList, ceAttrs, true);
            }
            else if (SitePropertyLevel == SitePropLevel.Both.ToString())
            {
                if (webUrl == rootWebUrl)
                {
                    GetAttrFromSiteProperty(rootWebUrl, sitesNodeList, ceAttrs, true);
                }
                else
                {
                    GetAttrFromSiteProperty(webUrl, sitesNodeList, ceAttrs, false);
                    GetAttrFromSiteProperty(rootWebUrl, sitesNodeList, ceAttrs, true);
                }
            }
            theLog.Debug("GetItemAttrs end");
            return ceAttrs;
        }

        private static void GetAttrFromSiteProperty(string webUrl, List<ZSiteNodeModel> sitesNodeList, CEAttres ceAttrs,bool isRootWeb)
        {
            theLog.Debug("GetAttrFromSiteProperty enter");
            Uri webUri = new Uri(webUrl);
            string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
                    webUri.Authority, TokenHelper.GetRealmFromTargetUrl(webUri)).AccessToken;
            using (ClientContext webContext = TokenHelper.GetClientContextWithAccessToken(webUrl, apponlyAccessToken))
            {
                webContext.Load(webContext.Web, web => web.AllProperties);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(webContext);
                PropertyValues allProps = webContext.Web.AllProperties;
                var currentNode = sitesNodeList.Where(p => p.id == webUrl).FirstOrDefault();
                if (currentNode == null)
                {
                    return;
                }
                foreach (KeyValuePair<string, object> field in allProps.FieldValues)
                {
                    SitePropertyModel selectedProp = currentNode.siteProperties.Where(p => p.displayName == field.Key).FirstOrDefault();
                    if (selectedProp != null)
                    {
                        theLog.Debug("selectedProp.displayName:"+ selectedProp.displayName+",value:"+ field.Value.ToString());
                        if (isRootWeb)
                        {
                            ceAttrs.AddAttribute(new CEAttribute("sc." + selectedProp.displayName, field.Value.ToString(), CEAttributeType.XacmlString));
                        }
                        else
                        {
                            ceAttrs.AddAttribute(new CEAttribute("ss." + selectedProp.displayName, field.Value.ToString(), CEAttributeType.XacmlString));
                        }
                    }
                }
            }
            theLog.Debug("GetAttrFromSiteProperty end");
        }

        public static List<string> GetItemColumnValues(ClientContext clientContext, ListItem item, List<string> listColumnName)
        {
            theLog.Debug("GetItemColumnValues enter");
            List list = item.ParentList;
            FieldCollection filedCollection = list.Fields;
            clientContext.Load(filedCollection);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            FieldStringValues fieldStringValues = item.FieldValuesAsText;
            // !!!Change, use it directly after initialize
            List<string> listColumnValue = new List<string>();
            if (listColumnName.Count > 0)
            {
                foreach (string columnName in listColumnName)
                {
                    theLog.Debug("columnName:"+ columnName);
                    string columnValue = "";
                    foreach(Field field in filedCollection)
                    {
                        if(columnName.Equals(field.Title, StringComparison.OrdinalIgnoreCase))
                        {
                            theLog.Debug("field Title:" + field.Title);
                            theLog.Debug("field.InternalName:"+ field.InternalName);
                            if(fieldStringValues.FieldValues.ContainsKey(field.InternalName))
                            {
                                columnValue = fieldStringValues.FieldValues[field.InternalName];
                                theLog.Debug("columnValue1:" + columnValue);
                                if (!string.IsNullOrEmpty(columnValue))
                                {
                                    /*if (field.FieldTypeKind == FieldType.Invalid && field.TypeDisplayName.Equals("Managed Metadata", StringComparison.OrdinalIgnoreCase))
                                    {
                                        columnValue = columnValue.Replace(";", TagSeparator);
                                        theLog.Debug("columnValue2:" + columnValue);
                                    }*/
                                    columnValue = columnValue.Replace("\r", TagSeparator);
                                    theLog.Debug("columnValue3:" + columnValue);
                                }
                            }
                            break;
                        }
                    }
                    listColumnValue.Add(columnValue);
                }
            }
            theLog.Debug("GetItemColumnValues end");
            return listColumnValue;
        }
        public static User LoadContextUser(ClientContext clientContext, Web web)
        {
            User currentUser = clientContext.Web.CurrentUser;
            clientContext.Load(currentUser);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            return currentUser;
        }
        public static List LoadContextList(ClientContext clientContext, Web web, SPRemoteEventProperties properties)
        {
            Guid listId = properties.ItemEventProperties.ListId;
            List docLibrary = web.Lists.GetById(listId);
            clientContext.Load(docLibrary, d => d.Id, d => d.Title, d => d.BaseType, d => d.BaseTemplate,d=>d.Fields);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            return docLibrary;
        }
        public static ListItem LoadContextListItem(ClientContext clientContext, SPRemoteEventProperties properties, List docLibrary)
        {
            int listItemID = properties.ItemEventProperties.ListItemId;
            ListItem listItem = docLibrary.GetItemById(listItemID);
            clientContext.Load(listItem,d=>d.DisplayName,d=>d.FieldValuesAsText,d=>d.Versions, d=>d.FileSystemObjectType);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            return listItem;
        }
        public static File LoadContextFile(ClientContext clientContext, ListItem listItem)
        {
            File file = listItem.File;
            clientContext.Load(file,p=>p.Name,p=>p.ServerRelativeUrl, p=>p.LockedByUser);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            return file;
        }
        public static File LoadAttachment(ClientContext clientContext, ListItem listItem,string fileName)
        {
            AttachmentCollection attachmentFiles = listItem.AttachmentFiles;
            Attachment attachment = attachmentFiles.GetByFileName(fileName);
            clientContext.Load(attachment,d=>d.ServerRelativeUrl);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            string serverRelativeUrl = attachment.ServerRelativeUrl;
            var file = clientContext.Web.GetFileByServerRelativeUrl(serverRelativeUrl);
            clientContext.Load(file, p => p.Name, p => p.ServerRelativeUrl);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            return file;
        }
        public static bool CheckWebEvent(Web web, string recieverName)
        {
            EventReceiverDefinitionCollection erdc = web.EventReceivers;
            foreach (EventReceiverDefinition erd in erdc)
            {
                if (erd.ReceiverName.Equals(recieverName))
                    return true;
            }
            return false;
        }

        public static bool CheckEvent(Web web, List list)
        {
            bool result = false;
            EventReceiverDefinitionCollection erdc = list.EventReceivers;
            foreach (EventReceiverDefinition erd in erdc)
            {
                if (erd.ReceiverName == ListRemoteRecieverName)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        public static bool RemoveListEditAction(ClientContext clientContext, List spList)
        {
            try
            {
                clientContext.Load(spList, oList => oList.UserCustomActions, oList => oList.Id);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                foreach (UserCustomAction uca in spList.UserCustomActions)
                {
                    if (uca.Name == SPOEUtility.ListEditItemName)
                    {
                        uca.DeleteObject();
                    }
                }

                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                return true;
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on RemoveListEditAction：" + ex.ToString());
                return false;
            }
        }

        public static void RemoveListEditActionFromLibaries(ClientContext clientContext, List<List> listCollection, List<string> failedList)
        {
            try
            {

                foreach (List spList in listCollection)
                {
                   if(!RemoveListEditAction(clientContext, spList))
                    {
                        failedList.Add(spList.Id.ToString());
                    }
                }
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on RemoveListEditActionForAllLibrary:" + ex.ToString());
            }
           
        }

        public static void AddListEditActionToLibraries(ClientContext clientContext, List<List> ListCollection, bool isalreadyLoadData, List<string> listFailed)
        {
            int ListCollectionCount = ListCollection.Count;
            if (ListCollectionCount == 0) return;
            int k = ListCollectionCount % MaxRequestCount == 0 ?
                ListCollectionCount / MaxRequestCount :
                ListCollectionCount / MaxRequestCount + 1;
            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * MaxRequestCount, ListCollectionCount);
                if (!isalreadyLoadData)
                {
                    //load data
                    for (int j = i * MaxRequestCount; j < maxCount; j++)
                    {
                        clientContext.Load(ListCollection[j], olist => olist.Title,
                            olist => olist.EventReceivers, olist => olist.ContentTypes, olist => olist.BaseTemplate, olist=>olist.Id, olist=>olist.UserCustomActions);
                    }
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
                // add event
                for (int j = i * MaxRequestCount; j < maxCount; j++)
                {
                    List cellList = ListCollection[j];
                    try
                    {
                        UserCustomActionCollection userCustomActionColl = cellList.UserCustomActions;

                        //check if already exist
                        bool bAlreadyExist = false;
                        foreach(UserCustomAction uca in userCustomActionColl)
                        {
                            if (uca.Name== SPOEUtility.ListEditItemName)
                            {
                               
                                bAlreadyExist = true;
                                break;
                            }
                        }

                        if (bAlreadyExist)
                        {
                            theLog.Info("already have listEdit item:" + cellList.Title);
                            continue;
                        }

                        theLog.Info("added edit list item:" + cellList.Title);

                        UserCustomAction listEditViewAction = userCustomActionColl.Add();
                        listEditViewAction.Name = SPOEUtility.ListEditItemName;
                        listEditViewAction.Location = "Microsoft.SharePoint.ListEdit";
                        listEditViewAction.Group = "Permissions";
                        listEditViewAction.Sequence = 10002;
                        listEditViewAction.Title = "NextLabs Rights Management";

                        listEditViewAction.Url = GeneralSettingController.m_strEditListUrl; //"javascript:LaunchApp('d1a5dc6e-ad21-45dc-8130-fb137e3406a7', 'i:0i.t|ms.sp.ext|98d0611a-c696-4309-8866-9d77c67a982c@0c955f71-24c4-47b0-801d-d3a393cf8791', 'https:\u002f\u002frmxspo01.edrm.cloudaz.com:8443\u002fGeneralSetting\u002fLibSettingView?{StandardTokens}\u0026listId={ListId}\u0026itemId={ItemId}\u0026siteUrl={SiteUrl}', null);";
                        listEditViewAction.Update();

                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + cellList.Title + ", add ListEdit failed, Exception: " + exp.ToString());
                        listFailed.Add(cellList.Title);
                    }
                }
            }
        }


        public static void RemoveEnforcerToLibaries(ClientContext clientContext, List<List> ListCollection,
                                                   bool isalreadyLoadData, List<string> listFailed)
        {
            int ListCollectionCount = ListCollection.Count;
            if (ListCollectionCount == 0) return;
            int k = ListCollectionCount % MaxRequestCount == 0 ?
                ListCollectionCount / MaxRequestCount :
                ListCollectionCount / MaxRequestCount + 1;
            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * MaxRequestCount, ListCollectionCount);
                if (!isalreadyLoadData)
                {
                    //load data
                    for (int j = i * MaxRequestCount; j < maxCount; j++)
                    {
                        clientContext.Load(ListCollection[j], olist => olist.Title, olist=>olist.Id,
                            olist => olist.EventReceivers, olist => olist.UserCustomActions, olist => olist.ContentTypes, olist => olist.BaseTemplate);
                    }
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }

          
                for (int j = i * MaxRequestCount; j < maxCount; j++)
                {
                    List cellList = ListCollection[j];
                    bool bFailed = false;

                    //remove event
                    try
                    {
                        RemoveEvent(clientContext, cellList);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + cellList.Title + ", Remove Event Reciever failed, Exception: " + exp.ToString());
                        bFailed = true;
                    }

                    //remove custom action
                    try
                    {
                        RemoveECBAction(clientContext, cellList);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + cellList.Title + ", remove custom action failed, Exception: " + exp.ToString());
                        bFailed = true;
                    }

                    // Reset batch mode status
                    BatchModeWorker.ResetBatchModeStatus(cellList.Id.ToString());

                    //added to failed list
                    if (bFailed)
                    {
                        listFailed.Add(cellList.Title);
                    }
                }
            }
        }


        public static void AddEnforcerToLibaries(ClientContext clientContext,  List<List> ListCollection,
                                                 string remoteEventRecieverUrl,  string strRightProtectUrl, 
                                                 string strSecurityViewUrl, string strEditListUrl,
                                                 bool isalreadyLoadData, List<string> listFailed)
        {
            int ListCollectionCount = ListCollection.Count;
            if (ListCollectionCount == 0) return;
            int k = ListCollectionCount % MaxRequestCount == 0 ?
                ListCollectionCount / MaxRequestCount :
                ListCollectionCount / MaxRequestCount + 1;
            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * MaxRequestCount, ListCollectionCount);
                if (!isalreadyLoadData)
                {
                    //load data
                    for (int j = i * MaxRequestCount; j < maxCount; j++)
                    {
                        clientContext.Load(ListCollection[j], olist => olist.Title, olist=>olist.Id,
                            olist => olist.EventReceivers, olist => olist.UserCustomActions, olist => olist.ContentTypes, olist => olist.BaseTemplate);
                    }
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
                // add event
                for (int j = i * MaxRequestCount; j < maxCount; j++)
                {
                    List cellList = ListCollection[j];
                    bool bFailed = false;

                    //added event
                    try
                    {
                        AddEvent(clientContext, cellList, remoteEventRecieverUrl);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + cellList.Title + ", add Event Reciever failed, Exception: " + exp.ToString());
                        bFailed = true;
                    }

                    //added custom action
                    try
                    {
                        AddECBAction(clientContext,cellList, strRightProtectUrl, strSecurityViewUrl, strEditListUrl);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + cellList.Title + ", add custom action failed, Exception: " + exp.ToString());
                        bFailed = true;
                    }

                    //added to failed list
                    if (bFailed){
                        listFailed.Add(cellList.Title);
                    }
                    

                }
            }
        }

        public static void AddEventToLibaries(ClientContext clientContext, List<List> ListCollection, string remoteEventRecieverUrl, bool isalreadyLoadData, List<string> listFailed)
        {
            int ListCollectionCount = ListCollection.Count;
            if (ListCollectionCount == 0) return;
            int k = ListCollectionCount % MaxRequestCount == 0 ?
                ListCollectionCount / MaxRequestCount :
                ListCollectionCount / MaxRequestCount + 1;
            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * MaxRequestCount, ListCollectionCount);
                if (!isalreadyLoadData)
                {
                    //load data
                    for (int j = i * MaxRequestCount; j < maxCount; j++)
                    {
                        clientContext.Load(ListCollection[j], olist => olist.Title,
                            olist => olist.EventReceivers, olist => olist.ContentTypes,olist=>olist.BaseTemplate);
                    }
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
                // add event
                for (int j = i * MaxRequestCount; j < maxCount; j++)
                {
                    List cellList = ListCollection[j];
                    try
                    {
                        AddEvent(clientContext, cellList, remoteEventRecieverUrl);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + cellList.Title + ", add Event Reciever failed, Exception: " + exp.ToString());
                        listFailed.Add(cellList.Title);
                    }
                }
            }
        }
        public static void RemoveEventFromLibaries(ClientContext clientContext, List<List> ListCollection, string remoteEventRecieverUrl, bool isalreadyLoadData, List<string> listFailed)
        {
            int ListCollectionCount = ListCollection.Count;

            if (ListCollectionCount == 0) return;
            int k = ListCollectionCount % MaxRequestCount == 0 ?
                ListCollectionCount / MaxRequestCount :
                ListCollectionCount / MaxRequestCount + 1;
            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * MaxRequestCount, ListCollectionCount);
                if (!isalreadyLoadData)
                {
                    //load data
                    for (int j = i * MaxRequestCount; j < maxCount; j++)
                    {
                        clientContext.Load(ListCollection[j], olist => olist.Title,
                            olist => olist.EventReceivers, olist => olist.ContentTypes);
                    }
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }

                // remove event
                for (int j = i * MaxRequestCount; j < maxCount; j++)
                {
                    List list = ListCollection.ElementAt(j);
                    try
                    {
                        RemoveEvent(clientContext, list);
                    }
                    catch (Exception exp)
                    {
                        theLog.Error("List: " + list.Title + ", Remove Event Reciever failed, Exception: " + exp.ToString());
                        listFailed.Add(list.Title);
                    }
                }
            }
        }
        public static void AddEvent(ClientContext clientContext, List list, string remoteEventRecieverUrl)
        {
            theLog.Debug("AddEvent enter,list:"+ list.Title);
            EventReceiverDefinitionCollection erdc = list.EventReceivers;
            //check whether there exists the event or not, if exists, we shouldn't add the event again.
            foreach (EventReceiverDefinition erd in erdc)
            {
                if (erd.ReceiverName.Equals(ListRemoteRecieverName))
                {
                    theLog.Debug("Event Reciever is added.");
                    return;
                }
            }
            if(SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate))
            {
                EventReceiverDefinitionCreationInformation addEventReceiver = new EventReceiverDefinitionCreationInformation()
                {
                    EventType = EventReceiverType.ItemAdded,
                    //       ReceiverAssembly = Assembly.GetExecutingAssembly().FullName,
                    ReceiverName = ListRemoteRecieverName,
                    ReceiverClass = ListRemoteRecieverName,
                    ReceiverUrl = remoteEventRecieverUrl,
                    SequenceNumber = 10000
                };
                erdc.Add(addEventReceiver);

                EventReceiverDefinitionCreationInformation updatedEventReceiver = new EventReceiverDefinitionCreationInformation()
                {
                    EventType = EventReceiverType.ItemUpdated,
                    //    ReceiverAssembly = Assembly.GetExecutingAssembly().FullName,
                    ReceiverName = ListRemoteRecieverName,
                    ReceiverClass = ListRemoteRecieverName,
                    ReceiverUrl = remoteEventRecieverUrl,
                    SequenceNumber = 10000
                };
                erdc.Add(updatedEventReceiver);

                //we need this event when use move file between folders within the same library.
                EventReceiverDefinitionCreationInformation RemovedEventReceiver = new EventReceiverDefinitionCreationInformation()
                {
                    EventType = EventReceiverType.ItemFileMoved,
                    //    ReceiverAssembly = Assembly.GetExecutingAssembly().FullName,
                    ReceiverName = ListRemoteRecieverName,
                    ReceiverClass = ListRemoteRecieverName,
                    ReceiverUrl = remoteEventRecieverUrl,
                    SequenceNumber = 10000
                }; 
                erdc.Add(RemovedEventReceiver);
               
            }
            else if(SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate))
            {
                EventReceiverDefinitionCreationInformation attachmentAddedEventReceiver = new EventReceiverDefinitionCreationInformation()
                {
                    EventType = EventReceiverType.ItemAttachmentAdded,
                    //    ReceiverAssembly = Assembly.GetExecutingAssembly().FullName,
                    ReceiverName = ListRemoteRecieverName,
                    ReceiverClass = ListRemoteRecieverName,
                    ReceiverUrl = remoteEventRecieverUrl,
                    SequenceNumber = 10000
                };
                erdc.Add(attachmentAddedEventReceiver);
            }
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            theLog.Debug("Create the remote event receiver end");
        }
        public static void RemoveEvent(ClientContext clientContext, List list)
        {
            EventReceiverDefinitionCollection erdc = list.EventReceivers;
            List<EventReceiverDefinition> toDelete = new List<EventReceiverDefinition>();
            foreach (EventReceiverDefinition erd in erdc)
            {
                if (erd.ReceiverName.Equals(ListRemoteRecieverName))
                {
                    toDelete.Add(erd);
                }
            }
            //Delete the remote event receiver from the list, when the app gets uninstalled
            foreach (EventReceiverDefinition item in toDelete)
            {
                list.EventReceivers.GetById(item.ReceiverId).DeleteObject();
            }
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
        }

        public static void AddECBAction(ClientContext clientContext, List spList,
            string rightProtectUrl, string secureViewUrl, string editListUrl)
        {
            theLog.Debug("AddECBAction enter for list:" + spList.Title);

            bool bIsDocLibarary = SPOEUtility.SupportedLibraryTypes.Contains(spList.BaseTemplate);

            //check exist
            UserCustomActionCollection userCustomActionColl = spList.UserCustomActions;
            bool bHaveRightProtectAction = false;
            bool bHaveSecurityViewAction = false;
            bool bHaveEditListAction = false;
            foreach (var action in userCustomActionColl)
            {
                if (action.Name.Equals(SPOEUtility.RightProtectItemName, StringComparison.OrdinalIgnoreCase))
                {
                    bHaveRightProtectAction = true;
                }
                else if (action.Name.Equals(SPOEUtility.SecurityViewItemName, StringComparison.OrdinalIgnoreCase))
                {
                    bHaveSecurityViewAction = true;
                }
                else if (action.Name.Equals(SPOEUtility.ListEditItemName, StringComparison.OrdinalIgnoreCase))
                {
                    bHaveEditListAction = true;
                }

                if (bHaveRightProtectAction && bHaveSecurityViewAction && bHaveEditListAction)
                {
                    break; 
                }
            }
            
            if (!bHaveRightProtectAction)
            {
                UserCustomAction menuItemProtectAction = userCustomActionColl.Add();
                menuItemProtectAction.Name = SPOEUtility.RightProtectItemName;
                menuItemProtectAction.Location = "EditControlBlock";
                menuItemProtectAction.Sequence = 10001;
                menuItemProtectAction.Title = "NextLabs Rights Protection";
                menuItemProtectAction.Url = rightProtectUrl;
                menuItemProtectAction.Update();
                theLog.Debug("AddECBAction right protect");
            }
           
            if (bIsDocLibarary && (!bHaveSecurityViewAction))
            {
                UserCustomAction menuItemViewAction = userCustomActionColl.Add();
                menuItemViewAction.Name = SPOEUtility.SecurityViewItemName;
                menuItemViewAction.Location = "EditControlBlock";
                menuItemViewAction.Sequence = 10002;
                menuItemViewAction.Title = "NextLabs Secure View";
                menuItemViewAction.Url = secureViewUrl;
                menuItemViewAction.Update();
                theLog.Debug("AddECBAction security view");
            }

            if (!bHaveEditListAction)
            {
                UserCustomAction listEditViewAction = userCustomActionColl.Add();
                listEditViewAction.Name = SPOEUtility.ListEditItemName;
                listEditViewAction.Location = "Microsoft.SharePoint.ListEdit";
                listEditViewAction.Group = "Permissions";
                listEditViewAction.Sequence = 10002;
                listEditViewAction.Title = "NextLabs Rights Management";
                listEditViewAction.Url = editListUrl; 
                listEditViewAction.Update();
                theLog.Debug("AddECBAction edit list.");
            }
 
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

            theLog.Debug("AddECBAction end");
        }
        public static void RemoveECBAction(ClientContext clientContext, List spList)
        {
            theLog.Debug("RemoveECBAction enter list:" + spList.Title);
          
            UserCustomActionCollection userCustomActionColl = spList.UserCustomActions;

            bool bRemoveRightProtect = false;
            bool bRemoveSecurityView = false;
            bool bRemoveEditList = false;
            List<UserCustomAction> lstDeleteItem = new List<UserCustomAction>();
            foreach (var action in userCustomActionColl)
            {
                if (action.Name.Equals(SPOEUtility.RightProtectItemName, StringComparison.OrdinalIgnoreCase))
                {
                    lstDeleteItem.Add(action);
                    bRemoveRightProtect = true;
                }
                else if (action.Name.Equals(SPOEUtility.SecurityViewItemName, StringComparison.OrdinalIgnoreCase))
                {
                    lstDeleteItem.Add(action);
                    bRemoveSecurityView = true;
                }
                else if (action.Name.Equals(SPOEUtility.ListEditItemName, StringComparison.OrdinalIgnoreCase))
                {
                    lstDeleteItem.Add(action);
                    bRemoveEditList = true;
                }

                if (bRemoveRightProtect && bRemoveSecurityView && bRemoveEditList)
                {
                    break;
                }
            }

            foreach(UserCustomAction action in lstDeleteItem)
            {
                theLog.Debug("RemoveECBAction name:" + action.Name);
                action.DeleteObject();
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            }
                      
            theLog.Debug("RemoveECBAction end");
        }
        public static void AddEventToWeb(ClientContext clientContext, string remoteEventRecieverUrl)
        {
            theLog.Debug("AddEventToWeb enter");
            Web web = clientContext.Web;
            EventReceiverDefinitionCollection erdc = web.EventReceivers;
            //check whether there exists the event or not, if exists, we shouldn't add the event again.
            foreach (EventReceiverDefinition erd in erdc)
            {
                if (erd.ReceiverName.Equals(WebRemoteRecieverName))
                    return;
            }
            //Create the remote event receiver definition
            EventReceiverDefinitionCreationInformation addListEventReceiver = new EventReceiverDefinitionCreationInformation()
            {
                EventType = EventReceiverType.ListAdded,
                //     ReceiverAssembly = Assembly.GetExecutingAssembly().FullName,
                ReceiverName = WebRemoteRecieverName,
                ReceiverClass = WebRemoteRecieverName,
                ReceiverUrl = remoteEventRecieverUrl,
                SequenceNumber = 10000
            };
            erdc.Add(addListEventReceiver);
            web.Update();
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            theLog.Debug("AddEventToWeb end");
        }
        public static void RemoveEventOfWeb(ClientContext clientContext, string remoteEventRecieverUrl)
        {
            theLog.Debug("RemoveEventOfWeb enter");
            Web web = clientContext.Web;
            EventReceiverDefinitionCollection erdc = web.EventReceivers;
            List<EventReceiverDefinition> toDelete = new List<EventReceiverDefinition>();
            foreach (EventReceiverDefinition erd in erdc)
            {
                if (erd.ReceiverName.Equals(WebRemoteRecieverName))
                {
                    toDelete.Add(erd);
                }
            }
            //Delete the remote event receiver from the web, when the app gets uninstalled

            foreach (EventReceiverDefinition item in toDelete)
            {
                erdc.GetById(item.ReceiverId).DeleteObject();
                web.Update();
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            }
            theLog.Debug("RemoveEventOfWeb end");
        }
        public static List<List> GetListsFromListCollection(ClientContext clientContext, ListCollection listCollection)
        {
            List<List> lists = new List<List>();
            int count = listCollection.Count;
            if (0 == count)
            {
                return lists;
            }
            int k = (count % MaxRequestCount) == 0 ?
                count / SPOEUtility.MaxRequestCount :
                count / SPOEUtility.MaxRequestCount + 1;

            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * SPOEUtility.MaxRequestCount, count);
                for (int j = i * SPOEUtility.MaxRequestCount; j < maxCount; j++)
                {
                    clientContext.Load(listCollection[j], olist => olist.Title,
                        olist => olist.EventReceivers, List => List.Fields);
                }
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                for (int j = i * SPOEUtility.MaxRequestCount; j < maxCount; j++)
                {
                    List list = listCollection[j];
                    //the support type according to spe
                    int templateId = list.BaseTemplate;
                    if (SPOEUtility.SupportedLibraryTypes.Contains(templateId) || SPOEUtility.SupportedListTypes.Contains(templateId))
                    {
                        // theLog.Debug("GetListsFromListCollection templateId:" + templateId);
                        // theLog.Debug("GetListsFromListCollection Title:" + list.Title);
                        lists.Add(list);
                    }
                }
            }
            return lists;
        }
        private void InitlibrariesTitleFromListCollection(ClientContext clientContext, ListCollection listCollection)
        {

            string webUrl = clientContext.Web.Url;
            int count = listCollection.Count;
            if (count == 0) return;
            int k = (count % SPOEUtility.MaxRequestCount) == 0 ?
                count / SPOEUtility.MaxRequestCount :
                count / SPOEUtility.MaxRequestCount + 1;

            for (int i = 0; i < k; i++)
            {
                int maxCount = Math.Min((i + 1) * SPOEUtility.MaxRequestCount, count);
                for (int j = i * SPOEUtility.MaxRequestCount; j < maxCount; j++)
                {
                    List list = listCollection[j];
                    clientContext.Load(list, olist => olist.Title, olist => olist.EventReceivers);
                }
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            }
        }
        public static bool CheckBackUpPath(ClientContext curctx,string backUpPath)
        {
            try
            {
                //for when path is empty still can get folder
                if(string.IsNullOrEmpty(backUpPath))
                {
                    return false;
                }
                Folder folder = curctx.Web.GetFolderByServerRelativeUrl(backUpPath);
                curctx.Load(folder,d=>d.ServerRelativeUrl,d=>d.Exists);
                curctx.ExecuteQuery();
                if(folder!=null&&folder.Exists)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                theLog.Error("CheckBackUpPath error:"+ex.Message+ex.StackTrace);
            }
            return false;
        }

        public static void SetBatchModeTimer(string webUrl, string listId, List<ScheduleModel> scheduleData)
        {
            try
            {
                ScheduleModel scheduleModel = new ScheduleModel();
                if (scheduleData != null && scheduleData.Count > 0)
                {
                    foreach (ScheduleModel cellData in scheduleData)
                    {
                        if (cellData.IsSelected)
                        {
                            scheduleModel = cellData;
                            break;
                        }
                    }
                }
                if (scheduleModel.ScheduleType == "Minutely")
                {
                    TimeSpan intervalTime = new TimeSpan(0, int.Parse(scheduleModel.TimeInterval), 0);
                    AddOrChangeTimer(webUrl, listId, intervalTime, intervalTime);
                }
                else if (scheduleModel.ScheduleType == "Hourly")
                {
                    TimeSpan intervalTime = new TimeSpan(int.Parse(scheduleModel.TimeInterval), 0, 0);
                    AddOrChangeTimer(webUrl, listId, intervalTime, intervalTime);
                }
                else if (scheduleModel.ScheduleType == "Daily")
                {
                    int days = int.Parse(scheduleModel.TimeInterval);
                    int nHours = GetHoursByString(scheduleModel.StartTime);
                    TimeSpan intervalTime = new TimeSpan(days, 0, 0, 0);
                    DateTime nextTime = DateTime.Now.Date + new TimeSpan(days, nHours, 0, 0);
                    TimeSpan startTime = nextTime - DateTime.Now;
                    AddOrChangeTimer(webUrl, listId, startTime, intervalTime);
                }
                else if (scheduleModel.ScheduleType == "Weekly")
                {
                    RemoveAllTimer(listId);
                    List<Timer> timers = new List<Timer>();
                    int weeks = int.Parse(scheduleModel.TimeInterval);
                    int nHours = GetHoursByString(scheduleModel.StartTime);
                    TimeSpan intervalTime = new TimeSpan(weeks * 7, 0, 0, 0);
                    string[] specificDays = scheduleModel.SpecificDays.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string strDay in specificDays)
                    {
                        int days = (int)Enum.Parse(typeof(DayOfWeek), strDay, true);
                        DateTime nextTime = DateTime.Now.Date + new TimeSpan(weeks * 7 + days - (int)DateTime.Now.DayOfWeek, nHours, 0, 0);
                        TimeSpan startTime = nextTime - DateTime.Now;
                        theLog.Debug("----AddOrChangeTimer----startTime-(h)--" + startTime.TotalHours);
                        theLog.Debug("----AddOrChangeTimer---intervalTime-(h)--" + intervalTime.TotalHours);
                        Timer timer = new Timer(new TimerCallback(RunBatchModeSchedule), webUrl + ";" + listId, startTime, intervalTime);
                        timers.Add(timer);
                    }
                    lock (ScheduleLock)
                    {
                        DicScheduleTimmers.Add(listId, timers);
                    }
                }
                else
                {
                    RemoveAllTimer(listId);
                }
            }
            catch (Exception exp)
            {
                theLog.Error("SetBatchModeTimer Error:" + exp);
            }
        }

        private static void RemoveAllTimer(string listId)
        {
            lock (ScheduleLock)
            {
                if (DicScheduleTimmers.ContainsKey(listId))
                {
                    List<Timer> timers = DicScheduleTimmers[listId];
                    foreach (Timer timer in timers)
                    {
                        timer.Dispose();
                    }
                    timers.Clear();
                    DicScheduleTimmers.Remove(listId);
                }
            }
        }

        private static void AddOrChangeTimer(string webUrl, string listId, TimeSpan startTime, TimeSpan intervalTime)
        {
            theLog.Debug("----AddOrChangeTimer---webUrl---" + webUrl);
            theLog.Debug("----AddOrChangeTimer---listId---" + listId);
            theLog.Debug("----AddOrChangeTimer----startTime---" + startTime.TotalMinutes);
            theLog.Debug("----AddOrChangeTimer---intervalTime---" + intervalTime.TotalMinutes);
            lock (ScheduleLock)
            {
                if (DicScheduleTimmers.ContainsKey(listId))
                {
                    List<Timer> timers = DicScheduleTimmers[listId];
                    if (timers.Count >= 1)
                    {
                        timers[0].Change(startTime, intervalTime);
                        for (int i = 1; i < timers.Count; i++)
                        {
                            timers[i].Dispose();
                        }
                        timers.RemoveRange(1, timers.Count - 1);
                    }
                }
                else
                {
                    Timer timer = new Timer(new TimerCallback(RunBatchModeSchedule), webUrl + ";" + listId, startTime, intervalTime);
                    DicScheduleTimmers.Add(listId, new List<Timer> { timer });
                }
            }
        }

        private static void RunBatchModeSchedule(object obj)
        {
            string strObj = obj as string;
            int ind = strObj.IndexOf(";");
            if (-1 != ind)
            {
                string webUrl = strObj.Substring(0, ind);
                string listId = strObj.Substring(ind + 1);
                using (ClientContext clientContext = RMXUtility.GetSharePointApponlyClientContext(webUrl))
                {
                    BatchModeWorker batchModeWorker = new BatchModeWorker(clientContext, listId);
                    Task.Run(() => batchModeWorker.RunBatchModeForList());
                }
            }
        }

        private static int GetHoursByString(string strHour)
        {
            int nHours = 0;
            switch (strHour)
            {
                case "1:00 AM":
                    nHours = 1;
                    break;
                case "2:00 AM":
                    nHours = 2;
                    break;
                case "3:00 AM":
                    nHours = 3;
                    break;
                case "4:00 AM":
                    nHours = 4;
                    break;
                case "5:00 AM":
                    nHours = 5;
                    break;
                case "6:00 AM":
                    nHours = 6;
                    break;
                case "7:00 AM":
                    nHours = 7;
                    break;
                case "8:00 AM":
                    nHours = 8;
                    break;
                case "9:00 AM":
                    nHours = 9;
                    break;
                case "10:00 AM":
                    nHours = 10;
                    break;
                case "11:00 AM":
                    nHours = 11;
                    break;
                case "12:00 PM":
                    nHours = 12;
                    break;
                case "1:00 PM":
                    nHours = 13;
                    break;
                case "2:00 PM":
                    nHours = 14;
                    break;
                case "3:00 PM":
                    nHours = 15;
                    break;
                case "4:00 PM":
                    nHours = 16;
                    break;
                case "5:00 PM":
                    nHours = 17;
                    break;
                case "6:00 PM":
                    nHours = 18;
                    break;
                case "7:00 PM":
                    nHours = 19;
                    break;
                case "8:00 PM":
                    nHours = 20;
                    break;
                case "9:00 PM":
                    nHours = 21;
                    break;
                case "10:00 PM":
                    nHours = 22;
                    break;
                case "11:00 PM":
                    nHours = 23;
                    break;
                default:
                    nHours = 0;
                    break;
            }
            return nHours;
        }
    }
}