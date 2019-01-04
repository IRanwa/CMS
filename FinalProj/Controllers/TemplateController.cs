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
            Login login = (Login)Session["user"];
            Website web = db.getWebsite(login);
            ViewBag.webTitle = web.webTitle;
            ViewBag.BackgroundImage = web.featuredImage;
            List<Post> postList = db.getPostsByStatus(login, "Publish");
            foreach (Post post in postList)
            {
                post.postData = System.IO.File.ReadAllText(Server.MapPath(post.postLoc));
            }
            ViewBag.PostsList = postList;
            List<Category> catList = db.getCatList(0, db.getCategoryCount(), login);
            catList.RemoveAt(0);
            catList.Insert(0, new Category(0,"All Categories","All Categories"));
            ViewBag.catList = catList;
            return View();
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
    }
}