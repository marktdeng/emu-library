// Emu packet generator test engine
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

using KiwiSystem;

namespace EmuLibrary
{
    public class emu_packet_generator : Emu
    {
        private const uint BUF_SIZE = 20U; // Max frame size = 1526 Bytes ~ 191x8Bxmd entries
        private const ulong DEST_ADDRESS = 0xabcdef012345;
        private const ulong SRC_ADDRESS = 0x559c48d25a9f;

        private const ulong SRC_IP = 0x0100a8c0;
        private const ulong DEST_IP = 0x0200a8c0;
        private const uint SRC_PORT = 0x901f;
        private const uint DEST_PORT = 0x5000;
        private const uint UDP_LENGTH = 0x0800;


        private static readonly CircularFrameBuffer cfb = new CircularFrameBuffer(BUF_SIZE);

        private static readonly UDPParser up = new UDPParser();
        private static readonly EthernetParserGenerator ep = new EthernetParserGenerator();
        private static readonly IPv4ParserGenerator ip = new IPv4ParserGenerator();

        private static readonly crc32 crc = new crc32();

        private static void SetPacketData()
        {
            up.SrcPort = SRC_PORT;
            up.DestPort = DEST_PORT;
            up.Length = UDP_LENGTH;

            ep.SrcMac = SRC_ADDRESS;
            ep.DestMac = DEST_ADDRESS;
            ep.Ethertype = EthernetParserGenerator.ETHERTYPE_IPV4;
            InterfaceFunctions.SetDestInterface(InterfaceFunctions.PORT_BROADCAST, ep);
            ip.Version = 4;
            ip.IHL = 5;
            ip.DSCP = 0;
            ip.ECN = 0;
            ip.TotalLength = 0;
            ip.Identification = 0;
            ip.Flags = 0;
            ip.FragmentOffset = 0;
            ip.TTL = 255;
            ip.Protocol = 0;
            ip.SrcIp = SRC_IP;
            ip.DestIp = DEST_IP;
            ip.HeaderChecksum = 0xA8C054FA;
        }

        private static void generate_packet()
        {
            SetPacketData();

            HeaderGen.WriteUDPHeader(cfb, up, ep, ip, InterfaceFunctions.PORT_BROADCAST);

            if (!Kiwi.inHardware())
            {
                cfb.PrintContents();
            }
            
            CircularNetworkFunctions.SendWithFCS(cfb, crc);

            Kiwi.Pause();
        }

        [Kiwi.HardwareEntryPoint]
        private static int EntryPoint()
        {
            CircularNetworkFunctions.RecvOne(cfb, true, false);

            while (true)
            {
                generate_packet();
            }
        }

        private static int Main()
        {
            if (!Kiwi.inHardware())
            {
                Emu.m_axis_tready = true;
            }

            SetPacketData();
            generate_packet();

            //cfb.PrintContents();
            //cfb.ResetPeek();
            //ep.Parse(cfb);
            //System.Console.WriteLine(ep.Ethertype);
            //System.Console.WriteLine((cfb.PeekData.Tdata1 >> 52) & 0x0f);
            //System.Console.WriteLine(ep.IsIPv4);

            return 0;
        }
    }
}
