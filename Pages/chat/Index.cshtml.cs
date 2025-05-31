using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AiPoweredRealTimeChatBot.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AiPoweredRealTimeChatBot.Pages
{
    [Authorize]
    public class ChatModel : PageModel
    {
        public ChatModel()
        {
        }

        [BindProperty]
        public InputModelDtos Input { get; set; } = new InputModelDtos();

        public List<MessageDtos> Messages { get; set; } = new List<MessageDtos>();

        public void OnGet()
        {
            Input.SessionId = Guid.NewGuid();
            // Initialize page
        }
    }
}