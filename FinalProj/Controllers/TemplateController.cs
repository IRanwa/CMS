using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class TemplateController : Controller
    {
        [SessionListener]
        public ActionResult Index()
        {
            DBConnect db = new DBConnect();
            Login login = Session["user"] as Login;
            Website web = db.getWebsite(login);
            openTemplate(web,login,0,web.noOfPosts);
            return View();
        }

        [SessionListener]
        private void openTemplate(Website web, Login login, int startIndex, int endIndex)
        {
            DBConnect db = new DBConnect();
            ViewBag.webTitle = web.webTitle;
            if (System.IO.File.Exists(Server.MapPath(web.featuredImage))) {
                ViewBag.BackgroundImage = web.featuredImage;
            }
            else
            {
                ViewBag.BackgroundImage = "~/background_image.jpg";
            }
            List<Post> postList = db.getPostsByStatus(login, "Publish", startIndex, endIndex);
            foreach (Post post in postList)
            {
                string path = Server.MapPath(post.postLoc);
                if (System.IO.File.Exists(path)) {
                    post.postData = System.IO.File.ReadAllText(path);
                }
            }
            ViewBag.PostsList = postList;
            List<Category> catList = db.getCatList(0, db.getCategoryCount(login), login);
            catList.RemoveAt(0);
            catList.Insert(0, new Category(0, "All Categories", "All Categories"));
            ViewBag.catList = catList;
        }

        [SessionListener]
        public ActionResult RedirectPage()
        {
            DBConnect db = new DBConnect();
            Uri url = HttpContext.Request.Url;
            string path = url.Query;
            string query = Request.QueryString["Website"].Replace("?Website=", "");
            if (query!=null || !query.Equals("")) {
                Website web = db.getWebsiteByTitle(query);
                if (web.webID != 0) {
                    Login login = new Login();
                    login.webID = web.webID;
                    openTemplate(web, login,0,web.noOfPosts);
                    return View("Index");
                }
            }
            Response.StatusCode = 404;
            return View("Error");
        }

        [SessionListener]
        public ActionResult Error()
        {
            return View();
        }
    }
}