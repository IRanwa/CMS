using FinalProj.Models;
using Microsoft.Web.Administration;
using System;
using System.Collections;
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
        public ActionResult ImageLibrary()
        {
            ViewBag.Display = "none";
            ViewBag.layoutView = "Grid";
            new DisplayImageLibrary((Login)Session["user"], ViewBag).getTotalCount(NO_OF_IMAGES);
            return View();
        }

        public ActionResult changeLayout(string layout)
        {
            ViewBag.Display = "none";
            ViewBag.layoutView = layout;
            new DisplayImageLibrary((Login)Session["user"], ViewBag).getTotalCount(NO_OF_IMAGES);
            return View("ImageLibrary");
        }

        public ActionResult nextPage(int nextPage, string layout)
        {
            Login login = (Login)Session["user"];
            new DisplayImageLibrary((Login)Session["user"],ViewBag).reqNextPage(nextPage, NO_OF_IMAGES);
            ViewBag.Display = "none";
            ViewBag.layoutView = layout;
            return View("ImageLibrary");
        }

        public ActionResult LibraryAddNew()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
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
                List<Task> tasksList = new List<Task>();
                CancellationTokenSource source = new CancellationTokenSource();
                var token = source.Token;
                Task<List<ImageLibrary>> mainTask = Task.Factory.StartNew(() =>
                {
                    List<ImageLibrary> imagesList = new List<ImageLibrary>();
                    Parallel.ForEach(files, (file) =>
                    {
                        if (file != null)
                        {
                            string InputFileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string Extension = Path.GetExtension(file.FileName);
                            string fileName="";
                            
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

                            if (token.IsCancellationRequested)
                            {
                                token.ThrowIfCancellationRequested();
                            }
                            ServerSavePath = Path.Combine(Server.MapPath(ServerSavePath));
                            file.SaveAs(ServerSavePath);

                            if (InputFileName.Length > 50)
                            {
                                InputFileName = InputFileName.Substring(0, 50);
                            }
                            ImageLibrary tempImage = new ImageLibrary(web.webID, InputFileName, "", serverPath + fileName + Extension, date, date);
                            imagesList.Add(tempImage);

                            Task thumbTask = Task.Factory.StartNew(() =>
                            {
                                if (token.IsCancellationRequested)
                                {
                                    token.ThrowIfCancellationRequested();
                                }
                                Image imgPhoto = Image.FromFile(ServerSavePath);
                                Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.thumbWidth, web.thumbHeight);
                                image.Save(Path.Combine(Server.MapPath(serverPath) + fileName + "_thumb" + Extension));
                                image.Dispose();
                                imgPhoto.Dispose();

                            }, token);

                            Task mediumTask = Task.Factory.StartNew(() =>
                            {
                                if (token.IsCancellationRequested)
                                {
                                    token.ThrowIfCancellationRequested();
                                }
                                Image imgPhoto = Image.FromFile(ServerSavePath);
                                Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.mediumWidth, web.mediumHeight);
                                image.Save(Path.Combine(Server.MapPath(serverPath) + fileName + "_medium" + Extension));
                                image.Dispose();
                                imgPhoto.Dispose();
                            }, token);

                            Task largeTask = Task.Factory.StartNew(() =>
                            {
                                if (token.IsCancellationRequested)
                                {
                                    token.ThrowIfCancellationRequested();
                                }
                                Image imgPhoto = Image.FromFile(ServerSavePath);
                                Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.largeWidth, web.largeHeight);
                                image.Save(Path.Combine(Server.MapPath(serverPath) + fileName + "_large" + Extension));
                                image.Dispose();
                                imgPhoto.Dispose();
                            }, token);

                            tasksList.Add(thumbTask);
                            tasksList.Add(mediumTask);
                            tasksList.Add(largeTask);
                        }
                    });
                    return imagesList;
                }, token);

                try
                {
                    List<ImageLibrary> result = mainTask.Result;
                    Task.WaitAll(tasksList.ToArray());
                    if (result.Count > 0)
                    {
                        List<string> filesList = new List<string>();

                        Parallel.ForEach(result, (image) =>
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
                    source.Cancel();
                    List<ImageLibrary> result = mainTask.Result;
                    Parallel.ForEach(result, (image) =>
                    {
                        deleteImageByLoc(image.imgLoc);
                    });
                    ViewBag.Message = "Images Uploaded Un-Successful!";
                }
                ViewBag.Display = "Block";
            }
            return View();
        }

        public ActionResult deleteImage(int imageID, string layout)
        {
            deleteSingleImage(imageID);
            changeLayout(layout);
            return View("ImageLibrary");
        }

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

        public void deleteSingleImage(int imageID)
        {
            DBConnect db = new DBConnect();
            string imageLoc = db.getImageLoc(new ImageLibrary(imageID));
            db.deleteImage(new ImageLibrary(imageID));
            deleteImageByLoc(imageLoc);
        }

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