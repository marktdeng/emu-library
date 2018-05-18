using System.Diagnostics;
using KiwiSystem; 

namespace EmuLibrary
{
    public class emu_library_test
    {
        private const uint BUF_SIZE = 100;
        
        private const ulong DEST_ADDRESS = 0xabababababab;
        private const ulong SRC_ADDRESS = 0xcdcdcdcdcdcd;
        private const ulong SRC_IP =  0xeeeeeeee;
        private const ulong DEST_IP = 0xffffffff;
        private const uint SRC_PORT = 0x1010;
        private const uint DEST_PORT = 0x0101;
        private const uint UDP_LENGTH = 0x0800;
        
        private static readonly CircularFrameBuffer cfb = new CircularFrameBuffer(BUF_SIZE);
        
        private static readonly UDPParser up = new UDPParser();
        private static readonly EthernetParserGenerator ep = new EthernetParserGenerator();
        private static readonly IPv4ParserGenerator ip = new IPv4ParserGenerator();
        
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
            ip.HeaderChecksum = ip.CalculateCheckSum();
        }

        private static void run()
        {
            //Test Packet Generation
            SetPacketData();
            
            //Generate packet (uses ethernet, ip and udp generator)
            HeaderGen.WriteUDPHeader(cfb, up, ep, ip, InterfaceFunctions.PORT_BROADCAST);
            
            //Write Contents of Buffer
            cfb.PrintContents();

            cfb.ResetPeek();
            
            // Verify Parsed Values
            ep.Parse(cfb);
            System.Console.WriteLine($"src_mac: {ep.SrcMac:X12} dest_mac: {ep.DestMac:X12} ethertype: {ep.Ethertype:X4}");
            ip.Parse(cfb);
            System.Console.WriteLine($"version: {ip.Version} IHL: {ip.IHL} DSCP: {ip.DSCP} ECN: {ip.ECN}");
            System.Console.WriteLine($"total_length: {ip.TotalLength} id: {ip.Identification} flags: {ip.Flags}");
            System.Console.WriteLine($"frag_offset: {ip.FragmentOffset} TTL: {ip.TTL} protocol: {ip.Protocol}");
            System.Console.WriteLine($"src_ip: {ip.SrcIp:X8} dest_ip: {ip.DestIp:X8}");
            up.Parse(cfb, ip.IHL);
            System.Console.WriteLine($"src_port: {up.SrcPort:x4} dest_port: {up.DestPort:x4} length: {up.Length:x4}");
            
            // Verify Interrupts
            DebugFunctions.interrupts_enable();
            DebugFunctions.push_interrupt(DebugFunctions.Errors.PACKET_DROP);
            DebugFunctions.push_interrupt(DebugFunctions.Errors.PARSE_FAIL);
            System.Console.WriteLine($"errors: {Emu.Interrupts:X16}");
            DebugFunctions.reset_interrupt();
            System.Console.WriteLine($"errors: {Emu.Interrupts:X16}");

            
            // Verify Sending Packets
            CircularNetworkFunctions.SendOne(cfb, false, true);
            System.Console.WriteLine($"data0: {Emu.m_axis_tdata_0:X16}");
            System.Console.WriteLine($"data1: {Emu.m_axis_tdata_1:X16}");
            System.Console.WriteLine($"data2: {Emu.m_axis_tdata_2:X16}");
            System.Console.WriteLine($"data3: {Emu.m_axis_tdata_3:X16}");

            CircularNetworkFunctions.SendOne(cfb, false, true);
            System.Console.WriteLine($"data0: {Emu.m_axis_tdata_0:X16}");
            System.Console.WriteLine($"data1: {Emu.m_axis_tdata_1:X16}");
            System.Console.WriteLine($"data2: {Emu.m_axis_tdata_2:X16}");
            System.Console.WriteLine($"data3: {Emu.m_axis_tdata_3:X16}");

            
        }
        
        [Kiwi.HardwareEntryPoint]
        private static int EntryPoint()
        {
            run();

            return 0;
        }

        private static int Main()
        {
            if (!Kiwi.inHardware())
            {
                Emu.m_axis_tready = true;
            }

            run();
            
            return 0;
        }
    }
}