using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FinalProj.Models;

namespace FinalProj.Controllers
{
    public class ExportDetails
    {
        private HttpServerUtilityBase mServer;
        private string webPath;
        private WebSettings webSettings;
        private readonly object locker = new object();

        public WebSettings getWebsettings()
        {
            return webSettings;
        }
        public int exportStart(Login login,List<string> checkboxes, HttpServerUtilityBase server)
        {
            webSettings = new WebSettings();
            mServer = server;
            webPath = "~/Website_" + login.webID;
            string path = mServer.MapPath(webPath + "/Export/");
            FolderHandler.getInstance().deleteFiles(path);
            FolderHandler.getInstance().createDirectory(path);
            
            int count = 0;
                
            Parallel.ForEach<string,int>(checkboxes, () => 0, (chkbox, loop, subtotal) =>
            {
                int tempCount = 0;
                DBConnect db = new DBConnect();
                if (chkbox.Equals("posts"))
                {
                    tempCount = db.getPostsCount(login);
                    if (tempCount > 0)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            exportPosts(tempCount, login);
                        });
                        tempCount++;
                    }
                } else if (chkbox.Equals("images"))
                {
                    tempCount = db.getImageCount(login);
                    if (tempCount > 0)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            exportImages(tempCount, login);
                        });
                        tempCount++;
                    }
                }
                else if (chkbox.Equals("categories"))
                {
                    tempCount = db.getCategoryCount(login);
                    if (tempCount > 0)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            exportCategories(tempCount, login);
                        });
                    }
                } else if (chkbox.Equals("website"))
                {
                    tempCount = 1;
                    Task.Factory.StartNew(() =>
                    {
                        exportWebsite(login);
                    });
                    tempCount++;
                }
                subtotal += tempCount;
                return subtotal;
            }, (x) => Interlocked.Add(ref count, x));
            return count;
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
            foreach(Post post in postsList)
            {
                DBConnect innerDB = new DBConnect();
                string catTitle = escSequence(innerDB.getCategoryByPost(post, login).title);
                string postContent = FolderHandler.getInstance().readFileText(mServer.MapPath(post.postLoc));
                if (postContent!=null && postContent.Contains("\n"))
                {
                    int index = postContent.LastIndexOf("\n");
                    postContent = escSequence(postContent.Substring(0, index));
                }

                dt.Rows.Add(escSequence(post.postTitle), catTitle, postContent, post.postStatus
                    , post.createdDate, post.modifyDate);
                webSettings.setExportProgress(1);
            }

            System.IO.File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportPosts.csv"), generateCsv(dt));
            webSettings.setExportProgress(1);
        }

        public void exportImages(int count, Login login)
        {
            DataTable dt = new DataTable("Images Table");
            dt.Columns.Add("Image Title");
            dt.Columns.Add("Image Description");
            dt.Columns.Add("Image Path");
            dt.Columns.Add("Image Upload Date");
            dt.Columns.Add("Image Modify Date");

            DBConnect db = new DBConnect();
            List<ImageLibrary> imageslist = db.getImages(0, count, login);
            FolderHandler.getInstance().deleteFolders(mServer.MapPath(webPath + "/TempImages/"));
            foreach(ImageLibrary Img in imageslist)
            {
                if (File.Exists(mServer.MapPath(Img.imgLoc)))
                {
                    string filename = Img.imgLoc.Split('/').Last();
                    string path = Img.imgLoc.Substring(0, Img.imgLoc.IndexOf(filename)).Replace(webPath + "/Images/", "");
                    FolderHandler.getInstance().createDirectory(mServer.MapPath(webPath + "/TempImages/" + path));
                    string newPath = mServer.MapPath(webPath + "/TempImages/" + path + "/" + filename);
                    FolderHandler.getInstance().copyFile(mServer.MapPath(Img.imgLoc), newPath);
                    dt.Rows.Add(escSequence(Img.title), escSequence(Img.imgDesc), escSequence(Img.imgLoc), Img.uploadDate, Img.modifyDate);
                }
                webSettings.setExportProgress(1);
            }
            
            System.IO.File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportImages.csv"), generateCsv(dt));
            FolderHandler.getInstance().deleteFile(mServer.MapPath(webPath + "/Export/ImagesLibrary.zip"));
            
            if (Directory.Exists(mServer.MapPath(webPath+"/TempImages/")))
            {
                ZipFile.CreateFromDirectory(mServer.MapPath(webPath+"/TempImages/"), mServer.MapPath(webPath+"/Export/ImagesLibrary.zip"));
                FolderHandler.getInstance().deleteFolders(mServer.MapPath(webPath + "/TempImages/"));
            }
            webSettings.setExportProgress(1);
        }

        public void exportCategories(int count, Login login)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Categories Table");
            dt.Columns.Add("Category Title");
            dt.Columns.Add("Category Description");

            DBConnect db = new DBConnect();
            List<Category> catList = db.getCatList(0, count, login);
            catList.RemoveAt(0);
            foreach(Category cat in catList)
            {
                dt.Rows.Add(escSequence(cat.title), escSequence(cat.desc));
                webSettings.setExportProgress(1);
            }
            File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportCategories.csv"), generateCsv(dt));
            webSettings.setExportProgress(1);
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
            dt.Rows.Add(escSequence(web.webTitle), web.noOfPosts, web.thumbWidth, web.thumbHeight, web.mediumWidth
                , web.mediumHeight, web.largeWidth, web.largeHeight);

            webSettings.setExportProgress(1);
            File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportWebsite.csv"), generateCsv(dt));
            webSettings.setExportProgress(1);
        }

        private string generateCsv(DataTable dt)
        {
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

            return builder.Append(string.Join("\n", rows.ToArray())).ToString();
        }

        private string escSequence(string text)
        {
            if (text.Contains(","))
            {
                return "\"" + text + "\"";
            }
            return text;
        }
    }
}