using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;

namespace RmxForSPOWeb.Common
{
    public class DelayedItemMgr
    {
        private static List<DelayedItem> m_lstDelayedItems = new List<DelayedItem>();
        private static readonly object m_DelItemLock = new object();
        private static System.Threading.Thread m_delayedThread = null;
        private static CLog theLog = CLog.GetLogger("DelayedItemMgr");

        private class DelayedItem
        {
            public ClientContext clientContext;
            public List spList;
            public ListItem spListItem;
            public File spFile;
            public string strFileUrl;
            public Dictionary<string, string> dicTags;
            public DateTime processTime;
        }

        private static bool FindAndUpdateDelayItemByUrl(string fileUrl, DateTime processTime)
        {
            lock (m_DelItemLock)
            {
                for (int i = 0; i < m_lstDelayedItems.Count; i++)
                {
                    DelayedItem di = m_lstDelayedItems[i];
                    if (di.strFileUrl.Equals(fileUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        di.processTime = processTime;
                        return true;
                    }
                }
            }
            return false;
        }


        public static void AddedDelayedItem(ClientContext context, List spList, ListItem spListItem,
           File spFile, string fileUrl, Dictionary<string, string> dicTags)
        {
           DateTime processTime = DateTime.Now + (new TimeSpan(0, 5, 0));

           if (!FindAndUpdateDelayItemByUrl(fileUrl, processTime))
           {
                //log
                string strLog = string.Format("AddedDelayedItem file url:{0}", fileUrl);
                theLog.Info(strLog);

                //added item
                DelayedItem delayedItem = new DelayedItem();
                delayedItem.clientContext = context;
                delayedItem.spList = spList;
                delayedItem.spListItem = spListItem;
                delayedItem.spFile = spFile;
                delayedItem.strFileUrl = fileUrl;
                delayedItem.dicTags = dicTags;
                delayedItem.processTime = processTime;

                lock (m_DelItemLock)
                {
                    m_lstDelayedItems.Add(delayedItem);
                }
            }

          
            //start thread
            if (m_delayedThread == null)
            {
                m_delayedThread = new System.Threading.Thread(DelayedItemWorker);
                m_delayedThread.Start();
            }

        }

        private static DelayedItem GetDelayDeleteItem(DateTime dt)
        {
            lock (m_DelItemLock)
            {
                for (int i = 0; i < m_lstDelayedItems.Count; i++)
                {
                    DelayedItem di = m_lstDelayedItems[i];
                    if (di.processTime < dt)
                    {
                        m_lstDelayedItems.RemoveAt(i);
                        return di;
                    }
                }
            }
            return null;
        }


        public static void DelayedItemWorker()
        {
            List<DelayedItem> lstDelayedItems = new List<DelayedItem>();
            while (true)
            {
                System.Threading.Thread.Sleep(3*60 * 1000);


                DelayedItem delItem = null;
                while ((delItem = GetDelayDeleteItem(DateTime.Now)) != null)
                {
                    if (!DoDelayedItem(delItem))
                        lstDelayedItems.Add(delItem);
                }

                // re-add failed item
                if (lstDelayedItems.Count>0)
                {
                    lock (m_DelItemLock)
                    {
                        m_lstDelayedItems.AddRange(lstDelayedItems);
                    }
                    lstDelayedItems.Clear();
                }
               
            }
        }

        private static bool DoDelayedItem(DelayedItem delItem)
        {
            bool bOk = true;
            //get item
            try
            {
                //log
                string strLog = string.Format("DoDelayDeleteItem file url:{0}", delItem.strFileUrl);
                theLog.Info(strLog);

                //load file property
                delItem.clientContext.Load(delItem.spFile, p => p.CheckOutType, p => p.LockedByUser);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(delItem.clientContext);

                delItem.clientContext.Load(delItem.spFile.LockedByUser, p => p.UserPrincipalName);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(delItem.clientContext);

                //check lock status first
                string strLockUser = RMXUtility.GetUserPrincipalName(delItem.spFile.LockedByUser);
                if (!string.IsNullOrEmpty(strLockUser))
                {//still locked
                    bOk = false;
                    strLog = string.Format("DoDelayDeleteItem file is still locked, need to process it next time, url:{0}", delItem.strFileUrl);
                    theLog.Info(strLog);
                }
                //check checkout status
                else if (delItem.spFile.CheckOutType == CheckOutType.None)
                {
                    // bOk = true; if it is unlock and not checkedout, we call EncryptItem and ignore its return value.
                    bool bEncrypt = RmxModule.EncryptItemVerstions(delItem.clientContext,
                        delItem.spList, delItem.spListItem, delItem.spFile, delItem.dicTags);
                    theLog.Info("bEncrypt:" + bEncrypt);
                }
                else
                {
                    //here bOk=true. it is checkout status.
                }
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on DoDelayedItem:" + ex.ToString());
            }
            return bOk;
        }


    }
}