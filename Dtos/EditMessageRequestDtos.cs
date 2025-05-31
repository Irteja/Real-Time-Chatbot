using System.ComponentModel.DataAnnotations;

namespace AiPoweredRealTimeChatBot.Dtos;

public class EditMessageRequestDtos
{
    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}