using AiPoweredRealTimeChatBot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace AiPoweredRealTimeChatBot.Data;

public class AiPoweredChatAppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AiPoweredChatAppDbContext(DbContextOptions<AiPoweredChatAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChatMessage> ChatMessages { get; set; }
}