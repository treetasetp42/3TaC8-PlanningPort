using System.ComponentModel.DataAnnotations;

namespace _3TaC8_PlanningPort.DTOs
{
    public class LinkGoogleRequest
    {
        [Required, StringLength(8192, MinimumLength = 20)]
        public string Credential { get; set; } = string.Empty;
    }
}
