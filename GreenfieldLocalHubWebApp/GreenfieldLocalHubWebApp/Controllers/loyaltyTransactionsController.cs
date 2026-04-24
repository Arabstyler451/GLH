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
    // Handles all loyalty transaction actions: viewing, creating, editing and deleting transactions
    public class loyaltyTransactionsController : Controller
    {
        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;

        // Receives the database context via dependency injection when the controller is created
        public loyaltyTransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows a list of all loyalty transactions with their related accounts and orders
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.loyaltyTransaction.Include(l => l.loyaltyAccount).Include(l => l.orders);
            return View(await applicationDbContext.ToListAsync());
        }

        // Shows the full details of a single loyalty transaction
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loyaltyTransaction = await _context.loyaltyTransaction
                .Include(l => l.loyaltyAccount)
                .Include(l => l.orders)
                .FirstOrDefaultAsync(m => m.loyaltyTransactionId == id);
            if (loyaltyTransaction == null)
            {
                return NotFound();
            }

            return View(loyaltyTransaction);
        }

        // Shows the loyalty transaction creation page with account and order options
        public IActionResult Create()
        {
            ViewData["loyaltyAccountId"] = new SelectList(_context.loyaltyAccount, "loyaltyAccountId", "loyaltyAccountId");
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId");
            return View();
        }

        // Processes the submitted loyalty transaction form and saves a new transaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("loyaltyTransactionId,loyaltyAccountId,ordersId,loyaltyPoints,transactionType,transactionDate")] loyaltyTransaction loyaltyTransaction)
        {
            if (ModelState.IsValid)
            {
                // Save the loyalty transaction to the database
                _context.Add(loyaltyTransaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["loyaltyAccountId"] = new SelectList(_context.loyaltyAccount, "loyaltyAccountId", "loyaltyAccountId", loyaltyTransaction.loyaltyAccountId);
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId", loyaltyTransaction.ordersId);
            return View(loyaltyTransaction);
        }

        // Shows the edit form for an existing loyalty transaction
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loyaltyTransaction = await _context.loyaltyTransaction.FindAsync(id);
            if (loyaltyTransaction == null)
            {
                return NotFound();
            }
            ViewData["loyaltyAccountId"] = new SelectList(_context.loyaltyAccount, "loyaltyAccountId", "loyaltyAccountId", loyaltyTransaction.loyaltyAccountId);
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId", loyaltyTransaction.ordersId);
            return View(loyaltyTransaction);
        }

        // Saves changes made to an existing loyalty transaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("loyaltyTransactionId,loyaltyAccountId,ordersId,loyaltyPoints,transactionType,transactionDate")] loyaltyTransaction loyaltyTransaction)
        {
            if (id != loyaltyTransaction.loyaltyTransactionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loyaltyTransaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If the loyalty transaction no longer exists, return 404, otherwise rethrow the error
                    if (!loyaltyTransactionExists(loyaltyTransaction.loyaltyTransactionId))
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
            ViewData["loyaltyAccountId"] = new SelectList(_context.loyaltyAccount, "loyaltyAccountId", "loyaltyAccountId", loyaltyTransaction.loyaltyAccountId);
            ViewData["ordersId"] = new SelectList(_context.Set<orders>(), "ordersId", "ordersId", loyaltyTransaction.ordersId);
            return View(loyaltyTransaction);
        }

        // Shows the delete confirmation page for a loyalty transaction
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loyaltyTransaction = await _context.loyaltyTransaction
                .Include(l => l.loyaltyAccount)
                .Include(l => l.orders)
                .FirstOrDefaultAsync(m => m.loyaltyTransactionId == id);
            if (loyaltyTransaction == null)
            {
                return NotFound();
            }

            return View(loyaltyTransaction);
        }

        // Permanently deletes the loyalty transaction from the database after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loyaltyTransaction = await _context.loyaltyTransaction.FindAsync(id);
            if (loyaltyTransaction != null)
            {
                _context.loyaltyTransaction.Remove(loyaltyTransaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks whether a loyalty transaction with the given ID exists in the database
        private bool loyaltyTransactionExists(int id)
        {
            return _context.loyaltyTransaction.Any(e => e.loyaltyTransactionId == id);
        }
    }
}
