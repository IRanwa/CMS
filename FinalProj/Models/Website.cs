using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class Website
    {
        public int webID { get; set; }
        public string webTitle { get; set; }
        public int noOfPosts { get; set; }
        public int thumbWidth { get; set; }
        public int thumbHeight { get; set; }
        public int mediumWidth { get; set; }
        public int mediumHeight { get; set; }
        public int largeWidth { get; set; }
        public int largeHeight { get; set; }
        public string featuredImage { get; set; }

    }
}