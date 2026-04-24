namespace GreenfieldLocalHubWebApp.Models
{
    public class address 
    {
        // Primary key
        public int addressId { get; set; }

        // Foreign key to AspNetUsers, identifies which user owns this address
        public string UserId { get; set; }

        // Street line of the user's address
        public string street { get; set; }

        // City or town of the user's address
        public string city { get; set; }

        // Postal code used for delivery
        public string postalCode { get; set; }

        // Country of the user's address
        public string country { get; set; }

        // True if this is the user's default address
        public bool IsDefault { get; set; }

        // The date and time this address was created, used to show the newest addresses first
        public DateTime createdDate { get; set; }


        // Navigation property to orders linked to this address, nullable because an address may exist before any orders use it
        public ICollection<orders>? orders { get; set; }
    }
}
