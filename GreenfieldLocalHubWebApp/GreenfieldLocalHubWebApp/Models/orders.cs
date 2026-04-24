using System.Net;

namespace GreenfieldLocalHubWebApp.Models
{
    public class orders
    {
        // Primary key
        public int ordersId { get; set; }

        // Foreign key to the address table, nullable because collection orders have no delivery address
        public int? addressId { get; set; }

        // Foreign key to AspNetUsers, identifies which user placed this order
        public string UserId { get; set; }

        // Final amount the customer pays, includes product totals plus any delivery fee
        public float totalAmount { get; set; }

        // True if the customer chose home delivery for this order
        public bool delivery { get; set; }

        // True if the customer chose to collect this order in person
        public bool collection { get; set; }

        // The delivery speed chosen by the customer, e.g. Standard, First Class, or Next Day, nullable because collection orders have no delivery type
        public string? deliveryType { get; set; }

        // The fee charged for delivery, set to 0 if the customer chose collection
        public float deliveryFee { get; set; }

        // Current status of the order, starts as Pending and can progress to Processing, Completed or Cancelled
        public string orderStatus { get; set; }

        // The date the customer selected to collect their order, nullable because delivery orders have no collection date
        public DateOnly? orderCollectionDate { get; set; }

        // The date this order was placed, automatically set to today when the order is created
        public DateOnly orderDate { get; set; }


        // The following four fields are a snapshot of the delivery address at the time of ordering
        // They are copied from the address table so the order remains accurate even if the user later changes their address
        public string? DeliveryStreet { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryPostalCode { get; set; }
        public string? DeliveryCountry { get; set; }

        // Navigation property to the linked address record, nullable because collection orders have no address
        public address? address { get; set; }

        // Navigation property to the products in this order, a collection because an order can contain multiple products
        public ICollection<orderProducts>? orderProducts { get; set; }
    }
}
