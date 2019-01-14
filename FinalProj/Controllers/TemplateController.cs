using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class TemplateController : Controller
    {
        // GET: Template
        public ActionResult Index()
        {
            DBConnect db = new DBConnect();
            Login login = Session["user"] as Login;
            Website web = db.getWebsite(login);
            openTemplate(web,login);
            return View();
        }

        private void openTemplate(Website web, Login login)
        {
            DBConnect db = new DBConnect();
            ViewBag.webTitle = web.webTitle;
            ViewBag.BackgroundImage = web.featuredImage;
            List<Post> postList = db.getPostsByStatus(login, "Publish");
            foreach (Post post in postList)
            {
                post.postData = System.IO.File.ReadAllText(Server.MapPath(post.postLoc));
            }
            ViewBag.PostsList = postList;
            List<Category> catList = db.getCatList(0, db.getCategoryCount(login), login);
            catList.RemoveAt(0);
            catList.Insert(0, new Category(0, "All Categories", "All Categories"));
            ViewBag.catList = catList;
        }

        public void templateView(HttpSessionStateBase session)
        {
            DBConnect db = new DBConnect();
            Login login = (Login)session["user"];
            Website web = db.getWebsite(login);
            ViewBag.Title = web.webTitle;
            List<Post> postList = db.getPostsByStatus(login, "Publish");
            foreach (Post post in postList)
            {
                post.postData = System.IO.File.ReadAllText(Server.MapPath(post.postLoc));
            }
            ViewBag.PostsList = postList;
        }

        public ActionResult RedirectPage()
        {
            DBConnect db = new DBConnect();
            string aspxError = Request.QueryString["aspxerrorpath"];
            if (aspxError != null && !aspxError.Equals(""))
            {
                string query = aspxError.ToString().Replace("/","");
                Website web = db.getWebsiteByTitle(query);
                if (web.webID!=0) {
                    Login login = new Login();
                    login.webID = web.webID;
                    openTemplate(web,login );
                    Response.StatusCode = 200;
                    return View("Index");
                }
            }
            Response.StatusCode = 404;  //you may want to set this to 200
            return View("NotFound");
        }
    }
}