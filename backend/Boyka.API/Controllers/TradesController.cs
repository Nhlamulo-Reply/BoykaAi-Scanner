using Boyka.Application.DTOs.Trades;
using Boyka.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Boyka.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly ITradeOrderService _service;

    public TradesController(ITradeOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradeOrderResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TradeOrderResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/execution")]
    public async Task<ActionResult<IEnumerable<TradeExecutionResponse>>> GetExecutions(Guid id)
        => Ok(await _service.GetExecutionsAsync(id));

    [HttpPost("execute")]
    public async Task<ActionResult<TradeOrderResponse>> Execute([FromBody] ExecuteTradeRequest request)
    {
        var result = await _service.ExecuteTradeAsync(request);
        return Accepted(result);
    }

    /// <summary>
    /// Validates an order and queues it for the authenticated EA assigned to
    /// the requested trading account. This does not place a broker order;
    /// the EA collects the command on its next poll.
    /// </summary>
    [HttpPost("queue-mt5")]
    public async Task<ActionResult<TradeOrderResponse>> QueueForMt5([FromBody] ExecuteTradeRequest request)
    {
        var result = await _service.QueueForMT5Async(request);
        return Accepted(result);
    }
}
