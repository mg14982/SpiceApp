using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        //GET - CREATE
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupons)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count>0)
                {
                    byte[] b1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            b1 = ms1.ToArray();
                        }
                    }
                    coupons.Picture = b1;
                }
                _db.Coupon.Add(coupons);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupons);
        }

        //GET - EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int? Id)
        {
            //Check the null for Id
            if (Id == null)
            {
                return NotFound();
            }
            //Get the coupon details from db
            var couponFromDB = await _db.Coupon.Where(c => c.Id == Id).FirstOrDefaultAsync();
            if (couponFromDB == null)
            {
                return NotFound();
            }
            //Convert the image byte array into base64 string
            if (couponFromDB.Picture != null)
            {
                string imgBase64 = Convert.ToBase64String(couponFromDB.Picture);
                string imgDataURL = string.Format("data:image/png;base64,{0}", imgBase64);
                couponFromDB.ImageUrl = imgDataURL;
            }
            return View(couponFromDB);
        }

        //POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Coupon coupons)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                //Get the coupon details from db
                var couponFromDB = await _db.Coupon.Where(x => x.Id == coupons.Id).FirstOrDefaultAsync();
                if (files.Count > 0)
                {
                    byte[] b1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            b1 = ms1.ToArray();
                        }
                    }
                    coupons.Picture = b1;
                    couponFromDB.Picture = coupons.Picture;
                }
                if (couponFromDB ==  null)
                {
                    return NotFound();
                }
                couponFromDB.Name = coupons.Name;
                couponFromDB.Discount = coupons.Discount;
                couponFromDB.CouponType = coupons.CouponType;
                couponFromDB.MinimumAmount = coupons.MinimumAmount;
                couponFromDB.isActive = coupons.isActive;
                
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupons);
        }

        //GET - DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int? Id)
        {
            //Check the null for Id
            if (Id == null)
            {
                return NotFound();
            }
            //Get the coupon details from db
            var couponFromDB = await _db.Coupon.Where(c => c.Id == Id).FirstOrDefaultAsync();
            if (couponFromDB == null)
            {
                return NotFound();
            }
            //Convert the image byte array into base64 string
            if (couponFromDB.Picture != null)
            {
                string imgBase64 = Convert.ToBase64String(couponFromDB.Picture);
                string imgDataURL = string.Format("data:image/png;base64,{0}", imgBase64);
                couponFromDB.ImageUrl = imgDataURL;
            }
            return View(couponFromDB);
        }

        //POST - DETAILS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(int Id)
        {
            if (Id == null)
            {
                return NotFound();
            }
            return RedirectToAction("Edit", new { id = Id });
        }

        //GET - DELETE
        [HttpGet]
        public async Task<IActionResult> Delete(int? Id)
        {
            //Check the null for Id
            if (Id == null)
            {
                return NotFound();
            }
            //Get the coupon details from db
            var couponFromDB = await _db.Coupon.Where(c => c.Id == Id).FirstOrDefaultAsync();
            if (couponFromDB == null)
            {
                return NotFound();
            }
            //Convert the image byte array into base64 string
            if (couponFromDB.Picture != null)
            {
                string imgBase64 = Convert.ToBase64String(couponFromDB.Picture);
                string imgDataURL = string.Format("data:image/png;base64,{0}", imgBase64);
                couponFromDB.ImageUrl = imgDataURL;
            }
            return View(couponFromDB);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? Id)
        {
            if (ModelState.IsValid)
            {
                //Check the null for Id
                if (Id == null)
                {
                    return NotFound();
                }
                //Get the coupon details from db
                var couponFromDB = await _db.Coupon.Where(c => c.Id == Id).FirstOrDefaultAsync();
                if (couponFromDB == null)
                {
                    return NotFound();
                }
                _db.Coupon.Remove(couponFromDB);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(nameof(Index));
        }
    }
}
