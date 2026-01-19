namespace eBuildingBlocks.SMPP.Models
{
    /// <summary>
    /// Minimal SMPP status set used by this lite server.
    /// Values are SMPP command_status codes.
    /// </summary>
    public enum SmppCommandStatus : uint
    {
        // Success
        ESME_ROK = 0x00000000, // No Error

        // Message / PDU format errors
        ESME_RINVMSGLEN = 0x00000001, // Message length is invalid
        ESME_RINVCMDLEN = 0x00000002, // Command length is invalid
        ESME_RINVCMDID = 0x00000003, // Invalid command ID
        ESME_RINVPRTFLG = 0x00000006, // Invalid priority flag
        ESME_RINVREGDLVFLG = 0x00000007, // Invalid registered delivery flag
        ESME_RINVOPTPARSTREAM = 0x0000000C, // Invalid optional parameter stream
        ESME_RINVOPTPARVAL = 0x0000000D, // Invalid optional parameter value

        // Bind related errors
        ESME_RINVBNDSTS = 0x00000004, // Incorrect bind status
        ESME_RALYBND = 0x00000005, // ESME already bound
        ESME_RINVPASWD = 0x0000000E, // Invalid password
        ESME_RINVSYSID = 0x0000000F, // Invalid system_id
        ESME_RBINDFAIL = 0x0000000D, // Bind failed

        // Address errors
        ESME_RINVSRCADR = 0x0000000A, // Invalid source address
        ESME_RINVDSTADR = 0x0000000B, // Invalid destination address

        // Message submission errors
        ESME_RMSGQFUL = 0x00000014, // Message queue full
        ESME_RINVNUMDESTS = 0x00000033, // Invalid number of destinations
        ESME_RINVDLNAME = 0x00000034, // Invalid distribution list name
        ESME_RSUBMITFAIL = 0x00000045, // submit_sm failed

        // Throttling
        ESME_RTHROTTLED = 0x00000058, // Throttling error

        // System / internal errors
        ESME_RSYSERR = 0x00000008, // System error
        ESME_RUNKNOWNERR = 0x000000FF  // Unknown error
    }


}
