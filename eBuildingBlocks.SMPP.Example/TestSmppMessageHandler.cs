using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Reassembly;
using eBuildingBlocks.SMPP.Session;

internal sealed class TestSmppMessageHandler : ISmppMessageHandler
{
    private readonly ISmppMessageReassembler _reassembler;

    public TestSmppMessageHandler(ISmppMessageReassembler reassembler)
    {
        _reassembler = reassembler;
    }

    public Task<SmppSubmitResult> HandleSubmitAsync(
        SmppSessionContext session,
        SmppSubmitRequest request,
        CancellationToken cancellationToken)
    {
        var message = _reassembler.TryAddPart(session, request);

        if (message != null)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("[FULL SMS RECEIVED]");
            Console.WriteLine($"SystemId : {message.Metadata.SystemId}");
            Console.WriteLine($"From     : {message.SourceAddr}");
            Console.WriteLine($"To       : {message.DestinationAddr}");
            Console.WriteLine($"Text     : {message.Text ?? "(binary)"}");
            Console.WriteLine("=================================");
        }

        return Task.FromResult(
            new SmppSubmitResult(
                Guid.NewGuid().ToString("N")[..16],
                SmppCommandStatus.ESME_ROK));
    }
}
