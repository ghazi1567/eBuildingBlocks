using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Example
{
    internal sealed class TestSmppAuthenticator : ISmppAuthenticator
    {
        private static readonly Dictionary<string, SmppAccountPolicy> _policies =
    new(StringComparer.OrdinalIgnoreCase)
    {
        // =========================
        // POSITIVE (VALID) POLICIES
        // =========================
        ["client_ok"] = new SmppAccountPolicy
        {
            SystemId = "client_ok",
            IsActive = true,

            AllowedBindModes = SmppBindMode.Transceiver,
            MaxInFlightPerSession = 50,

            Tps = 10,
            Mpm = 600,

            AllowConcatenation = true,
            AllowRegisteredDelivery = true,

            AllowedDataCodings = new() { 0x00, 0x08 }, // GSM + UCS2
            AllowedSourceTon = new() { 1, 5 },
            AllowedDestTon = new() { 1 }
        },

        // =========================
        // NEGATIVE POLICIES (TESTING)
        // =========================

        // TPS violation
        ["client_low_tps"] = new SmppAccountPolicy
        {
            SystemId = "client_low_tps",
            IsActive = true,

            AllowedBindModes = SmppBindMode.Transceiver,
            MaxInFlightPerSession = 5,

            Tps = 1,
            Mpm = 5
        },

        //  Wrong bind mode
        ["client_rx_only"] = new SmppAccountPolicy
        {
            SystemId = "client_rx_only",
            IsActive = true,

            AllowedBindModes = SmppBindMode.Receiver
        },

        // Inactive account
        ["client_disabled"] = new SmppAccountPolicy
        {
            SystemId = "client_disabled",
            IsActive = false
        },

        // ❌ No UCS2 allowed
        ["client_gsm_only"] = new SmppAccountPolicy
        {
            SystemId = "client_gsm_only",
            IsActive = true,

            AllowedDataCodings = new() { 0x00 } // GSM only
        }
    };

        public Task<SmppAuthResult> AuthenticateAsync(SmppAuthContext ctx)
        {
            // Simple demo auth
            if (ctx.Password != "test")
                return Task.FromResult(
                    SmppAuthResult.Fail((uint)SmppCommandStatus.ESME_RINVSYSID));

            // Lookup policy by systemId
            if (!_policies.TryGetValue(ctx.SystemId, out var policy))
            {
                return Task.FromResult(
                    SmppAuthResult.Fail(
                        (uint)SmppCommandStatus.ESME_RINVSYSID));
            }

            if (!policy.IsActive)
            {
                return Task.FromResult(
                    SmppAuthResult.Fail(
                        (uint)SmppCommandStatus.ESME_RBINDFAIL));
            }


            return Task.FromResult(SmppAuthResult.Valid(policy));
        }
    }

}
