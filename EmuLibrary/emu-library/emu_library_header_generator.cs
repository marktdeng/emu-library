// Emu header generation library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

using KiwiSystem;

namespace EmuLibrary
{
    public static class HeaderGen
    {
        public static void WriteIPv4EthernetHeader(CircularFrameBuffer cfb, EthernetParserGenerator ep, IPv4ParserGenerator ip,
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

        public static void WriteUDPHeader(CircularFrameBuffer cfb, UDPParser up, EthernetParserGenerator ep,
            IPv4ParserGenerator ip, byte ports)
        {
            Emu.Status = 0xff0;
            ip.Protocol = 17;
            ip.AssembleHeader();
            //Emu.PktIn = ip.CalculateCheckSum();
            //ip.HeaderChecksum = ip.CalculateCheckSum();
            //ip.AssembleHeader();
            ep.WriteToBuffer(cfb.PushData);
            ip.WriteToBuffer(cfb.PushData, 0);

            InterfaceFunctions.SetDestInterface(ports, cfb.PushData);
            cfb.PushData.Tkeep = 0xFFFFFFFF;
            cfb.PushData.Tlast = false;
            Kiwi.Pause();
            
            cfb.Push(cfb.PushData, true);
            Emu.Status = 0xff1;
            cfb.ResetPeek();            

            cfb.PushData.Reset();

            ip.WriteToBuffer(cfb.PushData, 1);

            up.WriteToBuffer(cfb.PushData, (byte) (16 + (ip.IHL - 5) * 32));
            cfb.PushData.Tkeep = 0x0000003FF;
            cfb.PushData.Tlast = true;
            Kiwi.Pause();
            cfb.Push(cfb.PushData);
            Emu.Status = 0xff2;
        }
    }
}
