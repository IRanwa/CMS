using FinalProj.Models;
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;

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
            @ViewBag.templateUrl = "/Template/Index";
            //TemplateController temp = new TemplateController();
            //temp.Index();
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
                List<ImageLibrary> images = db.getImages(startIndex, 12, login);
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
                List<ImageLibrary> images = db.getImages(startIndex, 12, login);
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
            DBConnect db = new DBConnect();
            Website web = db.getWebsite((Login)Session["user"]);
            ViewBag.website = web;
            return View();
        }

        public void changeImgLibSettings(Website web)
        {
            DBConnect db = new DBConnect();
            Login login = (Login)Session["user"];
            Website previousWeb = db.getWebsite(login);
            db.updateImgLibSettings(web);
            int imgCount = db.getImageCount();
            List<ImageLibrary> imgList = db.getImages(0, imgCount, login);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Task mainTask = Task.Factory.StartNew((obj) =>
             {
                 Parallel.ForEach(imgList, img =>
                 {
                     string filename = img.imgLoc.Split('/')[3];
                     int index = filename.LastIndexOf(".");
                     string extension = filename.Substring(filename.LastIndexOf("."));
                     string path = img.imgLoc.Substring(0, img.imgLoc.IndexOf(filename));
                     filename = filename.Replace(extension, "");

                     List<Task> tasksList = new List<Task>();

                     if (previousWeb.thumbWidth != web.thumbWidth || previousWeb.thumbHeight != web.thumbHeight) {
                         Task thumbTask = Task.Factory.StartNew(() =>
                            {
                                string newFilePath = path + filename + "_thumb" + extension;
                                if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                                {
                                    System.IO.File.Delete(Server.MapPath(newFilePath));
                                }
                                Image imgPhoto = Image.FromFile(Server.MapPath(img.imgLoc));
                                Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.thumbWidth, web.thumbHeight);
                                image.Save(Path.Combine(Server.MapPath(path) + filename + "_thumb" + extension));
                                image.Dispose();
                                imgPhoto.Dispose();
                            });
                         tasksList.Add(thumbTask);
                     }

                     if (previousWeb.mediumWidth != web.mediumWidth || previousWeb.mediumHeight != web.mediumHeight)
                     {
                         Task mediumTask = Task.Factory.StartNew(() =>
                         {
                             string newFilePath = path + filename + "_medium" + extension;
                             if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                             {
                                 System.IO.File.Delete(Server.MapPath(newFilePath));
                             }
                             Image imgPhoto = Image.FromFile(Server.MapPath(img.imgLoc));
                             Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.mediumWidth, web.mediumHeight);
                             image.Save(Path.Combine(Server.MapPath(path) + filename + "_medium" + extension));
                             image.Dispose();
                             imgPhoto.Dispose();
                         });
                         tasksList.Add(mediumTask);
                     }

                     if (previousWeb.largeWidth != web.largeWidth || previousWeb.largeHeight != web.largeHeight)
                     {
                         Task largeTask = Task.Factory.StartNew(() =>
                         {
                             string newFilePath = path + filename + "_large" + extension;
                             if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                             {
                                 System.IO.File.Delete(Server.MapPath(newFilePath));
                             }
                             Image imgPhoto = Image.FromFile(Server.MapPath(img.imgLoc));
                             Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.largeWidth, web.largeHeight);
                             image.Save(Path.Combine(Server.MapPath(path) + filename + "_large" + extension));
                             image.Dispose();
                             imgPhoto.Dispose();
                         });
                         tasksList.Add(largeTask);
                     }

                     Task.WaitAll(tasksList.ToArray());

                 });
             }, imgList);
            mainTask.Wait();


            //foreach (ImageLibrary img in imgList)
            //{
            //    string filename = img.imgLoc.Split('/')[3];
            //    int index = filename.LastIndexOf(".");
            //    string extension = filename.Substring(filename.LastIndexOf("."));
            //    string path = img.imgLoc.Substring(0, img.imgLoc.IndexOf(filename));
            //    filename = filename.Replace(extension, "");

            //    Image imgPhoto = Image.FromFile(Server.MapPath(img.imgLoc));


            //    string newFilePath = path + filename + "_thumb" + extension;
            //    if (System.IO.File.Exists(Server.MapPath(newFilePath)))
            //    {
            //        System.IO.File.Delete(Server.MapPath(newFilePath));
            //        Bitmap image = ResizeImage(imgPhoto, web.thumbWidth, web.thumbHeight);
            //        image.Save(Path.Combine(Server.MapPath(path) + filename + "_thumb" + extension));
            //        image.Dispose();
            //        //imgPhoto.Dispose();
            //    }

            //    newFilePath = path + filename + "_medium" + extension;
            //    if (System.IO.File.Exists(Server.MapPath(newFilePath)))
            //    {
            //        System.IO.File.Delete(Server.MapPath(newFilePath));
            //        Bitmap image = ResizeImage(imgPhoto, web.mediumWidth, web.mediumHeight);
            //        image.Save(Path.Combine(Server.MapPath(path) + filename + "_medium" + extension));
            //        image.Dispose();
            //        //imgPhoto.Dispose();
            //    }

            //    newFilePath = path + filename + "_large" + extension;
            //    if (System.IO.File.Exists(Server.MapPath(newFilePath)))
            //    {
            //        System.IO.File.Delete(Server.MapPath(newFilePath));
            //        Bitmap image = ResizeImage(imgPhoto, web.largeWidth, web.largeHeight);
            //        image.Save(Path.Combine(Server.MapPath(path) + filename + "_large" + extension));
            //        image.Dispose();

            //    }
            //    imgPhoto.Dispose();
            //}
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.Write(ts);
            Response.Redirect("~/Home/LibrarySettings");
        }

        public ActionResult Export()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        public ActionResult Export(List<string> checkboxes)
        {
            Login login = (Login)Session["user"];
            ExportDetails export = ExportDetails.getInstance(login, Server, checkboxes);
            int totalCount = export.exportStart();
            return Json(new { totalCount = totalCount }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportTaskProgress()
        {
            int count = WebSettings.exportProgress;
            return Json(new
            {
                Progress = WebSettings.exportProgress
            }, JsonRequestBehavior.AllowGet);
        }

        public FileResult downloadExortFile()
        {
            if (System.IO.File.Exists(Server.MapPath("~/Export.zip")))
            {
                System.IO.File.Delete(Server.MapPath("~/Export.zip"));
            }
            ZipFile.CreateFromDirectory(Server.MapPath("~/Export/"), Server.MapPath("~/Export.zip"));
            byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath("~/Export.zip"));
            string fileName = "Export.zip";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult Import()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        public ActionResult Import(HttpPostedFileBase[] files)
        {
            
            ViewBag.Display = "none";
            if (!Directory.Exists(Server.MapPath("~/Import/")))
            {
                Directory.CreateDirectory(Server.MapPath("~/Import/"));
            }
            Login login = Session["user"] as Login;
            ImportDeatils import = ImportDeatils.getInstance(login, files,Server);
            bool status = import.importStart();
            if (!status) {
                return Json(new { status = false, message = "Import Category file is corrupted!" }
                                    , JsonRequestBehavior.AllowGet);
            }
            return View();
        }
    }
}