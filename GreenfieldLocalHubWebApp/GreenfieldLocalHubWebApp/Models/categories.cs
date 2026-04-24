namespace GreenfieldLocalHubWebApp.Models
{
    public class categories
    {
        // Primary key
        public int categoriesId { get; set; }

        // Name of the product category shown in the shop and filters
        public string categoryName { get; set; }

        
        // Navigation property to products in this category, nullable because a category may exist before any products are added
        public ICollection<products>? products { get; set; }
    }
}
