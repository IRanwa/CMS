using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public interface PagerInterface<T>
    {

        void getContent(T prop, int noOfContent);

        void getTotalCount(int noOfContent);

        void reqNextPage(int nextPage, int noOfContent);
    }

    public class DisplayImageLibrary : PagerInterface<ImageLibrary>
    {
        private Login login;
        private dynamic viewBag;

        public DisplayImageLibrary(Login login, dynamic viewBag)
        {
            this.login = login;
            this.viewBag = viewBag;
        }

        public void getContent(ImageLibrary img, int noOfImage)
        {
            if (img.currentPage != 0)
            {
                int startIndex = (img.currentPage - 1) * noOfImage;
                DBConnect db = new DBConnect();
                List<ImageLibrary> images = db.getImages(startIndex, noOfImage, login);
                viewBag.DisplayImages = images;
                viewBag.LibraryProp = img;
            }
        }

        public void getTotalCount(int noOfImage)
        {
            DBConnect db = new DBConnect();
            int count = db.getImageCount(login);

            ImageLibrary img = new ImageLibrary();
            img.totalImageCount = count;
            img.noOfPages = Convert.ToInt32(Math.Ceiling(count / Double.Parse(noOfImage.ToString())));
            if (count > 0)
            {
                img.currentPage = 1;
            }
            getContent(img, noOfImage);
        }

        public void reqNextPage(int nextPage, int noOfImage)
        {
            DBConnect db = new DBConnect();
            int count = db.getImageCount(login);
            ImageLibrary img = new ImageLibrary();
            img.totalImageCount = count;
            img.noOfPages = Convert.ToInt32(Math.Ceiling(count / Double.Parse(noOfImage.ToString())));
            if (nextPage > img.noOfPages)
            {
                nextPage = img.noOfPages;
            }

            img.currentPage = nextPage;


            if (nextPage != 0)
            {
                int startIndex = (nextPage - 1) * noOfImage;
                List<ImageLibrary> images = db.getImages(startIndex, noOfImage, login);
                viewBag.DisplayImages = images;
            }
            viewBag.LibraryProp = img;
        }
    }
}