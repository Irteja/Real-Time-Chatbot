
using System.ComponentModel.DataAnnotations;

namespace AiPoweredRealTimeChatBot.Models;

public class ChatMessage
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public string Sender { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = false;

    public bool IsDeleted { get; set; } = false;
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}