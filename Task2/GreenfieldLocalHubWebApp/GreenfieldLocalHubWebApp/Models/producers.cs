namespace GreenfieldLocalHubWebApp.Models
{
    public class producers
    {
        // Primary key
        public int producersId { get; set; }

        // Foreign key to AspNetUsers, identifies which user manages this producer profile
        public string UserId { get; set; }

        // Name of the producer shown in the catalogue
        public string producerName { get; set; }

        // Email address used to contact the producer
        public string producerEmail { get; set; }

        // Phone number used to contact the producer
        public string producerPhone { get; set; }

        // Description of the producer's business or products
        public string producerDescription { get; set; }

        // Location of the producer, such as their city or region
        public string producerLocation { get; set; }

        // Image path or URL used to display the producer
        public string producerImage { get; set; }

        // Navigation property to products owned by this producer, nullable because a producer may exist before any products are added
        public ICollection<products>? products { get; set; }
    }
}
