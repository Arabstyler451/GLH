using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;
using System.Security.Claims;

namespace GreenfieldLocalHubWebApp.Controllers
{
    // Handles all shopping cart item actions: viewing, creating, editing, deleting and updating quantities
    public class shoppingCartItemsController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public shoppingCartItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of all shopping cart items with their related products and carts
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            var applicationDbContext = _context.shoppingCartItems.Include(s => s.products).Include(s => s.shoppingCart);
            return View(await applicationDbContext.ToListAsync());
        }

        // Shows the full details of a single shopping cart item
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shoppingCartItems = await _context.shoppingCartItems
                .Include(s => s.products)
                .Include(s => s.shoppingCart)
                .FirstOrDefaultAsync(m => m.shoppingCartItemsId == id);
            if (shoppingCartItems == null)
            {
                return NotFound();
            }

            return View(shoppingCartItems);
        }

        // Shows the shopping cart item creation page with product and cart options
        public IActionResult Create()
        {
            ViewData["productsId"] = new SelectList(_context.products, "productsId", "productsId");
            ViewData["shoppingCartId"] = new SelectList(_context.shoppingCart, "shoppingCartId", "shoppingCartId");
            return View();
        }

        // Adds the selected product to the current user's active shopping cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productsId, int quantity = 1)
        {
            // Keep the submitted quantity within a valid range
            if (quantity < 1) quantity = 1;

            // Load the product being added to the cart
            var product = await _context.products.FirstOrDefaultAsync(p => p.productsId == productsId);

            if (product == null)
            {
                return NotFound();
            }

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Find the active shopping cart for this user, or create one if needed
            var shoppingCart = await _context.shoppingCart
                .FirstOrDefaultAsync(c => c.UserId == userId && c.shoppingCartStatus == true);

            if (shoppingCart == null)
            {
                shoppingCart = new shoppingCart
                {
                    UserId = userId,
                    shoppingCartCreatedAt = DateTime.Now,
                    shoppingCartStatus = true
                };
                _context.shoppingCart.Add(shoppingCart);
                await _context.SaveChangesAsync();
            }

            // Check whether this product is already in the active shopping cart
            var shoppingCartItem = await _context.shoppingCartItems
                .FirstOrDefaultAsync(sc => sc.shoppingCartId == shoppingCart.shoppingCartId && sc.productsId == productsId);

            if (shoppingCartItem != null)
            {
                // Add the chosen quantity without exceeding available stock
                shoppingCartItem.quantity = Math.Min(
                    shoppingCartItem.quantity + quantity,
                    product.stockQuantity
                );
            }
            else
            {
                // Add a new cart line for products that are not already in the cart
                shoppingCartItem = new shoppingCartItems
                {
                    shoppingCartId = shoppingCart.shoppingCartId,
                    productsId = productsId,
                    quantity = Math.Min(quantity, product.stockQuantity)
                };
                _context.shoppingCartItems.Add(shoppingCartItem);
            }

            // Save the updated cart item to the database
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "products");
        }

        // Shows the edit form for an existing shopping cart item
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shoppingCartItems = await _context.shoppingCartItems.FindAsync(id);
            if (shoppingCartItems == null)
            {
                return NotFound();
            }
            ViewData["productsId"] = new SelectList(_context.products, "productsId", "productsId", shoppingCartItems.productsId);
            ViewData["shoppingCartId"] = new SelectList(_context.shoppingCart, "shoppingCartId", "shoppingCartId", shoppingCartItems.shoppingCartId);
            return View(shoppingCartItems);
        }

        // Saves changes made to an existing shopping cart item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("shoppingCartItemsId,shoppingCartId,productsId,unitPrice,quantity")] shoppingCartItems shoppingCartItems)
        {
            if (id != shoppingCartItems.shoppingCartItemsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shoppingCartItems);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the cart item no longer exists, return 404, otherwise rethrow the error
                    if (!shoppingCartItemsExists(shoppingCartItems.shoppingCartItemsId))
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
            ViewData["productsId"] = new SelectList(_context.products, "productsId", "productsId", shoppingCartItems.productsId);
            ViewData["shoppingCartId"] = new SelectList(_context.shoppingCart, "shoppingCartId", "shoppingCartId", shoppingCartItems.shoppingCartId);
            return View(shoppingCartItems);
        }

        // Shows the delete confirmation page for a shopping cart item
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shoppingCartItems = await _context.shoppingCartItems
                .Include(s => s.products)
                .Include(s => s.shoppingCart)
                .FirstOrDefaultAsync(m => m.shoppingCartItemsId == id);
            if (shoppingCartItems == null)
            {
                return NotFound();
            }

            return View(shoppingCartItems);
        }

        // Permanently deletes the shopping cart item from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shoppingCartItems = await _context.shoppingCartItems.FindAsync(id);
            if (shoppingCartItems != null)
            {
                _context.shoppingCartItems.Remove(shoppingCartItems);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "shoppingCarts");
        }

        // Checks whether a shopping cart item with the given ID exists in the database
        private bool shoppingCartItemsExists(int id)
        {
            return _context.shoppingCartItems.Any(e => e.shoppingCartItemsId == id);
        }


        // Updates the quantity of a shopping cart item from the cart page controls
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int shoppingCartItemsId, int change)
        {
            // Load the cart item with product data so stock limits can be checked
            var item = await _context.shoppingCartItems
                .Include(i => i.products)
                .FirstOrDefaultAsync(i => i.shoppingCartItemsId == shoppingCartItemsId);

            if (item == null)
                return NotFound();

            var newQty = item.quantity + change;

            // Remove the cart item entirely if the quantity drops to zero
            if (newQty <= 0)
            {
                _context.shoppingCartItems.Remove(item);
            }
            else
            {
                // Cap the updated quantity at the available stock level
                item.quantity = Math.Min(newQty, item.products?.stockQuantity ?? newQty);
            }

            // Save the updated quantity to the database
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "shoppingCarts");
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
