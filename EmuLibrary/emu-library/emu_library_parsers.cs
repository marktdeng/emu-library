// Emu packet parsing library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

namespace EmuLibrary
{
    public class MetadataParser
    {
        public ulong Metadata;
        public ulong RecvInterface;
        public ulong BroadcastInterfaces;

        
        /*
        * Function: Parse
        * Description: Parse peeked data from buffer
        */    
        public int Parse(CircularFrameBuffer cfb)
        {
            cfb.Peek();
            return Parse(cfb.PeekData.TuserLow);
        }

        /*
        * Function: Parse
        * Description: Parse a raw tuser_low value
        */ 
        public int Parse(ulong tuserLow)
        {
            Metadata = tuserLow;
            RecvInterface = tuserLow & Emu.DEFAULT_oqs;
            BroadcastInterfaces = (RecvInterface ^ Emu.DEFAULT_oqs) << 8;
            return 0;
        }
    }
    
    public class EthernetParserGenerator
    {
        public const ushort ETHERTYPE_IPV4 = 0x0008;
        public const ushort ETHERTYPE_IPV6 = 0xdd86;
        public ulong DestMac;
        public uint Ethertype;
        public bool EthHeaderRdy;
        public bool IsIPv4;
        public bool IsIPv6;
        public ulong Metadata;
        public ulong SrcMac;

        /*
        * Function: Parse
        * Description: Parse peeked data from buffer
        */ 
        public int Parse(CircularFrameBuffer cfb)
        {
            //while (!cfb.CanAdvance()) return 2;
            
            cfb.Peek();
            //lock (cfb.PeekData)
            {
                return Parse(cfb.PeekData.Tdata0, cfb.PeekData.Tdata1, cfb.PeekData.TuserLow);
            }
        }

        /*
        * Function: Parse
        * Description: Parse values from the given data
        */ 
        public int Parse(ulong tdata0, ulong tdata1, ulong tuserLow)
        {
            Metadata = tuserLow;
            DestMac = tdata0 & 0xffffffffffff;
            SrcMac = ((tdata0 >> 48) & 0x00ffff) |
                     ((tdata1 & 0x00ffffffff) << 16);
            Ethertype = (uint) ((tdata1 >> 32) & 0x00ffff);
            IsIPv4 = ((tdata1 >> 32) & 0x00ffff) == ETHERTYPE_IPV4 &&
                     ((tdata1 >> 52) & 0x0f) == 0x04; 
            IsIPv6 = ((tdata1 >> 32) & 0x00ffff) == ETHERTYPE_IPV6 &&
                     ((tdata1 >> 52) & 0x0f) == 0x06;
            return 0;
        }

        /*
        * Function: PushHeader
        * Description: Deparse the data within the header and push onto the buffer
        */ 
        public void PushHeader(CircularFrameBuffer cfb)
        {
            WriteToBuffer(cfb.PushData);

            cfb.Push(cfb.PushData);
        }

        /*
        * Function: PushHeader
        * Description: Deparse the provided data and push onto the buffer
        */ 
        public static void PushHeader(CircularFrameBuffer cfb, ulong destMac, ulong srcMac, uint ethertype,
            ulong metadata)
        {
            WriteToBuffer(cfb.PushData, destMac, srcMac, ethertype, metadata);

            cfb.Push(cfb.PushData);
        }

        /*
        * Function: UpdateHeader
        * Description: Deparse the header data and write to current peek location
        */ 
        public void UpdateHeader(CircularFrameBuffer cfb)
        {
            UpdateHeader(cfb, DestMac, SrcMac, Ethertype, Metadata);
        }

        /*
        * Function: UpdateHeader
        * Description: Deparse the provided header data and write to current peek location
        */ 
        public void UpdateHeader(CircularFrameBuffer cfb, ulong destMac, ulong srcMac, uint ethertype,
            ulong metadata)
        {
            cfb.RewindPeek();
            WriteToBuffer(cfb.PeekData, destMac, srcMac, ethertype, metadata);

            cfb.UpdatePeek(cfb.PeekData);
        }

        /*
        * Function: WriteToBuffer
        * Description: Deparse the header data and write to a bufferentry
        */ 
        public void WriteToBuffer(CircularFrameBuffer.BufferEntry be)
        {
            WriteToBuffer(be, DestMac, SrcMac, Ethertype, Metadata);
        }

        /*
        * Function: UpdateHeader
        * Description: Deparse the provided header data and write to current peek data
        */ 
        public static void WriteToBuffer(CircularFrameBuffer.BufferEntry be, ulong destMac, ulong srcMac,
            uint ethertype, ulong metadata)
        {
            be.Tdata0 = destMac | (srcMac << 48);
            be.Tdata1 = (srcMac >> 16) | ((ulong) ethertype << 32);
            be.TuserLow = metadata;
        }
    }

    public class IPv4ParserGenerator
    {
        private ulong _tmp_dest_ip;

        private ulong data_0;
        private ulong data_1;
        private ulong data_2;
        private ulong data_3;
        public ulong DestIp;
        public byte DSCP;
        public byte ECN;
        public byte Flags;
        public uint FragmentOffset;
        public uint HeaderChecksum;
        public uint Identification;
        public byte IHL;
        public byte Protocol;
        public ulong SrcIp;
        public uint TotalLength;
        public byte TTL;
        public byte Version;

        /*
        * Function: Rearrange
        * Description: Rearrange the buffer data into a temporary holding buffer to prepare for parsing
        */ 
        public byte Rearrange(CircularFrameBuffer cfb, bool skip = false)
        {
            //lock (cfb.PeekData)
            {
                IHL = (byte) ((cfb.PeekData.Tdata1 >> 48) & 0x0f);

                data_0 = cfb.PeekData.Tdata1 >> 48;
                data_0 |= cfb.PeekData.Tdata2 << 16;
                data_1 = cfb.PeekData.Tdata2 >> 48;
                data_1 |= cfb.PeekData.Tdata3 << 16;
                data_2 = cfb.PeekData.Tdata3 >> 48;


                if (!cfb.CanAdvance()) return 2;

                cfb.AdvancePeek();


                if (IHL == 5)
                {
                    data_2 |= (cfb.PeekData.Tdata0 & 0xffff) << 16;
                }
                else
                {
                    data_2 |= cfb.PeekData.Tdata0 << 16;
                    if (IHL == 7)
                    {
                        data_3 = cfb.PeekData.Tdata0 >> 48;
                        data_3 |= (cfb.PeekData.Tdata1 & 0xffff) << 16;
                    }
                    else if (IHL == 8)
                    {
                        data_3 = cfb.PeekData.Tdata0 >> 48;
                        data_3 |= cfb.PeekData.Tdata1 << 16;
                    }
                }
            }

            return 0;
        }

        /*
        * Function: ParseArranged
        * Description: Parse rearranged data.
        */ 
        public void ParseArranged()
        {
            Version = (byte) ((data_0 >> 4) & 0x0f);
            IHL = (byte) (data_0 & 0x0f);
            DSCP = (byte) ((data_0 >> 10) & 0x3F);
            ECN = (byte) ((data_0 >> 8) & 0x3);
            TotalLength = (uint) ((data_0 >> 16) & 0x00ffff);
            Identification = (uint) ((data_0 >> 32) & 0x00ffff);
            Flags = (byte) ((data_0 >> 53) & 0x07);
            FragmentOffset = (uint) (((data_0 >> 48) & 0x01f) | ((data_0 >> 56) & 0x0ff)) << 8;
            TTL = (byte) (data_1 & 0x00ff);
            Protocol = (byte) ((data_1 >> 8) & 0x00ff);
            HeaderChecksum = ((uint) data_1 >> 16) & 0x00ffff;
            SrcIp = (data_1 >> 32) & 0x00ffffffff;
            DestIp = data_2 & 0x00ffffffff;
        }

        /*
        * Function: AssembleHeader
        * Description: Deparse the header fields into the temporary buffer
        */ 
        public void AssembleHeader()
        {
            data_0 = IHL | ((ulong) Version << 4) | ((ulong) DSCP << 10) | ((ulong) ECN << 8) |
                     ((ulong) TotalLength << 16) | ((ulong) Identification << 32) | ((ulong) Flags << 53) |
                     ((ulong) FragmentOffset << 48);

            data_1 = TTL | (((ulong) Protocol) << 8) | (((ulong) HeaderChecksum) << 16) | (SrcIp << 32);

            data_2 = DestIp;
        }

        /*
        * Function: WriteHeader
        * Description: Write the temporary buffer back into the main buffer.
        */ 
        public void WriteHeader(CircularFrameBuffer cfb, bool push, bool assembled = false)
        {
            if (!assembled) AssembleHeader();

            cfb.RewindPeek();
            //lock (cfb.PeekData)
            {
                cfb.PeekData.Tdata1 = (cfb.PeekData.Tdata1 & 0x0000ffffffffffff) | (data_0 << 48);
                cfb.PeekData.Tdata2 = (data_0 >> 16) | (data_1 << 48);
                cfb.PeekData.Tdata3 = (data_1 >> 16) | (data_2 << 48);

                cfb.UpdatePeek(cfb.PeekData);

                if (!push) cfb.AdvancePeek();

                if (IHL == 5)
                {
                    cfb.PeekData.Tdata0 = (cfb.PeekData.Tdata0 & 0xffffffffffff0000) | (data_2 >> 16);
                }
                else if (IHL == 6)
                {
                    cfb.PeekData.Tdata0 = (cfb.PeekData.Tdata0 & 0xffff000000000000) | (data_2 >> 16);
                }
                else if (IHL == 7)
                {
                    cfb.PeekData.Tdata0 = (data_2 >> 16) | (data_3 << 48);
                    cfb.PeekData.Tdata1 = (cfb.PeekData.Tdata1 & 0xffffffffffff0000) | (data_3 >> 16);
                }
                else if (IHL == 8)
                {
                    cfb.PeekData.Tdata0 = (data_2 >> 16) | (data_3 << 48);
                    cfb.PeekData.Tdata1 = (cfb.PeekData.Tdata1 & 0xffff000000000000) | (data_3 >> 16);
                }
            }
        }

        /*
        * Function: WriteHeader
        * Description: Write the temporary buffer back into a bufferentry.
        */ 
        public void WriteToBuffer(CircularFrameBuffer.BufferEntry be, uint part)
        {
            if (part == 0)
            {
                be.Tdata1 = (be.Tdata1 & 0x0000ffffffffffff) | (data_0 << 48);
                be.Tdata2 = (data_0 >> 16) | (data_1 << 48);
                be.Tdata3 = (data_1 >> 16) | (data_2 << 48);
            }
            else if (part == 1)
            {
                if (IHL == 5)
                {
                    be.Tdata0 = (be.Tdata0 & ~(ulong) 0xffff) | (data_2 >> 16);
                }
                else if (IHL == 6)
                {
                    be.Tdata0 = (be.Tdata0 & 0xffff000000000000) | (data_2 >> 16);
                }
                else if (IHL == 7)
                {
                    be.Tdata0 = (data_2 >> 16) | (data_3 << 48);
                    be.Tdata1 = (be.Tdata1 & 0xffffffffffff0000) | (data_3 >> 16);
                }
                else if (IHL == 8)
                {
                    be.Tdata0 = (data_2 >> 16) | (data_3 << 48);
                    be.Tdata1 = (be.Tdata1 & 0xffff000000000000) | (data_3 >> 16);
                }
            }
        }
    
        /*
        * Function: Parse
        * Description: Rearrange and then parse the rearranged data.
        */ 
        public byte Parse(CircularFrameBuffer cfb, bool skip = false)
        {
            var result = Rearrange(cfb, skip);
            ParseArranged();
            return result;
        }

        /*
        * Function: ParseSplit
        * Description: A two-part parser to allow the first section of the packet to be parsed.
        */ 
        public byte ParseSplit(CircularFrameBuffer cfb, bool skip = false)
        {
            //lock (cfb.PeekData)
            {
                if (!skip)
                {
                    data_0 = cfb.PeekData.Tdata1 >> 48;
                    data_0 |= cfb.PeekData.Tdata2 << 16;
                    data_1 = cfb.PeekData.Tdata2 >> 48;
                    data_1 |= cfb.PeekData.Tdata3 << 16;
                    data_2 = cfb.PeekData.Tdata3 >> 48;

                    Version = (byte) ((cfb.PeekData.Tdata1 >> 52) & 0x0f);
                    if (Version != 4) DebugFunctions.push_interrupt(DebugFunctions.Errors.ILLEGAL_PACKET_FORMAT);
                    IHL = (byte) ((cfb.PeekData.Tdata1 >> 48) & 0x0f);
                    if (IHL < 5 || IHL > 8) DebugFunctions.push_interrupt(DebugFunctions.Errors.ILLEGAL_PACKET_FORMAT);
                    DSCP = (byte) ((cfb.PeekData.Tdata1 >> 58) & 0x3F);
                    ECN = (byte) ((cfb.PeekData.Tdata1 >> 56) & 0x3);
                    TotalLength = (uint) (cfb.PeekData.Tdata2 & 0x00ffff);
                    Identification = (uint) ((cfb.PeekData.Tdata2 >> 16) & 0x00ffff);
                    Flags = (byte) ((cfb.PeekData.Tdata2 >> 37) & 0x07);
                    FragmentOffset = (uint) ((((cfb.PeekData.Tdata2 >> 32) & 0x01f) << 8) |
                                             ((cfb.PeekData.Tdata2 >> 40) & 0x0ff));
                    TTL = (byte) ((cfb.PeekData.Tdata2 >> 48) & 0x00ff);
                    Protocol = (byte) ((cfb.PeekData.Tdata2 >> 56) & 0x00ff);
                    HeaderChecksum = (uint) cfb.PeekData.Tdata3 & 0x00ffff;
                    SrcIp = (cfb.PeekData.Tdata3 >> 16) & 0x00ffffffff;
                    _tmp_dest_ip = (cfb.PeekData.Tdata3 >> 48) & 0x00ffff;
                }


                if (!cfb.CanAdvance()) return 2;

                cfb.AdvancePeek();

                if (IHL == 5)
                {
                    data_2 |= (cfb.PeekData.Tdata0 & 0xffff) << 16;
                }
                else if (IHL == 6)
                {
                    data_2 |= cfb.PeekData.Tdata0 << 16;
                    if (IHL == 7)
                    {
                        data_3 = cfb.PeekData.Tdata0 >> 48;
                        data_3 |= (cfb.PeekData.Tdata1 & 0xffff) << 16;
                    }
                    else if (IHL == 8)
                    {
                        data_3 = cfb.PeekData.Tdata0 >> 48;
                        data_3 |= cfb.PeekData.Tdata1 << 16;
                    }
                }

                DestIp = _tmp_dest_ip | ((cfb.PeekData.Tdata0 & 0x00ffff) << 16);
            }

            return 0;
        }

        /*
        * Function: CalculateCheckSum
        * Description: Calculate the header checksum
        */ 
        public uint CalculateCheckSum(bool includeHeader = false)
        {
            uint result = 0;
            for (var i = 0; i < 4; i++)
            {
                result += (ushort) (data_0 >> (i * 16));

                if (includeHeader || i == 1) result += (ushort) (data_1 >> (i * 16));

                result += (ushort) (data_2 >> (i * 16));
                result += (ushort) (data_3 >> (i * 16));
            }

            while (result > 0xffff) result = (result & 0xffff) + ((result >> 16) & 0xffff);

            return (result ^ 0xffff) & 0xFFFF;
        }

        /*
        * Function: VerifyCheckSum
        * Description: Check that the checksum is valid
        */ 
        public bool VerifyCheckSum()
        {
            return CalculateCheckSum(true) == 0x0000;
        }
    }

    public class IPv6Parser
    {
        private ulong _tmp_src_ip_2;
        public ulong DestIp1;
        public ulong DestIp2;
        public byte HopLimit;
        public uint PayloadLength;
        public byte Protocol;
        public ulong SrcIp1;
        public ulong SrcIp2;
        public byte TrafficClass;
        public byte Version;

        /*
        * Function: Parse
        * Description: Parse peeked data from buffer
        */ 
        public byte Parse(CircularFrameBuffer cfb, bool skip = false)
        {
            if (!skip)
                //lock (cfb.PeekData)
                {
                    Version = (byte) ((cfb.PeekData.Tdata1 >> 52) & 0x0f);
                    TrafficClass =
                        (byte) (((cfb.PeekData.Tdata1 >> 48) & 0x0f) | (((cfb.PeekData.Tdata1 >> 56) & 0x0f) << 4));

                    PayloadLength = (uint) ((cfb.PeekData.Tdata2 >> 16) & 0x00ffff);
                    Protocol = (byte) ((cfb.PeekData.Tdata2 >> 32) & 0x00ff);
                    HopLimit = (byte) ((cfb.PeekData.Tdata2 >> 40) & 0x00ff);
    
                    SrcIp1 = (cfb.PeekData.Tdata2 >> 48) & 0x00ffff;
                    SrcIp1 |= (cfb.PeekData.Tdata3 & 0x00ffffffffffff) << 16;
                    _tmp_src_ip_2 = (cfb.PeekData.Tdata3 >> 48) & 0x00ffff;
                }

            if (!cfb.CanAdvance()) return 2;

            cfb.AdvancePeek();

            //lock (cfb.PeekData)
            {
                SrcIp2 = _tmp_src_ip_2 | ((cfb.PeekData.Tdata0 & 0x00ffffffffffff) << 16);
                DestIp1 = (cfb.PeekData.Tdata0 >> 48) & 0x00ffff;
                DestIp1 |= (cfb.PeekData.Tdata1 & 0x00ffffffffffff) << 16;
                DestIp2 = (cfb.PeekData.Tdata1 >> 48) & 0x00ffff;
                DestIp2 |= (cfb.PeekData.Tdata2 & 0x00ffffffffffff) << 16;
            }

            return 0;
        }
    }

    public class UDPParser
    {
        public uint Checksum;
        public uint DestPort;
        public uint Length;
        public uint SrcPort;

        /*
        * Function: Parse
        * Description: Parse peeked data from buffer
        */ 
        public byte Parse(CircularFrameBuffer cfb, uint ipHeaderLength, bool skip = false)
        {
            var startloc = 1 + ipHeaderLength / 64 - 4;
            var offset = (int) (48 + ipHeaderLength % 64);
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

        /*
        * Function: WriteToBuffer
        * Description: Deparse and write to a bufferentry
        */ 
        public byte WriteToBuffer(CircularFrameBuffer.BufferEntry be, byte offset)
        {
            for (byte dataitem = 0; dataitem < 4; dataitem++)
            {
                ulong data;
                if (dataitem > 3) return 0;

                switch (dataitem)
                {
                    case 0:
                        data = SrcPort;
                        break;
                    case 1:
                        data = DestPort;
                        break;
                    case 2:
                        data = Length;
                        break;
                    case 3:
                        data = Checksum;
                        break;
                    default:
                        return 1;
                }

                var linenum = offset / 64;
                var lineoffset = offset % 64;
                if (lineoffset % 8 != 0) DebugFunctions.push_interrupt(DebugFunctions.Errors.ILLEGAL_PACKET_FORMAT);

                if (lineoffset <= 48)
                    switch (linenum)
                    {
                        case 0:
                            be.Tdata0 = (be.Tdata0 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            break;
                        case 1:
                            be.Tdata1 = (be.Tdata1 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            break;
                        case 2:
                            be.Tdata2 = (be.Tdata2 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            break;
                        case 3:
                            be.Tdata3 = (be.Tdata3 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            break;
                    }
                else
                    switch (linenum)
                    {
                        case 0:
                            be.Tdata0 = (be.Tdata0 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            be.Tdata1 = (be.Tdata1 & ~((ulong) 0xffff >> (64 - lineoffset))) |
                                        (data >> (64 - lineoffset));
                            break;
                        case 1:
                            be.Tdata1 = (be.Tdata1 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            be.Tdata2 = (be.Tdata2 & ~((ulong) 0xffff >> (64 - lineoffset))) |
                                        (data >> (64 - lineoffset));
                            break;
                        case 2:
                            be.Tdata2 = (be.Tdata2 & ~((ulong) 0xffff << lineoffset)) | (data << lineoffset);
                            be.Tdata3 = (be.Tdata3 & ~((ulong) 0xffff >> (64 - lineoffset))) |
                                        (data >> (64 - lineoffset));
                            break;
                        case 3:
                            DebugFunctions.push_interrupt(DebugFunctions.Errors.ILLEGAL_PACKET_FORMAT);
                            break;
                    }

                offset += 16;
            }

            return 0;
        }
    }

    public class TCPParser
    {
        public uint SrcPort;
        public uint DestPort;
        public uint SeqNumber;
        public uint AckNumber;
        public byte DataOffset;
        public uint WindowSize;
        
        /*
        * Function: Parse
        * Description: Parse peeked data from buffer
        */ 
        public byte Parse(CircularFrameBuffer cfb, uint ipHeaderLength, bool skip = false)
        {
            var startloc = 1 + ipHeaderLength / 64 - 4;
            var offset = (int) (48 + ipHeaderLength % 64);
            if (offset >= 64)
            {
                offset -= 64;
                startloc++;
            }

            ulong data0, data1, data2;
            switch (startloc)
            {
                case 0:
                    data0 = cfb.PeekData.Tdata0;
                    data1 = cfb.PeekData.Tdata1;
                    data2 = cfb.PeekData.Tdata2;
                    break;
                case 1:
                    data0 = cfb.PeekData.Tdata1;
                    data1 = cfb.PeekData.Tdata2;
                    data2 = cfb.PeekData.Tdata3;
                    break;
                case 2:
                    data0 = cfb.PeekData.Tdata2;
                    data1 = cfb.PeekData.Tdata3;
                    data2 = 0;
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
                data1 = data2;
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
                data1 = data2;
                DestPort |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }

            if (offset <= 32)
            {
                SeqNumber = (uint) (data0 >> offset) & 0xFFFFFFFF;
                offset += 32;
            }
            else
            {
                SeqNumber = (uint) (data0 >> offset);
                data0 = data1;
                data1 = data2;
                SeqNumber |= (uint) ((uint) data0 & (0xFFFFFFFF >> offset)) << offset;
                offset = 64 - offset;
            }
            
            if (offset <= 48)
            {
                uint tmp = (uint) (data0 >> offset) & 0xFFFF;
                DataOffset = (byte) ((tmp & 0xF0) >> 4);
                
                offset += 16;
            }
            else
            {
                uint tmp = (uint) (data0 >> offset);
                data0 = data1;
                data1 = data2;
                tmp |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
                DataOffset = (byte) ((tmp & 0xF0) >> 4);
            }
            
            if (offset <= 48)
            {
                WindowSize = (uint) (data0 >> offset) & 0xFFFF;
                offset += 16;
            }
            else
            {
                WindowSize = (uint) (data0 >> offset);
                data0 = data1;
                data1 = data2;
                WindowSize |= (uint) ((uint) data0 & (0xFFFF >> offset)) << offset;
                offset = 64 - offset;
            }


            return 0;
        }
    }
}
