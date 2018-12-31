using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class ImageLibrary
    {
        public HttpPostedFileBase[] files { get; set; }
        public int imageID { get; set; }
        public int webID{ get; set; }
        public string title { get; set; }
        public string imgDesc { get; set; }
        public string imgLoc { get; set; }
        public DateTime uploadDate { get; set; }
        public DateTime modifyDate { get; set; }
        public int noOfPages { get; set; }
        public int currentPage { get; set; }
        public int totalImageCount { get; set; }

        public ImageLibrary()
        {
        }

        public ImageLibrary(int imageID)
        {
            this.imageID = imageID;
        }

        public ImageLibrary(string imgLoc)
        {
            this.imgLoc = imgLoc;
        }

        public ImageLibrary(int webID, string title, string imgDesc, string imgLoc, DateTime uploadDate, DateTime modifyDate)
        {
            this.webID = webID;
            this.title = title;
            this.imgDesc = imgDesc;
            this.imgLoc = imgLoc;
            this.uploadDate = uploadDate;
            this.modifyDate = modifyDate;
        }
    }
}