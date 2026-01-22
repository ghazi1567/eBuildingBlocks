using eBuildingBlocks.SMPP;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Reassembly;

namespace eBuildingBlocks.API.Example.HostedServices
{
    public sealed class SmppServerHostedService : IHostedService
    {
        private readonly ILogger<SmppServerHostedService> _logger;
        private SmppServer _server;
        private Task? _serverTask;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISmppMessageReassembler _reassembler;
        private readonly IServiceProvider _serviceProvider;

        public SmppServerHostedService(
            ILogger<SmppServerHostedService> logger,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ISmppMessageReassembler reassembler
            )
        {
            _logger = logger;
            _reassembler = reassembler;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SMPP Server...");

            _serverTask = Task.Run(async () =>
            {
                try
                {

                    // Build server ONCE, inside hosted service
                    _server = SmppServerBuilder.Create()
                        .ListenOn(2775)
                        .ListenOn(2776)
                        .WithAuthenticator(async authContext =>
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var _smppService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                            return await _smppService.Authenticate(authContext);
                        })
                        .WithMessageHandler(async (session, request, ct) =>
                        {
                            try
                            {
                                var msg = _reassembler.TryAddPart(session, request);

                                if (msg != null)
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var _smppService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                                  
                                }
                                return await Task.FromResult(
                                    new SmppSubmitResult(
                                        Guid.NewGuid().ToString("N")[..16],
                                        SmppCommandStatus.ESME_ROK));
                            }
                            catch (Exception)
                            {

                                return await Task.FromResult(
                                   new SmppSubmitResult(
                                       Guid.Empty.ToString("N")[..16],
                                       SmppCommandStatus.ESME_RSYSERR));
                            }
                        })
                        .Build(_serviceProvider);

                    // START SERVER HERE
                    await _server.StartAsync(cancellationToken);


                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("SMPP Server start cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "SMPP Server crashed");
                }
            }, cancellationToken);

            _logger.LogInformation("SMPP Server started and listening");

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SMPP Server...");

            //await _smppServer.StopAsync(cancellationToken);

            _logger.LogInformation("SMPP Server stopped");
        }
    }
}
