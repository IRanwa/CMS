using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class PostController : Controller,PagerInterface<Post>
    {
        private const int NO_OF_IMAGES = 12;
        private const int NO_OF_POSTS = 20;

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
                List<Post> posts = db.getPostList(startIndex, startIndex + noOfPosts, login);
                ViewBag.DisplayPosts = posts;
                ViewBag.PostsProp = post;
            }
        }

        [SessionListener]
        public void getTotalCount(int noOfPosts)
        {
            DBConnect db = new DBConnect();
            int count = db.getPostsCount();

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
            DBConnect db = new DBConnect();
            int count = db.getPostsCount();
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
                Login login = (Login)Session["user"];
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

                Task.Factory.StartNew(() =>
                {
                    System.IO.File.WriteAllText(Server.MapPath(path), content);
                });
                Task<long> dbSaveTask = Task.Factory.StartNew(() =>
                {
                    Website web = db.getWebsite((Login)Session["user"]);
                    return  db.uploadPost(new Post(category, web.webID, title, path, status, date, date));
                });

                uploadId = dbSaveTask.Result;
                return Json(new { postID = uploadId }, JsonRequestBehavior.AllowGet);
            }
            else
            {
               Task dbSaveTask = Task.Factory.StartNew(() =>
                {
                    db.updatePost(new Post(uploadId, category, title, date));
                    changeStatus(uploadId, status,"single");
                });

                Task.Factory.StartNew(() =>
                {
                    string postLoc = db.getPostLoc(new Post(uploadId));
                    if (System.IO.File.Exists(Server.MapPath(postLoc)))
                    {
                        System.IO.File.WriteAllText(Server.MapPath(postLoc), content);

                    }
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
            db.deletePosts(post);
            string postLoc = db.getPostLoc(post);

            Task.Factory.StartNew(() =>
            {
                DeleteFolders.getInstance(Server).deleteFile(Server.MapPath(postLoc));
            });
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
                        //foreach (int index in postsList)
                        //{

                        //}
                        //db.deletePosts(postsList);
                        break;
                    case "publishAll":
                        Parallel.ForEach(postsList, (index) =>
                        {
                            changeStatus(index, "Publish", "bulk");
                        });
                        //foreach (int index in postsList)
                        //{
                        //    changeStatus(index, "Publish");
                        //}
                        break;
                    case "draftAll":
                        Parallel.ForEach(postsList, (index) =>
                        {
                            changeStatus(index, "Draft","bulk");
                        });
                        //foreach (int index in postsList)
                        //{
                        //    changeStatus(index, "Draft");
                        //}
                        //db.changePostStatus(postsList, "Draft");
                        break;
                }
            });
            Task contentEditTask = Task.Factory.StartNew(() =>
            {
                contentEditor();
            });
            Task.WaitAll(new Task[] {bulkActionTask,contentEditTask });
            //ViewBag.Display = "none";
            return View("Posts");
        }

        [SessionListener]
        public ActionResult changeStatus(long postId, string status, string action)
        {
            DateTime date = DateTime.Now;
            DBConnect db = new DBConnect();
            Post post = new Post(postId, status,date);
            db.changePostStatus(post);
            //Task.Factory.StartNew((arg) =>
            //{
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

                if (!Directory.Exists(Server.MapPath(path)))
                {
                    Directory.CreateDirectory(Server.MapPath(path));
                }

                string newFilePath = path + filename;

                int count = 0;
                bool exists = false;
                do
                {
                    if (count == 0)
                    {
                        newFilePath = path + filename + ".txt";
                    }
                    else
                    {
                        newFilePath = path + filename + '_' + count + ".txt";
                    }
                    exists = System.IO.File.Exists(Server.MapPath(newFilePath));
                    if (!exists)
                    {
                        try
                        {
                            System.IO.File.Move(Server.MapPath(postLoc), Server.MapPath(newFilePath));
                            break;
                        }
                        catch (IOException ex)
                        {
                            exists = true;
                        }
                    }
                    count++;
                } while (exists);
                post.postLoc = newFilePath;
                db.changePostLoc(post);

            //},post);
            if (!action.Equals("bulk")) {
                Task postsTask = Task.Factory.StartNew(() =>
                {
                    Posts();
                });
                postsTask.Wait();
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
            string path = Server.MapPath(post.postLoc);
            if (System.IO.File.Exists(path))
            {
                post.postData = System.IO.File.ReadAllText(path);
            }
            ViewBag.post = post;
            return View("PostsAddNew");
        }

        [SessionListener]
        public ActionResult addCommonText(List<int> postsList, string content, string position)
        {
            Login login = (Login)Session["user"];
            DateTime date = DateTime.Now;
            Parallel.ForEach(postsList, (index) =>
            {
                DBConnect db = new DBConnect();
                Post post = new Post(index);
                post = db.getPostsById(login, post);
                if (post.postLoc!=null)
                {
                    string path = Server.MapPath(post.postLoc);
                    if (System.IO.File.Exists(path))
                    {
                        string pastContent = System.IO.File.ReadAllText(path);
                        if (position.Equals("top"))
                        {
                            pastContent = content + "<br><br>" + pastContent;
                        }
                        else
                        {
                            pastContent += "<br><br>"+content;
                        }
                        System.IO.File.WriteAllText(path, pastContent);
                        post.modifyDate = date;
                        db.updatePost(post);
                    }
                }
            });
            
            Task contentTask = Task.Factory.StartNew(() =>
            {
                contentEditor();
            });
            contentTask.Wait();
            return View("Posts");
        }
    }
}