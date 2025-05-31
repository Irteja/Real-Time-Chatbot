using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AiPoweredRealTimeChatBot.Models;
using System.Security.Claims;
using AiPoweredRealTimeChatBot.Data;
using AiPoweredRealTimeChatBot.Dtos;
using AiPoweredRealTimeChatBot.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AiPoweredRealTimeChatBot.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize] 
    public class ChatController : ControllerBase
    {
        private readonly AiPoweredChatAppDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly string _tavilyApiKey;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IHubContext<ChatHub> hubContext, AiPoweredChatAppDbContext dbContext, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _httpClient = httpClientFactory.CreateClient();
            _tavilyApiKey = configuration["Tavily:ApiKey"]!;
            _hubContext = hubContext;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDtos request)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User is not authenticated or UserId is invalid.");
            }

            var sessionId = request.SessionId ?? Guid.NewGuid();


            var userMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                Sender = "User",
                Message = request.Message,
                IsApproved = true,
                Timestamp = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(userMessage);
            await _dbContext.SaveChangesAsync();
            await _hubContext.Clients.Group(sessionId.ToString())
            .SendAsync("ReceiveMessage", new
            {
                userMessage.Id,
                userMessage.SessionId,
                userMessage.Sender,
                userMessage.Message,
                userMessage.Timestamp
            });
            await _hubContext.Clients.Group("admin@example.com")
            .SendAsync("RequestMessage", new
            {
                userMessage.Id,
                userMessage.SessionId,
                userMessage.UserId,
                userMessage.Sender,
                userMessage.Message,
                userMessage.IsApproved,
                userMessage.Timestamp
            });

            // Call Tavily AI API
            var aiResponse = await CallTavilyAiApi(request.Message, sessionId.ToString());
            if (aiResponse == null)
                return StatusCode(500, "Failed to get AI response");

            // Console.WriteLine("coming this far->");
            // Console.WriteLine(aiResponse);

            var botMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = userMessage.Id,
                SessionId = sessionId,
                Sender = "Bot",
                Message = aiResponse,
                Timestamp = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(botMessage);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.Group("admin@example.com")
            .SendAsync("RequestMessage", new
            {
                botMessage.Id,
                botMessage.SessionId,
                botMessage.UserId,
                botMessage.Sender,
                botMessage.Message,
                botMessage.IsApproved,
                botMessage.Timestamp
            });


            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageRequestDtos request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("User is not authenticated or UserId is invalid.");


            var userMessage = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.Sender == "User" && !m.IsDeleted);
            if (userMessage == null)
                return NotFound("Message not found or you don't have permission to edit it.");


            userMessage.Message = request.Message;
            userMessage.Timestamp = DateTime.UtcNow;
            _dbContext.ChatMessages.Update(userMessage);

            await _hubContext.Clients.Group(userMessage.SessionId.ToString())
            .SendAsync("ReceiveMessage", new
            {
                userMessage.Id,
                userMessage.SessionId,
                userMessage.Sender,
                userMessage.Message,
                userMessage.Timestamp
            });

            await _hubContext.Clients.Group("admin@example.com")
            .SendAsync("RequestMessage", new
            {
                userMessage.Id,
                userMessage.SessionId,
                userMessage.UserId,
                userMessage.Sender,
                userMessage.Message,
                userMessage.IsApproved,
                userMessage.Timestamp
            });


            var botMessage = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.SessionId == userMessage.SessionId && m.Sender == "Bot" && m.UserId == id && !m.IsDeleted);

            // Generate new AI response
            var aiResponse = await CallTavilyAiApi(request.Message, userMessage.SessionId.ToString());
            if (aiResponse == null)
                return StatusCode(500, "Failed to get AI response");

            if (botMessage != null)
            {
                botMessage.Message = aiResponse;
                botMessage.Timestamp = DateTime.UtcNow;
                botMessage.IsApproved = false;
                _dbContext.ChatMessages.Update(botMessage);
            }
            else
            {

                botMessage = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SessionId = userMessage.SessionId,
                    Sender = "Bot",
                    Message = aiResponse,
                    Timestamp = DateTime.UtcNow
                };
                _dbContext.ChatMessages.Add(botMessage);
            }

            await _dbContext.SaveChangesAsync();


            return Ok(new
            {
                UserMessage = new { userMessage.Id, userMessage.SessionId, userMessage.Sender, userMessage.Message, userMessage.Timestamp },
                BotMessage = new { botMessage.Id, botMessage.SessionId, botMessage.Sender, botMessage.Message, botMessage.Timestamp }
            });
        }


        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("User is not authenticated or UserId is invalid.");

            var sessions = await _dbContext.ChatMessages
                .Where(m => m.UserId == userId && !m.IsDeleted && m.IsApproved)
                .GroupBy(m => m.SessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    FirstMessageTime = g.Min(m => m.Timestamp)
                })
                .OrderByDescending(s => s.FirstMessageTime)
                .ToListAsync();

            return Ok(sessions.Select(s => new
            {
                SessionId = s.SessionId,
                Name = $"Chat {s.FirstMessageTime.ToString("g")}"
            }));
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] Guid sessionId)
        {
            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("User is not authenticated or UserId is invalid.");

            var messages = await _dbContext.ChatMessages
                .Where(m => m.SessionId == sessionId && !m.IsDeleted && m.IsApproved)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.Sender,
                    m.Message,
                    m.Timestamp
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            var userIdString = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("User is not authenticated or UserId is invalid.");

            var message = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.Sender == "User" && !m.IsDeleted);
            if (message == null)
                return NotFound("Message not found or you don't have permission to delete it.");

            message.IsDeleted = true;
            _dbContext.ChatMessages.Update(message);
            await _dbContext.SaveChangesAsync();


            var botMessage = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.SessionId == message.SessionId && m.Sender == "Bot" && m.UserId == id && !m.IsDeleted);

            if (botMessage != null)
            {
                botMessage.IsDeleted = true;
                _dbContext.ChatMessages.Update(botMessage);
                await _dbContext.SaveChangesAsync();


                await _hubContext.Clients.Group(message.SessionId.ToString())
                    .SendAsync("MessageDeleted", botMessage.Id);
            }


            await _hubContext.Clients.Group(message.SessionId.ToString())
                .SendAsync("MessageDeleted", message.Id);

            return NoContent();
        }

        private async Task<string> CallTavilyAiApi(string message, string sessionId)
        {
            try
            {
                var requestBody = new
                {
                    api_key = _tavilyApiKey,
                    query = message,
                    include_answer = "basic"
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.tavily.com/search", content); // Replace with actual endpoint

                if (!response.IsSuccessStatusCode)
                    return null!;
                // Console.WriteLine("from here->");

                var responseContent = await response.Content.ReadAsStringAsync();
                // Console.WriteLine(responseContent);
                // Parse response (adjust based on Tavily AI's response format)
                var jsonDoc = JsonDocument.Parse(responseContent);
                var aiMessage = jsonDoc.RootElement.GetProperty("answer").GetString(); // Example property; adjust as needed

                return aiMessage ?? "Sorry, I couldn't generate a response.";
            }
            catch
            {
                return null!;
            }
        }

        [HttpGet("admin/history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminHistory()
        {
            var messages = await _dbContext.ChatMessages
                            .OrderByDescending(m => m.Timestamp)
                            .Select(m => new
                            {
                                m.Id,
                                m.UserId,
                                m.SessionId,
                                m.Sender,
                                m.Message,
                                m.Timestamp,
                                m.IsApproved
                            })
                            .ToListAsync();

            return Ok(messages);
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveMessage(Guid id)
        {
            var message = await _dbContext.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (message == null)
                return NotFound("Message not found.");

            if (message.IsApproved)
                return BadRequest("Message is already approved.");

            message.IsApproved = true;
            _dbContext.ChatMessages.Update(message);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.Group(message.SessionId.ToString())
            .SendAsync("ReceiveMessage", new
            {
                message.Id,
                message.SessionId,
                message.UserId,
                message.Sender,
                message.Message,
                message.Timestamp
            });

            return NoContent();
        }
    }


}