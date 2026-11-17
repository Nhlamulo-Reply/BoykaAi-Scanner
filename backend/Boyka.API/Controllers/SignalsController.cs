using Boyka.Application.DTOs.Signals;
using Boyka.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Boyka.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignalsController : ControllerBase
{
    private readonly ITradeSignalService _service;

    public SignalsController(ITradeSignalService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradeSignalResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TradeSignalResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TradeSignalResponse>> Create([FromBody] CreateTradeSignalRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
