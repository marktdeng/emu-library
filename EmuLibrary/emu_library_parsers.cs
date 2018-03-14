//	Reference learning Switch lite of the NetFPGA infrastructure
//	This program (C#) replicates the functionality and the logic of
//	the OPL module (verilog) of the reference datapath
//	It is supposed to be used with the kiwi compiler and for 
//	within the NetFPGA project
//
//	Copyright 2016	Salvator Galea	<salvator.galea@cl.cam.ac.uk>
//	All rights reserved
//
//	This software was developed by the University of Cambridge,
//	Computer Laboratory under EPSRC NaaS Project EP/K034723/1 
//
//	Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//
//
//	TODO:
//	 -need to take care of the tlast signal for the last receiving frame
//	  not all the bytes are valid data
//
//	Latest working set-up:
//		-Vivado 2014.4
//		-KiwiC Version Alpha 0.3.1x
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KiwiSystem;

namespace EmuLibrary
{
    public class EthernetParser
    {
        public ulong Metadata;
        public ulong DestMac;
        public ulong SrcMac;
        public bool EthHeaderRdy;
        public ulong BroadcastPorts;
        public bool IsIPv4;
        public bool IsIPv6;
        public uint Ethertype;

        /*
        public bool RecvAndParseEthHeader()
        {
            switch (_state)
            {
                case 0x00:
                    if(Emu.s_axis_tvalid & Emu.s_axis_tready)
                    {
                        Metadata = Emu.s_axis_tuser_low;
                        DestMac = Emu.s_axis_tdata_0<<(byte)16;
                        SrcMac = ((Emu.s_axis_tdata_0>>(byte)48) & (ulong)0x00ffff) | (Emu.s_axis_tdata_1 & (ulong)0x00ffffffff )<<(byte)16 ;
                        EthHeaderRdy = true;
                        BroadcastPorts = ( (Emu.s_axis_tuser_low & (ulong)0x00FF0000) ^ Emu.DEFAULT_oqs) >> (byte)16;
                        _state		= 0x01;
                    }
                    Emu.s_axis_tready	= true;
                    break;
                // WAIT FOR EOP
                case 0x01:
                    EthHeaderRdy = false; 
                    _state = (Emu.s_axis_tvalid & Emu.s_axis_tlast & Emu.s_axis_tready) ? (byte)0x00 : (byte)0x01;
                    Emu.s_axis_tready = !(Emu.s_axis_tvalid & Emu.s_axis_tlast);
                    break;
                default:
                    break;
            }
            return Emu.s_axis_tready;
        }
        */

        /*
        public void ParseEthernetBufferHeader(FrameBuffer buffer)
        {
            Metadata = buffer.tuser_low[0U];
            DestMac = buffer.tdata_0[0U] << (byte) 16;
            SrcMac = (buffer.tdata_0[0U] >> (byte) 48) & (ulong) 0x00ffff | (buffer.tdata_1[0U] & (ulong)0x00ffffffff )<<(byte)16;
            BroadcastPorts = ((buffer.tuser_low[0U] & (ulong)0x00FF0000) ^ Emu.DEFAULT_oqs)<<(byte)8;
            IsIPv4 = (buffer.tdata_1[0U] >> 32 & (ulong) 0x00ffff) == (ulong) 0x0008;
        }
        */

        public int Parse(CircularFrameBuffer cfb)
        {
            while (!cfb.CanAdvance())
            {
                return 2;
            }
            cfb.AdvancePeek();
            lock (cfb.PeekData)
            {
                Metadata = cfb.PeekData.TuserLow;
                DestMac = cfb.PeekData.Tdata0 & 0xffffffffffff;
                SrcMac = (cfb.PeekData.Tdata0 >> (byte) 48) & (ulong) 0x00ffff |
                         (cfb.PeekData.Tdata1 & (ulong) 0x00ffffffff) << (byte) 16;
                BroadcastPorts = ((cfb.PeekData.TuserLow & (ulong) 0x00FF0000) ^ Emu.DEFAULT_oqs) << (byte) 8;
                Ethertype = (uint) (cfb.PeekData.Tdata1 >> 32 & (ulong) 0x00ffff);
                IsIPv4 = (cfb.PeekData.Tdata1 >> 32 & (ulong) 0x00ffff) == (ulong) 0x0008 &&
                         (cfb.PeekData.Tdata1 >> 52 & (ulong) 0x0f) == (ulong) 0x04;
                IsIPv6 = (cfb.PeekData.Tdata1 >> 32 & (ulong) 0x00ffff) == (ulong) 0xdd86 &&
                         (cfb.PeekData.Tdata1 >> 52 & (ulong) 0x0f) == (ulong) 0x06;
            }
            return 0;
        }
    }

    public class IPv4Parser
    {
        public byte Version;
        public byte IHL;
        public byte DSCP;
        public byte ECN;
        public byte Protocol;
        public uint TotalLength;
        public uint Identification;
        public byte Flags;
        public uint FragmentOffset;
        public byte TTL;
        public uint HeaderChecksum;
        public ulong SrcIp;
        private ulong _tmp_dest_ip;
        public ulong DstIp;

        public byte Parse(CircularFrameBuffer cfb, bool skip = false)
        {
            if (!skip)
            {
                lock (cfb.PeekData)
                {
                    Version = (byte) (cfb.PeekData.Tdata1 >> 52 & 0x0f);
                    IHL = (byte) (cfb.PeekData.Tdata1 >> 48 & 0x0f);
                    DSCP = (byte) (cfb.PeekData.Tdata1 >> 58 & 0x3F);
                    ECN = (byte) (cfb.PeekData.Tdata1 >> 56 & 0x3);
                    TotalLength = (uint) (cfb.PeekData.Tdata2 & 0x00ffff);
                    Identification = (uint) (cfb.PeekData.Tdata2 >> 16 & 0x00ffff);
                    Flags = (byte) (cfb.PeekData.Tdata2 >> 37 & 0x07);
                    FragmentOffset = (uint) ((cfb.PeekData.Tdata2 >> 32 & 0x01f) << 8 |
                                             cfb.PeekData.Tdata2 >> 40 & 0x0ff);
                    TTL = (byte) (cfb.PeekData.Tdata2 >> 48 & 0x00ff);
                    Protocol = (byte) (cfb.PeekData.Tdata2 >> 56 & 0x00ff);
                    HeaderChecksum = (uint) cfb.PeekData.Tdata3 & 0x00ffff;
                    SrcIp = (cfb.PeekData.Tdata3 >> 16) & (ulong) 0x00ffffffff;
                    _tmp_dest_ip = (cfb.PeekData.Tdata3 >> 48) & (ulong) 0x00ffff;
                }
            }

            if (!cfb.CanAdvance())
            {
                return 2;
            }
            cfb.AdvancePeek();

            lock (cfb.PeekData)
            {
                DstIp = _tmp_dest_ip | (cfb.PeekData.Tdata0 & (ulong) 0x00ffff) << 16;
            }
            return 0;
        }
    }

    public class IPv6Parser
    {
        public byte Version;
        public byte TrafficClass;
        public uint PayloadLength;
        public byte Protocol;
        public byte HopLimit;
        public ulong SrcIp1;
        private ulong _tmp_src_ip_2;
        public ulong SrcIp2;
        public ulong DestIp1;
        public ulong DestIp2;

        public byte Parse(CircularFrameBuffer cfb, bool skip = false)
        {
            if (!skip)
            {
                lock (cfb.PeekData)
                {
                    Version = (byte) (cfb.PeekData.Tdata1 >> 52 & 0x0f);
                    TrafficClass =
                        (byte) ((cfb.PeekData.Tdata1 >> 48 & 0x0f) | ((cfb.PeekData.Tdata1 >> 56 & 0x0f) << 4));

                    PayloadLength = (uint) (cfb.PeekData.Tdata2 >> 16 & (ulong) 0x00ffff);
                    Protocol = (byte) (cfb.PeekData.Tdata2 >> 32 & (ulong) 0x00ff);
                    HopLimit = (byte) (cfb.PeekData.Tdata2 >> 40 & (ulong) 0x00ff);

                    SrcIp1 = (cfb.PeekData.Tdata2 >> 48) & (ulong) 0x00ffff;
                    SrcIp1 |= (cfb.PeekData.Tdata3 & 0x00ffffffffffff) << 16;
                    _tmp_src_ip_2 = (cfb.PeekData.Tdata3 >> 48) & (ulong) 0x00ffff;
                }
            }

            if (!cfb.CanAdvance())
            {
                return 2;
            }
            cfb.AdvancePeek();

            lock (cfb.PeekData)
            {
                SrcIp2 = _tmp_src_ip_2 | (cfb.PeekData.Tdata0 & (ulong) 0x00ffffffffffff) << 16;
                DestIp1 = (cfb.PeekData.Tdata0 >> 48) & (ulong) 0x00ffff;
                DestIp1 |= (cfb.PeekData.Tdata1 & 0x00ffffffffffff) << 16;
                DestIp2 = (cfb.PeekData.Tdata1 >> 48) & (ulong) 0x00ffff;
                DestIp2 |= (cfb.PeekData.Tdata2 & 0x00ffffffffffff) << 16;
            }
            return 0;
        }
    }
    
    public class UDPParser
    {
        public uint SrcPort;
        public uint DestPort;
        public uint Length;
        public uint Checksum;

        public byte Parse(CircularFrameBuffer cfb, uint ipHeaderLength, bool skip = false)
        {
            uint startloc = (1 + (ipHeaderLength / 64)) - 4;
            int offset = (int) (48 + (ipHeaderLength % 64));
            if (offset >= 64)
            {
                offset -= 64;
                startloc++;
            }

            ulong data0, data1;
            switch (startloc)
            {
                case 0:
                    data0 = cfb.PeekData.Tdata0;
                    data1 = cfb.PeekData.Tdata1;
                    break;
                case 1:
                    data0 = cfb.PeekData.Tdata1;
                    data1 = cfb.PeekData.Tdata2;
                    break;
                case 2:
                    data0 = cfb.PeekData.Tdata2;
                    data1 = cfb.PeekData.Tdata3;
                    break;
                default:
                    return 1;
            }

            if (offset <= 48)
            {
                SrcPort = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                SrcPort = (uint) (data0 >> offset);
                data0 = data1;
                SrcPort |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            if (offset <= 48)
            {
                DestPort = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                DestPort = (uint) (data0 >> offset);
                data0 = data1;
                DestPort |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            if (offset <= 48)
            {
                Length = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                Length = (uint) (data0 >> offset);
                data0 = data1;
                Length |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            if (offset <= 48)
            {
                Checksum = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                Checksum = (uint) (data0 >> offset);
                data0 = data1;
                Checksum |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            return 0;
        }
    }

    public class TCPParser 
    {
        public uint SrcPort;
        public uint DestPort;
        public uint SeqNumber;

        public byte Parse(CircularFrameBuffer cfb, uint ipHeaderLength, bool skip = false)
        {
            uint startloc = (1 + (ipHeaderLength / 64)) - 4;
            int offset = (int) (48 + (ipHeaderLength % 64));
            if (offset >= 64)
            {
                offset -= 64;
                startloc++;
            }

            ulong data0, data1;
            switch (startloc)
            {
                case 0:
                    data0 = cfb.PeekData.Tdata0;
                    data1 = cfb.PeekData.Tdata1;
                    break;
                case 1:
                    data0 = cfb.PeekData.Tdata1;
                    data1 = cfb.PeekData.Tdata2;
                    break;
                case 2:
                    data0 = cfb.PeekData.Tdata2;
                    data1 = cfb.PeekData.Tdata3;
                    break;
                default:
                    return 1;
            }

            if (offset <= 48)
            {
                SrcPort = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                SrcPort = (uint) (data0 >> offset);
                data0 = data1;
                SrcPort |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            if (offset <= 48)
            {
                DestPort = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                DestPort = (uint) (data0 >> offset);
                data0 = data1;
                DestPort |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            return 0;
        }
    }

    public class HeaderParse
    {
        private bool _ethParsed = false;
        private bool _ipParsed1 = false;
        private bool _ipParsed2 = false;
        public byte IpVersion = 0;
        private bool _transportParsed = false;
        public byte Protocol = 0;

        private uint _ipHeaderLength = 0;

        public EthernetParser ep = new EthernetParser();
        public IPv4Parser ipv4 = new IPv4Parser();
        public IPv6Parser ipv6 = new IPv6Parser();
        public UDPParser udp = new UDPParser();
        public TCPParser tcp = new TCPParser();

        public void Parse(CircularFrameBuffer cfb, bool newFrame)
        {
            if (newFrame)
            {
                _ethParsed = false;
                _ipParsed1 = false;
                _ipParsed2 = false;
                IpVersion = 0;
                _transportParsed = false;
                Protocol = 0;
            }

            if (!_ethParsed)
            {
                if (ep.Parse(cfb) == 0)
                {
                    _ethParsed = true;
                    if (ep.IsIPv4)
                    {
                        IpVersion = 4;
                    }
                    else if (ep.IsIPv6)
                    {
                        IpVersion = 6;
                    }
                }
            }

            switch (IpVersion)
            {
                case 4:
                    if (!_ipParsed1)
                    {
                        _ipParsed1 = true;
                        if (ipv4.Parse(cfb, false) == 0) _ipParsed2 = true;
                    }
                    else if (!_ipParsed2)
                    {
                        if (ipv4.Parse(cfb, true) == 0) _ipParsed2 = true;
                    }
                    _ipHeaderLength = ipv4.IHL * 4U * 8U;

                    if (_ipParsed2) Protocol = ipv4.Protocol;


                    break;
                case 6:
                    if (!_ipParsed1)
                    {
                        _ipParsed1 = true;
                        if (ipv6.Parse(cfb, false) == 0) _ipParsed2 = true;
                    }
                    else if (!_ipParsed2)
                    {
                        if (ipv6.Parse(cfb, true) == 0) _ipParsed2 = true;
                    }
                    _ipHeaderLength = 320U;

                    if (_ipParsed2) Protocol = ipv6.Protocol;
                    break;
                default:
                    return;
            }

            switch (Protocol)
            {
                case 6:
                    tcp.Parse(cfb,_ipHeaderLength);
                    break;
                case 17:
                    udp.Parse(cfb,_ipHeaderLength);
                    break;
                default:
                    return;
            }
        }
    }

    public class PacketGen
    {
        public void WriteEthHeader(CircularFrameBuffer cfb, EthernetParser ep)
        {
            WriteEthHeader(cfb, ep.DestMac, ep.SrcMac, ep.Ethertype);
        }

        public void WriteEthHeader(CircularFrameBuffer cfb, ulong destMac, ulong srcMac, uint ethertype)
        {
            ulong data0, data1, data2, data3;

            data0 = destMac | srcMac << (byte) 48;
            data1 = srcMac >> 16 | ethertype << 32;
            
            
        }

        public void WriteIPv4Header(CircularFrameBuffer cfb, IPv4Parser ip)
        {
            ulong data1 = 0, data2, data3, data0_nxt;

            data1 |= (ulong) (ip.Version) << 52 | (ulong) (ip.IHL) << 48 | (ulong) (ip.DSCP) << 58 | (ulong) (ip.ECN) << 56;

            data2 = ip.TotalLength | ip.Identification << 16 | (ulong) (ip.Flags << 37) | (ip.FragmentOffset >> 8) << 32 | (ip.FragmentOffset & 0x00ff) << 40;

            data3 = ip.HeaderChecksum | ip.SrcIp << 16 | ip.DstIp << 48;

            data0_nxt = ip.DstIp >> 16;
        }
        
        
    }
}