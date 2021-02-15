using Shop.Models.Data;
using Shop.Models.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Controllers
{
    public class PagesController : Controller
    {
        // GET: Index/{page}
        public ActionResult Index(string page = "")
        {
            // получаем slug
            if (page == "")
                page = "home";

            //объявляем модель и DTO
            PageVM model;
            PagesDTO dto;

            //проверяем доступна ли текущая страница
            using (Db db = new Db())
            {
                if (!db.Pages.Any(x => x.Slug.Equals(page)))
                    return RedirectToAction("Index", new { page = "" });
            }

            //получаем контекст данных
            using (Db db = new Db())
            {
                dto = db.Pages.Where(x => x.Slug == page).FirstOrDefault();
            }

            //устанавливаем заголовок страницы
            ViewBag.PageTitle = dto.Title;

            //Проверяем боковую панель
            if (dto.HasSidebar == true)
            {
                ViewBag.Sidebar = "Yes";
            }
            else
            {
                ViewBag.Sidebar = "No";
            }

            //Заполняем модель данными
            model = new PageVM(dto);

            //Возвращаем представление вместе с моделью
            return View(model);
        }

        public ActionResult PagesMenuPartial()
        {
            //инициализируем лист PageVM
            List<PageVM> pageVMList;

            //получаем все страницы кроме home
            using (Db db = new Db())
            {
                pageVMList = db.Pages.ToArray().OrderBy(x => x.Sorting).Where(x => x.Slug != "home")
                    .Select(x => new PageVM(x)).ToList();
            }

            //возвращаем частичное представление с листом данных
            return PartialView("_PagesMenuPartial", pageVMList);
        }

        public ActionResult SidebarPartial()
        {
            //Объявляем модель
            SidebarVM model;

            //Инициализируем модель
            using (Db db = new Db())
            {
                SidebarDTO dto = db.Sidebars.Find(1);

                model = new SidebarVM(dto);
            }

            //Возвращаем модель в частичное представление
            return PartialView("_SidebarPartial", model);
        }
    }
}