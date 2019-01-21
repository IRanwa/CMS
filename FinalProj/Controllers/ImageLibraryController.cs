using FinalProj.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class ImageLibraryController : Controller
    {
        private const int NO_OF_IMAGES = 50;

        [SessionListener]
        public ActionResult ImageLibrary()
        {
            ViewBag.Display = "none";
            ViewBag.layoutView = "Grid";
            new DisplayImageLibrary((Login)Session["user"], ViewBag).getTotalCount(NO_OF_IMAGES);
            return View();
        }

        [SessionListener]
        public ActionResult changeLayout(string layout)
        {
            ViewBag.Display = "none";
            ViewBag.layoutView = layout;
            new DisplayImageLibrary((Login)Session["user"], ViewBag).getTotalCount(NO_OF_IMAGES);
            return View("ImageLibrary");
        }

        [SessionListener]
        public ActionResult nextPage(int nextPage, string layout)
        {
            Login login = (Login)Session["user"];
            new DisplayImageLibrary((Login)Session["user"],ViewBag).reqNextPage(nextPage, NO_OF_IMAGES);
            ViewBag.Display = "none";
            ViewBag.layoutView = layout;
            return View("ImageLibrary");
        }

        [SessionListener]
        public ActionResult LibraryAddNew()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        [SessionListener]
        public ActionResult LibraryAddNew(HttpPostedFileBase[] files)
        {
            DBConnect db = new DBConnect();
            Website web = db.getWebsite((Login)Session["user"]);
            
            DateTime date = DateTime.Now;
            string serverPath = "~/Website_"+web.webID.ToString()+"/Images/"+ date.ToString("yyyy-MM-dd")+"/";
            string path = Server.MapPath(serverPath);
            FolderHandler.getInstance().createDirectory(path);
            
            if (files != null)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                ParallelOptions po = new ParallelOptions();
                po.CancellationToken = cts.Token;
                
                ConcurrentBag<ImageLibrary> imagesList = new ConcurrentBag<ImageLibrary>();
                try
                {
                    int[] resizeHeights = new int[] { web.thumbHeight, web.mediumHeight, web.largeHeight };
                    int[] resizeWidths = new int[] { web.thumbWidth, web.mediumWidth, web.largeWidth };
                    string[] resizes = new string[] { "_thumb", "_medium", "_large" };
                    object locker = new object();
                    Parallel.ForEach(files, po, file =>
                    {
                        if (file != null)
                        {
                            string InputFileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string Extension = Path.GetExtension(file.FileName);
                            string ServerSavePath = FolderHandler.getInstance().generateNewFileName(serverPath, InputFileName, Extension,Server);
                            string fileName = FolderHandler.getInstance().getFileName(ServerSavePath,Extension);
                            
                            if (InputFileName.Length > 50)
                            {
                                InputFileName = InputFileName.Substring(0, 50);
                            }
                            lock (locker)
                            {
                                po.CancellationToken.ThrowIfCancellationRequested();
                                file.SaveAs(Server.MapPath(ServerSavePath));
                            }
                            ImageLibrary tempImage = new ImageLibrary(web.webID, InputFileName, "", ServerSavePath, date, date);
                            imagesList.Add(tempImage);
                            ServerSavePath = Server.MapPath(ServerSavePath);
                            Parallel.For(0, resizes.Length, (range) =>
                            {
                                string SaveFilePath = Path.Combine(Server.MapPath(serverPath) + fileName + resizes[range] + Extension);
                                new ImageResizer().ResizeImage(ServerSavePath, resizeWidths[range], resizeHeights[range], SaveFilePath);
                            });
                        }
                    });
                    if (imagesList.Count > 0)
                    {
                        ConcurrentBag<string> filesList = new ConcurrentBag<string>();
                        Parallel.ForEach(imagesList, (image) =>
                        {
                            db = new DBConnect();
                            db.uploadImage(image);
                            filesList.Add(image.title);
                        });
                        ViewBag.UploadFiles = filesList;
                        ViewBag.Message = "Images Uploaded Successfully!";
                    }
                }
                catch (AggregateException ae)
                {
                    cts.Cancel();
                    try
                    {
                        Parallel.ForEach(imagesList, (image) =>
                        {
                            deleteImageByLoc(image.imgLoc, null);
                        });
                    }
                    catch{ }

                    ae = ae.Flatten();
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        Logger.getInstance().setMessage(ex.GetBaseException()+ ". Exception throw on Website ID "+web.webID
                            ,Server.MapPath("~/Log.txt"));
                    }
                    ViewBag.Message = "Images Uploaded Un-Successful!";
                }
                ViewBag.Display = "Block";
            }
            return View();
        }

        [SessionListener]
        public ActionResult deleteImage(int imageID, string layout)
        {
            deleteSingleImage(imageID);
            changeLayout(layout);
            return View("ImageLibrary");
        }

        [SessionListener]
        public void deleteSingleImage(int imageID)
        {
            DBConnect db = new DBConnect();
            string imageLoc = db.getImageLoc(new ImageLibrary(imageID));
            bool status = deleteImageByLoc(imageLoc, null);
            if (status)
            {
                db.deleteImage(new ImageLibrary(imageID));
            }
        }

        [SessionListener]
        public ActionResult deletAllImages(List<int> imageList)
        {
            Login login = (Login)Session["user"];
            CancellationTokenSource cts = new CancellationTokenSource();
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            try
            {
                Parallel.ForEach(imageList, po, imageIndex =>
                {
                    DBConnect db = new DBConnect();
                    string imageLoc = db.getImageLoc(new ImageLibrary(imageIndex));
                    bool status = deleteImageByLoc(imageLoc,po);
                    if (status) {
                        db.deleteImage(new ImageLibrary(imageIndex));
                    }
                    else
                    {
                        cts.Cancel();
                    }
                });
                
            }
            catch (OperationCanceledException ope)
            {
                Website web = new DBConnect().getWebsite(login);
                int[] resizeHeights = new int[] { web.thumbHeight, web.mediumHeight, web.largeHeight };
                int[] resizeWidths = new int[] { web.thumbWidth, web.mediumWidth, web.largeWidth };
                string[] resizes = new string[] { "_thumb", "_medium", "_large" };
                foreach(int imageIndex in imageList)
                {
                    DBConnect db = new DBConnect();
                    string imageLoc = db.getImageLoc(new ImageLibrary(imageIndex));
                    if (System.IO.File.Exists(Server.MapPath(imageLoc))) {
                        string filename = imageLoc.Split('/').Last();
                        int index = filename.LastIndexOf(".");
                        string extension = filename.Substring(filename.LastIndexOf("."));
                        string path = imageLoc.Substring(0, imageLoc.IndexOf(filename));
                        filename = filename.Replace(extension, "");
                        
                        for(int count=0;count<resizes.Length;count++)
                        {
                            string filePath = path + filename + resizes[count] + extension;
                            if (!System.IO.File.Exists(Server.MapPath(filePath)))
                            {
                                new ImageResizer().ResizeImage(Server.MapPath(imageLoc), resizeWidths[count], resizeHeights[count], 
                                    Server.MapPath(filePath));
                            }
                        }
                    }
                }
                return Json(new { success = false, responseText = "Images Deleting Un-Successful" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, responseText = "Images Deleted Successfully" }, JsonRequestBehavior.AllowGet);
            
        }

        [SessionListener]
        public bool deleteImageByLoc(string imageLoc, ParallelOptions po)
        {
            Login login = (Login)Session["user"];
            if (System.IO.File.Exists(Server.MapPath(imageLoc)))
            {
                string filename = imageLoc.Split('/').Last();
                int index = filename.LastIndexOf(".");
                string extension = filename.Substring(filename.LastIndexOf("."));
                string path = imageLoc.Substring(0, imageLoc.IndexOf(filename));
                filename = filename.Replace(extension, "");


                if (po!=null && po.CancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                string[] resizes = new string[] { "_thumb", "_medium", "_large" };
                try
                {
                    Parallel.ForEach(resizes, currentSize =>
                    {
                        string filePath = path + filename + currentSize + extension;
                        FolderHandler.getInstance().deleteFile(Server.MapPath(filePath));
                    });
                    FolderHandler.getInstance().deleteFile(Server.MapPath(imageLoc));
                }
                catch (AggregateException ae) {
                    ae = ae.Flatten();
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        Logger.getInstance().setMessage(ex.GetBaseException() + ". Exception throw on Website ID " + login.webID,
                            Server.MapPath("~/Log.txt"));
                    }
                    return false;
                }
            }
            return true;
        }

        [SessionListener]
        public ActionResult downloadImages(List<int> imageList)
        {
            Login login = (Login)Session["user"];
            string webPath = "~/Website_" + login.webID.ToString();
            FolderHandler.getInstance().deleteFile(Server.MapPath(webPath + "/downloadImages.zip"));

            CancellationTokenSource cts = new CancellationTokenSource();
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            try
            {
                FolderHandler.getInstance().createDirectory(Server.MapPath(webPath + "/Images/downloadImages"));
                Parallel.ForEach(imageList, po, (imageID) =>
                {
                    ImageLibrary img = new ImageLibrary(imageID);
                    DBConnect db = new DBConnect();
                    img = db.getImage(img);
                    string filename = img.imgLoc.Split('/').Last();
                    string path = img.imgLoc.Replace(filename, "").Replace(webPath + "/Images/", "");
                    FolderHandler.getInstance().createDirectory(Server.MapPath(webPath + "/Images/downloadImages/"+path));
                    po.CancellationToken.ThrowIfCancellationRequested();
                    FolderHandler.getInstance().copyFile(Server.MapPath(img.imgLoc), 
                        Server.MapPath(webPath + "/Images/downloadImages/" + path + filename));
                });
                ZipFile.CreateFromDirectory(Server.MapPath(webPath + "/Images/downloadImages"), Server.MapPath(webPath + "/downloadImages.zip"));
                FolderHandler.getInstance().deleteFolders(Server.MapPath(webPath + "/Images/downloadImages"));
                byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath(webPath + "/downloadImages.zip"));
                string fileName = "Images.zip";
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            catch (AggregateException ae)
            {
                cts.Cancel();
                FolderHandler.getInstance().deleteFolders(Server.MapPath(webPath + "/Images/downloadImages"));
                ae = ae.Flatten();
                foreach (Exception ex in ae.InnerExceptions)
                {
                    Logger.getInstance().setMessage(ex.GetBaseException() + ". Exception throw on Website ID " + login.webID,
                            Server.MapPath("~/Log.txt"));
                }
            }
            return Json(new { success = false, responseText = "Images Downloading Un-Successful" }, JsonRequestBehavior.AllowGet);
        }

        [SessionListener]
        public ActionResult downloadImage(int imageID)
        {
            ImageLibrary img = new ImageLibrary(imageID);
            DBConnect db = new DBConnect();
            img = db.getImage(img);
            string path = Server.MapPath(img.imgLoc);
            if (System.IO.File.Exists(path))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath(img.imgLoc));
                string extension = img.imgLoc.Split('/').Last().Split('.').Last();
                string fileName = img.title+'.'+extension;

                ViewBag.Display = "block";
                ViewBag.Messsage = "Image Download Successfully!";
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            ViewBag.Display = "none";
            ViewBag.layoutView = "List";
            new DisplayImageLibrary((Login)Session["user"], ViewBag).getTotalCount(NO_OF_IMAGES);
            return View("ImageLibrary");
        }

        [HttpPost]
        [SessionListener]
        public ActionResult imagePropChange(ImageLibrary img, string layout)
        {
            DBConnect db = new DBConnect();
            DateTime date = DateTime.Now;
            img.modifyDate = date;
            db.updateImage(img);
            changeLayout(layout);
            return View("ImageLibrary");
        }
    }
}