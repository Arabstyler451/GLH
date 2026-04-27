using GreenfieldLocalHubWebApp.Models;

namespace GreenfieldLocalHubWebApp.ViewModels
{
    public class productDetailsViewModel
    {
        // Product shown on the details page
        public products Product { get; set; }

        // Related products shown alongside the main product
        public IEnumerable<products> RelatedProducts { get; set; } = new List<products>();
    }
}
