using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using FinalProj.Models;

namespace FinalProj.Controllers
{
    public class ImportDeatils
    {
        private static Login login;
        private static HttpPostedFileBase[] files;
        private static HttpServerUtilityBase server;
        private static ImportDeatils instance = new ImportDeatils();

        private ImportDeatils() { }

        public static ImportDeatils getInstance(Login newLogin, HttpPostedFileBase[] newFiles, HttpServerUtilityBase newServer)
        {
            login = newLogin;
            files = newFiles;
            server = newServer;
            return instance;
        }

        public bool importStart()
        {
            int count = 0;
            bool status = true;
            foreach (HttpPostedFileBase file in files)
            {

                bool importStatus;
                string message = "";
                switch (count)
                {
                    case 0:
                        if (files[count] != null)
                        {
                            importStatus = ImportPosts(files[count], login);
                            if (!importStatus)
                            {
                                if (status)
                                {
                                    status = false;
                                }
                                message += "Import Posts file is corrupted!\n";
                            }
                        }
                        break;
                    case 1:
                        HttpPostedFileBase imageCSV = files[count];
                        HttpPostedFileBase imageZip = files[count + 1];
                        if (imageCSV != null && imageZip != null)
                        {
                            importStatus = ImportImages(imageCSV, imageZip, login);
                            if (!importStatus)
                            {
                                if (status)
                                {
                                    status = false;
                                }
                                message += "Import Image CSV or Zip file is corrupted!\n";
                            }
                        }
                        count++;
                        break;
                    case 3:
                        if (files[count] != null)
                        {
                            importStatus = ImportCategories(files[count], login);
                            if (!importStatus)
                            {
                                if (status)
                                {
                                    status = false;
                                }
                                message += "Import Category file is corrupted!\n";
                            }
                        }
                        count++;
                        break;
                    case 4:
                        if (files[count] != null)
                        {
                            importStatus = ImportWebsite(files[count], login);
                            if (!importStatus)
                            {
                                if (status)
                                {
                                    status = false;
                                }
                                message += "Import Website Settings file is corrupted!\n";
                            }
                        }
                        break;

                }
                count++;
            }
            return status;
        }

        public Boolean ImportPosts(HttpPostedFileBase file, Login login)
        {
            string csvPath = server.MapPath("~/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);

            string csvData = System.IO.File.ReadAllText(csvPath);
            List<Post> rows = new List<Post>();
            List<string> columns = new List<string>();
            int rowCount = 0;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (rowCount == 0)
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
                                    if (cell.Length > 150)
                                    {
                                        post.postTitle = cell.Substring(0, 150);
                                    }
                                    else
                                    {
                                        post.postTitle = cell;
                                    }
                                    break;
                                case "Post Category":
                                    DBConnect db = new DBConnect();
                                    Category category = db.checkCategoryAvailable(new Category(cell),login);
                                    if (category.catID == 0)
                                    {
                                        if (cell.Length > 50)
                                        {
                                            return false;
                                        }
                                        category.webID = login.webID;
                                        category.desc = cell;
                                        db.addCategory(category);
                                        category = db.checkCategoryAvailable(category,login);
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
                                        post.postStatus = "Draft";
                                    }

                                    break;
                                case "Post Created Date":
                                    DateTime createdDate;
                                    if (DateTime.TryParse(cell, out createdDate))
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
                string path = server.MapPath(serverPath);
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
                    exists = System.IO.File.Exists(server.MapPath(path));
                    count++;
                } while (exists);

                System.IO.File.WriteAllText(server.MapPath(path), post.postData);
                DBConnect db = new DBConnect();
                post.postLoc = path;
                post.webId = login.webID;
                db.uploadPost(post);
            }
            return true;
        }
        private bool ImportImages(HttpPostedFileBase imageCSV, HttpPostedFileBase imageZip, Login login)
        {
            string csvPath = server.MapPath("~/Import/") + Path.GetFileName(imageCSV.FileName);
            imageCSV.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);

            List<ImageLibrary> rows = new List<ImageLibrary>();
            List<string> columns = new List<string>();
            int rowCount = 0;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (rowCount == 0)
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
                        ImageLibrary img = new ImageLibrary();
                        foreach (string cell in row.Split(','))
                        {
                            switch (columns[i])
                            {
                                case "Image Title":
                                    if (cell.Length > 50)
                                    {
                                        img.title = cell.Substring(0, 50);
                                    }
                                    else
                                    {
                                        img.title = cell;
                                    }
                                    break;
                                case "Image Description":
                                    if (cell.Length > 150)
                                    {
                                        img.imgDesc = cell.Substring(0, 150);
                                    }
                                    else
                                    {
                                        img.imgDesc = cell;
                                    }
                                    break;
                                case "Image Path":
                                    if (cell.Length > 150)
                                    {
                                        img.imgLoc = cell.Substring(0, 150);
                                    }
                                    else
                                    {
                                        img.imgLoc = cell;
                                    }
                                    break;
                                case "Image Upload Date":
                                    DateTime uploadDate;
                                    if (DateTime.TryParse(cell, out uploadDate))
                                    {
                                        img.uploadDate = uploadDate;
                                    }
                                    else
                                    {
                                        img.uploadDate = DateTime.Now;
                                    }
                                    break;
                                case "Image Modify Date":
                                    DateTime modifyDate;
                                    if (DateTime.TryParse(cell, out modifyDate))
                                    {
                                        img.modifyDate = modifyDate;
                                    }
                                    else
                                    {
                                        img.modifyDate = DateTime.Now;
                                    }
                                    break;
                                default:
                                    return false;
                            }
                            i++;
                        }

                        rows.Add(img);
                    }
                }
            }

            csvPath = server.MapPath("~/Import/") + Path.GetFileName(imageZip.FileName);
            imageZip.SaveAs(csvPath);
            ZipFile.ExtractToDirectory(csvPath, server.MapPath("~/Import/TempImages/"));
            foreach (ImageLibrary tempImg in rows)
            {
                string serverPath = "~/Images/" + tempImg.uploadDate.ToString("yyyy-MM-dd") + "/";
                string path = server.MapPath(serverPath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                }

                string filename = tempImg.imgLoc.Split('/').Last();
                string extension = '.' + filename.Split('.').Last();
                filename = filename.Replace(extension, "");

                bool exists = false;
                int count = 0;
                do
                {
                    if (count == 0)
                    {
                        path = serverPath + filename + extension;
                    }
                    else
                    {
                        path = serverPath + filename + '_' + count + extension;
                    }
                    exists = System.IO.File.Exists(server.MapPath(path));
                    count++;
                } while (exists);
                System.IO.File.Copy(server.MapPath("~/Import/TempImages/" + tempImg.imgLoc.Replace("~/Images/", "")),
                    server.MapPath(path));
                
                DBConnect db = new DBConnect();
                tempImg.imgLoc = path;
                tempImg.webID = login.webID;
                db.uploadImage(tempImg);
            }
            DeleteFolders.getInstance(server).deleteFolders("~/Import/TempImages");
            return true;
        }

        private bool ImportCategories(HttpPostedFileBase file, Login login)
        {
            string csvPath = server.MapPath("~/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            List<Category> rows = new List<Category>();
            List<string> columns = new List<string>();
            int rowCount = 0;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (rowCount == 0)
                    {
                        Category category = new Category();
                        foreach (string cell in row.Split(','))
                        {
                            columns.Add(cell);
                        }
                        rowCount++;
                    }
                    else
                    {
                        Category category = new Category();
                        int i = 0;
                        foreach (string cell in row.Split(','))
                        {
                            switch (columns[i])
                            {
                                case "Category Title":
                                    if (cell.Length > 50)
                                    {
                                        category.title = cell.Substring(0, 50);
                                    }
                                    else
                                    {
                                        category.title = cell;
                                    }
                                    break;
                                case "Category Description":
                                    if (cell.Length > 150)
                                    {
                                        category.desc = cell.Substring(0, 150);
                                    }
                                    else
                                    {
                                        category.desc = cell;
                                    }
                                    break;
                                default:
                                    return false;
                            }
                            i++;
                        }
                        rows.Add(category);
                    }
                }
            }

            foreach (Category category in rows)
            {
                DBConnect db = new DBConnect();
                Category available = db.checkCategoryAvailable(category,login);
                if (available.catID == 0)
                {
                    category.webID = login.webID;
                    db.addCategory(category);
                }
            }
            return true;
        }

        private bool ImportWebsite(HttpPostedFileBase file, Login login)
        {
            string csvPath = server.MapPath("~/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            Website website = new Website();
            List<string> columns = new List<string>();
            int rowCount = 0;
            foreach (string row in csvData.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (rowCount == 0)
                    {
                        Category category = new Category();
                        foreach (string cell in row.Split(','))
                        {
                            columns.Add(cell);
                        }
                        rowCount++;
                    }
                    else
                    {
                        int i = 0;
                        foreach (string cell in row.Split(','))
                        {
                            switch (columns[i])
                            {
                                case "Website Title":
                                    if (cell.Length > 30)
                                    {
                                        website.webTitle = cell.Substring(0, 30);
                                    }
                                    else
                                    {
                                        website.webTitle = cell;
                                    }
                                    break;
                                case "No of Posts":
                                    if (cell.Length > 2)
                                    {
                                        website.noOfPosts = 10;
                                    }
                                    else
                                    {
                                        website.noOfPosts = Int32.Parse(cell);
                                    }
                                    break;
                                case "Thumbnail Image Width":
                                    if (cell.Length > 4)
                                    {
                                        website.thumbWidth = 100;
                                    }
                                    else
                                    {
                                        website.thumbWidth = Int32.Parse(cell);
                                    }
                                    break;
                                case "Thumbnail Image Height":
                                    if (cell.Length > 4)
                                    {
                                        website.thumbHeight = 100;
                                    }
                                    else
                                    {
                                        website.thumbHeight = Int32.Parse(cell);
                                    }
                                    break;
                                case "Medium Image Width":
                                    if (cell.Length > 4)
                                    {
                                        website.mediumWidth = 100;
                                    }
                                    else
                                    {
                                        website.mediumWidth = Int32.Parse(cell);
                                    }
                                    break;
                                case "Medium Image Height":
                                    if (cell.Length > 4)
                                    {
                                        website.mediumHeight = 100;
                                    }
                                    else
                                    {
                                        website.mediumHeight = Int32.Parse(cell);
                                    }
                                    break;
                                case "Large Image Width":
                                    if (cell.Length > 4)
                                    {
                                        website.largeWidth = 100;
                                    }
                                    else
                                    {
                                        website.largeWidth = Int32.Parse(cell);
                                    }
                                    break;
                                case "Large Image Height":
                                    if (cell.Length > 4)
                                    {
                                        website.largeHeight = 100;
                                    }
                                    else
                                    {
                                        website.largeHeight = Int32.Parse(cell);
                                    }
                                    break;
                                default:
                                    return false;
                            }
                            i++;
                        }
                    }
                }
            }

            DBConnect db = new DBConnect();
            db.updateWebsite(website);

            return true;
        }
    }
}