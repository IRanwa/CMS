using FinalProj.Models;
using Microsoft.Web.Administration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class ImageLibraryController : Controller
    {
        public ActionResult ImageLibrary()
        {
            ViewBag.Display = "none";
            ViewBag.layoutView = "Grid";
            getTotalImageCount();
            return View();
        }

        public ActionResult changeLayout(string layout)
        {
            ViewBag.Display = "none";
            ViewBag.layoutView = layout;
            getTotalImageCount();
            return View("ImageLibrary");
        }

        private void displayImages(ImageLibrary img)
        {
            if (img.currentPage!=0) {
                int startIndex = (img.currentPage - 1) * 20;
                DBConnect db = new DBConnect();
                Login login = (Login)Session["user"];
                List<ImageLibrary> images = db.getImages(startIndex, 20,login);
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
            img.noOfPages = Convert.ToInt32(Math.Ceiling(count / 20.0));
            if (count>0) {
                img.currentPage = 1;
            }
            displayImages(img);
        }

        public ActionResult nextPage(int nextPage, string layout)
        {
            
            DBConnect db = new DBConnect();
            int count = db.getImageCount();
            ImageLibrary img = new ImageLibrary();
            img.totalImageCount = count;
            img.noOfPages = Convert.ToInt32(Math.Ceiling(count / 20.0));
            if (nextPage > img.noOfPages)
            {
                nextPage = img.noOfPages;
            }

            img.currentPage = nextPage;
            

            if (nextPage != 0)
            {
                int startIndex = (nextPage - 1) * 20;
                Login login = (Login)Session["user"];
                List<ImageLibrary> images = db.getImages(startIndex, 20,login);
                ViewBag.DisplayImages = images;
                ViewBag.LibraryProp = img;
            }
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
            DateTime date = DateTime.Now;
            string serverPath = "~/Images/"+ date.ToString("yyyy-MM-dd")+"/";
            string path = Server.MapPath(serverPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                
            }
            
            if (files != null)
            {
                DBConnect db = new DBConnect();
                Website web = db.getWebsite((Login)Session["user"]);
                List<string> filesList = new List<string>();
                List<ImageLibrary> images = new List<ImageLibrary>();
                foreach (HttpPostedFileBase file in files)
                {
                    if (file != null)
                    {
                        string InputFileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string Extension = Path.GetExtension(file.FileName);
                        string fileName;

                        int count=0;
                        string ServerSavePath;
                        do
                        {
                            fileName = InputFileName;
                            if (count==0)
                            {
                                ServerSavePath = serverPath + fileName + Extension;
                            }
                            else
                            {
                                fileName = fileName + '_' + count;
                                ServerSavePath = serverPath + fileName + Extension;
                            }
                            path = db.checkImageExists(new ImageLibrary(ServerSavePath));
                            count++;
                        } while (path!=null);
                        
                        ServerSavePath = Path.Combine(Server.MapPath(ServerSavePath));
                        file.SaveAs(ServerSavePath);
                        
                       Image imgPhoto = Image.FromFile(ServerSavePath);


                        Bitmap image = new ImageResizer().ResizeImage(imgPhoto, web.thumbWidth, web.thumbHeight);
                        image.Save(Path.Combine(Server.MapPath(serverPath) + fileName + "_thumb" + Extension));
                        image.Dispose();
                        imgPhoto.Dispose();

                        imgPhoto = Image.FromFile(ServerSavePath);
                        image = new ImageResizer().ResizeImage(imgPhoto, web.mediumWidth, web.mediumHeight);
                        image.Save(Path.Combine(Server.MapPath(serverPath) + fileName + "_medium" + Extension));
                        image.Dispose();
                        imgPhoto.Dispose();

                        imgPhoto = Image.FromFile(ServerSavePath);
                        image = new ImageResizer().ResizeImage(imgPhoto, web.largeWidth, web.largeHeight);
                        image.Save(Path.Combine(Server.MapPath(serverPath) + fileName + "_large" + Extension));
                        image.Dispose();
                        imgPhoto.Dispose();

                        if (InputFileName.Length>50)
                        {
                            InputFileName = InputFileName.Substring(0, 50);
                        }
                        images.Add(new ImageLibrary(web.webID, InputFileName, "", serverPath+fileName+Extension, date , date));
                        ViewBag.Message = "Images Uploaded Successfully!";
                        ViewBag.Display = "Block";
                        filesList.Add(InputFileName);

                        
                    }
                    else
                    {
                        ViewBag.Display = "none";
                    }
                }
                if (filesList.Count>0) {
                    db.uploadImages(images);
                    ViewBag.UploadFiles = filesList;
                }
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
            foreach (int imageIndex in imageList)
            {
                deleteSingleImage(imageIndex);
            }
            changeLayout(layout);
        }

        public void deleteSingleImage(int imageID)
        {
            DBConnect db = new DBConnect();
            string imageLoc = db.getImageLoc(new ImageLibrary(imageID));
            db.deleteImage(new ImageLibrary(imageID));

            if (System.IO.File.Exists(Server.MapPath(imageLoc)))
            {
                System.IO.File.Delete(Request.MapPath(imageLoc));

                string filename = imageLoc.Split('/')[3];
                int index = filename.LastIndexOf(".");
                string extension = filename.Substring(filename.LastIndexOf("."));
                string path = imageLoc.Substring(0, imageLoc.IndexOf(filename));
                filename = filename.Replace(extension, "");

                string newFilePath = path + filename + "_thumb" + extension;
                if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                {
                    System.IO.File.Delete(Server.MapPath(newFilePath));
                }

                newFilePath = path + filename + "_medium" + extension;
                if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                {
                    System.IO.File.Delete(Server.MapPath(newFilePath));
                }

                newFilePath = path + filename + "_large" + extension;
                if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                {
                    System.IO.File.Delete(Server.MapPath(newFilePath));
                }
            }
        }

        [HttpPost]
        public ActionResult imagePropChange(ImageLibrary img, string layout)
        {
            DBConnect db = new DBConnect();
            db.updateImage(img);
            changeLayout(layout);
            return View("ImageLibrary");
        }
    }
}