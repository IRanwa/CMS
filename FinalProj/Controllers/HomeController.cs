﻿using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading;

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
            return View();
        }

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
                Login login = (Login)Session["user"];
                List<ImageLibrary> images = db.getImages(startIndex, 12,login);
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
                Login login = (Login)Session["user"];
                List<ImageLibrary> images = db.getImages(startIndex, 12,login);
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

        public ActionResult Export()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        public ActionResult Export(List<string> checkboxes)
        {
            Console.Write(checkboxes.Count);
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            List<Task> taskList = new List<Task>();
            Task<int> mainTask = Task.Factory.StartNew(() =>
             {
                 int count = 0;
                 foreach (string chkbox in checkboxes)
                 {
                     int tempCount = 0;
                     switch (chkbox)
                     {
                         case "posts":
                             tempCount = db.getPostsCount();
                             taskList.Add(Task.Factory.StartNew(() => exportPosts(tempCount, login)));
                             break;
                         case "images":
                             tempCount = db.getImageCount();
                             taskList.Add(Task.Factory.StartNew(() => exportImages(tempCount, login)));
                             break;
                         case "website":
                             tempCount = 1;
                             taskList.Add(Task.Factory.StartNew(() => exportWebsite(login)));
                             break;
                         case "categories":
                             tempCount = db.getCategoryCount();
                             taskList.Add(Task.Factory.StartNew(() => exportCategories(tempCount, login)));
                             break;
                     }
                     count += tempCount;
                 }
                 return count;
             });
            
            int total = mainTask.Result;
            HttpContext.Application["export"] = total;
            
            do
            {
                int index = Task.WaitAny(taskList.ToArray());
                taskList.RemoveAt(index);
            } while (taskList.Count != 0);
            //exportZipFile();

            if (System.IO.File.Exists(Server.MapPath("~/Export.zip")))
            {
                System.IO.File.Delete(Server.MapPath("~/Export.zip"));
            }
            ZipFile.CreateFromDirectory(Server.MapPath("~/Export/"), Server.MapPath("~/Export.zip"));
            byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath("~/Export.zip"));
            string fileName = "Export.zip";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public void exportPosts(int count,Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Posts Table");
            dt.Columns.Add("Post Title");
            dt.Columns.Add("Post Category");
            dt.Columns.Add("Post Content");
            dt.Columns.Add("Post Status");
            dt.Columns.Add("Post Created Date");
            dt.Columns.Add("Post Modify Date");

            DBConnect db = new DBConnect();
            List<Post> postsList = db.getPostList(0, count, login);
            List<string> catList = new List<string>();
            Parallel.ForEach(postsList, post =>
            {
                string catTitle = db.getCategoryByPost(post, login).title;
                string postContent = System.IO.File.ReadAllText(Server.MapPath(post.postLoc));
                dt.Rows.Add(post.postTitle, catTitle, postContent, post.postStatus, post.createdDate, post.modifyDate);
            });
            //foreach (Post post in postsList)
            //{
            //    string catTitle = db.getCategoryByPost(post, login).title;
            //    string postContent = System.IO.File.ReadAllText(Server.MapPath(post.postLoc));
            //    dt.Rows.Add(post.postTitle, catTitle, postContent, post.postStatus, post.createdDate, post.modifyDate);
            //    HttpContext.Application["export"] = Int32.Parse(HttpContext.Application["export"].ToString()) - 1;
            //}

            StringBuilder builder = new StringBuilder();
            List<string> columnNames = new List<string>();
            List<string> rows = new List<string>();

            foreach (DataColumn column in dt.Columns)
            {
                columnNames.Add(column.ColumnName);
            }

            builder.Append(string.Join(",", columnNames.ToArray())).Append("\n");

            foreach (DataRow row in dt.Rows)
            {
                List<string> currentRow = new List<string>();

                foreach (DataColumn column in dt.Columns)
                {
                    object item = row[column];

                    currentRow.Add(item.ToString());
                }

                rows.Add(string.Join(",", currentRow.ToArray()));
            }

            builder.Append(string.Join("\n", rows.ToArray()));
            
            System.IO.File.WriteAllText(Server.MapPath("~/Export/ExportPosts.csv"), builder.ToString());
        }

        public void exportImages(int count, Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Images Table");
            dt.Columns.Add("Image Title");
            dt.Columns.Add("Image Description");
            dt.Columns.Add("Image Path");
            dt.Columns.Add("Image Upload Date");
            dt.Columns.Add("Image Modify Date");

            DBConnect db = new DBConnect();
            List<ImageLibrary> imageslist = db.getImages(0, count, login);
            Parallel.ForEach(imageslist, Img =>
            {
                dt.Rows.Add(Img.title, Img.imgDesc, Img.imgLoc, Img.uploadDate, Img.modifyDate);
            });
            //foreach (ImageLibrary Img in imageslist)
            //{
            //    dt.Rows.Add(Img.title, Img.imgDesc, Img.imgLoc, Img.uploadDate, Img.modifyDate);
            //    HttpContext.Application["export"] = Int32.Parse(HttpContext.Application["export"].ToString()) - 1;
            //}

            StringBuilder builder = new StringBuilder();
            List<string> columnNames = new List<string>();
            List<string> rows = new List<string>();

            foreach (DataColumn column in dt.Columns)
            {
                columnNames.Add(column.ColumnName);
            }

            builder.Append(string.Join(",", columnNames.ToArray())).Append("\n");

            foreach (DataRow row in dt.Rows)
            {
                List<string> currentRow = new List<string>();

                foreach (DataColumn column in dt.Columns)
                {
                    object item = row[column];

                    currentRow.Add(item.ToString());
                }

                rows.Add(string.Join(",", currentRow.ToArray()));
            }

            builder.Append(string.Join("\n", rows.ToArray()));

            System.IO.File.WriteAllText(Server.MapPath("~/Export/ExportImages.csv"), builder.ToString());
            if (System.IO.File.Exists(Server.MapPath("~/Export/ImagesLibrary.zip")))
            {
                System.IO.File.Delete(Server.MapPath("~/Export/ImagesLibrary.zip"));
            }
            ZipFile.CreateFromDirectory(Server.MapPath("~/Images"), Server.MapPath("~/Export/ImagesLibrary.zip"));
        }

        public void exportCategories(int count, Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Categories Table");
            dt.Columns.Add("Category Title");
            dt.Columns.Add("Category Description");

            DBConnect db = new DBConnect();
            List<Category> catList = db.getCatList(0, count, login);
            Parallel.ForEach(catList, cat=>{
                dt.Rows.Add(cat.title, cat.desc);
            });
            //foreach (Category cat in catList)
            //{
            //    dt.Rows.Add(cat.title, cat.desc);
            //    //HttpContext.Application["export"] = Int32.Parse(HttpContext.Application["export"].ToString()) - 1;
            //}

            StringBuilder builder = new StringBuilder();
            List<string> columnNames = new List<string>();
            List<string> rows = new List<string>();

            foreach (DataColumn column in dt.Columns)
            {
                columnNames.Add(column.ColumnName);
            }

            builder.Append(string.Join(",", columnNames.ToArray())).Append("\n");

            foreach (DataRow row in dt.Rows)
            {
                List<string> currentRow = new List<string>();

                foreach (DataColumn column in dt.Columns)
                {
                    object item = row[column];

                    currentRow.Add(item.ToString());
                }

                rows.Add(string.Join(",", currentRow.ToArray()));
            }

            builder.Append(string.Join("\n", rows.ToArray()));

            System.IO.File.WriteAllText(Server.MapPath("~/Export/ExportCategories.csv"), builder.ToString());
        }

        public void exportWebsite( Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Website Table");
            dt.Columns.Add("Website Title");
            dt.Columns.Add("No of Posts");
            dt.Columns.Add("Thumbnail Image Width");
            dt.Columns.Add("Thumbnail Image Height");
            dt.Columns.Add("Medium Image Width");
            dt.Columns.Add("Medium Image Height");
            dt.Columns.Add("Large Image Width");
            dt.Columns.Add("Large Image Height");

            DBConnect db = new DBConnect();
            Website web = db.getWebsite(login);
            dt.Rows.Add(web.webTitle, web.noOfPosts, web.thumbWidth, web.thumbHeight, web.mediumWidth
                , web.mediumHeight,web.largeWidth, web.largeHeight);
            HttpContext.Application["export"] = Int32.Parse(HttpContext.Application["export"].ToString()) - 1;
            StringBuilder builder = new StringBuilder();
            List<string> columnNames = new List<string>();
            List<string> rows = new List<string>();

            foreach (DataColumn column in dt.Columns)
            {
                columnNames.Add(column.ColumnName);
            }

            builder.Append(string.Join(",", columnNames.ToArray())).Append("\n");

            foreach (DataRow row in dt.Rows)
            {
                List<string> currentRow = new List<string>();

                foreach (DataColumn column in dt.Columns)
                {
                    object item = row[column];

                    currentRow.Add(item.ToString());
                }

                rows.Add(string.Join(",", currentRow.ToArray()));
            }

            builder.Append(string.Join("\n", rows.ToArray()));

            System.IO.File.WriteAllText(Server.MapPath("~/Export/ExportWebsite.csv"), builder.ToString());
        }

        public ActionResult ExportTaskProgress()
        {
            return Json(new
            {
                Progress = HttpContext.Application["export"]
            }, JsonRequestBehavior.AllowGet);
        }
    }
}