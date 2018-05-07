//	Emu header generation library
//
//	Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
//	All rights reserved
//
//	Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

namespace EmuLibrary
{
    public class HeaderGen
    {
        public void WriteIPv4EthernetHeader(CircularFrameBuffer cfb, EthernetParserGenerator ep, IPv4ParserGenerator ip,
            byte ports)
        {
            ip.AssembleHeader();
            ep.WriteToBuffer(cfb.PushData);
            ip.WriteToBuffer(cfb.PushData, 0);

            cfb.PushData.Tkeep = 0xFFFFFFFF;
            cfb.PushData.Tlast = false;
            
            InterfaceFunctions.SetDestInterface(ports, cfb.PushData);
            
            cfb.Push(cfb.PushData);

            cfb.PushData.Reset();

            ip.WriteToBuffer(cfb.PushData, 1);

            cfb.Push(cfb.PushData);
        }

        public void WriteUDPHeader(CircularFrameBuffer cfb, UDPParser up, EthernetParserGenerator ep,
            IPv4ParserGenerator ip, byte ports)
        {
            ip.Protocol = 17;
            ip.AssembleHeader();
            ip.HeaderChecksum = ip.CalculateCheckSum();
            ip.AssembleHeader();
            ep.WriteToBuffer(cfb.PushData);
            ip.WriteToBuffer(cfb.PushData, 0);

            InterfaceFunctions.SetDestInterface(ports, cfb.PushData);
            
            cfb.Push(cfb.PushData, true);

            cfb.ResetPeek();            

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