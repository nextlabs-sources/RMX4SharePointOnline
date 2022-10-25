using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using RmxForSPOWeb.Common;
using RmxForSPOWeb.Controllers;

namespace RmxForSPOWeb.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        protected static CLog theLog = CLog.GetLogger("AppEventReceiver");
       
        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            theLog.Debug("ProcessEvent enter");
            SPRemoteEventResult result = new SPRemoteEventResult();
            switch (properties.EventType)
            {
                case SPRemoteEventType.AppUninstalling:
                    {
                        if (DoAppUninstallingEvent(properties))
                        {
                            result.Status = SPRemoteEventServiceStatus.Continue;
                        }
                        else
                        {
                            /* result.Status = SPRemoteEventServiceStatus.CancelWithError;
                             result.ErrorMessage = "Sorry,something went wrong when uninstall";*/
                            theLog.Error("Sorry,something went wrong when uninstall");
                        }
                        break;
                    }
                case SPRemoteEventType.AppInstalled:
                        DoAppInstalledEvent(properties);
                    break;
                default:
                    {
                        break;
                    }
            }
            theLog.Debug("ProcessEvent end");
            return result;
        }

        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
        }

        private bool DoAppInstalledEvent(SPRemoteEventProperties properties)
        {
            theLog.Debug("DoAppInstalledEvent enter");
            bool bRet = true;
            try
            {
                using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
                {
                    if (clientContext != null)
                    {
                        clientContext.Load(clientContext.Web, d => d.Url, d => d.Lists);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);

                        //init custom action url
                        GeneralSettingController.InitlizeCustomActionUrl(clientContext);

                        //Added ListEdit item in rootWeb for all lib/list because right-protect menu item exist in rootweb
                        List<List> lists = SPOEUtility.GetListsFromListCollection(clientContext, clientContext.Web.Lists);     
                        SPOEUtility.AddListEditActionToLibraries(clientContext, lists, false, new List<string>());          
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Error("DoAppInstalledEvent error:" + ex.Message + ex.StackTrace);
                bRet = false;
            }
            theLog.Debug("DoAppInstalledEvent result:" + bRet);
            return bRet;

        }


        private bool DoAppUninstallingEvent(SPRemoteEventProperties properties)
        {
            theLog.Debug("DoAppUninstallingEvent enter");
            bool bRet = true;
            try
            {
                using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
                {
                    if (clientContext != null)
                    {
                        clientContext.Load(clientContext.Web, d => d.Url, d => d.Lists, d => d.EventReceivers);
                        ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
                  
                        RemoveList(clientContext);
                        RemoveSubsite(clientContext);
                    }
                }
            }
            catch (Exception ex)
            {
                theLog.Error("DoAppUninstallingEvent error:"+ex.Message+ex.StackTrace);
                bRet = false;
            }
            theLog.Debug("DoAppUninstallingEvent result:" + bRet);
            return bRet;
        }

        private void RemoveList(ClientContext clientContext)
        {
            theLog.Debug("RemoveList enter");
            ListCollection listCollection = clientContext.Web.Lists;
            List<List> lists = new List<List>();
            lists = SPOEUtility.GetListsFromListCollection(clientContext, listCollection);
            foreach (List list in lists)
            {
                // Reset batch mode status
                BatchModeWorker.ResetBatchModeStatus(list.Id.ToString());

                SPOEUtility.RemoveListEditAction(clientContext, list);

                if(SPOEUtility.CheckEvent(clientContext.Web, list))
                {
                    theLog.Debug("remove list title:"+list.Title);
                    SPOEUtility.RemoveEvent(clientContext, list);
                }
            }
        }
        private void RemoveSubsite(ClientContext clientContext)
        {
            theLog.Debug("RemoveSubsite enter");
            WebCollection webcol = clientContext.Web.GetSubwebsForCurrentUser(null);
            clientContext.Load(webcol);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            foreach(var web in webcol)
            {
                RemoveSubsiteNode(clientContext,web);
            }
        }
        private void RemoveSubsiteNode(ClientContext clientContext,Web web)
        {
            clientContext.Load(web, d => d.Url);
            ExecuteQueryWorker.AddedWaitExecuteQueryTask(clientContext);
            Uri site = new Uri(web.Url);
            string apponlyAccessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal,
                       site.Authority, TokenHelper.GetRealmFromTargetUrl(site)).AccessToken;
            using (ClientContext subClientContext = TokenHelper.GetClientContextWithAccessToken(
                web.Url, apponlyAccessToken))
            {
                Web subWeb = subClientContext.Web;
                subClientContext.Load(subWeb, d => d.Title, d => d.Url, d => d.Lists, d => d.EventReceivers);
                ExecuteQueryWorker.AddedWaitExecuteQueryTask(subClientContext);
                theLog.Debug("remove subsite:"+subWeb.Title);
                SPOEUtility.RemoveECBAction(subClientContext, null);
                RemoveList(subClientContext);
                RemoveSubsite(subClientContext);
            }
        }
    }
}
