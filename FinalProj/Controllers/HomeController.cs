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
        private const int NO_OF_IMAGES = 12;

        [SessionListener]
        public ActionResult Dashboard()
        {
            ViewBag.Display = "none";
            DBConnect db = new DBConnect();
            Login login = (Login)Session["user"];
            ViewBag.noOfImages = db.getImageCount(login);
            ViewBag.noOfPublishPosts = db.getPostsCountByStatus("Publish");
            ViewBag.noOfDraftPosts = db.getPostsCountByStatus("Draft");
            @ViewBag.templateUrl = "/Template/Index";
            return View();
        }

        [SessionListener]
        public ActionResult Settings()
        {
            ViewBag.Display = "none";
            new DisplayImageLibrary((Login)Session["user"], ViewBag).getTotalCount(NO_OF_IMAGES);
            DBConnect db = new DBConnect();
            Website web = db.getWebsite((Login)Session["user"]);
            ViewBag.website = web;
            return View();
        }

        //private void displayImages(ImageLibrary img)
        //{
        //    if (img.currentPage != 0)
        //    {
        //        int startIndex = (img.currentPage - 1) * 12;
        //        DBConnect db = new DBConnect();
        //        Login login = (Login)Session["user"];
        //        List<ImageLibrary> images = db.getImages(startIndex, 12, login);
        //        ViewBag.DisplayImages = images;
        //        ViewBag.LibraryProp = img;
        //    }
        //}

        //private void getTotalImageCount()
        //{
        //    DBConnect db = new DBConnect();
        //    int count = db.getImageCount();

        //    ImageLibrary img = new ImageLibrary();
        //    img.totalImageCount = count;
        //    img.noOfPages = Convert.ToInt32(Math.Ceiling(count / 12.0));
        //    if (count > 0)
        //    {
        //        img.currentPage = 1;
        //    }
        //    displayImages(img);
        //}

        [SessionListener]
        public ActionResult nextImagePage(int nextPage)
        {
            Login login = (Login)Session["user"];
            new DisplayImageLibrary(login, ViewBag).reqNextPage(nextPage, NO_OF_IMAGES);
            //DBConnect db = new DBConnect();
            //int count = db.getImageCount();
            //ImageLibrary img = new ImageLibrary();
            //img.totalImageCount = count;
            //img.noOfPages = Convert.ToInt32(Math.Ceiling(count / 12.0));
            //if (nextPage > img.noOfPages)
            //{
            //    nextPage = img.noOfPages;
            //}

            //img.currentPage = nextPage;


            //if (nextPage != 0)
            //{
            //    int startIndex = (nextPage - 1) * 12;
            //    Login login = (Login)Session["user"];
            //    List<ImageLibrary> images = db.getImages(startIndex, 12, login);
            //    ViewBag.DisplayImages = images;
            //    ViewBag.LibraryProp = img;
            //}
            DBConnect db = new DBConnect();
            Website web = db.getWebsite(login);
            ViewBag.website = web;
            ViewBag.Display = "none";
            ViewBag.popup = "block";
            return View("Settings");
        }

        [SessionListener]
        public ActionResult changeSettings(Website website)
        {
            DBConnect db = new DBConnect();
            db.updateWebsite(website);
            Settings();
            return View("Settings");
        }

        [SessionListener]
        public ActionResult LibrarySettings()
        {
            ViewBag.Display = "none";
            DBConnect db = new DBConnect();
            Website web = db.getWebsite((Login)Session["user"]);
            ViewBag.website = web;
            return View();
        }

        [SessionListener]
        public ActionResult changeImgLibSettings(Website web)
        {
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            Website previousWeb = db.getWebsite(login);
            int imgCount = db.getImageCount(login);
            List<ImageLibrary> imgList = db.getImages(0, imgCount, login);

            string[] resizes = new string[] { "_thumb", "_medium", "_large" };
            int[,] newResize = new int[,] { { web.thumbWidth, web.thumbHeight }, { web.mediumWidth, web.mediumHeight }, { web.largeWidth, web.largeHeight } };
            int[,] prevResize = new int[,] { { previousWeb.thumbWidth, previousWeb.thumbHeight }, { previousWeb.mediumWidth, previousWeb.mediumHeight },
                { previousWeb.largeWidth, previousWeb.largeHeight } };

            CancellationTokenSource cts = new CancellationTokenSource();
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;

            try
            {
                Parallel.ForEach(imgList, po, img =>
                {
                    string filename = img.imgLoc.Split('/').Last();
                    int index = filename.LastIndexOf(".");
                    string extension = filename.Substring(filename.LastIndexOf("."));
                    string path = img.imgLoc.Substring(0, img.imgLoc.IndexOf(filename));
                    filename = filename.Replace(extension, "");

                    Parallel.For(0, resizes.Length, po, (range) =>
                    {
                        po.CancellationToken.ThrowIfCancellationRequested();
                        if (prevResize[range, 0] != newResize[range, 0] || prevResize[range, 1] != newResize[range, 1])
                        {
                            string newFilePath = Server.MapPath(path + filename + resizes[range] + extension);
                            if (System.IO.File.Exists(newFilePath))
                            {
                                System.IO.File.Delete(newFilePath);
                            }
                            new ImageResizer().ResizeImage(Server.MapPath(img.imgLoc), newResize[range, 0], newResize[range, 1], newFilePath);
                        }
                    });
                });
                ViewBag.Display = "Block";
                ViewBag.Message = "Updated Image Library Successfully!";
                db.updateImgLibSettings(web);
                ViewBag.website = web;
            }
            catch (AggregateException ae)
            {
                cts.Cancel();
                ViewBag.Display = "Block";
                ViewBag.Message = "Updated Image Library Un-Successful!";
                ViewBag.website = previousWeb;
            }
            
            //Response.Redirect("~/Home/LibrarySettings",false);
            return View("LibrarySettings");
        }

        [SessionListener]
        public ActionResult Export()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        [SessionListener]
        public ActionResult Export(List<string> checkboxes)
        {
            Login login = (Login)Session["user"];
            ExportDetails export = ExportDetails.getInstance(login, Server);
            int totalCount = export.exportStart(login,checkboxes);
            return Json(new { totalCount = totalCount }, JsonRequestBehavior.AllowGet);
        }

        [SessionListener]
        public ActionResult ExportTaskProgress()
        {
            int count = WebSettings.exportProgress;
            return Json(new
            {
                Progress = WebSettings.exportProgress
            }, JsonRequestBehavior.AllowGet);
        }

        [SessionListener]
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

        [SessionListener]
        public ActionResult Import()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        [SessionListener]
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