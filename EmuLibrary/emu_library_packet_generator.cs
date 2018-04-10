namespace EmuLibrary
{
    public class PacketGen
    {
        public void WriteIPv4EthernetHeader(CircularFrameBuffer cfb, EthernetParserGenerator ep, IPv4ParserGenerator ip)
        {
            ip.AssembleHeader();
            ep.WriteToBuffer(cfb.PushData);
            ip.WriteToBuffer(cfb.PushData, 0);

            cfb.Push(cfb.PushData);

            cfb.PushData.Reset();

            ip.WriteToBuffer(cfb.PushData, 1);

            cfb.Push(cfb.PushData);
        }

        public void WriteUDPHeader(CircularFrameBuffer cfb, UDPParser up, EthernetParserGenerator ep,
            IPv4ParserGenerator ip)
        {
            ip.Protocol = 17;
            ip.AssembleHeader();
            ip.HeaderChecksum = ip.CalculateCheckSum();
            ip.AssembleHeader();
            ep.WriteToBuffer(cfb.PushData);
            ip.WriteToBuffer(cfb.PushData, 0);

            cfb.Push(cfb.PushData, true);

            cfb.PushData.Reset();

            ip.WriteToBuffer(cfb.PushData, 1);

            up.WriteToBuffer(cfb.PushData, (byte) (16 + (ip.IHL - 5) * 32));

            cfb.Push(cfb.PushData);
        }

        public void WriteEthernetFCS()
        {
        }
    }
}