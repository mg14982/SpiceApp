using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModel;
using Spice.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel indexVM = new IndexViewModel()
            {
                MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync(),
                Category = await _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(m => m.isActive == true).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claims != null)
            {
                var count = _db.ShoppingCartModel.Where(x => x.ApplicationUserId == claims.Value).ToList().Count();
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
            }

            return View(indexVM);
        }

        //GET - DETAILS
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var menuItemFromDB = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).Where(m => m.Id == id).FirstOrDefaultAsync();
            if (menuItemFromDB == null)
            {
                return NotFound();
            }
            ShoppingCartModel shoppingCartObj = new ShoppingCartModel()
            {
                MenuItem = menuItemFromDB,
                MenuItemId = menuItemFromDB.Id
            };
            return View(shoppingCartObj);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCartModel ShoppingCartObj)
        {
            ShoppingCartObj.Id = 0;
            if (ModelState.IsValid)
            {
                //Get the User id from claims identity
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                //Set the Application User id
                ShoppingCartObj.ApplicationUserId = claims.Value;

                ShoppingCartModel shopingCartFromDb = await _db.ShoppingCartModel.Where(c => c.ApplicationUserId == ShoppingCartObj.ApplicationUserId
                                                      && c.MenuItemId == ShoppingCartObj.MenuItemId).FirstOrDefaultAsync();

                if (shopingCartFromDb == null)
                {
                    //not exist in shopping cart
                    await _db.ShoppingCartModel.AddAsync(ShoppingCartObj);
                }
                else
                {
                    shopingCartFromDb.Count = shopingCartFromDb.Count + ShoppingCartObj.Count;
                }
                await _db.SaveChangesAsync();
                //Get the total items from db and display on cart count using session
                var count = _db.ShoppingCartModel.Where(x => x.ApplicationUserId == ShoppingCartObj.ApplicationUserId).ToList().Count();
                //Set count into session object
                HttpContext.Session.SetInt32("ssCartCount", count);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var menuItemFromDB = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).Where(m => m.Id == ShoppingCartObj.MenuItemId).FirstOrDefaultAsync();
                if (menuItemFromDB == null)
                {
                    return NotFound();
                }
                ShoppingCartModel shoppingCartObj = new ShoppingCartModel()
                {
                    MenuItem = menuItemFromDB,
                    MenuItemId = menuItemFromDB.Id
                };
                return View(shoppingCartObj);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
