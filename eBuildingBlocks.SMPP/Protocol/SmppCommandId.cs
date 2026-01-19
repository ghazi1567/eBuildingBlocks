namespace eBuildingBlocks.SMPP.Protocol
{
    public static class SmppCommandIds
    {
        public const uint bind_receiver = 0x00000001;
        public const uint bind_receiver_resp = 0x80000001;

        public const uint bind_transmitter = 0x00000002;
        public const uint bind_transmitter_resp = 0x80000002;

        public const uint bind_transceiver = 0x00000009;
        public const uint bind_transceiver_resp = 0x80000009;

        public const uint submit_sm = 0x00000004;
        public const uint submit_sm_resp = 0x80000004;

        public const uint enquire_link = 0x00000015;
        public const uint enquire_link_resp = 0x80000015;

        public const uint unbind = 0x00000006;
        public const uint unbind_resp = 0x80000006;
    }


}
