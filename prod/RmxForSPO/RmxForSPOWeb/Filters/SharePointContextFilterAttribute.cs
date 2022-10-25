using System;
using System.Web.Mvc;
using System.Web;

namespace RmxForSPOWeb
{
    /// <summary>
    /// SharePoint action filter attribute.
    /// </summary>
    public class SharePointContextFilterAttribute : ActionFilterAttribute
    {
        protected static CLog theLog = CLog.GetLogger("SharePointContextFilterAttribute");

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            theLog.Debug(string.Format("Enter OnActionExecuting. Method:{2}, QueryString:{0}, postData:{1}", 
                HttpContext.Current.Request.QueryString.ToString(), 
                HttpContext.Current.Request.Form.ToString(), HttpContext.Current.Request.HttpMethod ));

            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            Uri redirectUrl;
            switch (SharePointContextProvider.CheckRedirectionStatus(filterContext.HttpContext, out redirectUrl))
            {
                case RedirectionStatus.Ok:
                    return;
                case RedirectionStatus.ShouldRedirect:
                    filterContext.Result = new RedirectResult(redirectUrl.AbsoluteUri);
                    break;
                case RedirectionStatus.CanNotRedirect:
                    filterContext.Result = new ViewResult { ViewName = "Error" };
                    break;
            }
        }
    }
}
