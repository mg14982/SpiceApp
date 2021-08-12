using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModel;
using Spice.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private int PageSize = 2;
        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public OrderDetailsCart orderDetailsCart { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //create viewmodel object
            orderDetailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            //set order total to 0
            orderDetailsCart.OrderHeader.OrderTotal = 0;
            //Get the user id frm claims identity
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            //Get the shopping cart list by user id
            var shoppingCart = _db.ShoppingCartModel.Where(c => c.ApplicationUserId == claims.Value);

            if (shoppingCart != null)
            {
                orderDetailsCart.ListCart = shoppingCart.ToList();
            }
            //calculate the total amount
            foreach(var list in orderDetailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItem.Where(m => m.Id == list.MenuItemId).FirstOrDefaultAsync();
                orderDetailsCart.OrderHeader.OrderTotal = orderDetailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
                //convert description to raw html format
                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);
                if (list.MenuItem.Description.Length > 100)
                {
                    list.MenuItem.Description = list.MenuItem.Description.Substring(0, 99) + "...";
                }
            }
            //set the order total original as order total since we are not using coupon code
            orderDetailsCart.OrderHeader.OrderTotalOriginal = orderDetailsCart.OrderHeader.OrderTotal;
            //get the coupon code from session
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                orderDetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
            }
            //get the coupon object by coupon code from database
            if (!string.IsNullOrEmpty(orderDetailsCart.OrderHeader.CouponCode))
            {
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower().Trim() == orderDetailsCart.OrderHeader.CouponCode.ToLower().Trim()).FirstOrDefaultAsync();
                orderDetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, orderDetailsCart.OrderHeader.OrderTotalOriginal);
            }
            return View(orderDetailsCart);
        }

        /// <summary>
        /// Dispay the Cart Summary page 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Summary()
        {
            //create viewmodel object
            orderDetailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            //set order total to 0
            orderDetailsCart.OrderHeader.OrderTotal = 0;
            //Get the user id frm claims identity
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser = await _db.ApplicationUser.Where(x => x.Id == claims.Value).FirstOrDefaultAsync();
            //Get the shopping cart list by user id
            var shoppingCart = _db.ShoppingCartModel.Where(c => c.ApplicationUserId == claims.Value);

            if (shoppingCart != null)
            {
                orderDetailsCart.ListCart = shoppingCart.ToList();
            }
            //calculate the total amount
            foreach (var list in orderDetailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItem.Where(m => m.Id == list.MenuItemId).FirstOrDefaultAsync();
                orderDetailsCart.OrderHeader.OrderTotal = orderDetailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
            }
            //set the order total original as order total since we are not using coupon code
            orderDetailsCart.OrderHeader.OrderTotalOriginal = orderDetailsCart.OrderHeader.OrderTotal;
            orderDetailsCart.OrderHeader.PickupaName = applicationUser.FirstName + " " + applicationUser.LastName;
            orderDetailsCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            //get the coupon code from session
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                orderDetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
            }
            //get the coupon object by coupon code from database
            if (!string.IsNullOrEmpty(orderDetailsCart.OrderHeader.CouponCode))
            {
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower().Trim() == orderDetailsCart.OrderHeader.CouponCode.ToLower().Trim()).FirstOrDefaultAsync();
                orderDetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, orderDetailsCart.OrderHeader.OrderTotalOriginal);
            }
            return View(orderDetailsCart);
        }

        /// <summary>
        /// Post the Cart Summary Details 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {
            //Get the user id frm claims identity
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //Get the shoppling list from db on the basis of user id
            orderDetailsCart.ListCart = await _db.ShoppingCartModel.Where(s => s.ApplicationUserId == claims.Value).ToListAsync();

            //set the order header 
            orderDetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            orderDetailsCart.OrderHeader.OrderDate = DateTime.Now;
            orderDetailsCart.OrderHeader.UserId = claims.Value;
            orderDetailsCart.OrderHeader.Status = SD.PaymentStatusPending;
            orderDetailsCart.OrderHeader.PickupTime = Convert.ToDateTime(orderDetailsCart.OrderHeader.PickupDate.ToShortDateString()+" "+orderDetailsCart.OrderHeader.PickupTime.ToShortTimeString());

            //create list of order details objects
            List<OrderDetails> orderDetailsList = new List<OrderDetails>();

            //add order header into db
            _db.OrderHeader.Add(orderDetailsCart.OrderHeader);
            await _db.SaveChangesAsync();

            //interate the order details list and add into db
            foreach (var item in orderDetailsCart.ListCart)
            {
                item.MenuItem = await _db.MenuItem.Where(m => m.Id == item.MenuItemId).FirstOrDefaultAsync();
                OrderDetails orderDetails = new OrderDetails()
                {
                    MenuItemId = item.MenuItemId,
                    Description = item.MenuItem.Description,
                    OrderId = orderDetailsCart.OrderHeader.Id,
                    Count = item.Count,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price
                };
                orderDetailsCart.OrderHeader.OrderTotalOriginal += item.Count * item.MenuItem.Price;
                _db.OrderDetails.Add(orderDetails);
            }

            //get the coupon code from session
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                orderDetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDB = await _db.Coupon.Where(c => c.Name.ToLower() == orderDetailsCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                orderDetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDB, orderDetailsCart.OrderHeader.OrderTotalOriginal);
            }

            //get the coupon object by coupon code from database
            if (!string.IsNullOrEmpty(orderDetailsCart.OrderHeader.CouponCode))
            {
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower().Trim() == orderDetailsCart.OrderHeader.CouponCode.ToLower().Trim()).FirstOrDefaultAsync();
                orderDetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, orderDetailsCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                orderDetailsCart.OrderHeader.OrderTotal = orderDetailsCart.OrderHeader.OrderTotalOriginal;
            }
            //get the coupon code discount amount
            orderDetailsCart.OrderHeader.CouponCodeDiscount = orderDetailsCart.OrderHeader.OrderTotalOriginal - orderDetailsCart.OrderHeader.OrderTotal;
            //remove the shopping cart from db and from session
            _db.ShoppingCartModel.RemoveRange(orderDetailsCart.ListCart);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();
            //Stripe payment 
            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(orderDetailsCart.OrderHeader.OrderTotal * 100),
                Currency = "inr",
                Description = "Order ID : " + orderDetailsCart.OrderHeader.Id,
                Source = stripeToken
            };
            //Create the stripe service object
            var service = new ChargeService();
            Charge charge = service.Create(options);
            if (charge.BalanceTransactionId == null)
            {
                orderDetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                
            }
            else
            {
                orderDetailsCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
                
            }
            //Check the payment status
            if (charge.Status.ToLower() == "succeeded")
            {
                orderDetailsCart.OrderHeader.Status = SD.OrderStatusSubmitted;
                orderDetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusApprove;
            }
            else
            {
                orderDetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            await _db.SaveChangesAsync();
            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm","Order", new { id = orderDetailsCart.OrderHeader.Id });
        }

        //Add Coupon Code into Shopping Cart
        public IActionResult AddCoupon()
        {
            if (orderDetailsCart == null)
            {
                orderDetailsCart.OrderHeader.CouponCode = string.Empty;
            }

            HttpContext.Session.SetString(SD.ssCouponCode, orderDetailsCart.OrderHeader.CouponCode);

            return RedirectToAction(nameof(Index));
        }

        //Add Coupon Code into Shopping Cart
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        //Add item quantity when click on add button
        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCartModel.Where(c => c.Id == cartId).FirstOrDefaultAsync();
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //Minus the item quantity
        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCartModel.Where(c => c.Id == cartId).FirstOrDefaultAsync();
            if (cart.Count == 1)
            {
                _db.ShoppingCartModel.Remove(cart);
                await _db.SaveChangesAsync();

                var cnt = _db.ShoppingCartModel.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCartModel.Where(c => c.Id == cartId).FirstOrDefaultAsync();

            _db.ShoppingCartModel.Remove(cart);
            await _db.SaveChangesAsync();

            var cnt = _db.ShoppingCartModel.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage=1)
        {
            //Get the user id frm claims identity
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            List<OrderHeader> listOrderHeader = await _db.OrderHeader.Include("ApplicationUser").Where(c => c.UserId == claims.Value).ToListAsync();

            foreach(var orderHeader in listOrderHeader)
            {
                OrderDetailsViewModel orderdetailvm = new OrderDetailsViewModel()
                {
                     OrderHeader = orderHeader,
                     OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == orderHeader.Id).ToListAsync()
                };
                orderListVM.Orders.Add(orderdetailvm);
            }

            //get the count 
            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id).
                Skip((productPage - 1) * PageSize).
                Take(PageSize).ToList();

            orderListVM.PageInfo = new PageInfo()
            {
                CurrentPage = productPage,
                ItemsPerpage = PageSize,
                TotalItems = count,
                utlParam = "/Customer/Cart/OrderHistory?productPage=:"
            };
            return View(orderListVM);
        }

        public async Task<IActionResult> GetOrderDetails(int Id)
        {
            OrderDetailsViewModel orderDetailsVM = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Where(o => o.Id == Id).FirstOrDefaultAsync(),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == Id).ToListAsync()
            };
            orderDetailsVM.OrderHeader.ApplicationUser = await _db.ApplicationUser.Where(c => c.Id == orderDetailsVM.OrderHeader.UserId).FirstOrDefaultAsync();
            return PartialView("_IndividualOrderDetails", orderDetailsVM);
        }
    }
}
