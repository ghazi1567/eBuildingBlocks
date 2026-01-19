
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
    {
        return Task.FromResult(SmppAuthResult.Valid());
    })
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
            SmppCommandStatus.ESME_ROK);
    })
    .WithSessionPolicy(opt =>
    {
        opt.GetMaxInFlight = session => 50;
        // Validate each submit_sm
        opt.ValidateSubmit = (session, request) =>
        {
            if (session.InFlightSubmits >= 50)
            {
                return SmppPolicyResult.Deny((uint)SmppCommandStatus.ESME_RTHROTTLED);
            }

            return SmppPolicyResult.Allow();
        };

        // Optional: restrict multiple binds per system_id
        opt.AllowMultipleBinds = systemId => false;
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
