using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Dashboard()
        {
            ViewBag.Display = "none";
            return View();
        }

        public ActionResult Settings()
        {
            ViewBag.Display = "none";
            return View();
        }

        public ActionResult LibrarySettings()
        {
            ViewBag.Display = "none";
            return View();
        }
    }
}