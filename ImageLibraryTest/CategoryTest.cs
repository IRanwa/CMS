using FinalProj.Controllers;
using FinalProj.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcContrib.TestHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ImageLibraryTest
{
    [TestClass]
    public class CategoryTest
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
        public void Category()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            CategoryController controller = new CategoryController();
            builder.InitializeController(controller);

            ViewResult result = controller.Category() as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void nextPage()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            CategoryController controller = new CategoryController();
            builder.InitializeController(controller);

            ViewResult result = controller.Category() as ViewResult;

            Category cat = result.ViewBag.CategoryProp as Category;
            if (cat!=null && cat.totalCategoryCount>0)
            {
                result = controller.nextPage(1) as ViewResult;
                Assert.IsNotNull(result.ViewBag.DisplayCategories);
                Assert.IsNotNull(result);
            }
            
        }

        [TestMethod]
        public void CategoryAddNew_Test1()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            CategoryController controller = new CategoryController();
            builder.InitializeController(controller);

            ViewResult result = controller.CategoryAddNew() as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void CategoryAddNew_Test2()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            CategoryController controller = new CategoryController();
            builder.InitializeController(controller);

            Category category = new Category();
            category.title = "New Category";
            category.desc = "New Category";

            ViewResult result = controller.CategoryAddNew(category) as ViewResult;
            result = controller.Category() as ViewResult;

            List<Category> catList = result.ViewBag.DisplayCategories as List<Category>;

            if (catList.Count>0)
            {
                Assert.IsNotNull(catList);

                foreach (Category cat in catList) {
                    if (cat.title.Equals(category.title)) {
                        Assert.AreEqual(category.title, cat.title);
                        Assert.AreEqual(category.desc, cat.desc);
                    }
                }
            }
        }

        [TestMethod]
        public void deleteCategory()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            CategoryController controller = new CategoryController();
            builder.InitializeController(controller);

            ViewResult result = controller.Category() as ViewResult;
            List<Category> catList = result.ViewBag.DisplayCategories as List<Category>;

            if (catList.Count > 0)
            {
                Assert.IsNotNull(catList);
                int tempCatID = catList[0].catID;

                result = controller.deleteCategory(tempCatID) as ViewResult;
                result = controller.Category() as ViewResult;

                catList = result.ViewBag.DisplayCategories as List<Category>;
                if (catList.Count > 0)
                {
                    foreach (Category temp in catList)
                    {
                        Assert.AreNotEqual(tempCatID, temp.catID);
                    }
                }
            }
        }

        [TestMethod]
        public void deleteCategories()
        {
            TestControllerBuilder builder = new TestControllerBuilder();
            Login(builder);

            CategoryController controller = new CategoryController();
            builder.InitializeController(controller);

            ViewResult result = controller.Category() as ViewResult;
            List<Category> catList = result.ViewBag.DisplayCategories as List<Category>;

            if (catList.Count > 2)
            {
                Assert.IsNotNull(catList);
                int tempCatID1 = catList[0].catID;
                int tempCatID2 = catList[1].catID;

                result = controller.deleteAllCategories(new List<int> { tempCatID1,tempCatID2}) as ViewResult;
                result = controller.Category() as ViewResult;

                catList = result.ViewBag.DisplayCategories as List<Category>;
                if (catList.Count > 0)
                {
                    foreach (Category temp in catList)
                    {
                        Assert.AreNotEqual(tempCatID1, temp.catID);
                        Assert.AreNotEqual(tempCatID2, temp.catID);
                    }
                }
            }
        }
    }
}
