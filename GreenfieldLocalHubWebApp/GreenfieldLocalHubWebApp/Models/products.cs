namespace GreenfieldLocalHubWebApp.Models
{
    public class products
    {
        // Primary key
        public int productsId { get; set; }

        // Foreign key to the producer that owns this product
        public int producersId { get; set; }

        // Foreign key to the category this product belongs to
        public int categoriesId { get; set; }

        // Name of the product shown in the product pages
        public string productName { get; set; }

        // Description of the product shown on product pages
        public string productDescription { get; set; }

        // Number of units currently available in stock
        public int stockQuantity { get; set; }

        // Current price of the product
        public float productPrice { get; set; }

        // True if the product is currently available for purchase
        public bool productAvailability { get; set; }

        // Image path or URL used to display the product
        public string productImage { get; set; }

        // Unit of measurement for the product, such as kg, litres or pieces
        public string productUnit { get; set; }

        // Navigation property to the producer, nullable because the related producer may not be loaded
        public producers? producers { get; set; }

        // Navigation property to the category this product belongs to
        public categories categories { get; set; }

        // Navigation property to order lines for this product, nullable because a product may exist before any orders include it
        public ICollection<orderProducts>? orderProducts { get; set; }

        // Navigation property to cart items for this product, nullable because a product may exist before being added to any carts
        public ICollection<shoppingCartItems>? shoppingCartItems { get; set; }

    }
}
