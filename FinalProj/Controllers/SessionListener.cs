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
            if (login == null)
            {
                // check if a new session id was generated
                filterContext.Result = new RedirectResult("~/User/Index");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}