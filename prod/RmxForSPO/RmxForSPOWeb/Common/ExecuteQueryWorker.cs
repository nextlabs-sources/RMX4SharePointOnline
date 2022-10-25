using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using System.Threading;
using System.Net;

namespace RmxForSPOWeb.Common
{
    public class ExecuteQueryWorker
    {

        public class  ExecuteQueryTask
        {
            private static CLog theLog = CLog.GetLogger("ExecuteQueryTask");

            //when server return http status 429, we wait for some seconds
            //we no need to lock this var for multi thread, because even if it is rewriter by other, will works well.
            private static int m_nTryAfter = 0;

            public ExecuteQueryTask(ClientContext ctx)
            {
                m_clientContext = ctx;
                m_notifyEvent = new AutoResetEvent(false);
                m_ex = null;
            }

            //here we will retry when http 429,503 happened
            //please reference to https://docs.microsoft.com/en-us/sharepoint/dev/general-development/how-to-avoid-getting-throttled-or-blocked-in-sharepoint-online
            public void Execute()
            {
                const int nRetryCount = 10;
                const int nDefaultInterval = 120;
                int nAttemptCount = 0;
                
                bool bRetry = false;
                bool bWait = false;
                ClientRequestWrapper clientRequest = null;

                while (nAttemptCount < nRetryCount)
                {
                    nAttemptCount++;

                    try
                    {
                        if (m_nTryAfter > 0)
                        {
                            bWait = true;
                            System.Threading.Thread.Sleep(m_nTryAfter * 1000);
                        }
                        
                        if (bRetry)
                        {
                            // retry the previous request
                            if (clientRequest != null && clientRequest.Value != null)
                            {
                                theLog.Info("ExecuteQueryTask::Executer attempt count:" + nAttemptCount);
                                m_clientContext.RetryQuery(clientRequest.Value);
                            }
                        }
                        else
                        {
                            m_clientContext.ExecuteQuery();
                        }

                        
                    }
                    catch (WebException webEx)
                    {
                        theLog.Error("ExecuteQuery web exception:" + webEx.Message);

                        HttpWebResponse webResponse = webEx.Response as HttpWebResponse;
                        if (webResponse != null)
                        {
                            int nStatusCode = (int)webResponse.StatusCode;
                            if (nStatusCode == 429 || nStatusCode==503)
                            {
                                clientRequest = (ClientRequestWrapper)webEx.Data["ClientRequest"];
                                bRetry = true;


                                m_nTryAfter = nDefaultInterval;
                                string retryAfterHeader = webResponse.Headers.Get("Retry-after");
                                if (!string.IsNullOrEmpty(retryAfterHeader))
                                {
                                    Int32.TryParse(retryAfterHeader, out m_nTryAfter);
                                }
                               
                                theLog.Error("ExecuteQuery web Exception, status:429, Retry-after:" + m_nTryAfter.ToString());

                                m_nTryAfter += 10;
                                continue;
                            }
                        }

                        this.m_ex = webEx;
                        break;
                    }
                    catch (System.Exception ex)
                    {
                        this.m_ex = ex;
                        break;
                    }

                    break;
                }
                
                //when no http 429 status, reset the TryAfter value
                if (bWait)
                {
                    m_nTryAfter = 0;
                }  
                m_notifyEvent.Set();
            }

            public void Wait(int nMilliSec=0)
            {
                TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, nMilliSec);
                if (nMilliSec == 0)
                {
                    waitTime = new TimeSpan(0, 0, 0, 120+30, 0);//when http status is 429, TryAfter value is 120 second
                   
                }

                bool bWaitSucess = m_notifyEvent.WaitOne(waitTime);

                //if timeout, we execute direct
                if (!bWaitSucess)
                {
                    theLog.Info("Wait timeout, execute direct.");
                    Execute();
                }
              
                //if exception, throw
                if (this.m_ex != null)
                {
                    throw m_ex;
                }
            }

            ClientContext m_clientContext;
            AutoResetEvent m_notifyEvent;
            Exception m_ex;
        }

        private static List<ExecuteQueryTask> m_lstExecuteQueryTask = new List<ExecuteQueryTask>();
        private static Object m_taskLock = new Object();
        private static AutoResetEvent m_taskReady = new AutoResetEvent(false);
        private static Thread[] m_ThreadWorker = null;
        private static readonly int m_nWorkThreads = 2;

        private static CLog theLog = CLog.GetLogger("ExecuteQueryWorker");

        public static void AddedWaitExecuteQueryTask(ClientContext ctx)
        {
            ExecuteQueryTask task = AddedExecuteQueryTask(ctx);
            task.Wait();
        }

        public static ExecuteQueryTask AddedExecuteQueryTask(ClientContext ctx)
        {
            //new task
            ExecuteQueryTask task = new ExecuteQueryTask(ctx);
            lock(m_taskLock)
            {
                m_lstExecuteQueryTask.Add(task);
            }
            m_taskReady.Set();

            //start thread
            if (m_ThreadWorker==null)
            {
                m_ThreadWorker = new Thread[m_nWorkThreads];
                for (int i=0; i<m_ThreadWorker.Length; i++)
                {
                    m_ThreadWorker[i] = new Thread(DoExecuteQueryWorker);
                    m_ThreadWorker[i].Start();
                }
            }
            return task;
        }

        public static List<ExecuteQueryTask> GetTask()
        {
            List<ExecuteQueryTask> lst = null;
            lock (m_taskLock)
            {
                if(m_lstExecuteQueryTask.Count>0)
                {
                    int nCount = (m_lstExecuteQueryTask.Count + (m_nWorkThreads - 1)) / m_nWorkThreads;
                    lst = m_lstExecuteQueryTask.GetRange(0, nCount);
                    m_lstExecuteQueryTask.RemoveRange(0, nCount);
                   
                }
              
            }
            return lst;
        }


        public static void DoExecuteQueryWorker()
        {
            while(m_taskReady.WaitOne())
            {
                try
                {
                    List<ExecuteQueryTask>  lstTask = null;
                    while ((lstTask = GetTask()) != null)
                    {
                        foreach(ExecuteQueryTask task in lstTask)
                        {
                            task.Execute();
                        }        
                    }
                }
                catch (System.Exception ex)
                {
                    theLog.Error("Exception on DoExecuteQueryWorker:" + ex.ToString());
                }
               
            }
        }


    }
}