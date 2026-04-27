using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GreenfieldLocalHubWebApp.Models
{
    public class orderProducts
    {
        // Primary key
        public int orderProductsId { get; set; }

        // Foreign key to the order this line item belongs to
        public int ordersId { get; set; }

        // Foreign key to the product included in this order line
        public int productsId { get; set; }

        // Quantity of this product included in the order
        public int quantity { get; set; }

        // Price of the product at the time of ordering, kept even if the product price later changes
        public float unitPrice { get; set; }

        // Navigation property to the order this line item belongs to
        public orders orders { get; set; }

        // Navigation property to the product included in this order line
        public products products { get; set; }

    }
}
