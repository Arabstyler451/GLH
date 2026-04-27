using GreenfieldLocalHubWebApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GreenfieldLocalHubWebApp.Controllers
{
    [Authorize(Roles = "Producer")]
    // Handles the producer dashboard and producer order status updates
    public class producerDashboardController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public producerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }


        // Shows the producer dashboard with products, stock totals and recent orders
        public async Task<IActionResult> Index(string activeTab = null)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the producer profile that belongs to this user
            var producer = await _context.producers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null) return NotFound();

            // Load all products owned by this producer
            var products = await _context.products
                .Where(p => p.producersId == producer.producersId)
                .ToListAsync();

            // Load all orders that contain this producer's products
            var orders = await _context.orders
                .Include(o => o.orderProducts)
                    .ThenInclude(op => op.products)
                .Where(o => o.orderProducts
                    .Any(op => op.products.producersId == producer.producersId))
                .ToListAsync();

            // Load email addresses for the users who placed these orders
            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email);

            // Prepare dashboard summary values for the view
            ViewBag.totalProducts = products.Count;
            ViewBag.lowStockProducts = products.Count(p => p.stockQuantity < 5);
            ViewBag.totalStockUnits = products.Sum(p => p.stockQuantity);
            ViewBag.recentOrders = orders;
            ViewBag.userEmails = users;
            ViewBag.activeTab = activeTab ?? "products";

            return View(products);
        }

        // Advances an order to the next status for the logged in producer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the producer profile that belongs to this user
            var producer = await _context.producers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null) return NotFound();

            // Load the selected order only if it contains this producer's products
            var order = await _context.orders
                .Include(o => o.orderProducts)
                    .ThenInclude(op => op.products)
                .FirstOrDefaultAsync(o => o.ordersId == orderId &&
                    o.orderProducts.Any(op => op.products.producersId == producer.producersId));

            if (order == null) return NotFound();

            // Move collection and delivery orders through their own status flows
            order.orderStatus = order.collection
                ? order.orderStatus switch
                {
                    "Pending" => "Processing",
                    "Processing" => "Ready to Collect",
                    "Ready to Collect" => "Collected",
                    _ => order.orderStatus
                }
                : order.orderStatus switch
                {
                    "Pending" => "Processing",
                    "Processing" => "Dispatched",
                    "Dispatched" => "Delivered",
                    _ => order.orderStatus
                };

            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { activeTab = "orders" });
        }
    }
}
