using eBuildingBlocks.SMPP.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eBuildingBlocks.SMPP.BackgroundJobs
{
    public class SmppSessionCleanupJob : BackgroundService
    {
        private readonly IBindRegistry _bindRegistry;
        private readonly ILogger<SmppSessionCleanupJob> _logger;
        private int CleanupInterval = 20000;
        public SmppSessionCleanupJob(
            IBindRegistry bindRegistry,
            ILogger<SmppSessionCleanupJob> logger)
        {
            _bindRegistry = bindRegistry;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SMPP session cleanup service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupIdleSessions();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during SMPP session cleanup.");
                }

                await Task.Delay(CleanupInterval, stoppingToken);
            }
        }

        private void CleanupIdleSessions()
        {
            var now = DateTime.UtcNow;

            foreach (var session in _bindRegistry.GetAll())
            {
                var idleTimeoutMin = session.Policy is null ? 10 : session.Policy.IdleTimeoutMin;
                CleanupInterval = session.Policy is null ? CleanupInterval : session.Policy.CleanupInterval;

                if (now - session.LastActivityUtc > TimeSpan.FromMinutes(idleTimeoutMin))
                {
                    _logger.LogWarning(
                        "Disconnecting idle SMPP session. SystemId={SystemId}",
                        session.SystemId);

                    _bindRegistry.Unregister(session);
                }
            }
        }
    }
}
