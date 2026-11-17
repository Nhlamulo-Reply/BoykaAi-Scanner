using Boyka.Application.DTOs.Risk;
using Boyka.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Boyka.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RiskController : ControllerBase
{
    private readonly IRiskProfileService _service;

    public RiskController(IRiskProfileService service)
    {
        _service = service;
    }

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<RiskProfileResponse>> GetByAccountId(Guid accountId)
    {
        var result = await _service.GetByAccountIdAsync(accountId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{accountId:guid}")]
    public async Task<ActionResult<RiskProfileResponse>> Upsert(Guid accountId, [FromBody] UpsertRiskProfileRequest request)
    {
        var result = await _service.UpsertAsync(accountId, request);
        return Ok(result);
    }
}
