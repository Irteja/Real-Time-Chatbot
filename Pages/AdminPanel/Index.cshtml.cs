using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AiPoweredRealTimeChatBot.Dtos;

namespace AiPoweredRealTimeChatBot.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminMessagesModel : PageModel
    {
        public List<AdminMessageDto> Messages { get; set; } = new List<AdminMessageDto>();

        public void OnGet()
        {
            // Initialize page
        }
    }


}