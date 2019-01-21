using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class Login
    {

        public string email { get; set; }
        public string pass { get; set; }
        public string cPass { get; set; }
        public string role { get; set; }
        public long webID { get; set; }
    }
}