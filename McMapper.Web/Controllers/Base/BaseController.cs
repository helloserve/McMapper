using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace McMapper.Web.Controllers.Base
{
    public class BaseController : Controller
    {
        protected override IAsyncResult BeginExecute(System.Web.Routing.RequestContext requestContext, AsyncCallback callback, object state)
        {
            bool isSub = (requestContext.HttpContext.Request.Url.Segments.Length > 1) && (requestContext.HttpContext.Request.Url.Segments[1].ToLower().Contains("mcmapper"));
            ViewBag.BaseUrl = string.Format("{0}://{1}{2}{3}{4}", requestContext.HttpContext.Request.Url.Scheme, requestContext.HttpContext.Request.Url.Host, (requestContext.HttpContext.Request.Url.IsDefaultPort) ? "" : string.Format(":{0}", requestContext.HttpContext.Request.Url.Port), requestContext.HttpContext.Request.Url.Segments[0], (isSub) ? requestContext.HttpContext.Request.Url.Segments[1] : "");

            return base.BeginExecute(requestContext, callback, state);
        }
    }
}