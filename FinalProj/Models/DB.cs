using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

//Add MySql Library
using MySql.Data.MySqlClient;

namespace FinalProj.Models
{
    class DBConnect
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        //Constructor
        public DBConnect()
        {
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            server = "localhost";
            database = "CMS";
            uid = "root";
            password = "Imesh@77";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        //open connection to database
        private bool OpenConnection()
        {
            connection.Open();
            return true;
        }

        //Close connection
        private bool CloseConnection()
        {
            connection.Close();
            return true;
        }
        
        //Login Query Start
        public void registerUser(Login login, long id)
        {
            String query = "Insert into login (userEmail,webID,pass,role) values ('" + login.email +
                "','" + id + "','" + login.pass + "','" + login.role + "')";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = query;
                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }

        }

        public Login checkUserReg(Login login)
        {
            String query = "Select * from Login where userEmail=@userEmail and pass=@pass";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userEmail", login.email);
                cmd.Parameters.AddWithValue("@pass", login.pass);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    login.role = dataReader["role"].ToString();
                    login.webID = long.Parse(dataReader["webID"].ToString());
                }
                this.CloseConnection();
                return login;
            }
            return login;

        }
        //Login Query End

        //Website Query Start
        public Login registerWebsite(Registration reg)
        {
            string query = "Insert into website " +
                "(webTitle,noOfPosts,thumbWidth,thumbHeight,mediumWidth,mediumHeight,largeWidth,largeHeight) " +
                "values (@webTitle,@noOfPosts,@thumbWidth,@thumbHeight,@mediumWidth,@mediumHeight,@largeWidth,@largeHeight)";

            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.Parameters.AddWithValue("@webTitle", reg.webTitle);
                cmd.Parameters.AddWithValue("@noOfPosts", 10);
                cmd.Parameters.AddWithValue("@thumbWidth",100);
                cmd.Parameters.AddWithValue("@thumbHeight", 100);
                cmd.Parameters.AddWithValue("@mediumWidth", 200);
                cmd.Parameters.AddWithValue("@mediumHeight", 200);
                cmd.Parameters.AddWithValue("@largeWidth", 300);
                cmd.Parameters.AddWithValue("@largeHeight", 300);

                cmd.CommandText = query;
                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
                long id = cmd.LastInsertedId;
                this.CloseConnection();
                registerUser(reg.user, id);
                reg.user.webID = id;
                return reg.user;
            }
            return reg.user;

        }

        public Website getWebsite(Login login)
        {
            String query = "select * from Website where webID = @webID";// + login.webID;
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@webID", login.webID);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                Website web = new Website();

                while (dataReader.Read())
                {
                    web.webID = Int32.Parse(dataReader["webID"].ToString());
                    web.webTitle = dataReader["webTitle"].ToString();
                    web.noOfPosts = Int32.Parse(dataReader["noOfPosts"].ToString());
                    web.thumbWidth = Int32.Parse(dataReader["thumbWidth"].ToString());
                    web.thumbHeight = Int32.Parse(dataReader["thumbHeight"].ToString());
                    web.mediumWidth = Int32.Parse(dataReader["mediumWidth"].ToString());
                    web.mediumHeight = Int32.Parse(dataReader["mediumHeight"].ToString());
                    web.largeWidth = Int32.Parse(dataReader["largeWidth"].ToString());
                    web.largeHeight = Int32.Parse(dataReader["largeHeight"].ToString());

                }
                this.CloseConnection();
                return web;

            }
            return null;
        }
        //Website Query End

        //Image Library Query Start
        public void uploadImages(List<ImageLibrary> images)
        {
            string query = "Insert into image_library " +
                "(webID,title,imgDesc,imgLoc,uploadDate,modifyDate) " +
                "values (@webID,@title,@imgDesc,@imgLoc,@uploadDate,@modifyDate)";

            foreach (ImageLibrary img in images) {
                if (this.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Parameters.AddWithValue("@webID", img.webID);
                    cmd.Parameters.AddWithValue("@title", img.title);
                    cmd.Parameters.AddWithValue("@imgDesc", img.imgDesc);
                    cmd.Parameters.AddWithValue("@imgLoc", img.imgLoc);
                    cmd.Parameters.AddWithValue("@uploadDate", img.uploadDate);
                    cmd.Parameters.AddWithValue("@modifyDate", img.modifyDate);

                    cmd.CommandText = query;
                    cmd.Connection = connection;
                    cmd.ExecuteNonQuery();
                    long id = cmd.LastInsertedId;
                    this.CloseConnection();
                }
            }
        }

        public string checkImageExists(ImageLibrary image)
        {
            string imgLoc = null;
            string query = "select * from Image_Library where imgLoc like @imgLoc";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@imgLoc",'%'+image.imgLoc+'%');
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    imgLoc = dataReader["imgLoc"].ToString();

                }
                this.CloseConnection();
                return imgLoc;

            }
            return imgLoc;
        } 

        public List<ImageLibrary> getImages(int startIndex, int endIndex)
        {
            List<ImageLibrary> images = new List<ImageLibrary>();
            string query = "select * from Image_Library limit @startIndex, @endIndex";
            if (this.OpenConnection()==true){
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@startIndex", startIndex);
                cmd.Parameters.AddWithValue("@endIndex", endIndex);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    ImageLibrary img = new ImageLibrary();
                    img.imageID = Int32.Parse(dataReader["imageID"].ToString());
                    img.title = dataReader["title"].ToString();
                    img.imgDesc = dataReader["imgDesc"].ToString();
                    img.imgLoc = dataReader["imgLoc"].ToString();
                    img.uploadDate = Convert.ToDateTime(dataReader["uploadDate"].ToString());
                    img.modifyDate = Convert.ToDateTime(dataReader["modifyDate"].ToString());
                    images.Add(img);
                }
                this.CloseConnection();
                return images;
            }
            return images;
        }

        public int getImageCount()
        {
            string query = "select count(*) from Image_Library";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                int count = int.Parse(cmd.ExecuteScalar().ToString());
                this.CloseConnection();
                return count;
            }
            return 0;
        }

        public string getImageLoc(ImageLibrary image)
        {
            String imageLoc = "";
            string query = "select imgLoc from image_library where imageID=@imageID";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@imageID", image.imageID);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    imageLoc = dataReader["imgLoc"].ToString();
                }
                this.CloseConnection();
            }
            return imageLoc;
        }

        public void deleteImage(ImageLibrary image)
        {
            string query = "delete from image_library where ImageID=@ImageID";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ImageID", image.imageID);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void updateImage(ImageLibrary image) {
            string query = "update image_library set title=@title, imgDesc=@imgDesc where ImageID=@ImageID";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand(query,connection);
                cmd.Parameters.AddWithValue("@title", image.title);
                cmd.Parameters.AddWithValue("@imgDesc", image.imgDesc);
                cmd.Parameters.AddWithValue("@ImageID", image.imageID);
                cmd.ExecuteNonQuery();
                
                this.CloseConnection();
            }
        }
        //Image Library Query End

        //Category Query Stat
        public void addCategory(Category cat)
        {
            string query = "Insert into Categories (webID,catTitle,catDesc) values (@webID,@catTitle,@catDesc)";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.Parameters.AddWithValue("@webID", cat.webID);
                cmd.Parameters.AddWithValue("@catTitle", cat.title);
                cmd.Parameters.AddWithValue("@catDesc", cat.desc);

                cmd.CommandText = query;
                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public List<Category> getCatList(int startIndex,int endIndex)
        {
            List<Category> categories = new List<Category>();
            string query = "select * from Categories limit @startIndex, @endIndex";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@startIndex", startIndex);
                cmd.Parameters.AddWithValue("@endIndex", endIndex);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    Category cat = new Category();
                    cat.catID = Int32.Parse(dataReader["catID"].ToString());
                    cat.title = dataReader["catTitle"].ToString();
                    cat.desc = dataReader["catDesc"].ToString();
                    categories.Add(cat);
                }
                this.CloseConnection();
                return categories;
            }
            return categories;
        }

        public int getCategoryCount()
        {
            string query = "select count(*) from Categories";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                int count = int.Parse(cmd.ExecuteScalar().ToString());
                this.CloseConnection();
                return count;
            }
            return 0;
        }

        public void deleteCategory(Category cat)
        {
            string query = "Delete from Categories where catID = @catID";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@catID", cat.catID);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void deleteCategory(List<int> catList)
        {
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand();
                string[] parameters = new string[catList.Count];
                for (int i = 0; i < catList.Count; i++)
                {
                    parameters[i] = string.Format("@catID{0}", i);
                    cmd.Parameters.AddWithValue(parameters[i], catList[i]);
                }
                cmd.CommandText = string.Format("Delete from categories where catID in ({0})", string.Join(", ", parameters));
                cmd.Connection = connection;
                
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }
        //Category Query End

        //Posts Query Start
        public long uploadPost(Post post)
        {
            string query = "Insert into posts (postTitle,postLoc,postStatus) value (@postTitle,@postLoc,@postStatus)";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postTitle",post.postTitle);
                cmd.Parameters.AddWithValue("@postLoc", post.postLoc);
                cmd.Parameters.AddWithValue("@postStatus", post.postStatus);

                cmd.ExecuteNonQuery();
                long id = cmd.LastInsertedId;
                post.postId = id;
                
                this.CloseConnection();
                uploadPostCat(post);
                return id;
            }
            return 0;
        }

        public int getPostsCount()
        {
            string query = "select count(*) from Posts";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                int count = int.Parse(cmd.ExecuteScalar().ToString());
                this.CloseConnection();
                return count;
            }
            return 0;
        }

        public List<Post> getPostList(int startIndex, int endIndex)
        {
            List<Post> posts = new List<Post>();
            string query = "select * from Posts as p inner join posts_categories as pc where p.postID=pc.postID" +
                " limit @startIndex, @endIndex";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@startIndex", startIndex);
                cmd.Parameters.AddWithValue("@endIndex", endIndex);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                while (dataReader.Read())
                {
                    Post post = new Post();
                    post.postId = Int32.Parse(dataReader["postID"].ToString());
                    post.catId = Int32.Parse(dataReader["catID"].ToString());
                    post.postTitle = dataReader["postTitle"].ToString();
                    post.postLoc = dataReader["postLoc"].ToString();
                    post.postStatus = dataReader["postStatus"].ToString();
                    post.createdDate = Convert.ToDateTime(dataReader["createdDate"].ToString());
                    post.modifyDate = Convert.ToDateTime(dataReader["modifyDate"].ToString());

                    posts.Add(post);
                }
                this.CloseConnection();
                return posts;
            }
            return posts;
        }

        public string getPostLoc(Post post)
        {
            String postLoc = "";
            string query = "select postLoc from posts where postID=@postID";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                
                while (dataReader.Read())
                {
                    postLoc = dataReader["postLoc"].ToString();
                }
                this.CloseConnection();
            }
            return postLoc;
        }
        public void deletePosts(Post post)
        {
            string query = "Delete from Posts where postID = @postID";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void deletePosts(List<int> postsList)
        {
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand();
                string[] parameters = new string[postsList.Count];
                for (int i = 0; i < postsList.Count; i++)
                {
                    parameters[i] = string.Format("@postID{0}", i);
                    cmd.Parameters.AddWithValue(parameters[i], postsList[i]);
                }
                cmd.CommandText = string.Format("Delete from posts where postID in ({0})", string.Join(", ", parameters));
                cmd.Connection = connection;

                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void changePostStatus(Post post)
        {
            string query = "Update posts set postStatus=@postStatus where postID=@postID";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postStatus", post.postStatus);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void changePostStatus(List<int> postsList,string status)
        {
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand();
                string[] parameters = new string[postsList.Count];
                for (int i = 0; i < postsList.Count; i++)
                {
                    parameters[i] = string.Format("@postID{0}", i);
                    cmd.Parameters.AddWithValue(parameters[i], postsList[i]);
                }
                cmd.Parameters.AddWithValue("@postStatus", status);
                cmd.CommandText = string.Format("Update posts set postStatus=@postStatus where postID in ({0})", string.Join(", ", parameters));
                cmd.Connection = connection;

                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void changePostLoc(Post post)
        {
            string query = "Update posts set postLoc=@postLoc where postID=@postID";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postLoc", post.postLoc);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void updatePost(Post post)
        {
            string query = "update posts set postTitle=@postTitle, postStatus=@postStatus where postID=@postID";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postTitle", post.postTitle);
                cmd.Parameters.AddWithValue("@postStatus", post.postStatus);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
                updatePostCat(post);
            }
        }
        //Posts Query End

        //Posts Category Query Start
        public void uploadPostCat(Post post)
        {
            string query = "Insert into posts_categories (postID,catID,webID,createdDate,modifyDate) " +
                "value(@postID,@catID,@webID,@createdDate,@modifyDate)";
            if (this.OpenConnection()==true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                cmd.Parameters.AddWithValue("@catID", post.catId);
                cmd.Parameters.AddWithValue("@webID", post.webId);
                cmd.Parameters.AddWithValue("@createdDate", post.createdDate);
                cmd.Parameters.AddWithValue("@modifyDate", post.modifyDate);

                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        public void updatePostCat(Post post)
        {
            string query = "Update posts_categories set catID=@catID, modifyDate=@modifyDate where postID=@postID";
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@postID", post.postId);
                cmd.Parameters.AddWithValue("@catID", post.catId);
                cmd.Parameters.AddWithValue("@modifyDate", post.modifyDate);

                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }
        //Posts Category Query End
    }
}