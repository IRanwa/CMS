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
        public int webId { get; set; }
        public string postTitle { get; set; }
        public string postLoc { get; set; }
        public string postStatus { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime modifyDate { get; set; }

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
    }
}