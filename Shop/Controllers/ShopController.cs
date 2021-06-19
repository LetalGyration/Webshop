using PagedList;
using Shop.Models.Data;
using Shop.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Controllers
{
    public class ShopController : Controller
    {
        // GET: Shop
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Pages");
        }

        public ActionResult CategoryMenuPartial()
        {
            //объявляем модель ListCategoryVM
            List<CategoryVM> categoryVMList;

            //инициализируем модель
            using (Db db = new Db())
            {
                categoryVMList = db.Categories.ToArray().OrderBy(x => x.Sorting).Select(x => new CategoryVM(x)).ToList();
            }

            //возвращаем частичное представление с моделью
            return PartialView("_CategoryMenuPartial", categoryVMList);
        }

        public ActionResult Category(string name, string searchName)
        {
            //Объявляем список типа List
            List<ProductVM> productVMList;

            //Устанавливаем номер страницы
            using (Db db = new Db())
            {
                //Получаем id категории
                CategoryDTO categoryDTO = db.Categories.Where(x => x.Slug == name).FirstOrDefault();

                int catId = categoryDTO.Id;
                if (!string.IsNullOrEmpty(searchName))
                {

                    //Инициализируем список
                    productVMList = db.Products.ToArray().Where(x => x.CategoryId == catId && x.Name == searchName).Select(x => new ProductVM(x))
                        .ToList();
                }
                else 
                {
                    productVMList = db.Products.ToArray().Where(x => x.CategoryId == catId).Select(x => new ProductVM(x))
                        .ToList();
                }

                //Получаем имя категории
                var productCat = db.Products.Where(x => x.CategoryId == catId).FirstOrDefault();

                //Делаем проверку на null
                if (productCat == null)
                {
                    var catName = db.Categories.Where(x => x.Slug == name).Select(x => x.Name).FirstOrDefault();
                    ViewBag.CategoryName = catName;
                }
                else 
                {
                    ViewBag.CategoryName = productCat.CategoryName;
                }

            }

            //Возвращаем представление с моделью
            return View(productVMList);
        }

        [ActionName("product-details")]
        public ActionResult ProductDetails(string name)
        {
            //Объявляем dto и vm
            ProductDTO dto;
            ProductVM model;

            //Инициализируем id продукта
            int id = 0;

            using (Db db = new Db())
            {
                //проверяем доступность продукта
                if (!db.Products.Any(x => x.Slug.Equals(name)))
                {
                    return RedirectToAction("Index", "Shop");
                }

                //инициализируем модель dto данными
                dto = db.Products.Where(x => x.Slug == name).FirstOrDefault();

                //получаем id
                id = dto.Id;

                //инициализируем модель vm данными
                model = new ProductVM(dto);

            }

            //получаем изображение из галереи
            model.GalleryImages = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                .Select(fn => Path.GetFileName(fn));

            //возвращаем модель в представление
            return View("ProductDetails", model);
        }

    }
}