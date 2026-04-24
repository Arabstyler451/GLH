using System.ComponentModel.DataAnnotations;

namespace GreenfieldLocalHubWebApp.ViewModels
{
    public class contactEnquiryViewModel
    {
        // First name entered by the user on the contact form
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }

        // Last name entered by the user on the contact form
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }

        // Email address used so the producer can reply to the enquiry
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        // Subject selected by the user for the enquiry
        [Required(ErrorMessage = "Please select a subject.")]
        public string Subject { get; set; }

        // ID of the selected producer, nullable because the user may send a general enquiry
        public int? ProducerId { get; set; }

        // Message entered by the user for the enquiry
        [Required(ErrorMessage = "Please enter a message.")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters.")]
        public string Message { get; set; }
    }
}
