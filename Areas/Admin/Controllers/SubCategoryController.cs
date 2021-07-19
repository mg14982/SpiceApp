using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spice.Models.ViewModel;
using Spice.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> Index()
        {
            if (TempData["StatusMessage"] != null)
            {
                ViewBag.StatusMessage = TempData["StatusMessage"].ToString();
            }
            var subcategory = await _db.SubCategory.Include(m => m.Category).ToListAsync();
            return View(subcategory);
        }

        //GET - Create
        public async Task<IActionResult> Create()
        {
            CategorySubCategoryViewModel model = new CategorySubCategoryViewModel()
            {
                CategoryList = await _db.Category.OrderBy(c => c.Name).ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(m => m.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        //POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategorySubCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategoryExists = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);
                if (doesSubCategoryExists.Count()  >0)
                {
                    //Error
                    StatusMessage = "Error : Sub Category already exists under " + doesSubCategoryExists.FirstOrDefault().Category.Name + " category. Please use another name.";  
                }
                else
                {
                    _db.SubCategory.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            CategorySubCategoryViewModel newmodel = new CategorySubCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(m => m.Name).Distinct().ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(newmodel);
        }

        //GET - EDIT
        public async Task<IActionResult> Edit(int id)
        {
            //check id is null
            if (id == null)
            {
                return NotFound();
            }
            //get the subcategory from database using id
            var subCategory = await _db.SubCategory.FirstOrDefaultAsync(m => m.Id == id);
            //check for null
            if (subCategory == null)
            {
                return NotFound();
            }
            //create view model with values
            CategorySubCategoryViewModel model = new CategorySubCategoryViewModel()
            {
                CategoryList = await _db.Category.OrderBy(c => c.Name).ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(m => m.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        //POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategorySubCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategoryExists = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);
                if (doesSubCategoryExists.Count() > 0)
                {
                    //Error
                    StatusMessage = "Error : Sub Category already exists under " + doesSubCategoryExists.FirstOrDefault().Category.Name + " category. Please use another name.";
                }
                else
                {
                    var subcate = await _db.SubCategory.FindAsync(id);
                    if (subcate == null)
                    {
                        return NotFound();
                    }
                    subcate.Name = model.SubCategory.Name;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            CategorySubCategoryViewModel newmodel = new CategorySubCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(m => m.Name).Distinct().ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(newmodel);
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            //check id is null
            if (id == null)
            {
                return NotFound();
            }
            //get the subcategory from database using id
            var subCategory = await _db.SubCategory.FirstOrDefaultAsync(m => m.Id == id);
            //check for null
            if (subCategory == null)
            {
                return NotFound();
            }
            //create view model with values
            CategorySubCategoryViewModel model = new CategorySubCategoryViewModel()
            {
                CategoryList = await _db.Category.OrderBy(c => c.Name).ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(m => m.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            //check id is null
            if (id == null)
            {
                return NotFound();
            }
            //get the subcategory from database using id
            var subCategory = await _db.SubCategory.FirstOrDefaultAsync(m => m.Id == id);
            //check for null
            if (subCategory == null)
            {
                return NotFound();
            }
            //create view model with values
            CategorySubCategoryViewModel model = new CategorySubCategoryViewModel()
            {
                CategoryList = await _db.Category.OrderBy(c => c.Name).ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(m => m.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        //POST - DELETE

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var subCategory = await _db.SubCategory.FindAsync(id);
            if (subCategory == null)
            {
                return NotFound();
            }
            _db.SubCategory.Remove(subCategory);
            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Success : Subcategory deleted successfully!";
            return RedirectToAction(nameof(Index));
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


        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> subCategory = new List<SubCategory>();
            subCategory = await (from sub_category in _db.SubCategory
                             where sub_category.CategoryId == id
                             select sub_category).ToListAsync();
            return Json(new SelectList(subCategory, "Id", "Name"));
        }
    }
}
