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
        private readonly object locker = new object();
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
                     if (previousWeb.thumbWidth!=web.thumbWidth || previousWeb.thumbHeight!=web.thumbHeight) {
                         Task thumbTask = Task.Factory.StartNew(() =>
                            {
                                string newFilePath = path + filename + "_thumb" + extension;
                                if (System.IO.File.Exists(Server.MapPath(newFilePath)))
                                {
                                    System.IO.File.Delete(Server.MapPath(newFilePath));
                                }
                                Image imgPhoto = Image.FromFile(Server.MapPath(img.imgLoc));
                                Bitmap image = ResizeImage(imgPhoto, web.thumbWidth, web.thumbHeight);
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
                             Bitmap image = ResizeImage(imgPhoto, web.mediumWidth, web.mediumHeight);
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
                             Bitmap image = ResizeImage(imgPhoto, web.largeWidth, web.largeHeight);
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

        public Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            //lock (locker) {
                float img = image.HorizontalResolution;
                float img2 = image.VerticalResolution;
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            //}
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
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

                //List<Task> taskList = new List<Task>();

                if (Directory.Exists(Server.MapPath("~/Export/")))
                {
                    foreach (string file in Directory.GetFiles(Server.MapPath("~/Export/")))
                    {
                        System.IO.File.Delete(file);
                    }
                }
                else
                {
                    Directory.CreateDirectory(Server.MapPath("~/Export/"));
                }
                List<Task> taskList = new List<Task>();
                Task<int> mainTask = Task.Factory.StartNew(() =>
                 {
                     int count = 0;
                     foreach (string chkbox in checkboxes)
                     {
                         int tempCount = 0;
                         DBConnect db = new DBConnect();
                         Task currentTask;
                         switch (chkbox)
                         {
                             case "posts":
                                 tempCount = db.getPostsCount();
                                 currentTask = Task.Factory.StartNew((obj) => 
                                    {
                                        exportPosts(tempCount, login);
                                    },new { tempCount, login });
                                 taskList.Add(currentTask);

                                 break;
                             case "images":
                                 tempCount = db.getImageCount();
                                 currentTask = Task.Factory.StartNew((obj) =>
                                 {
                                     exportImages(tempCount, login);
                                 }, new { tempCount, login });
                                 taskList.Add(currentTask);
                                 break;
                //             case "website":
                //                 tempCount = 1;
                //                 currentTask = Task.Factory.StartNew((obj) =>
                //                 {
                //                     exportWebsite(login);
                //                 }, login);
                //                 taskList.Add(currentTask);
                //                 break;
                //             case "categories":
                //                 tempCount = db.getCategoryCount();
                //                 currentTask = Task.Factory.StartNew((obj) =>
                //                 {
                //                     exportCategories(tempCount, login);
                //                 }, new { tempCount, login });
                //                 taskList.Add(currentTask);
                //                 break;
                         }
                         count += tempCount;
                     }

                     return count;
                });


                //if (System.IO.File.Exists(Server.MapPath("~/Export.zip")))
                //{
                //    System.IO.File.Delete(Server.MapPath("~/Export.zip"));
                //}
                //int result = mainTask.Result;
                //Task.WaitAll(taskList.ToArray());
                //ZipFile.CreateFromDirectory(Server.MapPath("~/Export/"), Server.MapPath("~/Export.zip"));
                //byte[] fileBytes = System.IO.File.ReadAllBytes(Server.MapPath("~/Export.zip"));
                //string fileName = "Export.zip";
                //return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                return View();
            
            return View();
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
            foreach (Post post in postsList)
            {

                DBConnect paralleDB = new DBConnect();
                string catTitle = paralleDB.getCategoryByPost(post, login).title;
                string postContent = "\""+System.IO.File.ReadAllText(Server.MapPath(post.postLoc))+"\"";
                dt.Rows.Add(post.postTitle, catTitle, postContent, post.postStatus, post.createdDate, post.modifyDate);
            }

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

            
            foreach (ImageLibrary Img in imageslist)
            {
                if (System.IO.File.Exists(Server.MapPath(Img.imgLoc)))
                {
                    string filename = Img.imgLoc.Split('/')[3];
                    string path = Img.imgLoc.Substring(0, Img.imgLoc.IndexOf(filename)).Replace("~/","");
                    if (!Directory.Exists(Server.MapPath("~/Export/"+path)))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/Export/" + path));
                    }
                    System.IO.File.Copy(Server.MapPath(Img.imgLoc)
                        , Server.MapPath("~/Export/" + path + "/" + filename));
                }
                dt.Rows.Add(Img.title, Img.imgDesc, Img.imgLoc, Img.uploadDate, Img.modifyDate);
            }
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
            if (Directory.Exists(Server.MapPath("~/Export/Images/")))
            {
                ZipFile.CreateFromDirectory(Server.MapPath("~/Export/Images/"), Server.MapPath("~/Export/ImagesLibrary.zip"));
                foreach (string path in Directory.GetDirectories(Server.MapPath("~/Export/Images/")))
                {
                    foreach (string file in Directory.GetFiles(path))
                    {
                        System.IO.File.Delete(file);
                    }
                    Directory.Delete(path);
                }
            }
            
            
        }

        public void exportCategories(int count, Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Categories Table");
            dt.Columns.Add("Category Title");
            dt.Columns.Add("Category Description");

            DBConnect db = new DBConnect();
            List<Category> catList = db.getCatList(0, count, login);
            foreach (Category cat in catList) { 
                dt.Rows.Add(cat.title, cat.desc);
            }

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
            if (files[0]!=null)
            {
                bool postsStatus = ImportPosts(files[0], login);
                if (!postsStatus)
                {
                    ViewBag.Display = "block";
                    ViewBag.Message = "Import Posts file is corrupted!";
                }
            }
            
            return View();
        }

        public Boolean ImportPosts(HttpPostedFileBase file, Login login)
        { 
            string csvPath = Server.MapPath("~/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);

            string csvData = System.IO.File.ReadAllText(csvPath);
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[3] 
            {
                new DataColumn("Id", typeof(int)),
                new DataColumn("Name", typeof(string)),
                new DataColumn("Country",typeof(string))
            });
            List<Post> rows = new List<Post>();
            List<string> columns = new List<string>();
            int rowCount = 0;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (rowCount==0)
                    {
                        foreach (string cell in row.Split(','))
                        {
                            columns.Add(cell);
                        }
                        rowCount++;
                    }
                    else
                    {
                        int i = 0;
                        Post post = new Post();
                        foreach (string cell in row.Split(','))
                        {
                            switch (columns[i])
                            {
                                case "Post Title":
                                    if (cell.Length>150)
                                    {
                                        return false;
                                    }
                                    post.postTitle = cell;
                                    break;
                                case "Post Category":
                                    DBConnect db = new DBConnect();
                                    Category category = db.checkCategoryAvailable(new Category(cell));
                                    if (category.catID==0)
                                    {
                                        if (cell.Length>50)
                                        {
                                            return false;
                                        }
                                        category.webID = login.webID;
                                        category.desc = cell;
                                        db.addCategory(category);
                                        category = db.checkCategoryAvailable(category);
                                    }
                                    post.catId = category.catID;
                                    break;
                                case "Post Content":
                                    post.postData = cell;
                                    break;
                                case "Post Status":
                                    if (cell.ToLower().Equals("publish") || cell.ToLower().Equals("draft"))
                                    {
                                        string status = cell.First().ToString().ToUpper() + cell.Substring(1);
                                        post.postStatus = status;
                                    }
                                    else
                                    {
                                        post.postStatus = "Darft";
                                    }
                                    break;
                                case "Post Created Date":
                                    DateTime createdDate;
                                    if (DateTime.TryParse(cell,out createdDate))
                                    {
                                        post.createdDate = createdDate;
                                    }
                                    else
                                    {
                                        post.createdDate = DateTime.Now;
                                    }
                                    break;
                                case "Post Modify Date":
                                    DateTime modifyDate;
                                    if (DateTime.TryParse(cell, out modifyDate))
                                    {
                                        post.modifyDate = modifyDate;
                                    }
                                    else
                                    {
                                        post.modifyDate = DateTime.Now;
                                    }
                                    break;
                                default:
                                    return false;
                            }
                            i++;
                        }
                        rows.Add(post);
                    }
                }
            }

            foreach (Post post in rows)
            {
                string serverPath = "~/Posts/" + post.createdDate.ToString("yyyy-MM-dd") + "/" + post.postStatus + "/";
                string path = Server.MapPath(serverPath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                }

                path = serverPath + post.postTitle + ".txt";
                bool exists = false;
                int count = 0;
                do
                {
                    if (count == 0)
                    {
                        path = serverPath + post.postTitle + ".txt";
                    }
                    else
                    {
                        path = serverPath + post.postTitle + '_' + count + ".txt";
                    }
                    exists = System.IO.File.Exists(Server.MapPath(path));
                    count++;
                } while (exists);

                System.IO.File.WriteAllText(Server.MapPath(path), post.postData);
                DBConnect db = new DBConnect();
                post.postLoc = path;
                post.webId = login.webID;
                db.uploadPost(post);
            }
            return true;
        }
    }
}