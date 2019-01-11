﻿using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Index()
        {
            ViewBag.Display = "none";
            return View();
        }

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
                //Session["user"] = login;
                //ViewBag.Display = "none";
                Session.Add("user", login);
                Response.Redirect("~/Home/Dashboard",false);
                //return View("../Home/Dashboard");
            }
            else
            {
                ViewBag.Display = "block";
                ViewBag.Message = "User not registered!";
            }


            return View();
        }

        [HttpPost]
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
                    ViewBag.Display = "none";
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
            return View();
        }

        public ActionResult LogOut()
        {
            ViewBag.Display = "none";
            Session["user"] = null;
            return View("Index");
        }
    }

    
}