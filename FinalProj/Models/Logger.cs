using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace FinalProj.Models
{
    
    public class Logger
    {
        private static Logger instance = new Logger();
        private static DateTime date;
        private Logger() { }
        public static Logger getInstance()
        {
            date = DateTime.Now;
            return instance;
        }

        public void setMessage(string message, string path)
        {
            StreamWriter sw = new StreamWriter(path, true);
            sw.WriteLine("["+date +"] : " +message);
            sw.Flush();
            sw.Close();
        }
    }
}