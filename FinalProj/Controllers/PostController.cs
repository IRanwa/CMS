using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class PostController : Controller,PagerInterface<Post>
    {
        private const int NO_OF_IMAGES = 12;
        private const int NO_OF_POSTS = 50;

        [SessionListener]
        public ActionResult Posts()
        {
            ViewBag.Display = "none";
            getTotalCount(NO_OF_POSTS);
            contentEditor();
            return View();
        }

        [SessionListener]
        public void getContent(Post post, int noOfPosts)
        {
            if (post.currentPage != 0)
            {
                int startIndex = (post.currentPage - 1) * noOfPosts;
                DBConnect db = new DBConnect();
                Login login = (Login)Session["user"];
                List<Post> posts = db.getPostList(startIndex, noOfPosts, login);
                ViewBag.DisplayPosts = posts;
                ViewBag.PostsProp = post;
            }
        }

        [SessionListener]
        public void getTotalCount(int noOfPosts)
        {
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            int count = db.getPostsCount(login);

            Post post = new Post();
            post.totalCategoryCount = count;
            post.noOfPages = Convert.ToInt32(Math.Ceiling(count / Double.Parse(noOfPosts.ToString())));
            if (count > 0)
            {
                post.currentPage = 1;
            }
            getContent(post, noOfPosts);
        }

        [SessionListener]
        public void reqNextPage(int nextPage, int noOfImage)
        {
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            int count = db.getPostsCount(login);
            Post post = new Post();
            post.totalCategoryCount = count;
            post.noOfPages = Convert.ToInt32(Math.Ceiling(count / Double.Parse(NO_OF_POSTS.ToString())));
            if (nextPage > post.noOfPages)
            {
                nextPage = post.noOfPages;
            }

            post.currentPage = nextPage;


            if (nextPage != 0)
            {
                int startIndex = (nextPage - 1) * NO_OF_POSTS;
                List<Post> posts = db.getPostList(startIndex, startIndex + NO_OF_POSTS, login);
                ViewBag.DisplayPosts = posts;
                ViewBag.PostsProp = post;
            }
        }

        [SessionListener]
        public ActionResult nextPostsPage(int nextPage)
        {
            reqNextPage(nextPage, NO_OF_POSTS);
            ViewBag.Display = "none";
            contentEditor();
            return View("Posts");
        }

        [SessionListener]
        public ActionResult PostsAddNew()
        {
            ViewBag.Display = "none";
            contentEditor();
            return View();
        }

        [SessionListener]
        private void contentEditor()
        {
            Login login = (Login)Session["user"];
            new DisplayImageLibrary(login, ViewBag).getTotalCount(NO_OF_IMAGES);
            getCategoryList(login);
        }

        [SessionListener]
        private void getCategoryList(Login login)
        {
            DBConnect db = new DBConnect();
            int categoryCount = db.getCategoryCount(login);
            ViewBag.catList = db.getCatList(0, categoryCount, login);
            Post post = new Post(0);
            ViewBag.post = post;
        }

        [SessionListener]
        public PartialViewResult nextImagePage(int nextPage, string pageName)
        {
            Login login = (Login)Session["user"];
            new DisplayImageLibrary(login, ViewBag).reqNextPage(nextPage, NO_OF_IMAGES);
            ViewBag.Display = "none";
            ViewBag.popup = "block";
            Post post = new Post(0);
            ViewBag.post = post;
            return PartialView("ImagesContainer");
        }

        [SessionListener]
        public ActionResult uploadPost(string status, string content, string title, int category, long uploadId)
        {
            DateTime date = DateTime.Now;
            Login login = (Login)Session["user"];
            if (uploadId==0) {
                string serverPath = "~/Website_"+login.webID+"/Posts/" + date.ToString("yyyy-MM-dd") + "/" + status + "/";
                string path = Server.MapPath(serverPath);
                FolderHandler.getInstance().createDirectory(path);
                //if (!Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);

                //}
                path = FolderHandler.getInstance().generateNewFileName(serverPath, title, ".txt",Server);
                //path = serverPath + title + ".txt";
                //bool exists = false;
                //int count = 0;
                //do
                //{
                //    if (count == 0)
                //    {
                //        path = serverPath + title + ".txt";
                //    }
                //    else
                //    {
                //        path = serverPath + title + '_' + count + ".txt";
                //    }
                //    exists = System.IO.File.Exists(Server.MapPath(path));
                //    count++;
                //} while (exists);

                FolderHandler.getInstance().writeToNewFile(Server.MapPath(path), content);
                DBConnect db = new DBConnect();
                Website web = db.getWebsite(login);
                uploadId = db.uploadPost(new Post(category, web.webID, title, path, status, date, date));
                return Json(new { postID = uploadId }, JsonRequestBehavior.AllowGet);
            }
            else
            {
               Task dbSaveTask = Task.Factory.StartNew(() =>
                {
                    DBConnect db = new DBConnect();
                    db.updatePost(new Post(uploadId, category, title, date));
                    changeStatus(uploadId, status,"single");
                });

                Task.Factory.StartNew(() =>
                {
                    DBConnect db = new DBConnect();
                    string postLoc = db.getPostLoc(new Post(uploadId));
                    FolderHandler.getInstance().writeToFile(Server.MapPath(postLoc), content);
                    //if (System.IO.File.Exists(Server.MapPath(postLoc)))
                    //{
                    //    System.IO.File.WriteAllText(Server.MapPath(postLoc), content);

                    //}
                });
                dbSaveTask.Wait();
            }
            return Json(new { postID = uploadId }, JsonRequestBehavior.AllowGet);
        }

        [SessionListener]
        public ActionResult deletePost(int postId, string action)
        {
            DBConnect db = new DBConnect();
            Post post = new Post(postId);
            
            string postLoc = db.getPostLoc(post);
            FolderHandler.getInstance().deleteFile(Server.MapPath(postLoc));
            db.deletePosts(post);
            if (!action.Equals("bulk")) {
                Posts();
            }
            return View("Posts");
        }

        [SessionListener]
        public ActionResult bulkPostAction(List<int> postsList, string action)
        {
            Task bulkActionTask = Task.Factory.StartNew(() =>
            {
                switch (action)
                {
                    case "deleteAll":
                        Parallel.ForEach(postsList, (index) =>
                        {
                            deletePost(index,"bulk");
                        });
                        break;
                    case "publishAll":
                        Parallel.ForEach(postsList, (index) =>
                        {
                            changeStatus(index, "Publish", "bulk");
                        });
                        break;
                    case "draftAll":
                        Parallel.ForEach(postsList, (index) =>
                        {
                            changeStatus(index, "Draft","bulk");
                        });
                        break;
                }
            });
            Task contentEditTask = Task.Factory.StartNew(() =>
            {
                contentEditor();
            });
            Task.WaitAll(new Task[] {bulkActionTask,contentEditTask });
            return View("Posts");
        }

        [SessionListener]
        public ActionResult changeStatus(long postId, string status, string action)
        {
            DateTime date = DateTime.Now;
            DBConnect db = new DBConnect();
            Post post = new Post(postId, status,date);
            db.changePostStatus(post);

            string postLoc = db.getPostLoc(post);
            string[] split = postLoc.Split('/');
            string filename = split[split.Length - 1].Replace(".txt", "");
            if (filename.Contains('_'))
            {
                filename = filename.Substring(0, filename.IndexOf('_'));
            }

            int index = postLoc.IndexOf(filename);
            string path;
            if (status.Equals("Publish"))
            {
                path = postLoc.Substring(0, index).Replace("Draft", status);
            }
            else
            {
                path = postLoc.Substring(0, index).Replace("Publish", status);
            }

            FolderHandler folder = FolderHandler.getInstance();
            folder.createDirectory(Server.MapPath(path));
            string newFilePath = folder.generateNewFileName(path, filename, ".txt",Server);
            folder.moveFile(Server.MapPath(postLoc), Server.MapPath(newFilePath));
            post.postLoc = newFilePath;
            db.changePostLoc(post);
            
            if (!action.Equals("bulk")) {
                Posts();
            }
            return View("Posts");
        }

        [SessionListener]
        public ActionResult editPost(int postId)
        {
            PostsAddNew();
            DBConnect db = new DBConnect();
            Post post = new Post(postId);
            post = db.getPostsById((Login)Session["user"],post);
            post.postData = FolderHandler.getInstance().readFileText(Server.MapPath(post.postLoc));
            ViewBag.post = post;
            return View("PostsAddNew");
        }

        [SessionListener]
        public ActionResult addCommonText(List<int> postsList, string content, string position)
        {
            Login login = (Login)Session["user"];
            DateTime date = DateTime.Now;
            Task contentTask = Task.Factory.StartNew(() =>
            {
                contentEditor();
            });

            Parallel.ForEach(postsList, (index) =>
            {
                DBConnect db = new DBConnect();
                Post post = new Post(index);
                post = db.getPostsById(login, post);
                if (post.postLoc!=null)
                {
                    string path = Server.MapPath(post.postLoc);
                    string pastContent = FolderHandler.getInstance().readFileText(path);
                    if (position.Equals("top"))
                    {
                        pastContent = content + "<br><br>" + pastContent;
                    }
                    else
                    {
                        pastContent += "<br><br>"+content;
                    }
                    FolderHandler.getInstance().writeToFile(path, pastContent); 
                    post.modifyDate = date;
                    db.updatePost(post);
                }
            });
            
            
            contentTask.Wait();
            return View("Posts");
        }
    }
}