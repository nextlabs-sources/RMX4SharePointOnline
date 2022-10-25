using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using log4net;
using System.Web;
using RmxForSPOWeb.Common;

namespace RmxForSPOWeb
{
    public class CLog
    {
        protected static List<KeyValuePair<log4net.ILog,CLog>> m_lstLogger = new List<KeyValuePair<ILog,CLog>>();
        private static Object logLocker = new Object();

        protected log4net.ILog m_log = null;

        static CLog()
        {
            //set pid
            log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;

            //set log path
            string strLogPath = RMXUtility.GetRMXAppDataFolder() + @"Log\";
            log4net.GlobalContext.Properties["LogPath"] = strLogPath;

            //read config file
            string strCfgFile = RMXUtility.GetRMXAppFolder() + "logconfig.xml";
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(strCfgFile));
        }


        /*construct, all is Protected, CLog can't be create directory, and must be get by GetLogger.*/
        protected CLog() { }
        protected CLog(CLog log) { }
        protected CLog(log4net.ILog log)
        {
            m_log = log;
        }

       
        public static CLog GetLogger(string strName)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(strName);
            
            return GetWrapperLog(log);
        }

         public static CLog GetLogger(Type typeName)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(typeName);
            
            return GetWrapperLog(log);
        }

         /* Test if a level is enabled for logging */
         public bool IsDebugEnabled { get { return m_log.IsDebugEnabled; } }
         public bool IsInfoEnabled  { get { return m_log.IsInfoEnabled; } }
         public bool IsWarnEnabled  { get { return m_log.IsWarnEnabled; } }
         public bool IsErrorEnabled { get { return m_log.IsErrorEnabled; } }
         public bool IsFatalEnabled { get { return m_log.IsFatalEnabled; } }

         /* Log a message object */
         public void Debug(object message) { m_log.Debug(message); }
         public void Info(object message)  { m_log.Info(message); }
         public void Warn(object message)  { m_log.Warn(message); }
         public void Error(object message) { m_log.Error(message); }
         public void Fatal(object message) { m_log.Fatal(message); }

         /* Log a message object and exception */
         public void Debug(object message, Exception t) { m_log.Debug(message, t); }
         public void Info(object message, Exception t)  { m_log.Info(message, t); }
         public void Warn(object message, Exception t)  { m_log.Warn(message, t); }
         public void Error(object message, Exception t) { m_log.Error(message, t); }
         public void Fatal(object message, Exception t) { m_log.Fatal(message, t); }


        protected static CLog GetWrapperLog(log4net.ILog log)
        {
            CLog existWrapperLog = FindExistWrapperLog(log);

            if(null==existWrapperLog)
            {
                return CreateWrapperLog(log);
            }
            else
            {
                return existWrapperLog;
            }
        }

        protected static CLog FindExistWrapperLog(log4net.ILog log)
        {
            CLog wrapperLog = null;

            lock(logLocker)
            {
                foreach (KeyValuePair<log4net.ILog, CLog> wrapperLogInfo in m_lstLogger)
                {
                    if (wrapperLogInfo.Key.Equals(log))
                    {
                        wrapperLog = wrapperLogInfo.Value;
                        break;
                    }
                }
            }

            return wrapperLog;
        }

        protected static CLog CreateWrapperLog(log4net.ILog log)
        {
            CLog wrapperLog = new CLog(log);

            lock(logLocker)
            {
                m_lstLogger.Add(new KeyValuePair<ILog, CLog>(log, wrapperLog));
            }

            return wrapperLog;
        }

    }
}
