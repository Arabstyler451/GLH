using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;

namespace GreenfieldLocalHubWebApp.Controllers
{
    // Handles all category-related actions: viewing, creating, editing and deleting categories
    public class categoriesController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public categoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of all product categories
        public async Task<IActionResult> Index()
        {
            return View(await _context.categories.ToListAsync());
        }

        // Shows the full details of a single category
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categories = await _context.categories
                .FirstOrDefaultAsync(m => m.categoriesId == id);
            if (categories == null)
            {
                return NotFound();
            }

            return View(categories);
        }

        // Shows the category creation page
        public IActionResult Create()
        {
            return View();
        }

        // Processes the submitted category form and saves a new category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("categoriesId,categoryName")] categories categories)
        {
            if (ModelState.IsValid)
            {
                // Save the category to the database
                _context.Add(categories);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categories);
        }

        // Shows the edit form for an existing category
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categories = await _context.categories.FindAsync(id);
            if (categories == null)
            {
                return NotFound();
            }
            return View(categories);
        }

        // Saves changes made to an existing category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("categoriesId,categoryName")] categories categories)
        {
            if (id != categories.categoriesId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categories);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the category no longer exists, return 404, otherwise rethrow the error
                    if (!categoriesExists(categories.categoriesId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categories);
        }

        // Shows the delete confirmation page for a category
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categories = await _context.categories
                .FirstOrDefaultAsync(m => m.categoriesId == id);
            if (categories == null)
            {
                return NotFound();
            }

            return View(categories);
        }

        // Permanently deletes the category from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categories = await _context.categories.FindAsync(id);
            if (categories != null)
            {
                _context.categories.Remove(categories);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether a category with the given ID exists in the database
        private bool categoriesExists(int id)
        {
            return _context.categories.Any(e => e.categoriesId == id);
        }
    }
}
