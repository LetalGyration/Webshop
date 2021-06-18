using Shop.Models.Data;
using Shop.Models.ViewModels.Account;
using Shop.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Shop.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        [ActionName("create-account")]
        [HttpGet]
        public ActionResult CreateAccount()
        {
            return View("CreateAccount");
        }

        [ActionName("create-account")]
        [HttpPost]
        public ActionResult CreateAccount(UserVM model)
        {
            //проверяем на валидность модель
            if (!ModelState.IsValid)
                return View("CreateAccount", model);

            //проверяем соответствие пароля
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Password do not match!");
                return View("CreateAccount", model);
            }

            using (Db db = new Db())
            {
                //проверяем имя на уникальность
                if (db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", $"Username {model.Username} is taken!");
                    model.Username = "";
                    return View("CreateAccount", model);
                }

                //создаём экземпляр контекста данных
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAdress = model.EmailAdress,
                    Username = model.Username,
                    Password = model.Password
                };

                //добавляем все данные в модель
                db.Users.Add(userDTO);

                //сохраняем данные
                db.SaveChanges();

                //добавляем роль пользователю
                int id = userDTO.Id;

                UserRoleDTO userRoleDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2
                };

                db.UserRoles.Add(userRoleDTO);
                db.SaveChanges();

            }
            //Записываем сообщение в TempData
            TempData["SM"] = "You are now registered!";

            //Переадресовываем пользователя
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Login()
        {
            //подтвердить, что пользователь не авторизован
            string userName = User.Identity.Name;
            if (!string.IsNullOrEmpty(userName))
                return RedirectToAction("user-profile");

            //возвращаем представление
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
                return View(model);

            //проверяем пользователя на валидность
            bool isValid = false;
            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                    isValid = true;

                if (!isValid)
                {
                    ModelState.AddModelError("", "Invalid username or password!");
                    return View(model);
                }
                else
                {
                    FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                    return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
                }
            }

        }

        
        [HttpGet]
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            //получаем имя пользователя
            string userName = User.Identity.Name;
            //объявляем модель
            UserNavPartialVM model;

            using (Db db = new Db())
            {
                //получаем пользователя
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == userName);
                //заполняем модель данными из контекста
                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };

            }
            //возвращаем частичное представление с моделью
            return PartialView(model);
        }


        //GET: /account/user-profile
        [HttpGet]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile()
        {
            //получаем имя пользователя
            string userName = User.Identity.Name;

            //объявляем модель
            UserProfileVM model;

            using (Db db = new Db())
            {
                //получаем пользователя
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == userName);

                //инициализируем модель данными
                model = new UserProfileVM(dto);
            }

            //возвращаем модель в представление
            return View("UserProfile", model);
        }

        //POST: /account/user-profile
        [HttpPost]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile(UserProfileVM model)
        {
            bool IsUserNameChanged = false;
            //Проверяем модель на валидность
            if(!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            //Проверяем пароль при смене пароля
            if(!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Passwords does not match!");
                    return View("UserProfile", model);
                }
            }

            using (Db db = new Db())
            {
                //Получаем имя пользователя
                string userName = User.Identity.Name;

                //Проверяем, сменилось ли имя пользователя
                if(userName != model.Username)
                {
                    userName = model.Username;
                    IsUserNameChanged = true;
                }


                //Проверяем имя на уникальность
                if(db.Users.Where(x => x.Id != model.Id).Any(x => x.Username == userName))
                {
                    ModelState.AddModelError("", $"Username {model.Username} already exists!");
                    model.Username = "";
                    return View("UserProfile", model);
                }
                //Изменяем контекст данных
                UserDTO dto = db.Users.Find(model.Id);

                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAdress = model.EmailAdress;
                dto.Username = model.Username;

                if(!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }
                //Сохраняем изменения
                db.SaveChanges();
            }
            //Устанавливаем сообщение в TempData
            TempData["SM"] = "Profile edited!";

            //Возвращаем представление с моделью
            if (!IsUserNameChanged)
                return View("UserProfile", model);
            else
                return RedirectToAction("Logout");
        }

        [HttpGet]
        [Authorize(Roles = "User")]
        public ActionResult Orders()
        {
            //Инициализируем модель OrdersForUserVM
            List<OrdersForUserVM> ordersForUser = new List<OrdersForUserVM>();

            using (Db db = new Db())
            {
                //Получаем Id пользователя
                UserDTO user = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                int userId = user.Id;

                //Инициализируем OrderVM
                List<OrderVM> orders = db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderVM(x)).ToList();

                //Перебираем список товаров в OrderVM
                foreach (var order in orders)
                {
                    //Инициализируем словарь товаров
                    Dictionary<string, int> productsAndQuantity = new Dictionary<string, int>();

                    //Объявляем переменную суммы
                    decimal total = 0m;

                    //Инициализируем модель OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetails = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //Перебираем список OrderDetailsDTO
                    foreach (var item in orderDetails)
                    {
                        //Получаем товар
                        ProductDTO product = db.Products.FirstOrDefault(x => x.Id == item.ProductId);

                        //Получаем цену товара
                        decimal price = product.Price;

                        //Получаем имя товара
                        string productName = product.Name;

                        //Добавляем товар в словарь
                        productsAndQuantity.Add(productName, item.Quantity);

                        //Получаем конечную стоимость товара
                        total += item.Quantity * price;
                    }
                    //Добавляем данные в модель OrdersForUserVM
                    ordersForUser.Add(new OrdersForUserVM()
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        ProductsAndQuantity = productsAndQuantity,
                        CreatedAt = order.CreatedAt
                    });
                }

            }

            //Возвращаем представление с моделью OrdersForUserVM
            return View(ordersForUser);
        }
    }
}