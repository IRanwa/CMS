using System;
using System.Collections.Concurrent;
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
        private CancellationTokenSource cts = new CancellationTokenSource();

        public WebSettings getWebsettings()
        {
            return webSettings;
        }
        public int exportStart(Login login,List<string> checkboxes, HttpServerUtilityBase server)
        {
            if (webSettings==null) {
                webSettings = new WebSettings();
            }
            mServer = server;
            webPath = "~/Website_" + login.webID;
            string path = mServer.MapPath(webPath + "/Export/");
            FolderHandler.getInstance().deleteFiles(path);
            FolderHandler.getInstance().createDirectory(path);
            
            int count = 0;
            CancellationToken token = cts.Token;
            try
            {
                Parallel.ForEach<string, int>(checkboxes, () => 0, (chkbox, loop, subtotal) =>
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
                                 exportPosts(tempCount, login, token);
                             }, token);
                             tempCount++;
                         }
                     }
                     else if (chkbox.Equals("images"))
                     {
                         tempCount = db.getImageCount(login);
                         if (tempCount > 0)
                         {
                             Task.Factory.StartNew(() =>
                             {
                                 exportImages(tempCount, login, token);
                             }, token);
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
                                 exportCategories(tempCount, login, token);
                             }, token);
                         }
                     }
                     else if (chkbox.Equals("website"))
                     {
                         tempCount = 1;
                         Task.Factory.StartNew(() =>
                         {
                             exportWebsite(login, token);
                         }, token);
                         tempCount++;
                     }
                     subtotal += tempCount;
                     return subtotal;
                 }, (x) => Interlocked.Add(ref count, x));
                return count;
            }
            catch (AggregateException ae)
            {
                ae = ae.Flatten();
                foreach (Exception ex in ae.InnerExceptions)
                {
                    Logger.getInstance().setMessage(ex.GetBaseException() + ". Exception throw on Website ID " + login.webID,
                            server.MapPath("~/Log.txt"));
                }
                return 0;
            }
        }

        public void exportPosts(int count, Login login, CancellationToken token)
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
            foreach(Post post in postsList)
            {
                DBConnect innerDB = new DBConnect();
                string catTitle = escSequence(innerDB.getCategoryByPost(post, login).title);
                string postContent = FolderHandler.getInstance().readFileText(mServer.MapPath(post.postLoc));

                dt.Rows.Add(escSequence(post.postTitle), catTitle, postContent, post.postStatus
                    , post.createdDate, post.modifyDate);
                webSettings.setExportProgress(1);
            }
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            System.IO.File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportPosts.csv"), generateCsv(dt));
            webSettings.setExportProgress(1);
        }

        public void exportImages(int count, Login login, CancellationToken token)
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
                    dt.Rows.Add(escSequence(Img.title), escSequence(Img.imgDesc), escSequence(Img.imgLoc), 
                        Img.uploadDate, Img.modifyDate);
                }
                webSettings.setExportProgress(1);
            }

            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            System.IO.File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportImages.csv"), generateCsv(dt));
            FolderHandler.getInstance().deleteFile(mServer.MapPath(webPath + "/Export/ImagesLibrary.zip"));
            
            if (Directory.Exists(mServer.MapPath(webPath+"/TempImages/")))
            {
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
                ZipFile.CreateFromDirectory(mServer.MapPath(webPath+"/TempImages/"), 
                    mServer.MapPath(webPath+"/Export/ImagesLibrary.zip"));
                FolderHandler.getInstance().deleteFolders(mServer.MapPath(webPath + "/TempImages/"));
            }
            webSettings.setExportProgress(1);
        }

        public void exportCategories(int count, Login login, CancellationToken token)
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
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportCategories.csv"), generateCsv(dt));
            webSettings.setExportProgress(1);
        }

        public void exportWebsite(Login login, CancellationToken token)
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
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            File.WriteAllText(mServer.MapPath(webPath+"/Export/ExportWebsite.csv"), generateCsv(dt));
            webSettings.setExportProgress(1);
        }

        private string generateCsv(DataTable dt)
        {
            StringBuilder builder = new StringBuilder();
            ConcurrentBag<string> rows = new ConcurrentBag<string>();
            
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();

            builder.Append(string.Join(",", columnNames)).Append("\n");
            
            Parallel.ForEach(dt.Rows.Cast<DataRow>(), row =>
            {
                List<string> currentRow = new List<string>();
                foreach (string column in columnNames)
                {
                    object item = row[column];

                    currentRow.Add(item.ToString());
                }
                rows.Add(string.Join(",", currentRow.ToArray()));
            });

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

        public void stopExporting()
        {
            cts.Cancel();
        }
    }
}