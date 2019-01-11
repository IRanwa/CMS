using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FinalProj.Models;

namespace FinalProj.Controllers
{
    public class ExportDetails
    {
        private static ExportDetails instance = new ExportDetails();
        private static Login login;
        private static HttpServerUtilityBase server;
        private static List<string> checkboxes;

        private ExportDetails() { }

        public static ExportDetails getInstance(Login newLogin, HttpServerUtilityBase newServer, List<string> newCheckboxes)
        {
            login = newLogin;
            server = newServer;
            checkboxes = newCheckboxes;
            return instance;
        }

        public int exportStart()
        {
            if (Directory.Exists(server.MapPath("~/Export/")))
            {
                foreach (string file in Directory.GetFiles(server.MapPath("~/Export/")))
                {
                    System.IO.File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(server.MapPath("~/Export/"));
            }
            List<Task> taskList = new List<Task>();
            WebSettings.exportProgress = 0;
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
                            if (tempCount > 0)
                            {
                                currentTask = Task.Factory.StartNew((obj) =>
                                {
                                    exportPosts(tempCount, login);
                                }, new { tempCount, login });
                                taskList.Add(currentTask);
                                tempCount++;
                            }
                            break;
                        case "images":
                            tempCount = db.getImageCount();
                            if (tempCount > 0)
                            {
                                currentTask = Task.Factory.StartNew((obj) =>
                                {
                                    exportImages(tempCount, login);
                                }, new { tempCount, login });
                                taskList.Add(currentTask);
                                tempCount++;
                            }
                            break;
                        case "website":
                            tempCount = 1;
                            currentTask = Task.Factory.StartNew((obj) =>
                            {
                                exportWebsite(login);
                            }, new { login });
                            taskList.Add(currentTask);
                            tempCount++;
                            break;
                        case "categories":
                            tempCount = db.getCategoryCount();
                            if (tempCount > 0)
                            {
                                currentTask = Task.Factory.StartNew((obj) =>
                                {
                                    exportCategories(tempCount, login);
                                }, new { tempCount, login });
                                taskList.Add(currentTask);
                            }
                            break;
                    }
                    count += tempCount;
                }
                return count;
            });
            return mainTask.Result;
        }

        public void exportPosts(int count, Login login)
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
            int taskCount;
            foreach (Post post in postsList)
            {

                DBConnect paralleDB = new DBConnect();
                string catTitle = paralleDB.getCategoryByPost(post, login).title;
                string postContent = System.IO.File.ReadAllText(server.MapPath(post.postLoc));
                if (postContent.Contains("\n"))
                {
                    int index = postContent.LastIndexOf("\n");
                    postContent = postContent.Substring(0, index);
                }
                dt.Rows.Add(post.postTitle, catTitle, postContent, post.postStatus
                    , post.createdDate, post.modifyDate);
                WebSettings.setExportProgress(1);
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

            System.IO.File.WriteAllText(server.MapPath("~/Export/ExportPosts.csv"), builder.ToString());

            WebSettings.setExportProgress(1);

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
                if (System.IO.File.Exists(server.MapPath(Img.imgLoc)))
                {
                    string filename = Img.imgLoc.Split('/')[3];
                    string path = Img.imgLoc.Substring(0, Img.imgLoc.IndexOf(filename)).Replace("~/", "");
                    if (!Directory.Exists(server.MapPath("~/TempImages/" + path)))
                    {
                        Directory.CreateDirectory(server.MapPath("~/TempImages/" + path));
                    }
                    System.IO.File.Copy(server.MapPath(Img.imgLoc)
                        , server.MapPath("~/TempImages/" + path + "/" + filename));
                }
                dt.Rows.Add(Img.title, Img.imgDesc, Img.imgLoc, Img.uploadDate, Img.modifyDate);
                WebSettings.setExportProgress(1);
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

            System.IO.File.WriteAllText(server.MapPath("~/Export/ExportImages.csv"), builder.ToString());
            if (System.IO.File.Exists(server.MapPath("~/Export/ImagesLibrary.zip")))
            {
                System.IO.File.Delete(server.MapPath("~/Export/ImagesLibrary.zip"));
            }
            if (Directory.Exists(server.MapPath("~/TempImages/Images/")))
            {
                ZipFile.CreateFromDirectory(server.MapPath("~/TempImages/Images/"), server.MapPath("~/Export/ImagesLibrary.zip"));

                foreach (string path in Directory.GetDirectories(server.MapPath("~/TempImages/Images/")))
                {
                    foreach (string file in Directory.GetFiles(path))
                    {
                        System.IO.File.Delete(file);
                    }
                    Directory.Delete(path);
                }
            }
            WebSettings.setExportProgress(1);
        }

        public void exportCategories(int count, Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Categories Table");
            dt.Columns.Add("Category Title");
            dt.Columns.Add("Category Description");

            DBConnect db = new DBConnect();
            List<Category> catList = db.getCatList(0, count, login);
            catList.RemoveAt(0);
            int taskCount;
            foreach (Category cat in catList)
            {
                dt.Rows.Add(cat.title, cat.desc);
                WebSettings.setExportProgress(1);
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

            System.IO.File.WriteAllText(server.MapPath("~/Export/ExportCategories.csv"), builder.ToString());
            WebSettings.setExportProgress(1);
        }

        public void exportWebsite(Login login)
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
                , web.mediumHeight, web.largeWidth, web.largeHeight);

            WebSettings.setExportProgress(1);

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

            System.IO.File.WriteAllText(server.MapPath("~/Export/ExportWebsite.csv"), builder.ToString());
            WebSettings.setExportProgress(1);
        }
    }
}