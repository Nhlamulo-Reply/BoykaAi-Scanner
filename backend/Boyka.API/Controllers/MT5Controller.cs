using Boyka.Application.DTOs.MT5;
using Boyka.Application.Interfaces;
using Boyka.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<ActionResult> AccountSnapshot([FromBody] MT5AccountSnapshotRequest request)
    {
        var registration = GetRegistration();

        if (request.AccountLogin <= 0
            || string.IsNullOrWhiteSpace(request.BrokerCompany)
            || string.IsNullOrWhiteSpace(request.ServerName)
            || string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new InvalidOperationException("Account snapshot identity fields are required.");
        }

        var account = await _context.TradingAccounts
            .FirstOrDefaultAsync(a => a.Id == registration.TradingAccountId);

        if (account is null)
            throw new KeyNotFoundException($"Trading account {registration.TradingAccountId} not found.");

        if (!long.TryParse(account.AccountNumber, out var expectedLogin)
            || request.AccountLogin != expectedLogin)
        {
            throw new UnauthorizedAccessException("Account snapshot login does not match the authenticated EA account.");
        }

        _logger.LogInformation(
            "Account snapshot received for account {AccountId}: Login={AccountLogin}, Company={BrokerCompany}, Server={ServerName}, Currency={Currency}, Balance={Balance}, Equity={Equity}",
            registration.TradingAccountId,
            request.AccountLogin,
            request.BrokerCompany,
            request.ServerName,
            request.Currency,
            request.Balance,
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
