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
    // Handles all order product actions: viewing, creating, editing and deleting order lines
    public class orderProductsController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public orderProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of all order lines with their related orders and products
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.orderProducts.Include(o => o.orders).Include(o => o.products);
            return View(await applicationDbContext.ToListAsync());
        }

        // Shows the full details of a single order line
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProducts = await _context.orderProducts
                .Include(o => o.orders)
                .Include(o => o.products)
                .FirstOrDefaultAsync(m => m.orderProductsId == id);
            if (orderProducts == null)
            {
                return NotFound();
            }

            return View(orderProducts);
        }

        // Shows the order line creation page with order and product options
        public IActionResult Create()
        {
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId");
            ViewData["productsId"] = new SelectList(_context.Set<products>(), "productsId", "productsId");
            return View();
        }

        // Processes the submitted order line form and saves a new order line
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("orderProductsId,ordersId,productsId,quantity,unitPrice")] orderProducts orderProducts)
        {
            if (ModelState.IsValid)
            {
                // Save the order line to the database
                _context.Add(orderProducts);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId", orderProducts.ordersId);
            ViewData["productsId"] = new SelectList(_context.Set<products>(), "productsId", "productsId", orderProducts.productsId);
            return View(orderProducts);
        }

        // Shows the edit form for an existing order line
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProducts = await _context.orderProducts.FindAsync(id);
            if (orderProducts == null)
            {
                return NotFound();
            }
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId", orderProducts.ordersId);
            ViewData["productsId"] = new SelectList(_context.Set<products>(), "productsId", "productsId", orderProducts.productsId);
            return View(orderProducts);
        }

        // Saves changes made to an existing order line
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("orderProductsId,ordersId,productsId,quantity,unitPrice")] orderProducts orderProducts)
        {
            if (id != orderProducts.orderProductsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderProducts);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the order line no longer exists, return 404, otherwise rethrow the error
                    if (!orderProductsExists(orderProducts.orderProductsId))
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
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId", orderProducts.ordersId);
            ViewData["productsId"] = new SelectList(_context.Set<products>(), "productsId", "productsId", orderProducts.productsId);
            return View(orderProducts);
        }

        // Shows the delete confirmation page for an order line
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProducts = await _context.orderProducts
                .Include(o => o.orders)
                .Include(o => o.products)
                .FirstOrDefaultAsync(m => m.orderProductsId == id);
            if (orderProducts == null)
            {
                return NotFound();
            }

            return View(orderProducts);
        }

        // Permanently deletes the order line from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderProducts = await _context.orderProducts.FindAsync(id);
            if (orderProducts != null)
            {
                _context.orderProducts.Remove(orderProducts);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether an order line with the given ID exists in the database
        private bool orderProductsExists(int id)
        {
            return _context.orderProducts.Any(e => e.orderProductsId == id);
        }
    }
}
