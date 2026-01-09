using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;

namespace eBuildingBlocks.SMPP.Abstractions
{
    public interface ISmppSessionPolicy
    {
        bool CanBind(string systemId);
        bool CanSubmit(SmppSessionContext session);

        /// <summary>
        /// For consumers to enforce backpressure; Core increments/decrements in-flight submits.
        /// </summary>
        int MaxInFlightPerSession { get; }
    }

}
