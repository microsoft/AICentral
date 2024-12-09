using Microsoft.AspNetCore.Mvc;

namespace AICentral.Dapr.Audit;

[Route("api/aicentralaudit")]
public class AuditLogSubscriberController: ControllerBase
{
    private readonly ILogger<AuditLogSubscriberController> _logger;

    public AuditLogSubscriberController(ILogger<AuditLogSubscriberController> logger)
    {
        _logger = logger;
    }
    
    public Task<IActionResult> ReceiveAuditLog([FromBody] LogEntry logEntry)
    {
        _logger.LogInformation("Received audit log entry: {logEntry}", logEntry);
        return Task.FromResult<IActionResult>(Ok());
    }
}