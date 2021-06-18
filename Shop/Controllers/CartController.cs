using Shop.Models.Data;
using Shop.Models.ViewModels.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace Shop.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult Index()
        {
            //Объяявляем лист CartVM
            var cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            //Проверяем, не пустая ли корзина
            if (cart.Count == 0 || Session["cart"] == null)
            {
                ViewBag.Message = "Your cart is empty";
                return View();
            }

            //Складываем сумму и записываем в ViewBag
            decimal total = 0m;
            foreach (var item in cart)
            {
                total += item.Total;
            }

            ViewBag.GrandTotal = total;

            //возвращаем лист в представление
            return View(cart);
        }

        public ActionResult CartPartial()
        {
            //Объявляем модель CartVM
            CartVM model = new CartVM();

            //Объявляем переменную количества
            int qty = 0;

            //Объявляем переменную цены
            decimal price = 0m;

            //Проверяем сессию
            if (Session["cart"] != null)
            {
                //получаем общее количество товаров и цену
                var list = (List<CartVM>)Session["cart"];

                foreach (var item in list)
                {
                    qty += item.Quantity;
                    price += item.Quantity * item.Price;
                }

                model.Quantity = qty;
                model.Price = price;
            }
            else
            {
                //или устанавливаем количество и цену 0
                model.Quantity = 0;
                model.Price = 0m;
            }

            //возвращаем частичное представление с моделью
            return PartialView("_CartPartial", model);
        }

        public ActionResult AddToCartPartial(int id)
        {
            //Объявляем лист CartVM
            List<CartVM> cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            //объявляем модель CartVM
            CartVM model = new CartVM();

            using (Db db = new Db())
            {
                //получаем продукт по id
                ProductDTO dto = db.Products.Find(id);

                //проверяем есть товар в корзине или нет
                var productInCart = cart.FirstOrDefault(x => x.ProductId == id);

                //если нет то добавляем товар
                if (productInCart == null)
                {
                    cart.Add(new CartVM()
                    {
                        ProductId = dto.Id,
                        ProductName = dto.Name,
                        Quantity = 1,
                        Price = dto.Price,
                        Image = dto.ImageName
                    });
                }

                //если находится, то добавляем ещё один
                else 
                {
                    productInCart.Quantity++;
                }

            }

            //получаем общее количество, цену и добавляем данные в модель
            int qty = 0;
            decimal price = 0m;
            foreach (var item in cart)
            {
                qty += item.Quantity;
                price += item.Quantity * item.Price;
            }

            model.Quantity = qty;
            model.Price = price;

            //сохраняем состояние корзины в сессию
            Session["cart"] = cart;

            //возвращаем частичное представление с моделью
            return PartialView("_AddToCartPartial", model);
        }

        public JsonResult IncrementProduct(int productId)
        {
            //Объявляем list cart
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //получаем cartvm из листа
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //добавляем количество
                model.Quantity++;

                //сохраняем данные
                var result = new { qty = model.Quantity, price = model.Price };

                //возвращаем json ответ с данными

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DecrementProduct(int productId)
        {
            //Объявляем list cart
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //получаем cartvm из листа
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //отнимаем количество
                if (model.Quantity > 1)
                {
                    model.Quantity--;
                }
                else
                {
                    model.Quantity = 0;
                    cart.Remove(model);
                }

                //сохраняем данные
                var result = new { qty = model.Quantity, price = model.Price };

                //возвращаем json ответ с данными

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public void RemoveProduct(int productId)
        {
            //Объявляем list cart
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //получаем cartvm из листа
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                cart.Remove(model);
            }
        }

        public ActionResult PaypalPartial()
        {
            //Получаем список товаров в корзине
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            //Возвращаем частичное представление с листом
            return PartialView(cart);
        }

        [HttpPost]
        public void PlaceOrder()
        {
            //получаем список товаров
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            //получаем имя пользователя
            string userName = User.Identity.Name;

            //Объявляем OrderID
            int orderId = 0;

            using (Db db = new Db())
            {
                //объявляем модель OrderDTO
                OrderDTO orderDto = new OrderDTO();

                //Получаем id пользователя
                var temp = db.Users.FirstOrDefault(x => x.Username == userName);
                int userId = temp.Id;

                //Заполняем модель данными и сохраняем
                orderDto.UserId = userId;
                orderDto.CreatedAt = DateTime.Now;

                db.Orders.Add(orderDto);
                db.SaveChanges();

                //Получаем OrderID
                orderId = orderDto.OrderId;

                //Объявляем модель OrderDetailsDTO
                OrderDetailsDTO orderDetailsDTO = new OrderDetailsDTO();

                //Добавляем в модель данные
                foreach(var item in cart)
                {
                    orderDetailsDTO.OrderId = orderId;
                    orderDetailsDTO.UserId = userId;
                    orderDetailsDTO.ProductId = item.ProductId;
                    orderDetailsDTO.Quantity = item.Quantity;

                    db.OrderDetails.Add(orderDetailsDTO);
                    db.SaveChanges();
                }
            }

            //Отправляем письмо о заказе на почту администратора 
            var client = new SmtpClient("smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("7b1105a6a211e6", "d92fd97b9f888d"),
                EnableSsl = true
            };
            client.Send("shop@example.com", "admin@example.com", "New Order", $"You have a new order. Order number: {orderId}");

            //обновляем сессию
            Session["cart"] = null;
        }
    }
}