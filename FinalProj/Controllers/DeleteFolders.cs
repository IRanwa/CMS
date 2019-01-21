using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace FinalProj.Controllers
{
    public class DeleteFolders
    {
        private static HttpServerUtilityBase server;
        private static DeleteFolders instance = new DeleteFolders();

        private DeleteFolders() { }

        public static DeleteFolders getInstance(HttpServerUtilityBase currentServer)
        {
            server = currentServer;
            return instance;
        }

        public void deleteFolders(string mainPath)
        {
            ParallelLoopResult p = Parallel.ForEach(Directory.GetDirectories(server.MapPath(mainPath)), path =>
            {
                deleteFiles(path);
                Directory.Delete(path);
            });
            if (p.IsCompleted) {
                Directory.Delete(server.MapPath(mainPath));
            }
        }

        private void deleteFiles(string path)
        {
            Parallel.ForEach(Directory.GetFiles(path), file =>
            {
                deleteFile(file);
            });
        }

        public void deleteFile(string file)
        {
            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }
        }
    }
}