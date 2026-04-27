namespace GreenfieldLocalHubWebApp.Models
{
    public class shoppingCart
    {
        // Primary key
        public int shoppingCartId { get; set; }

        // Foreign key to AspNetUsers, identifies which user owns this shopping cart
        public string UserId { get; set; }

        // The date and time this shopping cart was created
        public DateTime shoppingCartCreatedAt { get; set; } = DateTime.Now;

        // True if the cart is active, false once it has been checked out
        public bool shoppingCartStatus { get; set; }

        // Navigation property to the items in this cart, nullable because a cart may exist before any products are added
        public ICollection<shoppingCartItems>? shoppingCartItems { get; set; }

    }
}
