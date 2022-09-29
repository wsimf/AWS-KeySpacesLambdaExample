using System.ComponentModel.DataAnnotations;
using MeterReading.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeterReading.Web.API.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class MeterReaderController : ControllerBase
{
    private readonly IMeterReadingService _service;

    public MeterReaderController(IMeterReadingService service)
    {
        _service = service;
    }

    [HttpGet("{meterId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReadingsByDate([FromRoute] string meterId, [Required] [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out DateOnly parsedDate))
        {
            return BadRequest($"Invalid date {date}");
        }

        int result = await _service.CalculateSum(meterId, parsedDate);
        return Ok(new { meterId, date = parsedDate.ToString("dd-MM-yyyy"), sum = result });
    }
}