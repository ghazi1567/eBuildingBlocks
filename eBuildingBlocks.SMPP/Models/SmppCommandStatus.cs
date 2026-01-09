using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Models
{
    /// <summary>
    /// Minimal SMPP status set used by this lite server.
    /// Values are SMPP command_status codes.
    /// </summary>
    public enum SmppCommandStatus : uint
    {
        OK = 0x00000000,

        // Common bind errors
        BIND_FAIL = 0x0000000D,          // ESME_RBINDFAIL
        INVALID_BIND_STATE = 0x00000005, // ESME_RINVBNDSTS

        // Flow control
        THROTTLED = 0x00000058,          // ESME_RTHROTTLED

        // Generic
        SYS_ERROR = 0x00000008,          // ESME_RSYSERR
        INVALID_CMD_ID = 0x00000003      // ESME_RINVCMDID
    }

}
