using Google.GenAI;
using Microsoft.AspNetCore.Mvc;

public class InventoryAiController : Controller
{
    private readonly Client _geminiClient;

    public InventoryAiController(Client geminiClient)
    {
        _geminiClient = geminiClient;
    }

    [HttpGet]
    public async Task<IActionResult> AnalyzeStock()
    {
        var response = await _geminiClient.Models.GenerateContentAsync(
            model: "gemini-2.5-flash",
            contents: "Analyze stock trends for a warehouse system."
        );

        return Content(response.Candidates[0].Content.Parts[0].Text);
    }
}