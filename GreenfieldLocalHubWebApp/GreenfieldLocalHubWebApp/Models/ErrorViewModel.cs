namespace GreenfieldLocalHubWebApp.Models
{
    public class ErrorViewModel
    {
        // The ID of the current request, used to help trace errors
        public string? RequestId { get; set; }

        // True when there is a request ID available to display on the error page
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
