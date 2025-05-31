using System.ComponentModel.DataAnnotations;

namespace AiPoweredRealTimeChatBot.Dtos;

public class InputModelDtos
{
    public Guid? SessionId { get; set; }
    [Required]
    [StringLength(4000, ErrorMessage = "Message cannot exceed 4000 characters.")]
    public string Message { get; set; } = string.Empty;
}


public class MessageDtos
{
    public Guid? Id{ get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}