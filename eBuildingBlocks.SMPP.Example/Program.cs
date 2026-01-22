using eBuildingBlocks.SMPP;
using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Example;
using eBuildingBlocks.SMPP.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
Console.OutputEncoding = Encoding.Unicode;

Console.WriteLine("Starting SMPP Lite Test Console...");
var services = new ServiceCollection();

// -------------------------------
// Register SMPP core (NuGet)
// -------------------------------
services.RegistarSMPPCore();

services.AddSingleton<ISmppAuthenticator, TestSmppAuthenticator>();
services.AddSingleton<ISmppMessageHandler, TestSmppMessageHandler>();
//services.AddSingleton<ISmppSessionPolicy, DefaultSmppSessionPolicy>();

var provider = services.BuildServiceProvider();

// Resolve dependencies
var authenticator = provider.GetRequiredService<ISmppAuthenticator>();
var messageHandler = provider.GetRequiredService<ISmppMessageHandler>();
var sessionPolicy = provider.GetRequiredService<ISmppSessionPolicy>();

var server = SmppServerBuilder.Create()
    .ListenOn(2775)
    .ListenOn(2776)
    .ListenOn(IPAddress.Loopback, 5000)
    .WithAuthenticator(ctx => authenticator.AuthenticateAsync(ctx))
    .WithMessageHandler(async (session, req, ct) =>
    {
        return await messageHandler.HandleSubmitAsync(session, req, ct);
    })
    .Build(provider);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("Shutting down SMPP server...");
    e.Cancel = true;
    cts.Cancel();
};

try
{
    Console.WriteLine("Starting SMPP server listeners...");
    await server.StartAsync(cts.Token); // 🔴 this may block by design
}
catch (OperationCanceledException)
{
    Console.WriteLine("SMPP server stopped.");
}
catch (Exception ex)
{
    Console.WriteLine("SMPP server failed to start:");
    Console.WriteLine(ex); // IMPORTANT: full exception
}

// Keep process alive
await Task.Delay(Timeout.Infinite, cts.Token);
