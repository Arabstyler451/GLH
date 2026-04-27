using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GreenfieldLocalHubWebApp.Controllers
{
    [Authorize]
    // Handles loyalty account actions: dashboard, offer redemption, transactions and account management
    public class loyaltyAccountsController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public loyaltyAccountsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // Shows the current user's loyalty dashboard with points, tier progress and available offers
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();
            ViewData["Layout"] = "_AccountLayout";

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Load the user's loyalty account with its transaction history
            var loyaltyAccount = await _context.loyaltyAccount
                .Include(l => l.loyaltyTransaction)
                .FirstOrDefaultAsync(l => l.UserId == userId);

            // If the user has no loyalty account yet, create one at Bronze tier
            if (loyaltyAccount == null)
            {
                loyaltyAccount = new loyaltyAccount
                {
                    UserId = userId,
                    pointsBalance = 0,
                    loyaltyTier = "Bronze",
                    redeemedOffers = string.Empty,
                    ActiveOffers = string.Empty
                };

                _context.loyaltyAccount.Add(loyaltyAccount);
                await _context.SaveChangesAsync();

                loyaltyAccount = await _context.loyaltyAccount
                    .Include(l => l.loyaltyTransaction)
                    .FirstOrDefaultAsync(l => l.UserId == userId);
            }

            // Build the permanent redeemed offer history
            var redeemedList = string.IsNullOrEmpty(loyaltyAccount.redeemedOffers)
                ? new List<string>()
                : loyaltyAccount.redeemedOffers.Split(',').ToList();

            // Build the consumed offer list so used offers do not reappear
            var consumedList = string.IsNullOrEmpty(loyaltyAccount.ConsumedOffers)
                ? new List<string>()
                : loyaltyAccount.ConsumedOffers.Split(',').ToList();

            // Prepare dashboard totals, tier progress and recent activity
            var pointsMonetaryValue = loyaltyAccount.pointsBalance / 100m;
            var pointsToNextTier = GetPointsToNextTier(loyaltyAccount.pointsBalance, loyaltyAccount.loyaltyTier);
            var nextTierName = GetNextTierName(loyaltyAccount.loyaltyTier);
            var tierProgress = GetTierProgress(loyaltyAccount.pointsBalance, loyaltyAccount.loyaltyTier);

            var totalPointsEarned = loyaltyAccount.loyaltyTransaction?
                .Where(t => t.transactionType == "Earn").Sum(t => t.loyaltyPoints) ?? 0;

            var totalPointsRedeemed = loyaltyAccount.loyaltyTransaction?
                .Where(t => t.transactionType == "Redeem").Sum(t => t.loyaltyPoints) ?? 0;

            var recentTransactions = loyaltyAccount.loyaltyTransaction?
                .OrderByDescending(t => t.transactionDate).Take(10).ToList() ?? new List<loyaltyTransaction>();

            var availableOffers = GetAvailableOffers(loyaltyAccount.pointsBalance, redeemedList, consumedList);

            ViewBag.PointsMonetaryValue = pointsMonetaryValue;
            ViewBag.PointsToNextTier = pointsToNextTier;
            ViewBag.NextTierName = nextTierName;
            ViewBag.TierProgress = tierProgress;
            ViewBag.TotalPointsEarned = totalPointsEarned;
            ViewBag.TotalPointsRedeemed = totalPointsRedeemed;
            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.AvailableOffers = availableOffers;

            return View(loyaltyAccount);
        }

        // Processes a loyalty offer redemption and adds the offer to the user's active offers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedeemOffer(string offerTitle, int pointsCost)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var loyaltyAccount = await _context.loyaltyAccount.FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccount == null || loyaltyAccount.pointsBalance < pointsCost)
            {
                TempData["Error"] = "Insufficient points to redeem this offer.";
                return RedirectToAction(nameof(Index));
            }

            // Build the permanent redeemed offer history
            var redeemedList = string.IsNullOrEmpty(loyaltyAccount.redeemedOffers)
                ? new List<string>()
                : loyaltyAccount.redeemedOffers.Split(',').ToList();

            // Remove the offer from consumed offers if it is being redeemed again
            var consumedList = string.IsNullOrEmpty(loyaltyAccount.ConsumedOffers)
                ? new List<string>()
                : loyaltyAccount.ConsumedOffers.Split(',').ToList();

            bool wasConsumed = consumedList.Contains(offerTitle);
            if (wasConsumed)
            {
                consumedList.Remove(offerTitle);
                loyaltyAccount.ConsumedOffers = string.Join(",", consumedList);
            }

            // Store the old tier before deducting points
            var oldTier = loyaltyAccount.loyaltyTier;

            // Create a redemption transaction record
            var transaction = new loyaltyTransaction
            {
                loyaltyAccountId = loyaltyAccount.loyaltyAccountId,
                loyaltyPoints = pointsCost,
                transactionType = "Redeem",
                transactionDate = DateTime.Now
            };

            loyaltyAccount.pointsBalance -= pointsCost;
            loyaltyAccount.loyaltyTier = GetLoyaltyTier(loyaltyAccount.pointsBalance);

            // Add the offer to redeemed history if it is not already there
            if (!redeemedList.Contains(offerTitle))
            {
                redeemedList.Add(offerTitle);
                loyaltyAccount.redeemedOffers = string.Join(",", redeemedList);
            }

            // Add the offer to active offers so it can be used at checkout
            var activeList = string.IsNullOrEmpty(loyaltyAccount.ActiveOffers)
                ? new List<string>()
                : loyaltyAccount.ActiveOffers.Split(',').ToList();

            if (!activeList.Contains(offerTitle))
            {
                activeList.Add(offerTitle);
                loyaltyAccount.ActiveOffers = string.Join(",", activeList);
            }

            _context.loyaltyTransaction.Add(transaction);
            _context.Update(loyaltyAccount);
            await _context.SaveChangesAsync();


            TempData["Success"] = $"Successfully redeemed {offerTitle}! {pointsCost} points deducted.";
            return RedirectToAction(nameof(Index));
        }





        // Moves a used loyalty offer from active to consumed after an order is placed
        public async Task ConsumeActiveOfferAsync(string userId, string offerTitle, int orderId)
        {
            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccount == null) return;

            // Remove the offer from the user's active offers
            var activeList = string.IsNullOrEmpty(loyaltyAccount.ActiveOffers)
                ? new List<string>()
                : loyaltyAccount.ActiveOffers.Split(',').ToList();

            bool removed = activeList.Remove(offerTitle);

            if (removed)
            {
                loyaltyAccount.ActiveOffers = string.Join(",", activeList);

                // Add the offer to consumed offers so it does not appear again
                var consumedList = string.IsNullOrEmpty(loyaltyAccount.ConsumedOffers)
                    ? new List<string>()
                    : loyaltyAccount.ConsumedOffers.Split(',').ToList();

                if (!consumedList.Contains(offerTitle))
                {
                    consumedList.Add(offerTitle);
                    loyaltyAccount.ConsumedOffers = string.Join(",", consumedList);
                }

                // Create a consumption transaction linked to the order
                var consumptionTransaction = new loyaltyTransaction
                {
                    loyaltyAccountId = loyaltyAccount.loyaltyAccountId,
                    ordersId = orderId,
                    loyaltyPoints = 0,
                    transactionType = "Consume",
                    transactionDate = DateTime.Now
                };
                _context.loyaltyTransaction.Add(consumptionTransaction);

                _context.Update(loyaltyAccount);
                await _context.SaveChangesAsync();
            }
        }

        // Shows the current user's loyalty transaction history with filtering and pagination
        public async Task<IActionResult> TransactionHistory(int? page, string transactionType = null)
        {
            ViewBag.CartItemCount = await GetCartItemCount();
            ViewData["Layout"] = "_AccountLayout";

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccount == null)
            {
                TempData["Info"] = "No loyalty transactions found. Start shopping to earn points!";
                return RedirectToAction(nameof(Index));
            }

            // Build the transaction query for this loyalty account
            var query = _context.loyaltyTransaction
                .Where(t => t.loyaltyAccountId == loyaltyAccount.loyaltyAccountId)
                .Include(t => t.orders)
                .AsQueryable();

            // Apply the selected transaction type filter
            if (!string.IsNullOrEmpty(transactionType) && transactionType != "All")
            {
                query = query.Where(t => t.transactionType == transactionType);
                ViewBag.CurrentType = transactionType;
            }

            // Apply pagination to the transaction list
            int pageSize = 15;
            int pageNumber = page ?? 1;
            var transactions = await query
                .OrderByDescending(t => t.transactionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TransactionTypes = new List<string> { "All", "Earn", "Redeem" };

            return View(transactions);
        }

        // Returns the current user's loyalty points as JSON for checkout
        [HttpGet]
        public async Task<IActionResult> GetPointsBalance()
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccount == null)
            {
                return Json(new { points = 0, tier = "Bronze", monetaryValue = 0 });
            }

            return Json(new
            {
                points = loyaltyAccount.pointsBalance,
                tier = loyaltyAccount.loyaltyTier,
                monetaryValue = loyaltyAccount.pointsBalance / 100m
            });
        }

        // Applies loyalty points as a checkout discount and stores the discount in session
        [HttpPost]
        public async Task<IActionResult> ApplyPoints(int pointsToRedeem)
        {
            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccount != null && loyaltyAccount.pointsBalance >= pointsToRedeem)
            {
                decimal discount = pointsToRedeem / 100m;

                // Store the redeemed points and discount amount for checkout
                HttpContext.Session.SetInt32("RedeemedPoints", pointsToRedeem);
                HttpContext.Session.SetString("LoyaltyDiscount", discount.ToString());

                return Json(new
                {
                    success = true,
                    discount = discount,
                    pointsLeft = loyaltyAccount.pointsBalance - pointsToRedeem
                });
            }

            return Json(new { success = false, message = "Insufficient points" });
        }

        // Shows the full details of a single loyalty account
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();
            if (id == null) return NotFound();

            var loyaltyAccount = await _context.loyaltyAccount
                .Include(l => l.loyaltyTransaction)
                .FirstOrDefaultAsync(m => m.loyaltyAccountId == id);

            if (loyaltyAccount == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only the account owner or an admin can view this loyalty account
            if (loyaltyAccount.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(loyaltyAccount);
        }

        // Shows the loyalty account creation page
        public IActionResult Create() => View();

        // Processes the submitted loyalty account form and saves a new account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("loyaltyAccountId,UserId,pointsBalance,loyaltyTier")] loyaltyAccount loyaltyAccount)
        {
            if (ModelState.IsValid)
            {
                // Save the loyalty account to the database
                _context.Add(loyaltyAccount);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loyaltyAccount);
        }

        // Shows the edit form for an existing loyalty account
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var loyaltyAccount = await _context.loyaltyAccount.FindAsync(id);
            if (loyaltyAccount == null) return NotFound();

            // Only admins can edit loyalty accounts directly
            if (!User.IsInRole("Admin")) return Forbid();

            return View(loyaltyAccount);
        }

        // Saves changes made to an existing loyalty account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("loyaltyAccountId,UserId,pointsBalance,loyaltyTier")] loyaltyAccount loyaltyAccount)
        {
            if (id != loyaltyAccount.loyaltyAccountId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loyaltyAccount);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the loyalty account no longer exists, return 404, otherwise rethrow the error
                    if (!loyaltyAccountExists(loyaltyAccount.loyaltyAccountId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loyaltyAccount);
        }

        // Shows the delete confirmation page for a loyalty account
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(m => m.loyaltyAccountId == id);

            if (loyaltyAccount == null) return NotFound();

            // Only admins can delete loyalty accounts directly
            if (!User.IsInRole("Admin")) return Forbid();

            return View(loyaltyAccount);
        }

        // Permanently deletes the loyalty account from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loyaltyAccount = await _context.loyaltyAccount.FindAsync(id);
            if (loyaltyAccount != null)
            {
                _context.loyaltyAccount.Remove(loyaltyAccount);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether a loyalty account with the given ID exists in the database
        private bool loyaltyAccountExists(int id)
        {
            return _context.loyaltyAccount.Any(e => e.loyaltyAccountId == id);
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
            return await _context.shoppingCartItems
                .Where(sci => sci.shoppingCartId == shoppingCart.shoppingCartId)
                .SumAsync(sci => sci.quantity);
        }

        // Calculates the user's loyalty tier from their current points balance
        private string GetLoyaltyTier(int pointsBalance)
        {
            if (pointsBalance >= 5000) return "Platinum";
            if (pointsBalance >= 2000) return "Gold";
            if (pointsBalance >= 500) return "Silver";
            return "Bronze";
        }

        // Calculates how many points are needed to reach the next loyalty tier
        private int GetPointsToNextTier(int currentPoints, string currentTier)
        {
            return currentTier switch
            {
                "Bronze" => 500 - currentPoints,
                "Silver" => 2000 - currentPoints,
                "Gold" => 5000 - currentPoints,
                "Platinum" => 0,
                _ => 0
            };
        }

        // Returns the name of the next loyalty tier
        private string GetNextTierName(string currentTier)
        {
            return currentTier switch
            {
                "Bronze" => "Silver",
                "Silver" => "Gold",
                "Gold" => "Platinum",
                "Platinum" => "Maximum Tier Reached",
                _ => "Silver"
            };
        }

        // Calculates the user's progress through their current loyalty tier
        private int GetTierProgress(int currentPoints, string currentTier)
        {
            return currentTier switch
            {
                "Bronze" => (currentPoints * 100) / 500,
                "Silver" => ((currentPoints - 500) * 100) / 1500,
                "Gold" => ((currentPoints - 2000) * 100) / 3000,
                "Platinum" => 100,
                _ => 0
            };
        }

        // Builds the list of loyalty offers and marks which ones are available to redeem
        private List<LoyaltyOfferViewModel> GetAvailableOffers(int pointsBalance, List<string> redeemedOffers, List<string> consumedOffers)
        {
            var allOffers = new List<LoyaltyOfferViewModel>
    {
        new LoyaltyOfferViewModel
        {
            Title = "10% off Fruits & Vegetables",
            Description = "10% off your next fruits and vegetable order",
            PointsCost = 200,
            IsRedeemed = redeemedOffers.Contains("10% off Fruits & Vegetables"),
            IsAvailable = pointsBalance >= 200
                          && !redeemedOffers.Contains("10% off Fruits & Vegetables")
                          && !consumedOffers.Contains("10% off Fruits & Vegetables")
        },

        new LoyaltyOfferViewModel
        {
            Title = "Free Cheese",
            Description = "One 200g cheese of your choice",
            PointsCost = 500,
            IsRedeemed = redeemedOffers.Contains("Free Cheese"),
            IsAvailable = pointsBalance >= 500
                          && !redeemedOffers.Contains("Free Cheese")
                          && !consumedOffers.Contains("Free Cheese")
        },

        new LoyaltyOfferViewModel
        {
            Title = "Free Delivery",
            Description = "Free delivery on your next order",
            PointsCost = 300,
            IsRedeemed = redeemedOffers.Contains("Free Delivery"),
            IsAvailable = pointsBalance >= 300
                          && !redeemedOffers.Contains("Free Delivery")
                          && !consumedOffers.Contains("Free Delivery")
        },

        new LoyaltyOfferViewModel
        {
            Title = "£5 Voucher",
            Description = "£5 off any order over £20",
            PointsCost = 800,
            IsRedeemed = redeemedOffers.Contains("£5 Voucher"),
            IsAvailable = pointsBalance >= 800
                          && !redeemedOffers.Contains("£5 Voucher")
                          && !consumedOffers.Contains("£5 Voucher")
        }
    };

            return allOffers;
        }

        // Represents a loyalty offer displayed on the dashboard
        public class LoyaltyOfferViewModel
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public int PointsCost { get; set; }
            public bool IsAvailable { get; set; }
            public bool IsRedeemed { get; set; }
        }
    }
}
