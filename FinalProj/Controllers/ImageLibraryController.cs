using FinalProj.Models;
using Microsoft.Web.Administration;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
        private const int NO_OF_IMAGES = 100;

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
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                
            }
            
            if (files != null)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                ParallelOptions op = new ParallelOptions();
                op.CancellationToken = cts.Token;
                
                ConcurrentBag<ImageLibrary> imagesList = new ConcurrentBag<ImageLibrary>();
                try
                {
                    int[] resizeHeights = new int[] { web.thumbHeight, web.mediumHeight, web.largeHeight };
                    int[] resizeWidths = new int[] { web.thumbWidth, web.mediumWidth, web.largeWidth };
                    string[] resizes = new string[] { "_thumb", "_medium", "_large" };
                    object locker = new object();
                    Parallel.ForEach(files, op, file =>
                    {
                        if (file != null)
                        {
                            string InputFileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string Extension = Path.GetExtension(file.FileName);
                            string fileName = "";

                            int count = 0;
                            string ServerSavePath;
                            bool exists = false;
                            do
                            {
                                fileName = InputFileName;
                                if (count == 0)
                                {
                                    ServerSavePath = serverPath + fileName + Extension;
                                }
                                else
                                {
                                    fileName = fileName + '_' + count;
                                    ServerSavePath = serverPath + fileName + Extension;
                                }
                                exists = System.IO.File.Exists(Server.MapPath(ServerSavePath));
                                count++;
                            } while (exists);

                            if (InputFileName.Length > 50)
                            {
                                InputFileName = InputFileName.Substring(0, 50);
                            }
                            ServerSavePath = Path.Combine(Server.MapPath(ServerSavePath));
                            lock (locker)
                            {
                                file.SaveAs(ServerSavePath);
                            }
                            ImageLibrary tempImage = new ImageLibrary(web.webID, InputFileName, "", serverPath + fileName + Extension, date, date);
                            imagesList.Add(tempImage);

                            
                            
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
                    Parallel.ForEach(imagesList, (image) =>
                    {
                        deleteImageByLoc(image.imgLoc);
                    });
                    ViewBag.Message = "Images Uploaded Un-Successful!";
                }
                ViewBag.Display = "Block";
            }
            ModelState.Clear();
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
        public void deletAllImages(List<int> imageList, string layout)
        {
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Parallel.ForEach(imageList, imageIndex =>
            {
                deleteSingleImage(imageIndex);
            });
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
            changeLayout(layout);
        }

        [SessionListener]
        public void deleteSingleImage(int imageID)
        {
            DBConnect db = new DBConnect();
            string imageLoc = db.getImageLoc(new ImageLibrary(imageID));
            db.deleteImage(new ImageLibrary(imageID));
            deleteImageByLoc(imageLoc);
        }

        [SessionListener]
        public void deleteImageByLoc(string imageLoc)
        {
            if (System.IO.File.Exists(Server.MapPath(imageLoc)))
            {
                System.IO.File.Delete(Server.MapPath(imageLoc));

                string filename = imageLoc.Split('/').Last();
                int index = filename.LastIndexOf(".");
                string extension = filename.Substring(filename.LastIndexOf("."));
                string path = imageLoc.Substring(0, imageLoc.IndexOf(filename));
                filename = filename.Replace(extension, "");

                Task.Factory.StartNew(() => {
                    string newFilePath = path + filename + "_thumb" + extension;
                    if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                    {
                        System.IO.File.Delete(Server.MapPath(newFilePath));
                    }
                });

                Task.Factory.StartNew(() => {
                    string newFilePath = path + filename + "_medium" + extension;
                    if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                    {
                        System.IO.File.Delete(Server.MapPath(newFilePath));
                    }
                });

                Task.Factory.StartNew(() => {
                    string newFilePath = path + filename + "_large" + extension;
                    if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                    {
                        System.IO.File.Delete(Server.MapPath(newFilePath));
                    }
                });
            }
        }

        [SessionListener]
        public FileResult downloadImages(List<int> imageList)
        {
            Login login = (Login)Session["user"];
            string webPath = "~/Website_" + login.webID.ToString();
            if (System.IO.File.Exists(Server.MapPath(webPath + "/downloadImages.zip")))
            {
                System.IO.File.Delete(Server.MapPath(webPath + "/downloadImages.zip"));
            }
            Parallel.ForEach(imageList, (imageID) =>
            {
                ImageLibrary img = new ImageLibrary(imageID);
                DBConnect db = new DBConnect();
                img = db.getImage(img);
                string filename = img.imgLoc.Split('/').Last();
                string path = img.imgLoc.Replace(filename, "").Replace(webPath + "/Images/", "");
                string folderPath = Server.MapPath(webPath + "/Images/downloadImages/" + path);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                System.IO.File.Copy(Server.MapPath(img.imgLoc), Server.MapPath(webPath + "/Images/downloadImages/" + path + filename));
            });
            ZipFile.CreateFromDirectory(Server.MapPath(webPath+"/Images/downloadImages"), Server.MapPath(webPath+"/downloadImages.zip"));
            DeleteFolders.getInstance(Server).deleteFolders(webPath+"/Images/downloadImages");
            byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath(webPath+"/downloadImages.zip"));
            string fileName = "Images.zip";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
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