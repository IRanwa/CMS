using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class Category
    {
        public int catID { get; set; }
        public int webID { get; set; }
        public string title { get; set; }
        public string desc { get; set; }
        public int currentPage { get; set; }
        public int totalCategoryCount { get; set; }
        public int noOfPages { get; set; }
    }
}