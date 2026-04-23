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
using Microsoft.AspNetCore.Authorization;

namespace GreenfieldLocalHubWebApp.Controllers
{
    // Handles all order-related actions: viewing, creating, editing and deleting orders
    public class ordersController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public ordersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of orders - what the user sees depends on their role
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Get the ID of the currently logged in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Admins see every order in the system
            if (User.IsInRole("Admin"))
            {
                var allOrders = await _context.orders.Include(o => o.orderProducts).ThenInclude(op => op.products).ToListAsync();

                ViewData["Layout"] = "_AccountLayout";
                return View(allOrders);
            }

            // Producers only see orders that contain their own products
            else if (User.IsInRole("Producer"))
            {
                // Get the IDs of products that belong to this producer
                var producerProducts = await _context.products.Where(p => p.producers.UserId == userId).Select(p => p.productsId).ToListAsync();

                // Find all order lines that contain any of this producer's products
                var producerOrders = await _context.orderProducts.Where(op => producerProducts.Contains(op.productsId)).Include(op => op.orders).Include(op => op.products).ToListAsync();

                ViewData["Layout"] = "_AccountLayout";

                // Return distinct orders so the same order doesn't appear twice
                return View(producerOrders.Select(vo => vo.orders).Distinct().ToList());
            }
            else
            {
                // Regular users only see their own orders
                var orders = await _context.orders.Where(o => o.UserId == userId).Include(o => o.orderProducts).ThenInclude(op => op.products).ToListAsync();

                ViewData["Layout"] = "_AccountLayout";
                return View(orders);
            }

        }


        // Shows the full details of a single order including all products and their producers
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            if (id == null)
            {
                return NotFound();
            }

            // Load all line items for this order along with the related order and product data
            var orderProducts = await _context.orderProducts
                .Where(op => op.ordersId == id)
                .Include(op => op.orders)
                .Include(op => op.products)
                    .ThenInclude(p => p.producers)
                .ToListAsync();

            if (!orderProducts.Any())
            {
                return NotFound();
            }

            return View(orderProducts);
        }

        // Loads the order creation page with the user's saved addresses and calculated cart totals
        public async Task<IActionResult> Create(int shoppingCartId, int? selectedAddressId = null)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            ViewBag.shoppingCartId = shoppingCartId;

            // Load the user's saved addresses to populate the delivery address dropdown
            var userAddresses = await _context.address
                .Where(a => a.UserId == userId)
                .ToListAsync();

            // Tells the view whether to prompt the user to add an address
            ViewBag.HasAddresses = userAddresses.Any();
            ViewData["AddressId"] = new SelectList(userAddresses, "addressId", "street", selectedAddressId);

            // Calculate subtotal, any loyalty discount, and the final total to display on the page
            var (subtotalBeforeDiscount, loyaltyDiscount, cartTotalAfterDiscount) =
                await CalculateOrderTotals(userId, shoppingCartId);

            // Check if the user has a Free Delivery offer waiting to be used
            var loyaltyAccountForView = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            ViewBag.CartTotal = cartTotalAfterDiscount;
            ViewBag.SubtotalBeforeDiscount = subtotalBeforeDiscount;
            ViewBag.LoyaltyDiscount = loyaltyDiscount;
            ViewBag.HasFreeDelivery = loyaltyAccountForView?.PendingOffer == "Free Delivery";

            return View();
        }


        // Processes the submitted order form, saves the order, deducts stock and awards loyalty points
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ordersId,addressId,deliveryType,orderCollectionDate")] orders orders, int shoppingCartId, string fulfilmentChoice)
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                ViewBag.shoppingCartId = shoppingCartId;
                return View(orders);
            }

            // Convert the fulfilment radio button value into the two bool fields on the model
            orders.delivery = fulfilmentChoice == "Delivery";
            orders.collection = fulfilmentChoice == "Collection";

            // Set fields that should never come from the form
            orders.UserId = userId;
            orders.orderDate = DateOnly.FromDateTime(DateTime.Today);
            orders.orderStatus = "Pending";

            // Remove server-set fields from ModelState so they don't cause false validation errors
            ModelState.Remove("UserId");
            ModelState.Remove("orderStatus");
            ModelState.Remove("orderDate");
            ModelState.Remove("totalAmount");
            ModelState.Remove("deliveryFee");
            ModelState.Remove("delivery");
            ModelState.Remove("collection");
            ModelState.Remove("DeliveryStreet");
            ModelState.Remove("DeliveryCity");
            ModelState.Remove("DeliveryPostalCode");
            ModelState.Remove("DeliveryCountry");

            // Collection orders don't need a delivery type or address
            if (orders.collection)
            {
                ModelState.Remove("deliveryType");
                ModelState.Remove("addressId");
                orders.addressId = null;
            }

            // Delivery orders don't need a collection date
            if (orders.delivery)
            {
                ModelState.Remove("orderCollectionDate");
            }

            if (!orders.collection && !orders.delivery)
                ModelState.AddModelError("delivery", "Please select either delivery or collection.");

            // Collection requires a date that is at least 2 days from today
            if (orders.collection)
            {
                if (orders.orderCollectionDate == null)
                    ModelState.AddModelError("orderCollectionDate", "Collection date is required.");
                else if (orders.orderCollectionDate.Value < DateOnly.FromDateTime(DateTime.Today.AddDays(2)))
                    ModelState.AddModelError("orderCollectionDate", "Collection date must be at least 2 days from today.");
            }

            // Delivery requires a delivery type and a valid address that belongs to this user
            if (orders.delivery)
            {
                if (string.IsNullOrWhiteSpace(orders.deliveryType))
                    ModelState.AddModelError("deliveryType", "Please select a delivery type.");

                if (!orders.addressId.HasValue || orders.addressId.Value <= 0)
                    ModelState.AddModelError("addressId", "Please select a delivery address.");

                // Make sure the selected address actually belongs to the logged in user
                if (orders.addressId.HasValue)
                {
                    var addressExists = await _context.address
                        .AnyAsync(a => a.addressId == orders.addressId.Value && a.UserId == userId);

                    if (!addressExists)
                        ModelState.AddModelError("addressId", "Invalid delivery address selected.");
                }
            }

            // Find the active cart that belongs to this user
            var cart = await _context.shoppingCart
                .FirstOrDefaultAsync(sc => sc.shoppingCartId == shoppingCartId
                                        && sc.UserId == userId
                                        && sc.shoppingCartStatus);

            if (cart == null)
                return NotFound();

            // Load all items in the cart along with their product and category info
            var cartItems = await _context.shoppingCartItems
                .Where(sci => sci.shoppingCartId == shoppingCartId)
                .Include(sci => sci.products)
                    .ThenInclude(p => p.categories)
                .ToListAsync();

            if (!cartItems.Any())
            {
                ModelState.AddModelError("", "Your shopping cart is empty.");
                ViewBag.shoppingCartId = shoppingCartId;
                return View(orders);
            }

            // Calculate the final totals including any loyalty discount
            var (subtotalBeforeDiscount, loyaltyDiscount, cartTotalAfterDiscount) =
                await CalculateOrderTotals(userId, shoppingCartId);

            // Work out the delivery fee based on order total, loyalty offer and chosen delivery type
            decimal deliveryFee = 0m;
            if (orders.delivery)
            {
                var loyaltyAccountForDelivery = await _context.loyaltyAccount
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                bool hasFreeDelivery = loyaltyAccountForDelivery?.PendingOffer == "Free Delivery";

                // Free delivery if the user has the offer or the cart total is £30 or more
                deliveryFee = hasFreeDelivery || cartTotalAfterDiscount >= 30m ? 0m :
                    orders.deliveryType switch
                    {
                        "First Class" => 5.50m,
                        "Next Day" => 7.99m,
                        _ => 3.50m
                    };
            }

            orders.deliveryFee = (float)deliveryFee;
            orders.totalAmount = (float)(cartTotalAfterDiscount + deliveryFee);

            // Snapshot the delivery address fields so the order retains the address even if the user later changes it
            if (orders.addressId.HasValue)
            {
                var selectedAddress = await _context.address.FindAsync(orders.addressId.Value);
                if (selectedAddress != null)
                {
                    orders.DeliveryStreet = selectedAddress.street;
                    orders.DeliveryCity = selectedAddress.city;
                    orders.DeliveryPostalCode = selectedAddress.postalCode;
                    orders.DeliveryCountry = selectedAddress.country;
                }
            }

            foreach (var kvp in ModelState)
            {
                foreach (var error in kvp.Value.Errors)
                {
                    Console.WriteLine($"ModelState error — Key: {kvp.Key}, Error: {error.ErrorMessage}");
                }
            }

            // Check that the pending loyalty offer conditions are still met by the current cart contents
            var loyaltyAccountForValidation = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (loyaltyAccountForValidation != null &&
                !string.IsNullOrEmpty(loyaltyAccountForValidation.PendingOffer))
            {
                var pendingOffer = loyaltyAccountForValidation.PendingOffer;
                bool offerConditionMet = true;
                string offerWarning = null;

                // The 10% Fruit & Veg offer requires at least one Fruit & Veg item in the cart
                if (pendingOffer == "10% off Fruits & Vegetables")
                {
                    bool hasFruitVeg = cartItems.Any(item =>
                        item.products.categories != null &&
                        string.Equals(item.products.categories.categoryName?.Trim(),
                            "Fruit & Veg", StringComparison.OrdinalIgnoreCase));

                    if (!hasFruitVeg)
                    {
                        offerConditionMet = false;
                        offerWarning = "Your cart contains no Fruit & Veg items. " +
                                       "Remove the offer or add qualifying products to continue.";
                    }
                }
                // The Free Cheese offer requires at least one cheese product in the cart
                else if (pendingOffer == "Free Cheese")
                {
                    bool hasCheese = cartItems.Any(item =>
                        item.products.productName?.Contains("Cheese",
                            StringComparison.OrdinalIgnoreCase) == true);

                    if (!hasCheese)
                    {
                        offerConditionMet = false;
                        offerWarning = "Your cart contains no Cheese products. " +
                                       "Remove the offer or add a cheese product to continue.";
                    }
                }
                // The £5 Voucher requires a minimum cart subtotal of £20
                else if (pendingOffer == "£5 Voucher")
                {
                    var subtotalCheck = cartItems.Sum(i =>
                        (decimal)i.products.productPrice * i.quantity);

                    if (subtotalCheck < 20m)
                    {
                        offerConditionMet = false;
                        offerWarning = "Your order must be at least £20.00 to use the £5 Voucher. " +
                                       "Remove the offer or add more items to continue.";
                    }
                }

                // If the offer conditions aren't met, show an error and redisplay the form
                if (!offerConditionMet)
                {
                    ModelState.AddModelError(string.Empty, offerWarning);

                    var (sub, disc, total) = await CalculateOrderTotals(userId, shoppingCartId);
                    ViewBag.CartTotal = total;
                    ViewBag.SubtotalBeforeDiscount = sub;
                    ViewBag.LoyaltyDiscount = disc;
                    ViewBag.HasFreeDelivery = false;
                    ViewBag.shoppingCartId = shoppingCartId;

                    var userAddresses = await _context.address
                        .Where(a => a.UserId == userId)
                        .ToListAsync();
                    ViewBag.HasAddresses = userAddresses.Any();
                    ViewData["AddressId"] = new SelectList(
                        userAddresses, "addressId", "street", orders.addressId);

                    return View(orders);
                }
            }

            // If any validation errors remain, repopulate the form data and show the page again
            if (!ModelState.IsValid)
            {
                ViewBag.CartTotal = cartTotalAfterDiscount;
                ViewBag.SubtotalBeforeDiscount = subtotalBeforeDiscount;
                ViewBag.LoyaltyDiscount = loyaltyDiscount;
                ViewBag.shoppingCartId = shoppingCartId;

                var userAddresses = await _context.address
                    .Where(a => a.UserId == userId)
                    .ToListAsync();
                ViewBag.HasAddresses = userAddresses.Any();
                ViewData["AddressId"] = new SelectList(userAddresses, "addressId", "street", orders.addressId);

                return View(orders);
            }

            // Save the order to the database
            _context.orders.Add(orders);
            await _context.SaveChangesAsync();

            // Store the order ID for the redirect
            int newOrderId = orders.ordersId;

            // Add each cart item as an order line and reduce the product's stock quantity
            foreach (var item in cartItems)
            {
                // Block the order if there isn't enough stock for any item
                if (item.products.stockQuantity < item.quantity)
                {
                    ModelState.AddModelError("", $"Sorry, we only have {item.products.stockQuantity} units of {item.products.productName} in stock.");
                    ViewBag.CartTotal = cartTotalAfterDiscount;
                    ViewBag.SubtotalBeforeDiscount = subtotalBeforeDiscount;
                    ViewBag.LoyaltyDiscount = loyaltyDiscount;
                    ViewBag.shoppingCartId = shoppingCartId;
                    return View(orders);
                }

                _context.orderProducts.Add(new orderProducts
                {
                    ordersId = orders.ordersId,
                    productsId = item.productsId,
                    quantity = item.quantity,
                    unitPrice = item.unitPrice
                });

                // Reduce stock so the purchased quantity is no longer available
                item.products.stockQuantity -= item.quantity;
            }

            // Mark the cart as inactive so it can no longer be used
            cart.shoppingCartStatus = false;
            await _context.SaveChangesAsync();

            // Award loyalty points and consume any pending offer
            try
            {
                var loyaltyAccountFinal = await _context.loyaltyAccount
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                // If the user has no loyalty account yet, create one at Bronze tier
                if (loyaltyAccountFinal == null)
                {
                    loyaltyAccountFinal = new loyaltyAccount
                    {
                        UserId = userId,
                        pointsBalance = 0,
                        loyaltyTier = "Bronze",
                        redeemedOffers = string.Empty,
                        ActiveOffers = string.Empty,
                        ConsumedOffers = string.Empty
                    };
                    _context.loyaltyAccount.Add(loyaltyAccountFinal);
                    await _context.SaveChangesAsync();
                }

                // If the user had a pending offer, move it from active to consumed and log the transaction
                if (!string.IsNullOrEmpty(loyaltyAccountFinal.PendingOffer))
                {
                    var offerToConsume = loyaltyAccountFinal.PendingOffer;

                    var activeList = string.IsNullOrEmpty(loyaltyAccountFinal.ActiveOffers)
                        ? new List<string>()
                        : loyaltyAccountFinal.ActiveOffers.Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                    var consumedList = string.IsNullOrEmpty(loyaltyAccountFinal.ConsumedOffers)
                        ? new List<string>()
                        : loyaltyAccountFinal.ConsumedOffers.Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                    // Remove from active and add to consumed
                    activeList.Remove(offerToConsume);

                    if (!consumedList.Contains(offerToConsume))
                        consumedList.Add(offerToConsume);

                    loyaltyAccountFinal.ActiveOffers = string.Join(",", activeList);
                    loyaltyAccountFinal.ConsumedOffers = string.Join(",", consumedList);
                    loyaltyAccountFinal.PendingOffer = null;

                    _context.loyaltyTransaction.Add(new loyaltyTransaction
                    {
                        loyaltyAccountId = loyaltyAccountFinal.loyaltyAccountId,
                        ordersId = orders.ordersId,
                        loyaltyPoints = 0,
                        transactionType = "Consume",
                        transactionDate = DateTime.Now
                    });
                }

                // Award 10 points for every £1 spent on the order
                int pointsEarned = (int)(orders.totalAmount * 10);

                _context.loyaltyTransaction.Add(new loyaltyTransaction
                {
                    loyaltyAccountId = loyaltyAccountFinal.loyaltyAccountId,
                    ordersId = orders.ordersId,
                    loyaltyPoints = pointsEarned,
                    transactionType = "Earn",
                    transactionDate = DateTime.Now
                });

                loyaltyAccountFinal.pointsBalance += pointsEarned;

                // Recalculate the tier based on the new points balance
                loyaltyAccountFinal.loyaltyTier = loyaltyAccountFinal.pointsBalance switch
                {
                    >= 5000 => "Platinum",
                    >= 2000 => "Gold",
                    >= 500 => "Silver",
                    _ => "Bronze"
                };

                await _context.SaveChangesAsync();

                TempData["LoyaltyMessage"] = $"You earned {pointsEarned} loyalty points!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loyalty error: {ex.Message}");
            }

            // Send the user to the details page of the order they just placed
            return RedirectToAction("Details", "orders", new { id = newOrderId });
        }


        // Shows the edit form for an existing order
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orders = await _context.orders.FindAsync(id);
            if (orders == null)
            {
                return NotFound();
            }
            ViewData["addressId"] = new SelectList(_context.address, "addressId", "addressId", orders.addressId);
            return View(orders);
        }


        // Saves changes made to an existing order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ordersId,addressId,UserId,totalAmount,delivery,collection,deliveryType,orderStatus,orderCollectionDate,orderDate,DeliveryStreet,DeliveryCity,DeliveryPostalCode,DeliveryCountry")] orders orders)
        {
            if (id != orders.ordersId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orders);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the order no longer exists, return 404, otherwise rethrow the error
                    if (!ordersExists(orders.ordersId))
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
            ViewData["addressId"] = new SelectList(_context.address, "addressId", "addressId", orders.addressId);
            return View(orders);
        }

        // Shows the delete confirmation page for an order
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orders = await _context.orders
                .Include(o => o.address)
                .FirstOrDefaultAsync(m => m.ordersId == id);
            if (orders == null)
            {
                return NotFound();
            }

            return View(orders);
        }

        // Permanently deletes the order from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orders = await _context.orders.FindAsync(id);
            if (orders != null)
            {
                _context.orders.Remove(orders);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether an order with the given ID exists in the database
        private bool ordersExists(int id)
        {
            return _context.orders.Any(e => e.ordersId == id);
        }


        // Logs a transaction record for each offer that was consumed on an order
        private async Task LogOfferConsumption(int loyaltyAccountId, int orderId, IEnumerable<string> consumedOffers)
        {
            foreach (var offer in consumedOffers.Distinct())
            {
                var transaction = new loyaltyTransaction
                {
                    loyaltyAccountId = loyaltyAccountId,
                    ordersId = orderId,
                    loyaltyPoints = 0,
                    transactionType = "Consume",
                    transactionDate = DateTime.Now
                };
                _context.loyaltyTransaction.Add(transaction);
            }
            await _context.SaveChangesAsync();
        }


        // Calculates the cart subtotal, the discount from any active loyalty offer, and the final total
        private async Task<(decimal subtotalBeforeDiscount, decimal loyaltyDiscount, decimal cartTotalAfterDiscount)> CalculateOrderTotals(string userId, int shoppingCartId)
        {
            var items = await _context.shoppingCartItems
                .Where(sci => sci.shoppingCartId == shoppingCartId)
                .Include(sci => sci.products)
                    .ThenInclude(p => p.categories)
                .ToListAsync();

            if (!items.Any())
                return (0m, 0m, 0m);

            decimal subtotal = items.Sum(i => (decimal)i.products.productPrice * i.quantity);
            decimal discount = 0m;

            var loyaltyAccount = await _context.loyaltyAccount
                .FirstOrDefaultAsync(l => l.UserId == userId);

            var pendingOffer = loyaltyAccount?.PendingOffer;

            if (!string.IsNullOrEmpty(pendingOffer))
            {
                switch (pendingOffer)
                {
                    // Apply 10% discount to all Fruit & Veg items in the cart
                    case "10% off Fruits & Vegetables":
                        foreach (var item in items)
                        {
                            if (item.products.categories != null &&
                                string.Equals(item.products.categories.categoryName?.Trim(),
                                    "Fruit & Veg", StringComparison.OrdinalIgnoreCase))
                            {
                                discount += (decimal)item.products.productPrice * item.quantity * 0.10m;
                            }
                        }
                        break;

                    // Discount the full price of any cheese products in the cart
                    case "Free Cheese":
                        foreach (var item in items)
                        {
                            if (item.products.productName?.Contains("Cheese",
                                    StringComparison.OrdinalIgnoreCase) == true)
                            {
                                discount += (decimal)item.products.productPrice * item.quantity;
                            }
                        }
                        break;

                    // Apply a flat £5 discount if the subtotal is at least £20
                    case "£5 Voucher":
                        if (subtotal >= 20m)
                            discount += 5m;
                        break;
                }
            }

            return (subtotal, discount, subtotal - discount);
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