using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using RmxForSPOWeb.Models;
using Newtonsoft.Json;

namespace RmxForSPOWeb.Common
{
    public class BatchModeWorker
    {
        private enum BATCHMODE_STATUS
        {
            NOT_RUNNING=0,
            RUNNING=1,
            FINISHED=2,
        };

        private string m_listId = "";
        private ClientContext m_clientContext = null;
        private static IniFiles m_libSettingFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "LibSetting.ini");
        private static object m_lock = new object();
        protected static CLog theLog = CLog.GetLogger("BatchModeWorker");

        protected static readonly string strBatchModeIniKeyStatus = "BatchModeStatus";
        protected static readonly string strBatchModeIniKeyDate = "DateTime";
        protected static readonly string strBatchModeIniKeyFailedFiles = "FailedFiles";
        protected static readonly string strBatchModeIniKeyFailedFilesCount = "FailedFilesCount";


        public BatchModeWorker(ClientContext ctx)
        {
            m_clientContext = ctx;
        }
        public BatchModeWorker(ClientContext ctx,string listId)
        {
            m_clientContext = ctx;
            m_listId = listId;
        }
        public static string GetBatchModeShowStatus(string listId)
        {
            string strShowStatus = "Unknow";
            string strDate = "";
            string strStatus = "";
            string strFailedFilesCount = "";
            lock (m_lock)
            {
                strDate = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyDate);
                strStatus = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyStatus);
                strFailedFilesCount = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyFailedFilesCount);
            }

            if (string.IsNullOrWhiteSpace(strStatus) ||
                strStatus.Equals(BATCHMODE_STATUS.NOT_RUNNING.ToString()))
            {
                strShowStatus = "";
                //strShowStatus = "Batch mode is not running.";
            }
            else if (strStatus.Equals(BATCHMODE_STATUS.RUNNING.ToString()))
            {
                strShowStatus = "Batch mode is performing rights protection for all items.";
            }
            else if (strStatus.Equals(BATCHMODE_STATUS.FINISHED.ToString()))
            {
                if(strFailedFilesCount.Equals("0")) strShowStatus = "Batch mode process was successful at:" + strDate+ ".";
                else strShowStatus = "Batch mode process was finished at:" + strDate + ".";
            }

            return strShowStatus;
        }
        public static string GetBatchModeFailedFiles(string listId)
        {
            string strFailedFiles = "";
            lock(m_lock)
            {
                strFailedFiles = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyFailedFiles);
            }
            return strFailedFiles;
        }
        public static string GetBatchModeFailedFilesCount(string listId)
        {
            string strFailedFilesCount = "";
            lock (m_lock)
            {
                strFailedFilesCount = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyFailedFilesCount);
            }
            return strFailedFilesCount;
        }

        //public void RunBatchMode()
        //{

        //    //get url
        //    m_clientContext.Load(m_clientContext.Web, d => d.Url);
        //    ExecuteQueryWorker.AddedWaitExecuteQueryTask(m_clientContext);
        //    m_strWebUrl = m_clientContext.Web.Url;

        //    theLog.Info("Batch mode process for:" + m_strWebUrl);
        //    if (!CheckBatchModeRunning(m_strWebUrl))
        //    {
        //        List<BatchModeFailedModel> listFailedItem = new List<BatchModeFailedModel>();
        //        //do batch
        //        try
        //        {
        //            //set run status to running
        //            RecordBatchModeStatus(BATCHMODE_STATUS.RUNNING, 0, 0, 0, "");
        //            SPOEUtility.DoBatchMode(m_clientContext,listFailedItem);
        //        }
        //        catch (System.Exception ex)
        //        {
        //            theLog.Error("SPOEUtility.DoBatchMode exception:" + ex.ToString());
        //        }
        //        finally
        //        {
        //            theLog.Info("Batch mode process end for:" + m_strWebUrl);
        //            //set run status to finished.
        //            string strFailedFile = JsonConvert.SerializeObject(listFailedItem);
        //            int failedFileCount = listFailedItem.Count;
        //            theLog.Debug("failedFileCount:"+ failedFileCount);
        //            theLog.Debug("strFailedFile:"+ strFailedFile);
        //            RecordBatchModeStatus(BATCHMODE_STATUS.FINISHED, 0, 0, failedFileCount, strFailedFile);
        //        }
        //    }
        //    else
        //    {
        //        theLog.Debug("BatchMode is Running, wait for it finished.");
        //    }
        //}
        public void RunBatchModeForList()
        {
            theLog.Debug("RunBatchModeForList enter");
            m_clientContext.Load(m_clientContext.Web, d => d.Url,d=>d.Lists);
            List list = m_clientContext.Web.Lists.GetById(new Guid(m_listId));
            m_clientContext.Load(list,d=>d.EventReceivers.Include(eventReceiver => eventReceiver.ReceiverName),d=>d.Title,d=>d.BaseTemplate,d=>d.Id,d=>d.Fields);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(m_clientContext);
            theLog.Info("Batch mode process for list:" + m_listId);
            if (!CheckBatchModeRunning(m_listId))
            {
                List<BatchModeFailedModel> listFailedItem = new List<BatchModeFailedModel>();
                //do batch
                try
                {
                    //set run status to running
                    RecordBatchModeStatus(BATCHMODE_STATUS.RUNNING, 0, 0, 0, "");
                    SPOEUtility.BatchModeForList(m_clientContext,list, listFailedItem);
                }
                catch (System.Exception ex)
                {
                    theLog.Error("SPOEUtility.DoBatchMode exception:" + ex.ToString());
                }
                finally
                {
                    theLog.Info("Batch mode process end for:" + m_listId);
                    //set run status to finished.
                    string strFailedFile = JsonConvert.SerializeObject(listFailedItem);
                    int failedFileCount = listFailedItem.Count;
                    theLog.Debug("failedFileCount:" + failedFileCount);
                    theLog.Debug("strFailedFile:" + strFailedFile);
                    RecordBatchModeStatus(BATCHMODE_STATUS.FINISHED, 0, 0, failedFileCount, strFailedFile);
                }
            }
            else
            {
                theLog.Debug("BatchMode is Running, wait for it finished.");
            }
        }
        private void RecordBatchModeStatus(BATCHMODE_STATUS batchStatus, int nTotal, int nSuccess, int nFailed, string strFailedDetailFile)
        {
            lock (m_lock)
            {
                m_libSettingFile.IniWriteValue(m_listId, strBatchModeIniKeyStatus, batchStatus.ToString());
                m_libSettingFile.IniWriteValue(m_listId, strBatchModeIniKeyDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                m_libSettingFile.IniWriteValue(m_listId, strBatchModeIniKeyFailedFilesCount, nFailed.ToString());
                m_libSettingFile.IniWriteValue(m_listId, strBatchModeIniKeyFailedFiles, strFailedDetailFile);
            }
            //record result if finished
            if (batchStatus==BATCHMODE_STATUS.FINISHED)
            {

            }
        }

        public static bool CheckBatchModeRunning(string listId)
        {
            string strDate = "";
            string strStatus = "";
            lock (m_lock)
            {
                strDate = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyDate);
                strStatus = m_libSettingFile.IniReadValue(listId, strBatchModeIniKeyStatus);
            }
            if (strStatus.Equals(BATCHMODE_STATUS.RUNNING.ToString()) && !string.IsNullOrEmpty(strDate))
            {
                DateTime runningTime = DateTime.Now;
                bool bParse = DateTime.TryParse(strDate, out runningTime);
                if (bParse && (DateTime.Now - runningTime).TotalSeconds < 10 * 60)
                {
                    return true;
                }
            }
            return false;
        }

        public static void SetRunningDateTime(string listId)
        {
            lock (m_lock)
            {
                m_libSettingFile.IniWriteValue(listId, strBatchModeIniKeyDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public static void ResetBatchModeStatus(string listId)
        {
            lock (m_lock)
            {
                m_libSettingFile.IniWriteValue(listId, strBatchModeIniKeyStatus, BATCHMODE_STATUS.NOT_RUNNING.ToString());
                m_libSettingFile.IniWriteValue(listId, strBatchModeIniKeyDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                m_libSettingFile.IniWriteValue(listId, strBatchModeIniKeyFailedFiles, "");
                m_libSettingFile.IniWriteValue(listId, strBatchModeIniKeyFailedFilesCount, "");
                m_libSettingFile.IniWriteValue(listId, SPOEUtility.strDeleteSourceFileEnable, "");
                m_libSettingFile.IniWriteValue(listId, SPOEUtility.strHistoryVersionEnable, "");
                m_libSettingFile.IniWriteValue(listId, SPOEUtility.strSchedultList, "");
            }
        }
    }
}