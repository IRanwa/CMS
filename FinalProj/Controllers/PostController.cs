using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class PostController : Controller
    {
        public ActionResult Posts()
        {
            ViewBag.Display = "none";
            Post post = getTotalPostsCount();
            displayPosts(post);
            return View();
        }

        private void displayPosts(Post post)
        {
            if (post.currentPage != 0)
            {
                int startIndex = (post.currentPage - 1) * 20;
                DBConnect db = new DBConnect();
                List<Post> posts = db.getPostList(startIndex, startIndex + 20);
                ViewBag.DisplayPosts = posts;
                ViewBag.PostsProp = post;
            }
        }

        private Post getTotalPostsCount()
        {
            DBConnect db = new DBConnect();
            int count = db.getPostsCount();

            Post post = new Post();
            post.totalCategoryCount = count;
            post.noOfPages = Convert.ToInt32(Math.Ceiling(count / 20.0));
            if (count > 0)
            {
                post.currentPage = 1;
            }
            return post;
        }

        public ActionResult nextPostsPage(int nextPage)
        {
            DBConnect db = new DBConnect();
            int count = db.getPostsCount();
            Post post = new Post();
            post.totalCategoryCount = count;
            post.noOfPages = Convert.ToInt32(Math.Ceiling(count / 20.0));
            if (nextPage > post.noOfPages)
            {
                nextPage = post.noOfPages;
            }

            post.currentPage = nextPage;


            if (nextPage != 0)
            {
                int startIndex = (nextPage - 1) * 20;
                List<Post> posts = db.getPostList(startIndex, startIndex + 20);
                ViewBag.DisplayPosts = posts;
                ViewBag.PostsProp = post;
            }
            ViewBag.Display = "none";
            return View("Posts");
        }

        public ActionResult PostsAddNew()
        {
            ViewBag.Display = "none";
            getTotalImageCount();
            DBConnect db = new DBConnect();
            int categoryCount = db.getCategoryCount();
            ViewBag.catList = db.getCatList(0, categoryCount);
            return View();
        }

        private void displayImages(ImageLibrary img)
        {
            if (img.currentPage != 0)
            {
                int startIndex = (img.currentPage - 1) * 12;
                DBConnect db = new DBConnect();
                List<ImageLibrary> images = db.getImages(startIndex,12);
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
                List<ImageLibrary> images = db.getImages(startIndex, 12);
                ViewBag.DisplayImages = images;
                ViewBag.LibraryProp = img;
            }
            ViewBag.Display = "none";
            ViewBag.popup = "block";
            return View("PostsAddNew");
        }

        public ActionResult uploadPost(string status, string content, string title, int category, int uploadId)
        {
            DateTime date = DateTime.Now;
            DBConnect db = new DBConnect();
            if (uploadId==0) {
                string serverPath = "~/Posts/" + date.ToString("yyyy-MM-dd") + "/" + status + "/";
                string path = Server.MapPath(serverPath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                }

                path = serverPath + title + ".txt";
                bool exists = false;
                int count = 0;
                do
                {
                    if (count == 0)
                    {
                        path = serverPath + title + ".txt";
                    }
                    else
                    {
                        path = serverPath + title + '_' + count + ".txt";
                    }
                    exists = System.IO.File.Exists(Server.MapPath(path));
                    count++;
                } while (exists);

                System.IO.File.WriteAllText(Server.MapPath(path), content);
                
                Website web = db.getWebsite((Login)Session["user"]);
                long id = db.uploadPost(new Post(category, web.webID, title, path, status, date, date));
                return Json(new { postID = id }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                db.updatePost(new Post(uploadId,category,title,status,date));
                string postLoc = db.getPostLoc(new Post(uploadId));
                if (System.IO.File.Exists(Server.MapPath(postLoc)))
                {
                    System.IO.File.WriteAllText(Server.MapPath(postLoc), content);

                }
                changeStatus(uploadId, status);
            }
            return Json(new { postID = uploadId }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult deletePost(int postId)
        {
            DBConnect db = new DBConnect();
            Post post = new Post(postId);
            string postLoc = db.getPostLoc(post);
            db.deletePosts(post);

            postLoc = Server.MapPath(postLoc);

            if (System.IO.File.Exists(postLoc))
            {
                System.IO.File.Delete(postLoc);
            }

            Posts();
            return View("Posts");
        }

        public ActionResult bulkPostAction(List<int> postsList, string action)
        {
            DBConnect db = new DBConnect();
            switch (action) {
                case "deleteAll":
                    db.deletePosts(postsList);
                    break;
                case "publishAll":
                    db.changePostStatus(postsList,"Publish");
                    break;
                case "draftAll":
                    db.changePostStatus(postsList, "Draft");
                    break;
            }
            return View("Posts");
        }

        public ActionResult changeStatus(int postId, string status)
        {
            DBConnect db = new DBConnect();
            Post post = new Post(postId, status);
            string postLoc = db.getPostLoc(post);
            db.changePostStatus(new Post(postId,status));

            string[] split = postLoc.Split('/');
            string filename = split[split.Length-1].Replace(".txt","");
            if (filename.Contains('_'))
            {
                filename = filename.Substring(0, filename.IndexOf('_'));
            }

            int index = postLoc.IndexOf(filename);
            string path;
            if (status.Equals("Publish"))
            {
                path = postLoc.Substring(0, index).Replace("Draft",status);
            }
            else
            {
                path = postLoc.Substring(0, index).Replace("Publish", status);
            }

            if (!Directory.Exists(Server.MapPath(path)))
            {
                Directory.CreateDirectory(Server.MapPath(path));
            }

            string newFilePath = path + filename;

            int count = 0;
            bool exists = false;
            do
            {
                if (count==0)
                {
                    newFilePath = path + filename+".txt";
                }
                else
                {
                    newFilePath = path + filename+'_'+count + ".txt";
                }
                exists = System.IO.File.Exists(Server.MapPath(newFilePath));
                count++;
            } while (exists);

            post.postLoc = newFilePath;
            db.changePostLoc(post);
            System.IO.File.Move(Server.MapPath(postLoc), Server.MapPath(newFilePath));

            Posts();
            return View("Posts");
        }
    }
}