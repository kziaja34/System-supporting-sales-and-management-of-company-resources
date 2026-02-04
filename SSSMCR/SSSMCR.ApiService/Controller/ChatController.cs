using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAiAssistantService _aiService;

    public ChatController(IAiAssistantService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return BadRequest("Message is empty");
        }

        var answer = await _aiService.GetAnswerAsync(request);
        
        return Ok(new ChatResponse 
        { 
            AiResponse = answer 
        });
    }
}