using Shop.Models.Data;
using Shop.Models.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PagesController : Controller
    {
        // GET: Admin/Pages/Index
        public ActionResult Index()
        {
            //Объявляем список для представления
            List<PageVM> pageVMList;
            //Инициализируем список (DB)
            using (Db db = new Db())
            {
                pageVMList = db.Pages.ToArray().OrderBy(x => x.Sorting).Select(x => new PageVM(x)).ToList();
            }

            //возвращаем список в представление
            return View(pageVMList);
        }

        // GET: Admin/Pages/AddPage
        [HttpGet]
        public ActionResult AddPage()
        {
            PageVM pageVM = new PageVM();
            return View();
        }

        // POST: Admin/Pages/AddPage
        [HttpPost]
        public ActionResult AddPage(PageVM pageVM)
        {
            //проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                return View(pageVM);
            }

            using (Db db = new Db())
            {

                //объявляем переменную для краткого описания (slug)
                string slug;

                //Инициализируем класс PagesDTO
                PagesDTO pageDTO = new PagesDTO();

                //Присваиваем заголовок модели
                pageDTO.Title = pageVM.Title.ToUpper();

                //проверяем есть ли краткое описание, если нет - присваиваем его
                if (string.IsNullOrEmpty(pageVM.Slug))
                {
                    slug = pageVM.Title.Replace(" ", "-").ToLower();
                }
                else
                {
                    slug = pageVM.Slug.Replace(" ", "-").ToLower();
                }

                //Убеждаемся что заголовок и краткое описание уникальны
                if (db.Pages.Any(x => x.Title == pageVM.Title))
                {
                    ModelState.AddModelError("", "This title already exists");
                    return View(pageVM);
                }
                else if (db.Pages.Any(x => x.Slug == pageVM.Slug))
                {
                    ModelState.AddModelError("", "This slug already exists");
                    return View(pageVM);
                }

                //Присваиваем оставшиеся значения модели
                pageDTO.Slug = slug;
                pageDTO.Body = pageVM.Body;
                pageDTO.Sorting = 100;
                pageDTO.HasSidebar = pageVM.HasSidebar;

                //Сохраняем модель в базу данных
                db.Pages.Add(pageDTO);
                db.SaveChanges();

            }

            //Передаём сообщение через TempData
            TempData["SM"] = "You have added a new page";

            //Переадресовываем пользователя на метод Index
            return RedirectToAction("Index");
        }

        // GET: Admin/Pages/EditPage/id
        [HttpGet]
        public ActionResult EditPage(int id)
        {
            //Объявляем модель PageVM
            PageVM pageVM;

            using (Db db = new Db())
            {
                //Получаем страницу
                PagesDTO pageDTO = db.Pages.Find(id);

                //Проверяем доступность страницы
                if (pageDTO == null)
                {
                    return Content("The page does not exists.");
                }

                //Если страница доступна, инициализируем модель данными
                pageVM = new PageVM(pageDTO);
            }

            //Возвращаем модель в представление
            return View(pageVM);
        }

        //POST: Admin/Pages/EditPage
        [HttpPost]
        public ActionResult EditPage(PageVM pageVM)
        {
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                return View(pageVM);
            }

            using (Db db = new Db())
            {
                //Получаем id страницы
                int Id = pageVM.Id;

                //Объявляем переменную для краткого описания
                string slug = null;

                //Получаем страницу (по id)
                PagesDTO pageDTO = db.Pages.Find(Id);

                //Присваиваем название из полученной модели в DTO
                pageDTO.Title = pageVM.Title;

                //Проверяем краткое описание и присваиваем его если необходимо
                if (string.IsNullOrEmpty(pageVM.Slug))
                {
                    slug = pageVM.Title.Replace(" ", "-").ToLower();
                }
                else
                {
                    slug = pageVM.Slug.Replace(" ", "-").ToLower();
                }

                //Проверяем краткое описание и заголовок на уникальность
                if (db.Pages.Where(x => x.Id != Id).Any(x => x.Title == pageVM.Title))
                {
                    ModelState.AddModelError("", "This title already exists.");
                    return View(pageVM);
                }
                else if (db.Pages.Where(x => x.Id != Id).Any(x => x.Slug == pageVM.Slug))
                {
                    ModelState.AddModelError("", "This slug already exists.");
                    return View(pageVM);
                }

                //Присваиваем остальные данные в DTO
                pageDTO.Slug = slug;
                pageDTO.Body = pageVM.Body;
                pageDTO.HasSidebar = pageVM.HasSidebar;

                //Сохранить изменения в БД
                db.SaveChanges();

            }
            //Передаем сообщение TempData
            TempData["SM"] = "You have edited page.";

            //Возвращаем редирект на метод EditPage
            return RedirectToAction("EditPage");
        }

        // GET: Admin/Pages/PageDetails/id
        [HttpGet]
        public ActionResult PageDetails(int id)
        {
            //Объявляем модель PageVM
            PageVM pageVM;

            using (Db db = new Db())
            {
                //Получаем страницу
                PagesDTO pageDTO = db.Pages.Find(id);

                //Проверяем что модель доступна
                if (pageDTO == null)
                {
                    return Content("The page does not exists.");
                }

                //Присваиваем модели данные из БД
                pageVM = new PageVM(pageDTO);
            }

            //Возвращаем модель в представление
            return View(pageVM);
        }

        // GET: Admin/Pages/DeletePage
        [HttpGet]
        public ActionResult DeletePage(int id)
        {
            using (Db db = new Db())
            {
                //Получаем страницу
                PagesDTO pageDTO = db.Pages.Find(id);

                //Удаляем страницу
                db.Pages.Remove(pageDTO);

                //Сохраняем изменения в БД
                db.SaveChanges();
            }

            //Добавляем сообщение об успешном удалении
            TempData["SM"] = "You have deleted a page";

            //Переадресовываем пользователя на Index
            return RedirectToAction("Index");
        }

        [HttpPost]
        public void ReorderPages(int[] id)
        {
            using (Db db = new Db())
            {
                //Создаём счётчик
                int count = 1;

                //Инициализируем модель данных
                PagesDTO pageDTO;

                //Устанавливаем сортировку для каждой страницы
                foreach (var pageId in id)
                {
                    pageDTO = db.Pages.Find(pageId);
                    pageDTO.Sorting = count;

                    db.SaveChanges();

                    count++;
                }
            }
        }

        // GET: Admin/Pages/EditSidebar
        [HttpGet]
        public ActionResult EditSidebar()
        {
            //Объявляем модель
            SidebarVM sidebarVM;
            using (Db db = new Db())
            {
                //Получаем данные из DTO
                SidebarDTO sidebarDTO = db.Sidebars.Find(1);

                //Заполняем модель данными
                sidebarVM = new SidebarVM(sidebarDTO);
            }

            //Вернуть представление с моделью
            return View(sidebarVM);
        }

        // POST: Admins/Pages/EditSideBar
        [HttpPost]
        public ActionResult EditSidebar(SidebarVM sidebarVM)
        {
            using (Db db = new Db())
            {
                //Получаем данные из БД
                SidebarDTO sidebarDTO = db.Sidebars.Find(1);

                //Присваиваем данные в тело
                sidebarDTO.Body = sidebarVM.Body;

                //Сохраняем данные
                db.SaveChanges();
            }

            //Передаём сообщение в TempData
            TempData["SM"] = "You have edited a sidebar";

            //Переадресовываем пользователя
            return RedirectToAction("EditSidebar");
        }
    }
}