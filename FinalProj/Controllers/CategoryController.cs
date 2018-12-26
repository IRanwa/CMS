using FinalProj.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FinalProj.Controllers
{
    public class CategoryController : Controller
    {
        //public ActionResult Category()
        //{
        //    DBConnect db = new DBConnect();
        //    db.getCatList(0,20)
        //    ViewBag.Display = "none";
        //    return View();
        //}
        public ActionResult Category()
        {
            ViewBag.Display = "none";
            Category cat = getTotalCategoryCount();
            displayCategories(cat);
            return View();
        }

        private void displayCategories(Category cat)
        {
            if (cat.currentPage != 0)
            {
                int startIndex = (cat.currentPage - 1) * 20;
                DBConnect db = new DBConnect();
                List<Category> categories = db.getCatList(startIndex, startIndex + 20);
                ViewBag.DisplayCategories = categories;
                ViewBag.CategoryProp = cat;
            }
        }

        private Category getTotalCategoryCount()
        {
            DBConnect db = new DBConnect();
            int count = db.getCategoryCount();

            Category cat = new Category();
            cat.totalCategoryCount = count;
            cat.noOfPages = Convert.ToInt32(Math.Ceiling(count / 20.0));
            if (count > 0)
            {
                cat.currentPage = 1;
            }
            return cat;
        }

        public ActionResult nextPage(int nextPage, int currentPage)
        {
            if (nextPage > currentPage)
            {
                currentPage++;
            }
            else
            {
                currentPage--;
            }
            DBConnect db = new DBConnect();
            int count = db.getCategoryCount();
            Category cat = new Category();
            cat.totalCategoryCount = count;
            cat.noOfPages = Convert.ToInt32(Math.Ceiling(count / 20.0));
            if (currentPage > cat.noOfPages)
            {
                currentPage = cat.noOfPages;
            }

            cat.currentPage = currentPage;


            if (currentPage != 0)
            {
                int startIndex = (currentPage - 1) * 20;
                List<Category> categories = db.getCatList(startIndex, startIndex + 20);
                ViewBag.DisplayCategories = categories;
                ViewBag.CategoryProp = cat;
            }
            ViewBag.Display = "none";
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

            Website web = db.getWebsite((Login)Session["user"]);
            category.webID = web.webID;

            db.addCategory(category);
            ViewBag.Display = "Block";
            ViewBag.Message = "Category Added Successfully!";
            ModelState.Clear();
            return View();
        }
    }
}