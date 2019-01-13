using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public interface DisplayImageInterface
    {

        void displayImages(ImageLibrary img, int noOfImage);

        void getTotalImageCount(int noOfImage);

        void nextPage(int nextPage, int noOfImage);
    }

    public class DisplayImageLibrary : DisplayImageInterface
    {
        private Login login;
        private dynamic viewBag;

        public DisplayImageLibrary(Login login, dynamic viewBag)
        {
            this.login = login;
            this.viewBag = viewBag;
        }

        public void displayImages(ImageLibrary img, int noOfImage)
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

        public void getTotalImageCount(int noOfImage)
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
            displayImages(img, noOfImage);
        }

        public void nextPage(int nextPage, int noOfImage)
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