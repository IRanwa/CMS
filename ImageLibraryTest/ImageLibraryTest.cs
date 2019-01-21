using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using FinalProj.Controllers;
using FinalProj.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MvcContrib.TestHelper;

namespace FinalProjTest
{
    [TestClass]
    public class ImageLibraryTest
    {
        private void Login(TestControllerBuilder builder)
        {
            UserController user = new UserController();
            builder.InitializeController(user);

            Login login = new Login();
            login.email = "ranawakaimesh97@gmail.com";
            login.pass = "Imesh@77";
            login.webID = 1;
            ActionResult res = user.Index(login);
        }

        [TestMethod]
        public void ImageLibrary()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            ImageLibraryController controller = new ImageLibraryController();
            builder.InitializeController(controller);

            ViewResult result = controller.ImageLibrary() as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void changeLayout_Grid()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            ImageLibraryController controller = new ImageLibraryController();
            builder.InitializeController(controller);

            ViewResult result = controller.changeLayout("Grid") as ViewResult;

            Assert.AreEqual("Grid",result.ViewBag.layoutView);
        }

        [TestMethod]
        public void changeLayout_List()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            ImageLibraryController controller = new ImageLibraryController();
            builder.InitializeController(controller);

            ViewResult result = controller.changeLayout("List") as ViewResult;

            Assert.AreEqual("List", result.ViewBag.layoutView);
        }

        [TestMethod]
        public void LibraryAddNew_Test()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            ImageLibraryController controller = new ImageLibraryController();
            builder.InitializeController(controller);
            
            ViewResult result = controller.LibraryAddNew() as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void nextPage()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            ImageLibraryController controller = new ImageLibraryController();
            builder.InitializeController(controller);

            ViewResult result = controller.nextPage(2,"List") as ViewResult;

            Assert.AreEqual("List",result.ViewBag.layoutView);
            Assert.IsNotNull(result.ViewBag.LibraryProp);
            if (result.ViewBag.LibraryProp.totalImageCount>0)
            {
                Assert.IsNotNull(result.ViewBag.DisplayImages);
            }
            //Assert.AreEqual("ImageLibrary", result.View);
        }

        [TestMethod]
        public void imagePropChange()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            ImageLibraryController controller = new ImageLibraryController();
            builder.InitializeController(controller);

            ViewResult imageLibrary = controller.ImageLibrary() as ViewResult;

            if (imageLibrary.ViewBag.DisplayImages!=null)
            {
                List <ImageLibrary> images = imageLibrary.ViewBag.DisplayImages as List<ImageLibrary>;
                if (images.Count > 0)
                {
                    Random r = new Random();
                    int generatedValue = r.Next();

                    ImageLibrary img = images[0];
                    img.imgDesc = generatedValue.ToString();

                    ViewResult result = controller.imagePropChange(img,"Grid") as ViewResult;
                    imageLibrary = controller.ImageLibrary() as ViewResult;
                    images = imageLibrary.ViewBag.DisplayImages as List<ImageLibrary>;
                    if (images.Count>0) {
                        Assert.AreEqual(generatedValue.ToString(),images[0].imgDesc);
                    }
                }
            } 
        }
    }
}
