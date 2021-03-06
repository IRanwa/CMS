﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class Category
    {
        public int catID { get; set; }
        public long webID { get; set; }
        public string title { get; set; }
        public string desc { get; set; }
        public int currentPage { get; set; }
        public int totalCategoryCount { get; set; }
        public int noOfPages { get; set; }

        public Category()
        {
        }

        public Category(int catID)
        {
            this.catID = catID;
        }

        public Category(string title)
        {
            this.title = title;
        }

        public Category(int catID, string title, string desc) : this(catID)
        {
            this.title = title;
            this.desc = desc;
        }
    }
}