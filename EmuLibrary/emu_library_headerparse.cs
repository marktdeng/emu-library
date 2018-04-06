﻿namespace EmuLibrary
{
public class HeaderParse
    {
        private bool _ethParsed;

        private uint _ipHeaderLength;
        private bool _ipParsed1;
        private bool _ipParsed2;
        private bool _transportParsed;

        public EthernetParserGenerator ep = new EthernetParserGenerator();
        public IPv4ParserGenerator ipv4 = new IPv4ParserGenerator();
        public IPv6Parser ipv6 = new IPv6Parser();
        public byte IpVersion;
        public byte Protocol;
        public TCPParser tcp = new TCPParser();
        public UDPParser udp = new UDPParser();

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
                if (ep.Parse(cfb) == 0)
                {
                    _ethParsed = true;
                    if (ep.IsIPv4)
                        IpVersion = 4;
                    else if (ep.IsIPv6) IpVersion = 6;
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
                    tcp.Parse(cfb, _ipHeaderLength);
                    break;
                case 17:
                    udp.Parse(cfb, _ipHeaderLength);
                    break;
                default:
                    return;
            }
        }
    }
}