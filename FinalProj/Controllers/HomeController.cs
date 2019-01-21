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
        //private static readonly object locker = new object();

        [SessionListener]
        public ActionResult Dashboard()
        {
            ViewBag.Display = "none";
            DBConnect db = new DBConnect();
            Login login = (Login)Session["user"];
            ViewBag.noOfImages = db.getImageCount(login);
            ViewBag.noOfPublishPosts = db.getPostsCountByStatus("Publish",login);
            ViewBag.noOfDraftPosts = db.getPostsCountByStatus("Draft",login);
            @ViewBag.templateUrl = "/Template/Index";
            return View();
        }

        [SessionListener]
        public ActionResult Settings()
        {
            ViewBag.Display = "none";
            Login login = (Login)Session["user"];
            new DisplayImageLibrary(login, ViewBag).getTotalCount(NO_OF_IMAGES);
            DBConnect db = new DBConnect();
            ViewBag.website = db.getWebsite(login);
            return View();
        }

        [SessionListener]
        public ActionResult nextImagePage(int nextPage)
        {
            Login login = (Login)Session["user"];
            new DisplayImageLibrary(login, ViewBag).reqNextPage(nextPage, NO_OF_IMAGES);
            DBConnect db = new DBConnect();
            ViewBag.website = db.getWebsite(login);
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
            ViewBag.Display = "block";
            ViewBag.Message = "Website Settings Updated Successfully!";
            return View("Settings");
        }

        [SessionListener]
        public ActionResult LibrarySettings()
        {
            ViewBag.Display = "none";
            DBConnect db = new DBConnect();
            ViewBag.website = db.getWebsite((Login)Session["user"]);
            return View();
        }

        [SessionListener]
        public ActionResult changeImgLibSettings(Website web)
        {
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            Website previousWeb = db.getWebsite(login);
            List<ImageLibrary> imgList = db.getImagesList(login);

            string[] resizes = new string[] { "_thumb", "_medium", "_large" };
            int[,] newResize = new int[,] { { web.thumbWidth, web.thumbHeight }, { web.mediumWidth, web.mediumHeight }, 
                { web.largeWidth, web.largeHeight } };
            int[,] prevResize = new int[,] { { previousWeb.thumbWidth, previousWeb.thumbHeight }, { previousWeb.mediumWidth,
                    previousWeb.mediumHeight },
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
                    Response.Write("<script>alert('" + Thread.CurrentThread.ManagedThreadId + "');</script>");
                    Parallel.For(0, resizes.Length, po, (range) =>
                    {
                        if (prevResize[range, 0] != newResize[range, 0] || prevResize[range, 1] != newResize[range, 1])
                        {
                            string newFilePath = Server.MapPath(path + filename + resizes[range] + extension);
                            FolderHandler.getInstance().deleteFile(newFilePath);
                            new ImageResizer().ResizeImage(Server.MapPath(img.imgLoc), newResize[range, 0], newResize[range, 1], newFilePath);
                        }
                    });
                    
                });
                ViewBag.Message = "Updated Image Library Successfully!";
                db.updateImgLibSettings(web);
                ViewBag.website = web;
            }
            catch (AggregateException ae)
            {
                ViewBag.Message = "Updated Image Library Un-Successful!";
                ViewBag.website = previousWeb;

                Parallel.ForEach(imgList, img =>
                {
                    string filename = img.imgLoc.Split('/').Last();
                    int index = filename.LastIndexOf(".");
                    string extension = filename.Substring(filename.LastIndexOf("."));
                    string path = img.imgLoc.Substring(0, img.imgLoc.IndexOf(filename));
                    filename = filename.Replace(extension, "");
                    Parallel.For(0, resizes.Length, (range) =>
                    {
                        string newFilePath = Server.MapPath(path + filename + resizes[range] + extension);
                        if (!System.IO.File.Exists(newFilePath))
                        {
                            new ImageResizer().ResizeImage(Server.MapPath(img.imgLoc), prevResize[range, 0], prevResize[range, 1], newFilePath);
                        }
                    });
                });

                ae = ae.Flatten();

                foreach (Exception ex in ae.InnerExceptions)
                {
                    Logger.getInstance().setMessage(ex.GetBaseException() + ". Exception throw on Website ID " + web.webID
                        , Server.MapPath("~/Log.txt"));
                }
            }
            ViewBag.Display = "Block";
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
            ExportDetails export = (ExportDetails)Session["ExportProgress"];
            if (export==null)
            {
                export = new ExportDetails();
            }
            else
            {
                export.getWebsettings().exportProgress = 0;
            }
            Session["ExportProgress"] = export;
            int totalCount = export.exportStart(login, checkboxes, Server);
            return Json(new { totalCount = totalCount }, JsonRequestBehavior.AllowGet);

        }
        
        [SessionListener]
        public ActionResult ExportTaskProgress()
        {
            Login login = (Login)Session["user"];
            ExportDetails export = (ExportDetails)Session["ExportProgress"];
            if (export != null)
            {
                return Json(new
                {
                    Progress = export.getWebsettings().exportProgress
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                Progress = -1
            }, JsonRequestBehavior.AllowGet);
        }

        [SessionListener]
        public ActionResult CancelExport()
        {
            Login login = (Login)Session["user"];
            ExportDetails export = (ExportDetails)Session["ExportProgress"];
            if (export != null)
            {
                export.stopExporting();
                Session["ExportProgress"] = null;
                return Json(new
                {
                    message = "Export Cancelled Successfully!"
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                message = "Export Cancellation Un-Successful!"
            }, JsonRequestBehavior.AllowGet);
        }

        [SessionListener]
        public ActionResult downloadExportFile()
        {
            Login login = (Login)Session["user"];
            string webPath = "~/Website_" + login.webID+"/";
            if (Directory.Exists(Server.MapPath(webPath + "Export/")))
            {
                FolderHandler.getInstance().deleteFile(Server.MapPath(webPath+"Export.zip"));
                ZipFile.CreateFromDirectory(Server.MapPath(webPath + "Export/"), Server.MapPath(webPath + "Export.zip"));
                FolderHandler.getInstance().deleteFiles(Server.MapPath(webPath + "Export/"));
                byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath(webPath + "Export.zip"));
                string fileName = "Export.zip";
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            return View("Export");
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
            Login login = (Login)Session["user"];
            ImportDeatils import = (ImportDeatils)Session["ImportProgress"];
            if (import == null)
            {
                import = new ImportDeatils();
            }
            else
            {
                import.getWebsettings().importProgress = 0;
            }
            Session["ImportProgress"] = import;
            string webPath = "~/Website_" + login.webID + "/";
            int totalCount = import.importStart(login, files, Server);
            return Json(new { totalCount = totalCount }, JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult ImportTaskProgress()
        {
            Login login = (Login)Session["user"];
            ImportDeatils import = (ImportDeatils)Session["ImportProgress"];
            if (import != null)
            {
                return Json(new
                {
                    Progress = import.getWebsettings().importProgress
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                Progress = -1
            }, JsonRequestBehavior.AllowGet);
        }
    }
}