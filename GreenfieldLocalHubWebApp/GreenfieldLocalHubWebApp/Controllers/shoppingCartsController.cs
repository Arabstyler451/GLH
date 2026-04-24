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
    // Handles all shopping cart actions: viewing, creating, editing, deleting, reordering and applying offers
    public class shoppingCartsController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public shoppingCartsController(ApplicationDbContext context)
        {
            _context = context;
        }




        // Shows the current user's active shopping cart with totals and loyalty offers
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Find the active shopping cart for this user, or create one if needed
            var shoppingCart = await _context.shoppingCart
                .FirstOrDefaultAsync(c => c.UserId == userId && c.shoppingCartStatus);

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

            // Load cart items with their related product and category data
            var shoppingCartItems = await _context.shoppingCartItems
                .Where(sci => sci.shoppingCartId == shoppingCart.shoppingCartId)
                .Include(sci => sci.shoppingCart)
                .Include(sci => sci.products)
                    .ThenInclude(p => p.categories)
                .ToListAsync();

            // Calculate the cart subtotal before any loyalty discount
            float subTotalAmount = shoppingCartItems.Sum(item =>
                item.products.productPrice * item.quantity);

            // Load the user's active and consumed loyalty offers
            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            var activeOffers = loyaltyAccount != null && !string.IsNullOrEmpty(loyaltyAccount.ActiveOffers)
                ? loyaltyAccount.ActiveOffers.Split(',').Select(s => s.Trim()).ToList()
                : new List<string>();

            var consumedOffers = loyaltyAccount != null && !string.IsNullOrEmpty(loyaltyAccount.ConsumedOffers)
                ? loyaltyAccount.ConsumedOffers.Split(',').Select(s => s.Trim()).ToList()
                : new List<string>();

            // Remove any consumed offers that are still listed as active
            activeOffers.RemoveAll(o => consumedOffers.Contains(o));

            // Calculate any discount from the user-selected pending offer
            float loyaltyDiscount = 0f;
            var pendingOffer = loyaltyAccount?.PendingOffer;

            // Clear the pending offer if it is no longer active
            if (!string.IsNullOrEmpty(pendingOffer) && !activeOffers.Contains(pendingOffer))
            {
                loyaltyAccount.PendingOffer = null;
                pendingOffer = null;
                _context.Update(loyaltyAccount);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(pendingOffer))
            {
                switch (pendingOffer)
                {
                    // Apply 10% discount to all Fruit & Veg items in the cart
                    case "10% off Fruits & Vegetables":
                        foreach (var item in shoppingCartItems)
                        {
                            if (item.products.categories != null &&
                                string.Equals(item.products.categories.categoryName?.Trim(),
                                    "Fruit & Veg", StringComparison.OrdinalIgnoreCase))
                            {
                                loyaltyDiscount += item.products.productPrice * item.quantity * 0.10f;
                            }
                        }
                        break;

                    // Discount the full price of any cheese products in the cart
                    case "Free Cheese":
                        foreach (var item in shoppingCartItems)
                        {
                            if (item.products.productName?.Contains("Cheese",
                                    StringComparison.OrdinalIgnoreCase) == true)
                            {
                                loyaltyDiscount += item.products.productPrice * item.quantity;
                            }
                        }
                        break;

                    // Apply a flat £5 discount if the subtotal is at least £20
                    case "£5 Voucher":
                        if (subTotalAmount >= 20f)
                            loyaltyDiscount += 5f;
                        break;
                }
            }

            float total = subTotalAmount - loyaltyDiscount;

            ViewBag.subTotalAmount = subTotalAmount;
            ViewBag.loyaltyDiscount = loyaltyDiscount;
            ViewBag.total = total;
            ViewBag.ActiveOffers = activeOffers;
            ViewBag.PendingOffer = pendingOffer;

            return View(shoppingCartItems);
        }

        // Shows the full details of a single shopping cart for admins and developers
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var shoppingCart = await _context.shoppingCart
                .FirstOrDefaultAsync(m => m.shoppingCartId == id);
            if (shoppingCart == null)
            {
                return NotFound();
            }

            return View(shoppingCart);
        }


        // Shows the shopping cart creation page for admins and developers
        [Authorize(Roles = "Admin, Developer")]
        public IActionResult Create()
        {

            return View();
        }

        // Processes the submitted shopping cart form and saves a new cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> Create([Bind("shoppingCartId,UserId,shoppingCartCreatedAt,shoppingCartStatus")] shoppingCart shoppingCart)
        {

            if (ModelState.IsValid)
            {
                // Save the shopping cart to the database
                _context.Add(shoppingCart);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(shoppingCart);
        }

        // Shows the edit form for an existing shopping cart
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var shoppingCart = await _context.shoppingCart.FindAsync(id);
            if (shoppingCart == null)
            {
                return NotFound();
            }
            return View(shoppingCart);
        }

        // Saves changes made to an existing shopping cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> Edit(int id, [Bind("shoppingCartId,UserId,shoppingCartCreatedAt,shoppingCartStatus")] shoppingCart shoppingCart)
        {
            if (id != shoppingCart.shoppingCartId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shoppingCart);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the shopping cart no longer exists, return 404, otherwise rethrow the error
                    if (!shoppingCartExists(shoppingCart.shoppingCartId))
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
            return View(shoppingCart);
        }

        // Shows the delete confirmation page for a shopping cart
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shoppingCart = await _context.shoppingCart
                .FirstOrDefaultAsync(m => m.shoppingCartId == id);
            if (shoppingCart == null)
            {
                return NotFound();
            }

            return View(shoppingCart);
        }

        // Permanently deletes the shopping cart from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Developer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shoppingCart = await _context.shoppingCart.FindAsync(id);
            if (shoppingCart != null)
            {
                _context.shoppingCart.Remove(shoppingCart);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether a shopping cart with the given ID exists in the database
        private bool shoppingCartExists(int id)
        {
            return _context.shoppingCart.Any(e => e.shoppingCartId == id);
        }


        
        // Adds products from a previous order back into the user's active shopping cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int orderId)
        {
            // Load all products that were included in the selected order
            var orderItems = await _context.orderProducts
                .Where(op => op.ordersId == orderId)
                .ToListAsync();

            if (!orderItems.Any())
            {
                TempData["Error"] = "No items found for this order.";
                return RedirectToAction("Index", "Orders");
            }

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Find the active shopping cart for this user, or create one if needed
            var cart = await _context.shoppingCart
                .FirstOrDefaultAsync(c => c.UserId == userId && c.shoppingCartStatus == true);

            if (cart == null)
            {
                cart = new shoppingCart
                {
                    UserId = userId,
                    shoppingCartStatus = true,
                    shoppingCartCreatedAt = DateTime.Now
                };

                _context.shoppingCart.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Load the current product records for the old order items
            var productIds = orderItems.Select(i => i.productsId).ToList();

            var products = await _context.products
                .Where(p => productIds.Contains(p.productsId))
                .ToDictionaryAsync(p => p.productsId);

            // Load the items already in the active shopping cart
            var cartItems = await _context.shoppingCartItems
                .Where(ci => ci.shoppingCartId == cart.shoppingCartId)
                .ToListAsync();

            var outOfStockItems = new List<string>();

            // Add available order items to the cart and track anything that cannot be added
            foreach (var item in orderItems)
            {
                if (!products.TryGetValue(item.productsId, out var product) ||
                    product.stockQuantity < item.quantity)
                {
                    outOfStockItems.Add(product?.productName ?? "Unknown product");
                    continue;
                }

                var existingCartItem = cartItems
                    .FirstOrDefault(ci => ci.productsId == item.productsId);

                if (existingCartItem != null)
                {
                    // Increase existing quantities without exceeding current stock
                    var newQty = existingCartItem.quantity + item.quantity;
                    existingCartItem.quantity = Math.Min(newQty, product.stockQuantity);
                }
                else
                {
                    // Add a new cart line for products that are not already in the cart
                    _context.shoppingCartItems.Add(new shoppingCartItems
                    {
                        shoppingCartId = cart.shoppingCartId,
                        productsId = item.productsId,
                        quantity = item.quantity,
                        unitPrice = item.unitPrice
                    });
                }
            }

            // Save the updated cart items to the database
            await _context.SaveChangesAsync();

            if (outOfStockItems.Any())
            {
                TempData["Warning"] = "Some items were out of stock: " + string.Join(", ", outOfStockItems);
            }
            else
            {
                TempData["Success"] = "Items added to your cart.";
            }

            return RedirectToAction("Index", "shoppingCarts");
        }



        // Applies the selected loyalty offer to the user's active shopping cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyOffer(string selectedOffer)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccount == null) return NotFound();

            // Make sure the chosen offer is genuinely active before accepting it
            var activeOffers = string.IsNullOrEmpty(loyaltyAccount.ActiveOffers)
                ? new List<string>()
                : loyaltyAccount.ActiveOffers.Split(',').Select(s => s.Trim()).ToList();

            // Store the selected offer as pending, or clear it if the user selected none
            loyaltyAccount.PendingOffer = (selectedOffer == "none" || !activeOffers.Contains(selectedOffer))
                ? null
                : selectedOffer;

            _context.Update(loyaltyAccount);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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
