using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public static class WebSettings
    {
        public static object export_locker = new object();

        public static int exportProgress { get; set; }

        public static void setExportProgress(int count)
        {
            lock (export_locker)
            {
                exportProgress += count;
            }
        }
    }
}