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
using FinalProj.Models;

namespace FinalProj.Controllers
{
    public class ImportDeatils
    {
        private Login login;
        private HttpServerUtilityBase server;
        private string webPath;
        private Boolean status = true;
        private WebSettings websettings;

        public WebSettings getWebsettings()
        {
            return websettings;
        }

        public int importStart(Login newLogin, HttpPostedFileBase[] files, HttpServerUtilityBase server)
        {
            websettings = new WebSettings();
            this.server = server;
            login = newLogin;

            webPath = "~/Website_" + newLogin.webID;
            string path = server.MapPath(webPath + "/Import/");
            FolderHandler.getInstance().deleteFiles(path);
            FolderHandler.getInstance().createDirectory(path);
            
            int count = 0;
            Parallel.For(0, files.Length, ()=>0, (range,loop,total) =>
            {
                if (files[range]!=null)
                {
                    switch (range)
                    {
                        case 0:
                            total += postsCount(files[range]);
                            Task.Factory.StartNew(() =>
                            {
                                ImportPosts(files[range]);
                            });
                            total++;
                            break;
                        case 1:
                            HttpPostedFileBase imageCSV = files[range];
                            range++;
                            HttpPostedFileBase imageZip = files[range];
                            total += imagesCount(imageCSV,imageZip);
                            Task.Factory.StartNew(() =>
                            {
                                ImportImages(imageCSV, imageZip);
                            });
                            total++;
                            break;
                        case 3:
                            total += categoriesCount(files[range]);
                            Task.Factory.StartNew(() =>
                            {
                                ImportCategories(files[range]);
                            });
                            total++;
                            break;
                        case 4:
                            total += websiteCount(files[range]);
                            Task.Factory.StartNew(() =>
                            {
                                ImportWebsite(files[range]);
                            });
                            break;
                    }
                }
                return total;
            }, (x)=>Interlocked.Add(ref count,x));
            Console.WriteLine(count);
            return count;
        }
        
        private int postsCount(HttpPostedFileBase file)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            return csvData.Split('\n').Length;
        }

        private int imagesCount(HttpPostedFileBase imageCSV, HttpPostedFileBase imageZip)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(imageCSV.FileName);
            imageCSV.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            int count = csvData.Split('\n').Length;
            
            csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(imageZip.FileName);
            imageZip.SaveAs(csvPath);
            count += count - 1;
            return count;
        }

        private int categoriesCount(HttpPostedFileBase file)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            return csvData.Split('\n').Length;
        }

        private int websiteCount(HttpPostedFileBase file)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            int count = csvData.Split('\n').Length;
            if (count>2)
            {
                count = 2;
            }
            return count;
        }


        public void ImportPosts(HttpPostedFileBase file)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(file.FileName);
            string csvData = System.IO.File.ReadAllText(csvPath);
            ConcurrentBag<Post> rows = new ConcurrentBag<Post>();
            List<string> columns = new List<string>();
            string[] rowsList = csvData.Split('\n');
            foreach (string cell in rowsList[0].Split(','))
            {
                columns.Add(cell);
            }
            websettings.setImportProgress(1);

            rowsList = rowsList.Where(val => val != rowsList[0]).ToArray();
            Parallel.ForEach(rowsList, row =>
            {
                if (!string.IsNullOrEmpty(row))
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
                                Category category = db.checkCategoryAvailable(new Category(cell), login);
                                if (category.catID == 0)
                                {
                                    if (cell.Length > 50)
                                    {
                                        status = false;
                                    }
                                    category.webID = login.webID;
                                    category.desc = cell;
                                    db.addCategory(category);
                                    category = db.checkCategoryAvailable(category, login);
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
                                status = false;
                                break;
                        }
                        i++;
                    }
                    rows.Add(post);
                }
                websettings.setImportProgress(1);
            });
            
            object locker = new object();
            Parallel.ForEach(rows, post =>
            {
                string serverPath = webPath + "/Posts/" + post.createdDate.ToString("yyyy-MM-dd") + "/" + post.postStatus + "/";
                string path = server.MapPath(serverPath);
                FolderHandler folder = FolderHandler.getInstance();
                folder.createDirectory(path);
                path = folder.generateNewFileName(serverPath, post.postTitle, ".txt",server);
                folder.writeToNewFile(server.MapPath(path), post.postData);
                DBConnect db = new DBConnect();
                post.postLoc = path;
                post.webId = login.webID;
                db.uploadPost(post);
            });
            websettings.setImportProgress(1);
        }
        private void ImportImages(HttpPostedFileBase imageCSV, HttpPostedFileBase imageZip)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(imageCSV.FileName);
            string csvData = System.IO.File.ReadAllText(csvPath);

            ConcurrentBag<ImageLibrary> rows = new ConcurrentBag<ImageLibrary>();
            List<string> columns = new List<string>();
            string[] rowsList = csvData.Split('\n');
            foreach (string cell in rowsList[0].Split(','))
            {
                columns.Add(cell);
            }
            websettings.setImportProgress(1);

            rowsList = rowsList.Where(val => val != rowsList[0]).ToArray();
            Parallel.ForEach(rowsList, row =>
            {
                if (!string.IsNullOrEmpty(row))
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
                                status = false;
                                break;
                        }
                        i++;
                    }
                    rows.Add(img);
                }
                websettings.setImportProgress(1);
            });

            Console.WriteLine(rows.Count);

            csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(imageZip.FileName);
            FolderHandler.getInstance().deleteFolders(server.MapPath(webPath + "/Import/TempImages/"));
            ZipFile.ExtractToDirectory(csvPath, server.MapPath(webPath + "/Import/TempImages/"));
            Parallel.ForEach(rows, tempImg =>
            {
                string serverPath = webPath + "/Images/" + tempImg.uploadDate.ToString("yyyy-MM-dd") + "/";
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
                string tempPath = server.MapPath(webPath + "/Import/TempImages/" + tempImg.imgLoc.Replace(webPath + "/Images/", ""));
                if (System.IO.File.Exists(tempPath)) {
                    System.IO.File.Copy(tempPath,server.MapPath(path));
                    
                    DBConnect db = new DBConnect();
                    Website web = db.getWebsite(login);
                    int[] resizeHeights = new int[] { web.thumbHeight, web.mediumHeight, web.largeHeight };
                    int[] resizeWidths = new int[] { web.thumbWidth, web.mediumWidth, web.largeWidth };
                    string[] resizes = new string[] { "_thumb", "_medium", "_large" };
                    Parallel.For(0, resizes.Length, (range) =>
                    {
                        string SaveFilePath = Path.Combine(server.MapPath(serverPath) + filename + resizes[range] + extension);
                        new ImageResizer().ResizeImage(server.MapPath(path), resizeWidths[range], resizeHeights[range], SaveFilePath);
                    });

                    tempImg.imgLoc = path;
                    tempImg.webID = login.webID;
                    db.uploadImage(tempImg);
                }
                websettings.setImportProgress(1);
            });
            FolderHandler.getInstance().deleteFolders(server.MapPath(webPath + "/Import/TempImages"));
            websettings.setImportProgress(1);
        }

        private void ImportCategories(HttpPostedFileBase file)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(file.FileName);
            string csvData = System.IO.File.ReadAllText(csvPath);
            List<Category> rows = new List<Category>();
            List<string> columns = new List<string>();
            string[] rowsList = csvData.Split('\n');
            foreach (string cell in rowsList[0].Split(','))
            {
                columns.Add(cell);
            }
            websettings.setImportProgress(1);
            rowsList = rowsList.Where(val => val != rowsList[0]).ToArray();
            Parallel.ForEach(rowsList, row =>
            {
                if (!string.IsNullOrEmpty(row))
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
                                    status = false;
                                    break;
                            }
                            i++;
                        }
                        rows.Add(category);
                    
                }
                websettings.setImportProgress(1);
            });

            Parallel.ForEach(rows, category =>
            {
                DBConnect db = new DBConnect();
                Category available = db.checkCategoryAvailable(category,login);
                if (available.catID == 0)
                {
                    category.webID = login.webID;
                    db.addCategory(category);
                }
            });
            websettings.setImportProgress(1);
        }

        private void ImportWebsite(HttpPostedFileBase file)
        {
            string csvPath = server.MapPath(webPath + "/Import/") + Path.GetFileName(file.FileName);
            file.SaveAs(csvPath);
            string csvData = System.IO.File.ReadAllText(csvPath);
            
            List<string> columns = new List<string>();
            string[] rowsList = csvData.Split('\n');
            foreach (string cell in rowsList[0].Split(','))
            {
                columns.Add(cell);
            }
            websettings.setImportProgress(1);

            rowsList = rowsList.Where(val => val != rowsList[0]).ToArray();
            Website website = new Website();
            if (rowsList.Length>0)
            {
                if (!string.IsNullOrEmpty(rowsList[0]))
                {
                    int i = 0;
                    foreach (string cell in rowsList[0].Split(','))
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
                                status = false;
                                break;
                        }
                        i++;
                    }
                }
            }
            
            DBConnect db = new DBConnect();
            website.webID = login.webID;
            db.updateWebsite(website);
            db.updateImgLibSettings(website);
            websettings.setImportProgress(1);
        }
    }
}