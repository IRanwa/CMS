using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class Post
    {
        public long postId { get; set; }
        public int catId { get; set; }
        public long webId { get; set; }
        public string postTitle { get; set; }
        public string postData { get; set; }
        public string postLoc { get; set; }
        public string postStatus { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime modifyDate { get; set; }
        public int currentPage { get; set; }
        public int totalCategoryCount { get; set; }
        public int noOfPages { get; set; }

        public Post()
        {
        }

        public Post(long postId)
        {
            this.postId = postId;
        }

        public Post(long postId, string postStatus) : this(postId)
        {
            this.postStatus = postStatus;
        }

        public Post(int catId, int webId, string postTitle, string postLoc, string postStatus, DateTime createdDate, DateTime modifyDate)
        {
            this.catId = catId;
            this.webId = webId;
            this.postTitle = postTitle;
            this.postLoc = postLoc;
            this.postStatus = postStatus;
            this.createdDate = createdDate;
            this.modifyDate = modifyDate;
        }

        public Post(long postId, int catId, string postTitle, string postStatus, DateTime modifyDate) : this(postId)
        {
            this.catId = catId;
            this.postTitle = postTitle;
            this.postStatus = postStatus;
            this.modifyDate = modifyDate;
        }
    }
}