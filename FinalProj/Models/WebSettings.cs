using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FinalProj.Models
{
    public class WebSettings
    {
        public object export_locker = new object();
        public object import_locker = new object();

        public int exportProgress { get; set; }
        public int importProgress { get; set; }

        public void setExportProgress(int count)
        {
            lock (export_locker)
            {
                exportProgress += count;
            }
        }

        public void setImportProgress(int count)
        {
            lock (import_locker)
            {
                importProgress += count;
            }
        }
    }
}