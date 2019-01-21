using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace FinalProj.Controllers
{
    public class FolderHandler
    {
        private static FolderHandler instance = new FolderHandler();
        private readonly object locker = new object();
        private FolderHandler() { }

        public static FolderHandler getInstance()
        {
            return instance;
        }

        public void deleteFolders(string mainPath)
        {
            if (Directory.Exists(mainPath)) {
                ParallelLoopResult p = Parallel.ForEach(Directory.GetDirectories(mainPath), path =>
                {
                    deleteFiles(path);
                    if (Directory.Exists(path) && Directory.GetFiles(path).Length == 0)
                    {
                        Directory.Delete(path);
                    }
                });
                if (p.IsCompleted && Directory.Exists(mainPath) && Directory.GetDirectories(mainPath).Length == 0) {
                    Directory.Delete(mainPath);
                }
            }
        }

        public void deleteFiles(string path)
        {
            if (Directory.Exists(path)) {
                ParallelLoopResult p = Parallel.ForEach(Directory.GetFiles(path), file =>
                {
                    deleteFile(file);
                });
                if (p.IsCompleted && Directory.GetFiles(path).Length==0)
                {
                    Directory.Delete(path);
                }
            }
        }

        public void deleteFile(string file)
        {
           
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }
            
        }

        public void createDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public string generateNewFileName(string path, string filename, string extension, HttpServerUtilityBase server)
        {
            string newFilePath;
            int count = 0;
            bool exists = false;
            do
            {
                if (count == 0)
                {
                    newFilePath = path + filename + extension;
                }
                else
                {
                    newFilePath = path + filename + '_' + count + extension;
                }
                exists = System.IO.File.Exists(server.MapPath(newFilePath));
                count++;
            } while (exists);
            return newFilePath;
        }

        public string getFileName(string path,string extension)
        {
            return path.Split('/').Last().Replace(extension, "");
        }

        public void moveFile(string prevPath, string newPath)
        {
            if (System.IO.File.Exists(prevPath)) {
                System.IO.File.Move(prevPath, newPath);
            }
        }

        public void copyFile(string prevPath, string newPath)
        {
            File.Copy(prevPath, newPath);
        }

        public string readFileText(string path)
        {
            if (System.IO.File.Exists(path))
            {
                return System.IO.File.ReadAllText(path);
            }
            return null;
        }

        public void writeToFile(string path, string content)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(path, content);
            }
        }

        public void writeToNewFile(string path, string content)
        {
            lock (locker) {
                System.IO.File.WriteAllText(path, content);
            }
        }
        
    }
}