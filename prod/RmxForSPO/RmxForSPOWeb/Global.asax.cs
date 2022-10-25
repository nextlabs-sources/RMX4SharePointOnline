using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using RmxForSPOWeb.Common;

namespace RmxForSPOWeb
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //set server
            RMXUtility.SetHttpServerUtility(this.Server);

            //capture output debug string information
            OutputDebugStringCapture.Instance().Init();

            //read config file
            RMXConfig.Instance().ReadConfigFromFile();

        }
    }
}
