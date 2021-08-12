using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModel;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private int PageSize = 2;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Display Confirm page after click on summary page
        public async Task<IActionResult> Confirm(int id)
        {
            //get the applicaiton user id  
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include("ApplicationUser").Where(c => c.Id == id && c.UserId == claim.Value).FirstOrDefaultAsync(),
                OrderDetails = await _db.OrderDetails.Where(c => c.OrderId == id).ToListAsync()
            };

            return View(orderDetailsViewModel);
        }

        //Manage Order List
        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> orderDetailsVM = new List<OrderDetailsViewModel>();

            List<OrderHeader> listOrderHeader = await _db.OrderHeader.Where(o => o.Status == SD.OrderStatusSubmitted || o.Status == SD.OrderStatusInProcess)
                                                    .OrderByDescending(o => o.PickupTime).ToListAsync();
            foreach (var orderHeader in listOrderHeader)
            {
                OrderDetailsViewModel orderdetailvm = new OrderDetailsViewModel()
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(x => x.OrderId == orderHeader.Id).ToListAsync()
                };
                orderDetailsVM.Add(orderdetailvm);
            }
            return View(orderDetailsVM);
        }

        //Prepare Order : Status update from Submitted --> In Process
        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            var orderheader = await _db.OrderHeader.Where(x => x.Id == OrderId).FirstOrDefaultAsync();
            if (orderheader == null)
            {
                return NotFound();
            }
            orderheader.Status = SD.OrderStatusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        //Manage Order List
        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            var orderheader = await _db.OrderHeader.Where(x => x.Id == OrderId).FirstOrDefaultAsync();
            if (orderheader == null)
            {
                return NotFound();
            }
            orderheader.Status = SD.OrderStatusReady;
            await _db.SaveChangesAsync();
            //Send Email Notification later on

            return RedirectToAction("ManageOrder", "Order");
        }

        //Manage Order List
        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            var orderheader = await _db.OrderHeader.Where(x => x.Id == OrderId).FirstOrDefaultAsync();
            if (orderheader == null)
            {
                return NotFound();
            }
            orderheader.Status = SD.OrderStatusCancelled;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickUp(int productPage = 1, string searchName = null, string searchPhone = null, string searchEmail = null)
        {
            //Get the user id frm claims identity
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            param.Append("&searchName=");
            if (!string.IsNullOrEmpty(searchName))
            {
                param.Append(searchName);
            }
            param.Append("&searchPhone=");
            if (!string.IsNullOrEmpty(searchPhone))
            {
                param.Append(searchName);
            }
            param.Append("&searchEmail=");
            if (!string.IsNullOrEmpty(searchEmail))
            {
                param.Append(searchEmail);
            }
            List<OrderHeader> listOrderHeader = new List<OrderHeader>();
            if (!string.IsNullOrEmpty(searchName) || !string.IsNullOrEmpty(searchName) || !string.IsNullOrEmpty(searchName))
            {
                var user = new ApplicationUser();
                if (!string.IsNullOrEmpty(searchName))
                {
                    listOrderHeader = await _db.OrderHeader.Include("ApplicationUser").Where(c => c.PickupaName.ToLower().Contains(searchName.ToLower()))
                                                    .OrderByDescending(o => o.OrderDate).ToListAsync();
                }
                else
                {
                    if (!string.IsNullOrEmpty(searchPhone))
                    {
                        listOrderHeader = await _db.OrderHeader.Include("ApplicationUser").Where(c => c.PhoneNumber.ToLower().Contains(searchPhone.ToLower()))
                                                        .OrderByDescending(o => o.OrderDate).ToListAsync();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(searchEmail))
                        {
                            user = await _db.ApplicationUser.Where(c => c.Email.ToLower().Contains(searchEmail.ToString())).FirstOrDefaultAsync();
                            listOrderHeader = await _db.OrderHeader.Include("ApplicationUser").Where(c => c.UserId == user.Id)
                                                            .OrderByDescending(o => o.OrderDate).ToListAsync();
                        }
                    }
                }
            }
            else
            {
                listOrderHeader = await _db.OrderHeader.Include("ApplicationUser")
                                                    .Where(c => c.Status == SD.OrderStatusReady).ToListAsync();
            }
            foreach (var orderHeader in listOrderHeader)
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
                utlParam = param.ToString()
            };
            return View(orderListVM);
        }

        //[HttpPost]
        //[Authorize(Roles =SD.FrontDeskUser+","+SD.ManagerUser)]
        //[ActionName("OrderPickUp")]
        //public async Task<IActionResult> OrderPickUpPost(int OrderId)
        //{
        //    var orderheader = await _db.OrderHeader.Where(x => x.Id == OrderId).FirstOrDefaultAsync();
        //    if (orderheader == null)
        //    {
        //        return NotFound();
        //    }
        //    orderheader.Status = SD.OrderStatusCompleted;
        //    await _db.SaveChangesAsync();
        //    return RedirectToAction("OrderPickUp", "Order");
        //}


        [HttpGet]
        public async Task<IActionResult> OrderPickUpPost(int OrderId)
        {
            var orderheader = await _db.OrderHeader.Where(x => x.Id == OrderId).FirstOrDefaultAsync();
            if (orderheader == null)
            {
                return NotFound();
            }
            orderheader.Status = SD.OrderStatusCompleted;
            await _db.SaveChangesAsync();
            return Json("Success");
        }


    }
}
