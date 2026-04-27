using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;
using Microsoft.AspNetCore.Identity;
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
    // Handles all address-related actions: viewing, creating, editing, deleting and setting default addresses
    public class addressesController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public addressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows the current user's saved addresses and keeps one address marked as default
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();
            ViewData["Layout"] = "_AccountLayout";

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Load this user's addresses with the default and newest addresses first
            var addresses = _context.address
                   .Where(a => a.UserId == userId)
                   .OrderByDescending(a => a.IsDefault)
                   .ThenByDescending(a => a.createdDate)
                   .ToList();

            // If the user only has one address, make it the default address
            if (addresses.Count == 1 && !addresses[0].IsDefault)
            {
                addresses[0].IsDefault = true;
                _context.address.Update(addresses[0]);
                await _context.SaveChangesAsync();
            }

            // If no address is marked as default, set the newest address as default
            if (addresses.Any() && !addresses.Any(a => a.IsDefault))
            {
                addresses[0].IsDefault = true;
                _context.address.Update(addresses[0]);
                await _context.SaveChangesAsync();
            }

            return View(addresses);
        }

        // Shows the full details of a single address
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var address = await _context.address
                .FirstOrDefaultAsync(m => m.addressId == id);
            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }

        // Shows the address creation page and keeps the checkout return URL if one was provided
        public IActionResult Create(string returnUrl = null)
        {
            ViewBag.CartItemCount = GetCartItemCount();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Processes the submitted address form and saves a new address
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("addressId,street,city,postalCode,country")] address address, string returnUrl = null)
        {

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Set fields that should never come from the form
            address.UserId = userId;
            ModelState.Remove("UserId");

            ViewBag.CartItemCount = await GetCartItemCount();


            if (ModelState.IsValid)
            {
                // Save the address to the database
                _context.Add(address);
                await _context.SaveChangesAsync();

                // Return to checkout and pre-select the newly created address
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl + "&selectedAddressId=" + address.addressId);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(address);
        }

        // Shows the edit form for an existing address
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var address = await _context.address.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            return View(address);
        }

        // Saves changes made to an existing address
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("addressId,street,city,postalCode,country")] address address)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Set fields that should never come from the form
            address.UserId = userId;
            ModelState.Remove("UserId");

            if (id != address.addressId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(address);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the address no longer exists, return 404, otherwise rethrow the error
                    if (!addressExists(address.addressId))
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
            return View(address);
        }

        // Shows the delete confirmation page for an address
        public async Task<IActionResult> Delete(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var address = await _context.address
                .FirstOrDefaultAsync(m => m.addressId == id);
            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }



        // Permanently deletes the address from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Find the address with its related orders
            var address = await _context.address
                .Include(a => a.orders)
                .FirstOrDefaultAsync(a => a.addressId == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            // Check whether this address is linked to any existing orders
            var linkedOrders = address.orders?.Where(o => o.addressId == id).ToList();

            if (linkedOrders != null && linkedOrders.Any())
            {
                // Break the foreign key relationship by setting addressId to null on all linked orders
                foreach (var order in linkedOrders)
                {
                    order.addressId = null;
                }

                // Now it's safe to delete the address
                _context.address.Remove(address);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Address deleted successfully. It was used in {linkedOrders.Count} order(s), but those orders still have the address details saved.";
            }
            else
            {
                // Delete the address directly when there are no linked orders
                _context.address.Remove(address);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Address deleted successfully.";
            }

            // If the deleted address was the default, set another address as default
            if (address.IsDefault)
            {
                var newDefaultAddress = await _context.address
                    .Where(a => a.UserId == userId && a.addressId != id)
                    .OrderByDescending(a => a.createdDate)
                    .FirstOrDefaultAsync();

                if (newDefaultAddress != null)
                {
                    newDefaultAddress.IsDefault = true;
                    await _context.SaveChangesAsync();
                    TempData["Info"] = "Another address has been set as your default.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Checks whether an address with the given ID exists in the database
        private bool addressExists(int id)
        {
            return _context.address.Any(e => e.addressId == id);
        }


        // Sets one address as the current user's default address
        [HttpPost]
        public IActionResult SetDefault(int id)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Load all saved addresses for this user
            var addresses = _context.address.Where(a => a.UserId == userId).ToList();

            // Find the address to set as default
            var addressToSetDefault = addresses.FirstOrDefault(a => a.addressId == id);
            if (addressToSetDefault == null)
            {
                return NotFound();
            }

            // Remove default flag from all addresses
            foreach (var addr in addresses)
            {
                addr.IsDefault = false;
            }

            // Set the selected address as default
            addressToSetDefault.IsDefault = true;

            _context.SaveChanges();

            return RedirectToAction("Index");
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
