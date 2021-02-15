using Shop.Models.Data;
using Shop.Models.ViewModels.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}