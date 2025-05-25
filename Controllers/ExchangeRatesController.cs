using ContractApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeRatesController : ControllerBase
{
    private readonly ExchangeRateService _service;

    public ExchangeRatesController(ExchangeRateService service)
    {
        _service = service;
    }

    [HttpPost("load")]
    public async Task<IActionResult> LoadRates([FromQuery] DateTime date)
    {
        var result = await _service.LoadRates(date);
        return Ok(result);
    }

    [HttpPost("load-range")]
    public async Task<IActionResult> LoadRatesRange([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        if (end < start)
            return BadRequest("The end date must be after the start date.");

        var result = await _service.LoadRates(start, end);
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> CheckStatus()
    {
        var result = await _service.CheckStatusForToday();
        return Ok(result);
    }

    


}
