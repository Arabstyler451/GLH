namespace GreenfieldLocalHubWebApp.Models
{
    public class shoppingCartItems
    {
        // Primary key
        public int shoppingCartItemsId { get; set; }

        // Foreign key to the shopping cart this item belongs to
        public int shoppingCartId { get; set; }

        // Foreign key to the product added to the cart
        public int productsId { get; set; }

        // Price of the product when it was added to the cart, kept in case the product price later changes
        public float unitPrice { get; set; }

        // Quantity of this product the user wants to buy
        public int quantity { get; set; }


        // Navigation property to the shopping cart this item belongs to
        public shoppingCart shoppingCart { get; set; }

        // Navigation property to the product this cart item represents
        public products products { get; set; }
    }
}
