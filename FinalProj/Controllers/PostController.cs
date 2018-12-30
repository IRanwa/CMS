using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class PostController : Controller
    {
        public ActionResult Posts()
        {
            ViewBag.Display = "none";
            return View();
        }

        public ActionResult PostsAddNew()
        {
            ViewBag.Display = "none";
            getTotalImageCount();
            return View();
        }

        private void displayImages(ImageLibrary img)
        {
            if (img.currentPage != 0)
            {
                int startIndex = (img.currentPage - 1) * 12;
                DBConnect db = new DBConnect();
                List<ImageLibrary> images = db.getImages(startIndex,12);
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

        public ActionResult nextPage(int nextPage)
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
            return View("PostsAddNew");
        }

        public ActionResult uploadPost(string status, string content, string title)
        {
            DateTime date = DateTime.Now;
            string serverPath = "~/Posts/" + date.ToString("yyyy-MM-dd") + "/"+status+"/";
            string path = Server.MapPath(serverPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

            }

            path = serverPath + title + ".txt";
            bool exists = false;
            int count = 0;
            do
            {
                if (count==0)
                {
                    path = serverPath + title + ".txt";
                }
                else
                {
                    path = serverPath + title + '_' + count + ".txt";
                }
                exists = System.IO.File.Exists(Server.MapPath(path));
                count++;
            } while (exists);

            System.IO.File.WriteAllText(Server.MapPath(path), content);
            DBConnect db = new DBConnect();
            Website web = db.getWebsite((Login)Session["user"]);
            db.uploadPost(new Post(1, web.webID,title, path,status, date, date));
            return Json(new { result = "Success" }, JsonRequestBehavior.AllowGet);
        }
    }
}