// IP packet class definition
//
// Copyright 2017 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//

using System;
using System.Linq;
using KiwiSystem;

// ReSharper disable InconsistentNaming

namespace EmuLibrary
{
    public class ParseHeaders
    {
        public static void parse_first()
        {
            
        }
    }
    
    public class IPv4Packet
    {
        private readonly byte[] _header;

        public IPv4Packet(byte[] header)
        {
            _header = header;
        }

        public static byte get_ihl(byte b)
        {
            return (byte) (b & 0x0f);
        }

        public byte get_version()
        {
            return (byte) ((_header[0] & 0xf0) >> 4);
        }

        public byte get_ihl()
        {
            return (byte) (_header[0] & 0x0f);
        }

        public byte get_dscp()
        {
            return (byte) ((_header[1] & 0xfc) >> 2);
        }

        public byte get_ecn()
        {
            return (byte) (_header[1] & 0x03);
        }

        public ushort get_total_length()
        {
            return (ushort) (_header[2] << 8 | _header[3]);
        }

        public ushort get_identification()
        {
            return (ushort) (_header[4] << 8 | _header[5]);
        }

        public byte get_flags()
        {
            return (byte) ((_header[6] & 0xe0) >> 5);
        }

        public ushort get_fragment_offset()
        {
            return (ushort) ((_header[6] & 0x1f) << 8 | _header[7]);
        }

        public byte get_ttl()
        {
            return _header[8];
        }

        public byte get_protocol()
        {
            return _header[9];
        }

        public bool verify_checksum()
        {
            //TODO: Write checksum code
            return true;
        }

        public uint get_src_address()
        {
            Console.Out.WriteLine(_header[12] + "." + _header[13] + "." + _header[14] + "." + _header[15]);
            return (uint) (_header[12] << 24 | _header[13] << 16 | _header[14] << 8 | _header[15]);
        }

        public uint get_dest_address()
        {
            Console.Out.WriteLine(_header[16] + "." + _header[17] + "." + _header[18] + "." + _header[19]);
            return (uint) (_header[16] << 24 | _header[17] << 16 | _header[18] << 8 | _header[19]);
        }

        //TODO: Add options
    }

    public class IPv6Packet
    {
        private readonly byte[] _header;

        public IPv6Packet(byte[] header)
        {
            _header = header;
        }

        public byte get_version()
        {
            return (byte) ((_header[0] & 0xff) >> 4);
        }

        public byte get_traffic_class()
        {
            return (byte) ((_header[0] & 0x0f) << 4 | (_header[1] & 0xf0) >> 4);
        }

        public uint get_flow_label()
        {
            return (uint) ((_header[1] & 0x0f) << 16 | _header[2] << 8 | _header[3]);
        }

        public ushort get_payload_length()
        {
            return (ushort) (_header[4] << 8 | _header[5]);
        }

        public byte get_next_header()
        {
            return _header[6];
        }

        public byte get_hop_limit()
        {
            return _header[7];
        }

        public byte[] get_src_address()
        {
            var result = new byte[16];
            Array.Copy(_header, 8, result, 0, 16);
            return result;
        }

        public byte[] get_dest_address()
        {
            var result = new byte[16];
            Array.Copy(_header, 24, result, 0, 16);
            return result;
        }
    }

    public class EthernetFrame
    {
        private readonly byte[] _header;

        private readonly bool _q1_tagged;

        public EthernetFrame(byte[] header)
        {
            _header = header;
            if (_header[20] == 0x81 && _header[21] == 0x00)
            {
                _q1_tagged = true;
            }
            else
            {
                _q1_tagged = false;
            }
        }

        public bool is_q1_tagged()
        {
            return _q1_tagged;
        }

        public byte[] get_dest_address()
        {
            var result = new byte[6];
            Array.Copy(_header, 8, result, 0, 6);
            return result;
        }

        public byte[] get_src_address()
        {
            var result = new byte[6];
            Array.Copy(_header, 14, result, 0, 6);
            return result;
        }

        public ushort get_ethertype_size()
        {
            if (_q1_tagged)
            {
                return (ushort) (_header[24] << 8 | _header[25]);
            }
            else
            {
                return (ushort) (_header[22] << 8 | _header[23]);
            }
        }
    }
    
    public class UDPPacket
    {
        private readonly byte[] _header;

        public UDPPacket(byte[] header)
        {
            _header = header;
        }

        public ushort get_src_port()
        {
            return (ushort) (_header[0] << 8 | _header[1]);
        }
        
        public ushort get_dest_port()
        {
            return (ushort) (_header[2] << 8 | _header[3]);
        }

        public ushort get_length()
        {
            return (ushort) (_header[4] << 8 | _header[5]);
        }
        
        public bool verify_checksum()
        {
            //TODO: Write Checksum Code
            return true;
        }
    }

    public class TCPPacket
    {
        private readonly byte[] _header;

        public TCPPacket(byte[] header)
        {
            _header = header;
        }
        
        public ushort get_src_port()
        {
            return (ushort) (_header[0] << 8 | _header[1]);
        }
        
        public ushort get_dest_port()
        {
            return (ushort) (_header[2] << 8 | _header[3]);
        }
        
        public uint get_seq_number()
        {
            return (uint) (_header[4] << 24 | _header[5] << 16 | _header[6] << 8 | _header[7]);
        }
        
        public uint get_ack_number()
        {
            return (uint) (_header[8] << 24 | _header[9] << 16 | _header[10] << 8 | _header[11]);
        }
        
        public byte get_data_offset()
        {
            return (byte) ((_header[12] & 0xf0) >> 4);
        }

        public bool get_ns_flag()
        {
            return (_header[12] & 0x01) > 0;
        }
        
        public bool get_cwr_flag()
        {
            return (_header[13] & 0x80) > 0;
        }
        
        public bool get_ece_flag()
        {
            return (_header[13] & 0x40) > 0;
        }
        
        public bool get_urg_flag()
        {
            return (_header[13] & 0x20) > 0;
        }
        
        public bool get_ack_flag()
        {
            return (_header[13] & 0x10) > 0;
        }
        
        public bool get_psh_flag()
        {
            return (_header[13] & 0x08) > 0;
        }
        
        public bool get_rst_flag()
        {
            return (_header[13] & 0x04) > 0;
        }
        
        public bool get_syn_flag()
        {
            return (_header[13] & 0x02) > 0;
        }
        
        public bool get_fin_flag()
        {
            return (_header[13] & 0x01) > 0;
        }

        public ushort get_window_size()
        {
            return (ushort) (_header[14] << 8 | _header[15]);
        }

        public bool verify_checksum()
        {
            //TODO: Write Checksum Code
            return true;
        }

        public ushort get_urg_pointer()
        {
            return (ushort) (_header[18] << 8 | _header[19]);
        }
    }
}
