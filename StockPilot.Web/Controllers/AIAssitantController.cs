using Google.GenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StockPilot.Web.Controllers
{
    [Authorize]
    [Route("api/ai")]
    public class AIAssistantController : Controller
    {
        private readonly Client _geminiClient;

        public AIAssistantController(Client geminiClient)
        {
            _geminiClient = geminiClient;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            try
            {
                // Send prompt to Gemini using the fast model
                var response = await _geminiClient.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: $"You are StockPilot AI, a helpful inventory and stock management assistant for the StockPilot system. Answer this query concisely: {request.Message}"
                );

                string reply = response.Candidates[0].Content.Parts[0].Text;
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate response from Gemini.", details = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}