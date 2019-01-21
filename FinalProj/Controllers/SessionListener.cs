using FinalProj.Models;
using System;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class SessionListener : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext ctx = HttpContext.Current;
            Login login = (Login)ctx.Session["user"];
            // check if session is supported
            string path = ctx.Request.Url.AbsolutePath;
            Console.WriteLine(path);

            if (path.Equals("/User/Index") || path.Equals("/User/Registration") || path.Equals("/"))
            {
                if (login != null)
                {
                    filterContext.Result = new RedirectResult("~/Home/Dashboard");
                    return;
                }
            }
            else
            {
                if (login == null)
                {
                    filterContext.Result = new RedirectResult("~/User/Index");
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}