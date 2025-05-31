using System.ComponentModel.DataAnnotations;

namespace AiPoweredRealTimeChatBot.Dtos;

public class SignUpUserDtos{

    [Required]
    public string Mail{get;set;}= string.Empty;
    [Required]
    public string Name{get;set;}= string.Empty;
    [Required]
    public string Phone{get;set;}= string.Empty;
    [Required]
    public string Password{get;set;}= string.Empty;

}