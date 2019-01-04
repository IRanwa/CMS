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
            DBConnect db = new DBConnect();
            ViewBag.noOfImages = db.getImageCount();
            ViewBag.noOfPublishPosts = db.getPostsCountByStatus("Publish");
            ViewBag.noOfDraftPosts = db.getPostsCountByStatus("Draft");

            //templateView();
            return View();
        }

        //public void templateView()
        //{
        //    DBConnect db = new DBConnect();
        //    Login login = (Login)Session["user"];
        //    Website web = db.getWebsite(login);
        //    ViewBag.webTitle = web.webTitle;
        //    List<Post> postList = db.getPostsByStatus(login, "Publish");
        //    foreach (Post post in postList)
        //    {
        //        post.postData = System.IO.File.ReadAllText(Server.MapPath(post.postLoc));
        //    }
        //    ViewBag.PostsList = postList;
        //}

        public ActionResult Settings()
        {
            ViewBag.Display = "none";
            getTotalImageCount();
            DBConnect db = new DBConnect();
            Website web = db.getWebsite((Login)Session["user"]);
            ViewBag.website = web;
            return View();
        }

        private void displayImages(ImageLibrary img)
        {
            if (img.currentPage != 0)
            {
                int startIndex = (img.currentPage - 1) * 12;
                DBConnect db = new DBConnect();
                List<ImageLibrary> images = db.getImages(startIndex, 12);
                ViewBag.DisplayImages = images;
                ViewBag.LibraryProp = img;
            }
        }

        private void getTotalImageCount()
        {
            DBConnect db = new DBConnect();
            int count = db.getImageCount();

            ImageLibrary img = new ImageLibrary();
            img.totalImageCount = count;
            img.noOfPages = Convert.ToInt32(Math.Ceiling(count / 12.0));
            if (count > 0)
            {
                img.currentPage = 1;
            }
            displayImages(img);
        }

        public ActionResult nextImagePage(int nextPage)
        {

            DBConnect db = new DBConnect();
            int count = db.getImageCount();
            ImageLibrary img = new ImageLibrary();
            img.totalImageCount = count;
            img.noOfPages = Convert.ToInt32(Math.Ceiling(count / 12.0));
            if (nextPage > img.noOfPages)
            {
                nextPage = img.noOfPages;
            }

            img.currentPage = nextPage;


            if (nextPage != 0)
            {
                int startIndex = (nextPage - 1) * 12;
                List<ImageLibrary> images = db.getImages(startIndex, 12);
                ViewBag.DisplayImages = images;
                ViewBag.LibraryProp = img;
            }
            ViewBag.Display = "none";
            ViewBag.popup = "block";
            return View("Settings");
        }

        public ActionResult changeSettings(Website website)
        {
            DBConnect db = new DBConnect();
            db.updateWebsite(website);
            Settings();
            return View("Settings");
        }

        public ActionResult LibrarySettings()
        {
            ViewBag.Display = "none";
            return View();
        }
    }
}