using Boyka.Application.DTOs.MT5;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Boyka.API.Controllers;

[ApiController]
[Route("api/mt5")]
public class MT5Controller : ControllerBase
{
    private readonly IEaRegistrationService _registrationService;
    private readonly IMT5CommandService _commandService;
    private readonly IBoykaDbContext _context;
    private readonly ILogger<MT5Controller> _logger;

    public MT5Controller(
        IEaRegistrationService registrationService,
        IMT5CommandService commandService,
        IBoykaDbContext context,
        ILogger<MT5Controller> logger)
    {
        _registrationService = registrationService;
        _commandService = commandService;
        _context = context;
        _logger = logger;
    }

    // POST /api/mt5/registrations — no EA key auth (provisioning)
    [HttpPost("registrations")]
    public async Task<ActionResult<ProvisionEaResponse>> Provision([FromBody] ProvisionEaRequest request)
    {
        var response = await _registrationService.ProvisionAsync(request);
        return CreatedAtAction(nameof(Provision), new { id = response.RegistrationId }, response);
    }

    // POST /api/mt5/heartbeat — requires EA key
    [HttpPost("heartbeat")]
    public async Task<ActionResult> Heartbeat([FromBody] MT5HeartbeatRequest request)
    {
        var registration = GetRegistration();

        registration.LastHeartbeatAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.EaVersion))
            registration.EaVersion = request.EaVersion;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accountId = registration.TradingAccountId,
            serverUtc = DateTime.UtcNow
        });
    }

    // POST /api/mt5/account-snapshots — requires EA key
    [HttpPost("account-snapshots")]
    public ActionResult AccountSnapshot([FromBody] MT5AccountSnapshotRequest request)
    {
        var registration = GetRegistration();

        _logger.LogInformation(
            "Account snapshot received for account {AccountId}: Balance={Balance} {Currency}, Equity={Equity}",
            registration.TradingAccountId,
            request.Balance,
            request.Currency,
            request.Equity);

        return Ok();
    }

    // GET /api/mt5/commands/next — requires EA key
    [HttpGet("commands/next")]
    public async Task<ActionResult<MT5CommandResponse>> GetNextCommand()
    {
        var registration = GetRegistration();

        await _commandService.ExpireStaleCommandsAsync();

        var command = await _commandService.GetNextPendingCommandAsync(registration.TradingAccountId);

        if (command is null)
            return NoContent();

        return Ok(command);
    }

    // POST /api/mt5/commands/{commandId}/acknowledge — requires EA key
    [HttpPost("commands/{commandId:guid}/acknowledge")]
    public async Task<ActionResult> Acknowledge(Guid commandId, [FromBody] MT5AcknowledgeRequest request)
    {
        var registration = GetRegistration();

        await _commandService.AcknowledgeCommandAsync(commandId, registration.TradingAccountId, request);

        return Ok();
    }

    // POST /api/mt5/positions/snapshot — requires EA key
    [HttpPost("positions/snapshot")]
    public ActionResult PositionSnapshot([FromBody] MT5PositionSnapshotRequest request)
    {
        var registration = GetRegistration();

        _logger.LogInformation(
            "Position snapshot received for account {AccountId}: {Count} position(s)",
            registration.TradingAccountId,
            request.Positions.Count);

        return Ok();
    }

    private EaRegistration GetRegistration()
    {
        if (HttpContext.Items["EaRegistration"] is not EaRegistration registration)
            throw new InvalidOperationException("EaRegistration not found in context.");

        return registration;
    }
}
