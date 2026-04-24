using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;
using GreenfieldLocalHubWebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GreenfieldLocalHubWebApp.Controllers
{
    // Handles all product-related actions: viewing, creating, editing, deleting, searching and filtering products
    public class productsController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public productsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of products - what the user sees depends on their role
        public async Task<IActionResult> Index()
        {
            
            ViewBag.CartItemCount = await GetCartItemCount();

            // Load categories for the filter dropdown
            ViewBag.Categories = await _context.categories.OrderBy(c => c.categoryName).ToListAsync();


            // Producers only see products that belong to them
            if (User.IsInRole("Producer"))
            {
                // Get the ID of the currently logged in user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if(userId == null)
                {
                    return Unauthorized();
                }

                // Find the producer profile that belongs to this user
                var producer = await _context.producers.FirstOrDefaultAsync(p => p.UserId == userId);

                if (producer == null)
                {
                    return NotFound();
                }

                // Load all products owned by this producer
                var producerProducts = await _context.products.Where(p => p.producersId == producer.producersId).Include(p => p.producers).ToListAsync();
                return View(producerProducts);
            }
            else
            {
                // Other users see every product in the catalogue
                var allProducts = await _context.products.Include(p => p.categories).Include(p => p.producers).ToListAsync();
                return View(allProducts);
            }
        }

        // Shows the full details of a single product with related product suggestions
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            var products = await _context.products
                .Include(p => p.categories)
                .Include(p => p.producers)
                .FirstOrDefaultAsync(m => m.productsId == id);
            if (products == null)
            {
                return NotFound();
            }

            // Build a related product list using the same category
            var related = new List<products>();
            if (products.categories != null)
            {
                related = await _context.products
                    .Include(p => p.producers)
                    .Include(p => p.categories)
                    .Where(p => p.categories != null
                             && p.categories.categoriesId == products.categories.categoriesId
                             && p.productsId != products.productsId)
                    .Take(4)
                    .ToListAsync();
            }

            var viewModel = new productDetailsViewModel
            {
                Product = products,
                RelatedProducts = related
            };

            return View(viewModel);
        }

        // Shows the product creation page with category and producer options
        [Authorize(Roles = "Producer, Admin, Developer")]
        public IActionResult Create()
        {
            // Load category options for the product form
            ViewBag.categoriesId = new SelectList(_context.categories, "categoriesId", "categoryName");

            // Admins and developers can manually choose the producer
            if (User.IsInRole("Admin") || User.IsInRole("Developer"))
                ViewBag.producersId = new SelectList(_context.producers, "producersId", "producerName");

            return View();
        }

        // Processes the submitted product form and saves a new product
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Create([Bind("productsId,producersId,categoriesId,productName,productDescription,stockQuantity,productPrice,productAvailability,productImage,productUnit")] products products)
        {
            // Remove navigation properties from ModelState so they don't cause false validation errors
            ModelState.Remove("producers");
            ModelState.Remove("categories");

            // For producers, always use their own producer ID instead of trusting the posted value
            if (User.IsInRole("Producer"))
            {
                // Get the producer profile for the logged in user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var producer = await _context.producers
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (producer == null) return NotFound();

                products.producersId = producer.producersId;
            }

            // Remove producer ID from ModelState because it has been set server-side
            ModelState.Remove("producersId");

            if (ModelState.IsValid)
            {
                // Save the product to the database
                _context.Add(products);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "producerDashboard", new { activeTab = "products" });
            }

            ViewBag.categoriesId = new SelectList(_context.categories, "categoriesId", "categoryName", products.categoriesId);
            ViewBag.producersId = new SelectList(_context.producers, "producersId", "producerName", products.producersId);
            return View(products);
        }

        // Shows the edit form for an existing product
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var products = await _context.products.FindAsync(id);
            if (products == null)
            {
                return NotFound();
            }
            ViewData["categoriesId"] = new SelectList(_context.categories, "categoriesId", "categoryName", products.categoriesId);
            return View(products);
        }

        // Saves changes made to an existing product
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Edit(int id, [Bind("productsId,categoriesId,productName,productDescription,stockQuantity,productPrice,productAvailability,productImage,productUnit")] products products)
        {
            if (id != products.productsId)
                return NotFound();

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Load the existing product from the database
            var existing = await _context.products.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Find the producer profile that belongs to this user
            var producer = await _context.producers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null)
                return NotFound();

            // Allow admins/developers to edit any product
            if (!User.IsInRole("Admin") && !User.IsInRole("Developer") &&
                existing.producersId != producer.producersId)
                return Forbid();

            // Apply only the editable fields to the tracked entity
            existing.categoriesId = products.categoriesId;
            existing.productName = products.productName;
            existing.productDescription = products.productDescription;
            existing.stockQuantity = products.stockQuantity;
            existing.productPrice = products.productPrice;
            existing.productAvailability = products.productAvailability;
            existing.productImage = products.productImage;
            existing.productUnit = products.productUnit;
            existing.productUnit = products.productUnit;

            // Remove navigation properties from ModelState so they don't cause false validation errors
            ModelState.Remove("producersId");
            ModelState.Remove("producers");
            ModelState.Remove("categories");


            foreach (var kvp in ModelState)
            {
                foreach (var error in kvp.Value.Errors)
                {
                    Console.WriteLine($"ModelState error — Key: {kvp.Key}, Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the product no longer exists, return 404, otherwise rethrow the error
                    if (!productsExists(products.productsId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction("Index", "producerDashboard", new { activeTab = "products" });
            }

            ViewData["categoriesId"] = new SelectList(
                _context.categories, "categoriesId", "categoryName", products.categoriesId);
            return View(existing);
        }


        // Shows the delete confirmation page for a product
        [Authorize(Roles = "Producer, Admin, Developer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var products = await _context.products
                .Include(p => p.categories)
                .Include(p => p.producers)
                .FirstOrDefaultAsync(m => m.productsId == id);
            if (products == null)
            {
                return NotFound();
            }

            return View(products);
        }

        // Permanently deletes the product from the database after confirmation
        [Authorize(Roles = "Producer, Admin, Developer")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var products = await _context.products
                .Include(p => p.producers)
                .FirstOrDefaultAsync(p => p.productsId == id);

            if (products == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only the producer who owns this product can delete it
            if (products.producers.UserId != userId)
            {
                return Forbid();
            }

            _context.products.Remove(products);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "producerDashboard", new { activeTab = "products" });
        }

        // Checks whether a product with the given ID exists in the database
        private bool productsExists(int id)
        {
            return _context.products.Any(e => e.productsId == id);
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


        // Shows only products that are currently marked as available
        public async Task<IActionResult> AvailableProducts()
        {
            var availableProducts = await _context.products
                .Where(p => p.productAvailability == true)
                .ToListAsync();
            return View("Index", availableProducts);
        }






        // Searches and filters products using query text, category, stock, sorting and pagination
        public async Task<IActionResult> Search(
            string query,
            int? categoryId,
            string stockFilter,
            string sortBy,
            int page = 1,
            int pageSize = 12)
        {
            // Load categories for the filter dropdown
            ViewBag.Categories = await _context.categories
                .OrderBy(c => c.categoryName)
                .ToListAsync();

            // Store current filter values to maintain state in the view
            ViewBag.CurrentQuery = query;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentStockFilter = stockFilter;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentPage = page;

            // Start building the product query with related category and producer data
            IQueryable<products> productsQuery = _context.products
                .AsNoTracking()
                .Include(p => p.categories)
                .Include(p => p.producers);

            // Producers only search products that belong to them
            if (User.IsInRole("Producer"))
            {
                // Find the producer profile that belongs to this user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var producer = await _context.producers
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (producer != null)
                {
                    productsQuery = productsQuery
                        .Where(p => p.producersId == producer.producersId);
                }
            }

            // Apply the search text across product, category and producer fields
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim().ToLower();

                productsQuery = productsQuery.Where(p =>
                    p.productName.ToLower().Contains(query) ||
                    p.productDescription.ToLower().Contains(query) ||
                    p.categories.categoryName.ToLower().Contains(query) ||
                    p.producers.producerName.ToLower().Contains(query));
            }

            // Apply the selected category filter
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery
                    .Where(p => p.categoriesId == categoryId.Value);
            }

            // Apply the selected stock filter
            if (!string.IsNullOrEmpty(stockFilter))
            {
                productsQuery = stockFilter switch
                {
                    "in_stock" => productsQuery.Where(p => p.stockQuantity > 0),
                    "out_of_stock" => productsQuery.Where(p => p.stockQuantity == 0),
                    "low_stock" => productsQuery.Where(p => p.stockQuantity <= 10 && p.stockQuantity > 0),
                    _ => productsQuery
                };
            }

            // Apply the selected sort order
            productsQuery = sortBy switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.productPrice),
                "price_desc" => productsQuery.OrderByDescending(p => p.productPrice),
                "name_desc" => productsQuery.OrderByDescending(p => p.productName),
                "name_asc" => productsQuery.OrderBy(p => p.productName),
                _ => productsQuery.OrderBy(p => p.productName)
            };

            // Apply pagination to the filtered product list
            var totalItems = await productsQuery.CountAsync();

            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View("Index", products);
        }






        // Filters products by stock, category and sort order
        public async Task<IActionResult> FilterProducts(string stockFilter, int? categoryId, string sortBy)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Load categories for the filter dropdown
            ViewBag.Categories = await _context.categories.OrderBy(c => c.categoryName).ToListAsync();

            // Store current values to maintain state in the view
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentStockFilter = stockFilter;

            // Start building the product query with related category and producer data
            IQueryable<products> query = _context.products.Include(p => p.categories).Include(p => p.producers);

            // Producers only filter products that belong to them
            if (User.IsInRole("Producer"))
            {
                // Get the ID of the currently logged in user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    return Unauthorized();
                }

                // Find the producer profile that belongs to this user
                var producer = await _context.producers.FirstOrDefaultAsync(p => p.UserId == userId);

                if (producer == null)
                {
                    return NotFound();
                }

                query = query.Where(p => p.producersId == producer.producersId);
            }

            // Apply the selected category filter
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.categoriesId == categoryId.Value);
            }

            // Apply the selected stock filter
            if (!string.IsNullOrEmpty(stockFilter))
            {
                switch (stockFilter)
                {
                    case "in_stock":
                        query = query.Where(p => p.stockQuantity > 0);
                        break;
                    case "out_of_stock":
                        query = query.Where(p => p.stockQuantity == 0);
                        break;
                    case "low_stock":
                        query = query.Where(p => p.stockQuantity <= 10 && p.stockQuantity > 0);
                        break;
                }
            }

            // Apply the selected sort order
            switch (sortBy)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.productPrice);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.productPrice);
                    break;
                case "name_asc":
                    query = query.OrderBy(p => p.productName);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(p => p.productName);
                    break;
                default:
                    query = query.OrderBy(p => p.productsId);
                    break;
            }

            var filteredProducts = await query.ToListAsync();
            return View("Index", filteredProducts);
        }
    }
}
