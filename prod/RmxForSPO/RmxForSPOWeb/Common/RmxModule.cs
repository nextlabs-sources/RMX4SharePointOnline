using Microsoft.SharePoint.Client;
using SDKWrapper4RMXLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using RmxForSPOWeb.Common.ConfigFileUtility;
using Microsoft.SharePoint.Client.Taxonomy;

namespace RmxForSPOWeb.Common
{
    public class RmxModule
    {
        protected static CLog theLog = CLog.GetLogger("RmxModule");
        public const int SuccessStatus = 0;
        public const int LimitTagSize = 60;
        public const int LimitRightFileName = 25;
        public const int LimitBackupFileName = 128;
        public const int LimitBackupFileUrl = 400;

        public static readonly string m_strNewDocumentNameFormat = WebConfigurationManager.AppSettings.Get("NewDocumentNameFormat");

        //in order to avoid we process the same file twice, here we maintain a list to the file that we current process
        private static System.Threading.ReaderWriterLockSlim m_LockSlim = new System.Threading.ReaderWriterLockSlim();
        private static System.Collections.Hashtable m_nlist = new System.Collections.Hashtable();
        static IniFiles libConfigFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "LibSetting.ini");

        public static CommonMessageConfig m_commonMessageConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~")
               .GetSection("CommonMessageConfig") as CommonMessageConfig;

        static RmxModule()
        {
            string commonPath = RMXUtility.GetRMXAppFolder() + @"bin\Common\";
            IGlobalConfig theGlobalConfig = new GlobalConfig();
            theGlobalConfig.SetCommonLibPath(commonPath);
        }
        public static bool EncryptItemVerstions(ClientContext clientContext, List list,ListItem item, File itemFile, Dictionary<string, string> dicTags)
        {
            bool bEncryptSatus = true;
            int nFileNeedProcess = -1;
            try
            {
                string strFileUrl = itemFile.ServerRelativeUrl;
                try
                { 
                    m_LockSlim.EnterWriteLock();
                    if (m_nlist.Contains(strFileUrl))  {
                        theLog.Info("EncryptItemVerstions return for this is already in-process." + strFileUrl);
                        nFileNeedProcess = 0;
                        return true;
                    }
                    else {
                        nFileNeedProcess = 1;
                        m_nlist.Add(strFileUrl, "");
                    }
                }
                finally
                {
                    m_LockSlim.ExitWriteLock();
                }

                //string strBackUpEnable = listconfigFile.IniReadValue(list.Id.ToString(), SPOEUtility.strBackUpEnable);
                // bool bBackup = strBackUpEnable == "true" ? true : false;
                //string strBackupPath = listconfigFile.IniReadValue(list.Id.ToString(), SPOEUtility.strBackUpPath);
                string strVersions = libConfigFile.IniReadValue(list.Id.ToString(), SPOEUtility.strHistoryVersionEnable); 
                bool bVersions = strVersions == "true" ? true : false;
                string strDeleteSourceFile = libConfigFile.IniReadValue(list.Id.ToString(), SPOEUtility.strDeleteSourceFileEnable);
                bool bDeleteSourceFile = strDeleteSourceFile == "true" ? true : false;
                theLog.Debug("bVersions:"+bVersions);
                theLog.Debug("bDeleteSourceFile:" + bDeleteSourceFile);
                List<byte[]> listData = new List<byte[]>();
                bool bListType = SPOEUtility.SupportedListTypes.Contains(list.BaseTemplate);
                clientContext.Load(itemFile.Versions);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                //check nxl file exist
                bool bExistedNxl = false;
                string strNxlFile = itemFile.ServerRelativeUrl + ".nxl";
                File nxlFile = GetFileByUrl(clientContext.Web, strNxlFile);
                clientContext.Load(nxlFile, d => d.Exists);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                theLog.Debug("nxlFile.Exists:" + nxlFile.Exists);
                if(nxlFile != null && nxlFile.Exists)
                {
                    //when exist a nxl file,don't do history version
                    bExistedNxl = true;
                    theLog.Debug("bExistedNxl:"+ bExistedNxl);
                }
                if (!bExistedNxl&&!bListType && bVersions && itemFile.Versions != null && itemFile.Versions.Count > 0)
                {                    
                    foreach (FileVersion fileVersion in itemFile.Versions)
                    {
                        if (fileVersion != null)
                        {
                            var versionStream = fileVersion.OpenBinaryStream();
                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                            if(!DoFileRMX(versionStream.Value, fileVersion.Url, dicTags, listData,list.Id.ToString()))
                            {
                                theLog.Debug("DoFileRMX version failed");
                                return false;
                            }
                        }
                    }
                }
               
                var clientResultStream = itemFile.OpenBinaryStream();
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                System.IO.Stream stream = clientResultStream.Value;
                var serverRelativeUrl = itemFile.ServerRelativeUrl;
                if (!DoFileRMX(stream, serverRelativeUrl, dicTags, listData,list.Id.ToString()))
                {
                    theLog.Debug("DoFileRMX failed");
                    return false;
                }

                theLog.Debug("EncryptItemVerstions listData.Count:" + listData.Count);
                if (listData.Count > 0)
                {
                    bool bNewNxl = false;

                    //create nxl file By CopyTo. we use CopyTo to keep it has same columns with the original file
                    // !!!Change , don't use copyto ,just copy file attributes to target side.
                    if (!bExistedNxl)
                    {
                        theLog.Info("nxl file didn't exist, create it:" + strNxlFile);
                        itemFile.CopyTo(strNxlFile, true);
                        nxlFile = GetFileByUrl(clientContext.Web, strNxlFile);
                        bNewNxl = true;

                        clientContext.Load(nxlFile, d => d.Exists, d => d.ServerRelativeUrl, d => d.CheckOutType);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                        theLog.Debug("nxlFile.Exists after create:" + nxlFile.Exists);
                    }

                    //check file require checkout before edit
                    clientContext.Load(list, oList => oList.ForceCheckout, oList=>oList.BaseTemplate);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                    theLog.Info("List.ForceCheckout:" + list.ForceCheckout.ToString());

                    bool bDocLib = SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate);

                    //update file content
                    if (nxlFile != null && nxlFile.Exists)
                    {
                        for (int i = 0; i < listData.Count; i++)
                        {  
                            if(!UpdateFileContent(clientContext,list, nxlFile, strNxlFile, listData[i]))
                            {
                                bEncryptSatus = false;
                                break;
                            }
                            //the first version of nxl file is normal file(created by CopyTo), we need to delete it. 
                            if (bNewNxl && i == 0 && bDocLib)
                            {
                              nxlFile.Versions.DeleteAll();   
                              ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                            }
                            if (i == listData.Count - 1 && bExistedNxl && SPOEUtility.SupportedLibraryTypes.Contains(list.BaseTemplate))
                            {
                                ListItem newListItem = nxlFile.ListItemAllFields;
                                clientContext.Load(newListItem);
                                clientContext.Load(item);
                                clientContext.Load(list, d => d.Fields.Include(field => field.ReadOnlyField, Field => Field.Title,Field=>Field.FieldTypeKind));
                                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                                try
                                {
                                    foreach (Field field in list.Fields)
                                    {
                                        try
                                        {
                                            if (item.FieldValues.ContainsKey(field.Title) && !field.ReadOnlyField)
                                            {
                                                if (field.GetType() == typeof(TaxonomyField))
                                                {
                                                    TaxonomyField taxonomyField = clientContext.CastTo<TaxonomyField>(field);
                                                    var taxonomyValue = item.FieldValues[field.Title];
                                                    if (taxonomyValue == null)
                                                    {
                                                        taxonomyField.ValidateSetValue(newListItem, null);
                                                        continue;
                                                    }
                                                    if (taxonomyValue.GetType() == typeof(TaxonomyFieldValue))
                                                    {
                                                        taxonomyField.SetFieldValueByValue(newListItem, taxonomyValue as TaxonomyFieldValue);
                                                    }
                                                    else if (taxonomyValue.GetType() == typeof(TaxonomyFieldValueCollection))
                                                    {
                                                        string[] termValuesarrary;
                                                        List<string> termValues = new List<string>();
                                                        foreach (TaxonomyFieldValue taxProductFieldValue in taxonomyValue as TaxonomyFieldValueCollection)
                                                        {
                                                            termValues.Add(taxProductFieldValue.WssId + ";#" + taxProductFieldValue.Label + "|" + taxProductFieldValue.TermGuid);
                                                        }
                                                        termValuesarrary = termValues.ToArray();
                                                        string strtermValues = string.Join(";#", termValuesarrary);
                                                        TaxonomyFieldValueCollection terms = new TaxonomyFieldValueCollection(clientContext, strtermValues, field);
                                                        taxonomyField.SetFieldValueByValueCollection(newListItem, terms);
                                                    }
                                                    continue;
                                                }
                                                newListItem[field.Title] = item.FieldValues[field.Title];
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            theLog.Error("update field value error:"+ex.Message);
                                        }
                                    }
                                    newListItem.Update();
                                    clientContext.Load(newListItem);
                                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                                }
                                catch (Exception ex)
                                {
                                    theLog.Error("update column value error:" + ex.Message);
                                }
                            }
                        }
                        // Delete or backup original file.
                        //DeleteOrBackupItem(clientContext, itemFile, bBackup, strBackupPath, bListType);
                        if(bDeleteSourceFile && bEncryptSatus)
                        {
                            itemFile.DeleteObject();
                            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bEncryptSatus = false;
                theLog.Error("EncryptItemVerstions error:"+ex.Message+ex.StackTrace);
            }
            finally
            {
                if (nFileNeedProcess==1)
                {
                    try
                    {
                        string strFileUrl = itemFile.ServerRelativeUrl;
                        m_LockSlim.EnterWriteLock();
                        m_nlist.Remove(strFileUrl);
                    }
                    finally
                    {
                        m_LockSlim.ExitWriteLock();
                    }
                }
            }
            
            return bEncryptSatus;
        }

        public static void DeleteOrBackupItem(ClientContext clientContext, File orgItemFile, bool bBackup, string strBackupPath, bool bListType)
        {
            theLog.Debug("DeleteOrBackupItem enter");
            if (bBackup)
            {
                ClientContext clientContextApp = RMXUtility.GetSharePointApponlyClientContext(clientContext.Web.Url);
                File itemFile = GetFileByUrl(clientContextApp.Web, orgItemFile.ServerRelativeUrl);
                clientContextApp.Load(itemFile, d => d.ServerRelativeUrl, d => d.Versions);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);

                // Backup file after rights protection.
                string newName = itemFile.ServerRelativeUrl.Replace("/", "_");
                if (newName.Length > LimitBackupFileName)
                {
                    newName = newName.Substring(0, LimitBackupFileName - LimitRightFileName - 3) + "..." + newName.Substring(newName.Length - LimitRightFileName, LimitRightFileName);
                }
                string strBackupFile = strBackupPath + "/" + newName;
                theLog.Debug("Original file:" + itemFile.ServerRelativeUrl);
                theLog.Debug("Backup file:" + strBackupFile);

                File backupFile = GetFileByFullUrl(clientContextApp.Web, strBackupFile);
                clientContextApp.Load(backupFile, d => d.Exists, d => d.ServerRelativeUrl);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);

                if (backupFile != null && backupFile.Exists)
                {
                    //backup history version
                    if (!bListType && itemFile.Versions != null && itemFile.Versions.Count > 0)
                    {
                        foreach (FileVersion fileVersion in itemFile.Versions)
                        {
                            if (fileVersion != null)
                            {
                                var versionStream = fileVersion.OpenBinaryStream();
                                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);
                                backupFile.SaveBinary(new FileSaveBinaryInformation { ContentStream = versionStream.Value });
                                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);
                            }
                        }
                    }

                    //backup current version
                    var clientResultStream = itemFile.OpenBinaryStream();
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);
                    backupFile.SaveBinary(new FileSaveBinaryInformation{ ContentStream = clientResultStream.Value });
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);

                    //delete the origin file
                    itemFile.DeleteObject();
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);
                }
                else
                {
                    itemFile.MoveTo(strBackupFile, MoveOperations.Overwrite);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContextApp);
                }
            }
            else
            {
                orgItemFile.DeleteObject();
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            }
        }

        public static bool DoRMX(string sourcePath, string destPath, Dictionary<string, string> dicSetTags, string listID)
        {
            theLog.Debug("DoRMX enter");
            bool bRet = false;
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath)) return bRet;
            if (sourcePath.EndsWith(".nxl", StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                bool bSetTag = false;
                RMXConfig cfg = RMXConfig.Instance();
                INLRightsManager rm = new NLRightsManager();
                rm.InitializeClass(cfg.RouterURL, cfg.AppKey, cfg.AppID);
                if (!IsNxlFileFormat(rm, sourcePath))
                {
                    List<string> lstTagKey = new List<string>();
                    List<string> lstTagValue = new List<string>();

                    if(dicSetTags!=null && dicSetTags.Count>0)
                    {
                        List<string> ListLowValue = new List<string>();
                        foreach (KeyValuePair<string, string> keyValue in dicSetTags)
                        {
                            if (keyValue.Key.Length > LimitTagSize || CheckInvalidTag(keyValue.Key)) continue;
                            theLog.Debug(keyValue.Key + "   NLSetTag   " + keyValue.Value);
                            string[] values = keyValue.Value.Split(new string[] { SPOEUtility.TagSeparator }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string value in values)
                            {
                                if (!CheckInvalidTag(keyValue.Value) && value.Length <= LimitTagSize && !ListLowValue.Contains(value.ToLower()))
                                {
                                    ListLowValue.Add(value.ToLower());
                                    lstTagKey.Add(keyValue.Key);
                                    lstTagValue.Add(value);
                                    bSetTag = true;
                                }
                            }
                            ListLowValue.Clear();
                        }
                    }
                    if (!bSetTag)
                    {
                        theLog.Debug("NextLabs tags");
                        lstTagKey.Add("Classification");
                        lstTagValue.Add("RMX For SPOL");
                    }               
                    //var projectTenantName = libConfigFile.IniReadValue(listID, "ProjectTenantName");
                   // int status = rm.NLEncryptProject(projectTenantName, sourcePath, destPath, lstTagKey.ToArray(), lstTagValue.ToArray());
                    int status = rm.NLEncryptTokenGroup(0,sourcePath, destPath, lstTagKey.ToArray(), lstTagValue.ToArray());
                    if (status == SuccessStatus)
                    {
                        theLog.Debug("DoRMX Success");
                        bRet = true;
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Error("DoRMX error:" + ex.Message+ex.StackTrace);
            }
            return bRet;
        }
        public static bool DoFileRMX(System.IO.Stream stream, string fileUrl, Dictionary<string, string> dicTags, List<byte[]> listData,string listID)
        {
            theLog.Debug("DoFileRMX enter");
            bool bRet = false;
            string sourcePath = "";
            string destPath = "";
            try
            {
                //Download the fileVersion
                sourcePath = SaveFileToTemp(stream, fileUrl);
                destPath = sourcePath + ".nxl";
                bool bEncrypt = DoRMX(sourcePath, destPath, dicTags,listID);
                theLog.Debug("bEncrypt:"+ bEncrypt);
                if (bEncrypt)
                {
                    byte[] data =System.IO.File.ReadAllBytes(destPath);
                    if (data!=null && data.Length > 16*1024) //the size of nxl file is at least 16kb
                    {
                        listData.Add(data);
                        bRet = true;
                    }
                    else
                    {
                        string strLog = string.Format("DoFileRMX: file size too small after encrypt:{0}, data.len:{1}, original file size:{2}",
                            fileUrl, data == null ? "NULL" : data.Length.ToString(), stream.Length);
                        theLog.Fatal(strLog);
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Error("DoFileRMX exception:"+ex.Message+ex.StackTrace);
            }
            finally
            {
                if (!string.IsNullOrEmpty(sourcePath) && System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Delete(sourcePath);
                }
                if (!string.IsNullOrEmpty(destPath) && System.IO.File.Exists(destPath))
                {
                    System.IO.File.Delete(destPath);
                }
            }
            theLog.Debug("DoFileRMX end");
            return bRet;
        }
        private static bool CheckInvalidTag(string strTag)
        {
            if (string.IsNullOrEmpty(strTag) || strTag.Contains("%") || strTag.Contains("'") || strTag.Contains("\""))
            {
                return true;
            }
            return false;
        }
        public static string SaveFileToTemp(System.IO.Stream stream, string fileUrl)
        {
            theLog.Debug("SaveFileToTemp enter");
            string strFilePath = "";
            try
            {
                strFilePath = System.IO.Path.GetTempFileName();
                System.IO.File.Delete(strFilePath);
                int ext = fileUrl.LastIndexOf(".");
                if(ext>0)
                {
                    string extName = fileUrl.Substring(ext);
                    strFilePath += extName;
                }

                stream.Seek(0,System.IO.SeekOrigin.Begin);
                using (System.IO.FileStream fs = new System.IO.FileStream(strFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    stream.CopyTo(fs);
                    fs.Flush();
                    fs.Close();

                    //reset the position
                    stream.Seek(0, System.IO.SeekOrigin.Begin);  
                }
            }
            catch (Exception ex)
            {
                theLog.Error("SaveFileToTemp failed:" + ex.Message + ex.StackTrace);
            }
            theLog.Debug("SaveFileToTemp end");
            return strFilePath;
        }
        public static File GetFileByUrl(Web web, string fileUrl)
        {
            File file = null;
            try
            {
                file = web.GetFileByServerRelativeUrl(fileUrl);
            }
            catch
            {
                file = null;
            }
            return file;
        }

        public static File GetFileByFullUrl(Web web, string fileUrl)
        {
            File file = null;
            try
            {
                file = web.GetFileByUrl(fileUrl);
            }
            catch
            {
                file = null;
            }
            return file;
        }

        public static bool UpdateFileContent(ClientContext clientContext,List list, 
                                             File spFile, string serverRelativeUrl, 
                                             byte[] filecontent)
        {
            theLog.Debug("UpdateFileContent enter:" + serverRelativeUrl);
            bool bCheckedOutByCode = false;
            try
            {
                if (list.ForceCheckout)
                {
                    theLog.Info("check out file:" + serverRelativeUrl);
                    spFile.CheckOut();
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                    bCheckedOutByCode = true;
                }

                using (System.IO.Stream stream = new System.IO.MemoryStream(filecontent))
                {
                    var fci = new FileCreationInformation
                    {
                        Url = serverRelativeUrl,
                        ContentStream = stream,
                        Overwrite = true
                    };
                    File updatedFile = list.RootFolder.Files.Add(fci);
                    clientContext.Load(updatedFile);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
            }
            catch (System.Exception ex)
            {
                theLog.Debug("UpdateFileContent exception:" + ex.ToString() );
                return false;
            }
            finally
            {
                if (bCheckedOutByCode)
                {
                    theLog.Info("check in file:" + serverRelativeUrl);
                    spFile.CheckIn(m_commonMessageConfig.CheckInComment, CheckinType.MinorCheckIn);
                    ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                }
            }
            
            theLog.Debug("UpdateFileContent End");
            return true;
        }
        public static bool IsNxlFileFormat(INLRightsManager nlRManager, string filePath)
        {
            int iNxl = 0;
            int iRet = nlRManager.NLIsNxl(filePath, out iNxl);
            if (iRet == SuccessStatus && iNxl != 0) // iNxl is 0, it means it is not nxl file.
            {
                return true;
            }
            return false;
        }
        public static bool GetNormalFileTags(string filePath, Dictionary<string, string> dicTags, string separator)
        {
            theLog.Debug("GetNormalFileTags enter");
            bool bRet = false;
            if (string.IsNullOrEmpty(filePath) || dicTags == null) return bRet;
            try
            {
                IFileTagManager fileTagManager = new FileTagManager();
                int nCount = 0;
                int iRet = fileTagManager.GetTagsCount(filePath, out nCount);
                theLog.Debug("iRet:"+ iRet);
                theLog.Debug("nCount:"+ nCount);
                if (iRet == SuccessStatus)
                {
                    theLog.Debug("iRet == SuccessStatus");
                    string tagName = null;
                    string tagValue = null;
                    string tagLowerName = null;
                    for (int i = 0; i < nCount; i++)
                    {
                        iRet = fileTagManager.GetTagByIndex(filePath, i, out tagName, out tagValue);
                        theLog.Debug("iRet1:"+ iRet);
                        if (iRet == SuccessStatus)
                        {
                            if (!string.IsNullOrEmpty(tagName))
                            {
                                tagLowerName = tagName.ToLower();
                                if (dicTags.ContainsKey(tagLowerName))
                                {
                                    dicTags[tagLowerName] += (separator + tagValue); // mutiple value, use separator to separate.
                                }
                                else
                                {
                                    theLog.Debug("tagLowerName:"+ tagLowerName);
                                    theLog.Debug("tagValue:"+ tagValue);
                                    dicTags[tagLowerName] = tagValue;
                                }
                                bRet = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Error("GetNormalFileTags failed:" + ex.Message + ex.StackTrace);
            }
            return bRet;
        }
    }
}