using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Reflection.Emit;
using System.Text.Json;
using MatchApi.Models;

namespace MatchApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpcodeController : Controller
{
    [HttpGet]
    // GET
    public IActionResult GetOpcode( [FromQuery] int? opcode)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "opcodes.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("opcodes.json not found");
        }
        
        string jsonString = System.IO.File.ReadAllText(filePath);
        
        var options = new JsonSerializerOptions
        {
           PropertyNameCaseInsensitive = true
        };
        
        
        var result = JsonSerializer.Deserialize<OpcodeRootContainer>(jsonString, options);

        if (result == null || result.Opcodes == null)
        {
            return BadRequest("Data could not be processed."); 
        }

        if (opcode.HasValue)
        {
            var singleOpcode = result.Opcodes.FirstOrDefault(o => o.code == opcode.Value);
            if (singleOpcode == null)
                return NotFound($"Opcode {opcode.Value} not found.");
            return Ok(singleOpcode);
        }
        return Ok(result.Opcodes);
    }
    
    
    
}

public class OpcodeRootContainer
{
    public List<OpcodeModel> Opcodes { get; set; } = new();
}