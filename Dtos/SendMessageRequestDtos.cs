using System.ComponentModel.DataAnnotations;

namespace AiPoweredRealTimeChatBot.Dtos;

    public class SendMessageRequestDtos
    {
        public Guid? SessionId { get; set; }
        [Required]
        [StringLength(4000)]
        public string Message { get; set; } = string.Empty;
    }