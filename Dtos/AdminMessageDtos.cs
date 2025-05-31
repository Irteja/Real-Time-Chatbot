namespace AiPoweredRealTimeChatBot.Dtos;

public class AdminMessageDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsApproved { get; set; }
}