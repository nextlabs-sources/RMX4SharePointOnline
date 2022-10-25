using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using RmxForSPOWeb.Common;

namespace RmxForSPOWeb.Services
{
    public class ListEventHandler : IRemoteEventService
    {
        protected static CLog theLog = CLog.GetLogger("ListEventHandler");

        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();
            return result;
        }
       
        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            string strLog = "";
            try{
               strLog = string.Format("EventType:{0}, url:{1}", properties.EventType.ToString(), properties.ItemEventProperties.AfterUrl);
            }
            catch (System.Exception) { } 
            theLog.Info("ProcessOneWayEvent enter," + strLog);

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdded:
                    {
                        ProcessEventForRmx(properties);
                        break;
                    }
                case SPRemoteEventType.ItemUpdated:
                    {
                        ProcessEventForRmx(properties);
                        break;
                    }
                case SPRemoteEventType.ItemAttachmentAdded:
                    {
                        ProcessEventForRmx(properties);
                        break;
                    }
                case SPRemoteEventType.ItemFileMoved:
                    {   //we need this event when use move file between folders within the same library.
                        ProcessEventForRmx(properties);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            theLog.Debug("ProcessOneWayEvent end");
        }
        private void ProcessEventForRmx(SPRemoteEventProperties properties)
        {
            theLog.Debug("ProcessEventForRmx enter");
            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    try
                    {
                        if (properties.ItemEventProperties.AfterProperties.ContainsKey("vti_filesize"))
                        {
                            var bvti_filesize = properties.ItemEventProperties.AfterProperties["vti_filesize"];
                            theLog.Debug("AfterProperties[\"vti_filesize\"]" + bvti_filesize);
                            if (bvti_filesize != null && bvti_filesize.ToString() == "0")
                            {
                                theLog.Info("return for AfterProperties[\"vti_filesize\"] is 0");
                               //.In New UI ItemAdded event, this value is 0, it will trigger ItemUpdate event then, we will process in that event.
                                return;
                            }
                        }
                        SPOEUtility.DoRMXEnforcer(properties,clientContext);
                    }
                    catch (Exception ex)
                    {
                        theLog.Error("get some error in rmx event: " + ex.Message+ex.StackTrace);
                    }
                }
            }
            theLog.Debug("ProcessEventForRmx end");
        }
    }
}
