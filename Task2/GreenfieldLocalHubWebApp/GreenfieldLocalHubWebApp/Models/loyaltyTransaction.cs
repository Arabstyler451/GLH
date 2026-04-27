namespace GreenfieldLocalHubWebApp.Models
{
    public class loyaltyTransaction
    {
        // Primary key
        public int loyaltyTransactionId { get; set; }

        // Foreign key to the loyalty account this transaction belongs to
        public int loyaltyAccountId { get; set; }

        // Foreign key to the linked order, nullable because some loyalty transactions are not tied to an order
        public int? ordersId { get; set; }

        // Number of points earned, redeemed or consumed in this transaction
        public int loyaltyPoints { get; set; }

        // Type of loyalty transaction, such as Earn, Redeem or Consume
        public string transactionType { get; set; }

        // The date and time this loyalty transaction was recorded
        public DateTime transactionDate { get; set; }

        // Navigation property to the loyalty account for this transaction
        public loyaltyAccount loyaltyAccount { get; set; }

        // Navigation property to the linked order, nullable because some loyalty transactions are not tied to an order
        public orders? orders { get; set; }
    }
}
