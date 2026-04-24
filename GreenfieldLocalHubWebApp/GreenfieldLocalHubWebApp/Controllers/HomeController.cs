using GreenfieldLocalHubWebApp.Data;
using GreenfieldLocalHubWebApp.Models;
using GreenfieldLocalHubWebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace GreenfieldLocalHubWebApp.Controllers
{
    // Handles public site pages: home, about, contact, privacy, terms and errors
    public class HomeController : Controller
    {
        // Holds the logger used for diagnostic messages in this controller
        private readonly ILogger<HomeController> _logger;

        // Holds the database connection used throughout this controller
        private readonly ApplicationDbContext _context;


        // Receives the logger and database context via dependency injection when the controller is created
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        // Shows the home page with producer options and featured products
        public async Task<IActionResult> Index()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Load producers so the contact form can offer producer-specific enquiries
            var producers = await _context.producers.OrderBy(p => p.producerName).ToListAsync();

            ViewBag.Producers = producers;

            // Load the newest featured products with their related producer data
            var featuredProducts = await _context.products
                .Include(p => p.producers)
                .OrderByDescending(p => p.productsId)
                .Take(8)
                .ToListAsync();

            return View(featuredProducts);
        }

        // Shows the about page
        public async Task<IActionResult> About()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            return View();
        }

        // Shows the contact page with producer options
        public async Task<IActionResult> Contact()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            // Load producers for the contact form dropdown
            ViewBag.Producers = await _context.producers
                .OrderBy(p => p.producerName)
                .ToListAsync();
            return View();
        }


        // Processes the submitted contact form and sends the enquiry to a selected producer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(contactEnquiryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload producer options before redisplaying the form
                ViewBag.Producers = await _context.producers
                    .OrderBy(p => p.producerName)
                    .ToListAsync();
                return View(model);
            }

            // If a producer was selected, attempt to send the email
            if (model.ProducerId.HasValue)
            {
                var producer = await _context.producers
                    .FirstOrDefaultAsync(p => p.producersId == model.ProducerId.Value);

                // Send the email only when the selected producer has an email address
                if (producer != null && !string.IsNullOrWhiteSpace(producer.producerEmail))
                {
                    try
                    {
                        // Configure the SMTP client used to send the contact email
                        var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
                        {
                            Credentials = new System.Net.NetworkCredential(
                                "your-email@gmail.com",
                                "your-app-password"),
                            EnableSsl = true
                        };

                        // Build the producer enquiry email from the submitted form values
                        var mail = new System.Net.Mail.MailMessage
                        {
                            From = new System.Net.Mail.MailAddress("your-email@gmail.com",
                                          "Greenfield Local Hub"),
                            Subject = $"[GLH Enquiry] {model.Subject}",
                            Body = $"""
                               You have received a new enquiry via the Greenfield Local Hub contact form.

                               From:    {model.FirstName} {model.LastName}
                               Email:   {model.Email}
                               Subject: {model.Subject}

                               Message:
                               {model.Message}

                               ---
                               Please reply directly to {model.Email}
                               """,
                            IsBodyHtml = false
                        };

                        mail.To.Add(producer.producerEmail);
                        mail.ReplyToList.Add(model.Email);

                        await smtp.SendMailAsync(mail);
                    }
                    catch (Exception ex)
                    {
                        // Log the email error but still show success to the user
                        Console.WriteLine($"Contact email error: {ex.Message}");
                    }
                }
            }
            // Show the same success message even if no producer was selected

            TempData["ContactSuccess"] = "true";
            return RedirectToAction(nameof(Contact));
        }

        // Shows the privacy page
        public async Task<IActionResult> Privacy()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            return View();
        }

        // Shows the terms page
        public async Task<IActionResult> Terms()
        {
            ViewBag.CartItemCount = await GetCartItemCount();

            return View();
        }

        // Shows the error page with the current request ID
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
