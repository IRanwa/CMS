using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class CategoryController : Controller, PagerInterface<Category>
    {
        private const int NO_OF_CATEGORY = 20;
        public ActionResult Category()
        {
            ViewBag.Display = "none";
            getTotalCount(NO_OF_CATEGORY);
            return View();
        }
        public void getContent(Category cat, int noOfImage)
        {
            if (cat.currentPage != 0)
            {
                int startIndex = (cat.currentPage - 1) * noOfImage;
                DBConnect db = new DBConnect();
                Login login = (Login)Session["user"];
                List<Category> categories = db.getCatList(startIndex, noOfImage, login);
                if (categories.Count > 1)
                {
                    categories.RemoveAt(0);
                    ViewBag.DisplayCategories = categories;
                }
                ViewBag.CategoryProp = cat;
            }
        }

        public void getTotalCount(int noOfImage)
        {
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            int count = db.getCategoryCount(login);

            Category cat = new Category();
            cat.totalCategoryCount = count;
            cat.noOfPages = Convert.ToInt32(Math.Ceiling(count / Double.Parse(noOfImage.ToString())));
            if (count > 0)
            {
                cat.currentPage = 1;
            }
            getContent(cat, noOfImage);
        }

        public void reqNextPage(int nextPage, int noOfImage)
        {
            Login login = (Login)Session["user"];
            DBConnect db = new DBConnect();
            int count = db.getCategoryCount(login);
            Category cat = new Category();
            cat.totalCategoryCount = count;
            cat.noOfPages = Convert.ToInt32(Math.Ceiling(count / Double.Parse(NO_OF_CATEGORY.ToString())));
            if (nextPage > cat.noOfPages)
            {
                nextPage = cat.noOfPages;
            }

            cat.currentPage = nextPage;


            if (nextPage != 0)
            {
                int startIndex = (nextPage - 1) * NO_OF_CATEGORY;
                List<Category> categories = db.getCatList(startIndex, NO_OF_CATEGORY, login);
                ViewBag.DisplayCategories = categories;
                ViewBag.CategoryProp = cat;
            }
            ViewBag.Display = "none";
        }

        public ActionResult nextPage(int nextPage)
        {
            reqNextPage(nextPage,NO_OF_CATEGORY);
            return View("Category");
        }

        public ActionResult CategoryAddNew()
        {
            ViewBag.Display = "none";
            return View();
        }

        [HttpPost]
        public ActionResult CategoryAddNew(Category category )
        {
            DBConnect db = new DBConnect();
            Login login = (Login)Session["user"];
            Website web = db.getWebsite(login);
            category.webID = web.webID;
            category = db.checkCategoryAvailable(category, login);
            if (category.catID==0) {
                db.addCategory(category);
                ViewBag.Display = "Block";
                ViewBag.Message = "Category Added Successfully!";
            }
            else
            {
                ViewBag.Display = "Block";
                ViewBag.Message = "Category already exists!";
            }
            ModelState.Clear();
            return View();
        }

        public ActionResult deleteCategory(int catID)
        {
            DBConnect db = new DBConnect();
            db.deleteCategory(new Category(catID));
            Category();
            return View("Category");
        }

        public ActionResult deleteAllCategories(List<int> catList )
        {
            DBConnect db = new DBConnect();
            db.deleteCategory(catList);
            return View("Category");
        }
    }
}