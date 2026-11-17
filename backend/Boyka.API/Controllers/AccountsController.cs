using Boyka.Application.DTOs.Accounts;
using Boyka.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Boyka.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly ITradingAccountService _service;

    public AccountsController(ITradingAccountService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradingAccountResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TradingAccountResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TradingAccountResponse>> Create([FromBody] CreateTradingAccountRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
