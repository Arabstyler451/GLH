namespace GreenfieldLocalHubWebApp.Models
{
    public class loyaltyAccount
    {
        // Primary key
        public int loyaltyAccountId { get; set; }

        // Foreign key to AspNetUsers, identifies which user owns this loyalty account
        public string UserId { get; set; }

        // Current points available for the user to redeem
        public int pointsBalance { get; set; }

        // Current loyalty tier for the user, such as Bronze, Silver, Gold or Platinum
        public string loyaltyTier { get; set; }

        // Permanent record of offers the user has redeemed
        public string redeemedOffers { get; set; } = string.Empty;

        // Offers the user has redeemed and can still apply to an order
        public string ActiveOffers { get; set; } = string.Empty;

        // Offers the user has already used on an order
        public string ConsumedOffers { get; set; } = string.Empty;

        // Offer the user has selected to apply at checkout, nullable because no offer may be selected
        public string? PendingOffer { get; set; }



        // Navigation property to the transactions for this account, nullable because an account may exist before any transactions are recorded
        public ICollection<loyaltyTransaction>? loyaltyTransaction { get; set; }
    }
}
