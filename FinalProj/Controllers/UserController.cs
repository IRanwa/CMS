using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class UserController : Controller
    {
        [SessionListener]
        public ActionResult Index()
        {
            ViewBag.Display = "none";
            return View();
        }

        [SessionListener]
        public ActionResult Registration()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        public ActionResult Index(Login login)
        {
            DBConnect db = new DBConnect();
            login = db.checkUserReg(login);
            if (login.role != null)
            {
                Session.Add("user", login);
                Session.Timeout = 1;
                ModelState.Clear();
                Response.Redirect("~/Home/Dashboard",false);
            }
            else
            {
                ViewBag.Display = "block";
                ViewBag.Message = "User not registered!";
            }

            ModelState.Clear();
            return View();
        }

        [HttpPost]
        [SessionListener]
        public ActionResult Registration(Registration reg)
        {
            string pass = reg.user.pass;
            string cPass = reg.user.cPass;
            if (pass.Equals(cPass))
            {
                DBConnect db = new DBConnect();
                Login login = db.checkUserReg(reg.user);
                if (login.role == null)
                {
                    reg.user.role = "Administrator";
                    login = db.registerWebsite(reg);

                    Category category = new Category();
                    category.webID = login.webID;
                    category.title = "Un-Category";
                    category.desc = "Un-Category";

                    db.addCategory(category);

                    Session.Add("user", login);
                    Session.Timeout = 20;
                    ViewBag.Display = "none";
                    ModelState.Clear();
                    Response.Redirect("~/Home/Dashboard",false);
                }
                else
                {
                    ViewBag.Display = "block";
                    ViewBag.Message = "User already registered!";
                }
            }
            else
            {
                ViewBag.Display = "block";
                ViewBag.Message = "Password not matched!";
            }
            ModelState.Clear();
            return View();
        }

        [SessionListener]
        public ActionResult LogOut()
        {
            ViewBag.Display = "none";
            Session["user"] = null;
            return View("Index");
        }
    }

    
}