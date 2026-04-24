using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GreenfieldLocalHubWebApp.Controllers
{
    // Handles all producer-related actions: viewing, creating, editing and deleting producers
    public class producersController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public producersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of all producers
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            return View(await _context.producers.ToListAsync());
        }

        // Shows the full details of a single producer including their products
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var producers = await _context.producers
                .Include(p => p.products)
                .FirstOrDefaultAsync(m => m.producersId == id);

            if (producers == null)
            {
                return NotFound();
            }

            return View(producers);
        }

        // Shows the producer creation page for admins and developers
        [Authorize(Roles = "Admin, Developer")]
        public IActionResult Create()
        {
            return View();
        }

        // Processes the submitted producer form and saves a new producer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> Create([Bind("producersId,UserId,producerName,producerEmail,producerPhone,producerDescription,producerLocation,producerImage")] producers producers)
        {
            if (ModelState.IsValid)
            {
                // Save the producer to the database
                _context.Add(producers);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producers);
        }

        // Shows the edit form for an existing producer
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producers = await _context.producers.FindAsync(id);
            if (producers == null)
            {
                return NotFound();
            }
            return View(producers);
        }

        // Saves changes made to an existing producer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Edit(int id, [Bind("producersId,producerName,producerEmail,producerPhone,producerDescription,producerLocation,producerImage")] producers producers)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Remove server-set fields from ModelState so they don't cause false validation errors
            ModelState.Remove("UserId");


            if (id != producers.producersId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producers);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the producer no longer exists, return 404, otherwise rethrow the error
                    if (!producersExists(producers.producersId))
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
            return View(producers);
        }

        // Shows the delete confirmation page for a producer
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producers = await _context.producers
                .FirstOrDefaultAsync(m => m.producersId == id);
            if (producers == null)
            {
                return NotFound();
            }

            return View(producers);
        }

        // Permanently deletes the producer from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producers = await _context.producers.FindAsync(id);
            if (producers != null)
            {
                _context.producers.Remove(producers);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether a producer with the given ID exists in the database
        private bool producersExists(int id)
        {
            return _context.producers.Any(e => e.producersId == id);
        }



        // Returns the total number of items currently in the logged in user's active shopping cart
        public async Task<int> GetCartItemCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return 0;

            var shoppingCart = await _context.shoppingCart
                .FirstOrDefaultAsync(c => c.UserId == userId && c.shoppingCartStatus);

            if (shoppingCart == null) return 0;

            // Sum quantities rather than counting rows so multi-quantity items are counted correctly
            var totalItems = await _context.shoppingCartItems
                .Where(sci => sci.shoppingCartId == shoppingCart.shoppingCartId)
                .SumAsync(sci => sci.quantity);

            return totalItems;
        }
    }
}
