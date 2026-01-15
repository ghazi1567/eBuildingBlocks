
using eBuildingBlocks.SMPP;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Reassembly;
using System.Net;
using System.Text;
Console.OutputEncoding = Encoding.Unicode;

Console.WriteLine("Starting SMPP Lite Test Console...");

var reassembler = new InMemorySmppMessageReassembler();

var server = SmppServerBuilder.Create()
    .ListenOn(2775)
    .ListenOn(2776)
    .ListenOn(IPAddress.Loopback, 5000)
    .WithAuthenticator(authContext =>
        Task.FromResult(authContext.SystemId == authContext.Password))
    .WithMessageHandler(async (session, request, ct) =>
    {
        var msg = reassembler.TryAddPart(session, request);

        if (msg != null)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("[FULL SMS RECEIVED]");
            Console.WriteLine($"User : {msg.Metadata.SystemId}");
            Console.WriteLine($"From : {msg.SourceAddr}");
            Console.WriteLine($"To   : {msg.DestinationAddr}");
            Console.WriteLine($"Text : {msg.Text ?? "Binary SMS"}");
            Console.WriteLine("=================================");
        }

        return new SmppSubmitResult(
            Guid.NewGuid().ToString("N")[..16],
            SmppCommandStatus.OK);
    })
    .WithSessionPolicy(opt =>
    {
        opt.CanBind = _ => true;
        opt.CanSubmit = s => s.InFlightSubmits < 50;
        opt.MaxInFlightPerSession = 50;
    })
    .Build();

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
