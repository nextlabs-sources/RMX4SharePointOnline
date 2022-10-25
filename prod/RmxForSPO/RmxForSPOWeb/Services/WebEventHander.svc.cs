using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;

namespace RmxForSPOWeb.Services
{
    public class WebEventHander : IRemoteEventService
    {
        protected static CLog theLog = CLog.GetLogger("WebEventHander");

        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            theLog.Debug("ProcessEvent enter");
            SPRemoteEventResult result = new SPRemoteEventResult();

            theLog.Debug("ProcessEvent end");
            return result;
        }

        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            theLog.Debug("ProcessOneWayEvent enter");
            theLog.Debug("ProcessOneWayEvent end");
        }
    }
}
