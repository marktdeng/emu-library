//  Reference learning Switch lite of the NetFPGA infrastructure
//  This program (C#) replicates the functionality and the logic of
//  the OPL module (verilog) of the reference datapath
//  It is supposed to be used with the kiwi compiler and for 
//  within the NetFPGA project
//
//  Version 2 -- Reference Switch Lite
//
//  Modified to make use of the Emu Library
//
//  Copyright 2016 Salvator Galea <salvator.galea@cl.cam.ac.uk>
//
//  Modifications Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
//
//  All rights reserved
//
//  This software was developed by the University of Cambridge,
//  Computer Laboratory under EPSRC NaaS Project EP/K034723/1 
//
//  Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//
//  TODO:
//   -need to take care of the tlast signal for the last receiving frame
//    not all the bytes are valid data
//   -remove the buffers for the incoming packet, not needed
//
//  Latest working set-up:
//   -Vivado 2016.4
//   -KiwiC Version Alpha 0.3.6a
//

using EmuLibrary;
using KiwiSystem;

internal class Reference_Switch_Lite_V2_Library : Emu
{
    // Constants variables
    private const uint LUT_SIZE = 16U;

    private const uint BUF_SIZE = 200U; // Max frame size = 1526 Bytes ~ 191x8Bxmd entries
    // This class describes the OPL of the reference_switch_lite of the NetFPGA

    // Lookup Table -- Register based LUT
    // Here we need to initialise (with something) the table with 0x01 instead of
    // 0x00 because in this case, in the simulation we get undefined values ('ZZ)
    //
    // Format of the LUT entry ( 64bit x LUT_SIZE)
    //  ______________________________
    // |-        64bit        -|
    // |-    48bit    --    16bit    -|
    // |-    MAC    --    port    -|
    private static readonly ulong[] LUT = new ulong[]//[LUT_SIZE]
    {
        0x0000000000000001, 0x0000000000000001, 0x0000000000000001, 0x0000000000000001,
        0x0000000000000001, 0x0000000000000001, 0x0000000000000001, 0x0000000000000001,
        0x0000000000000001, 0x0000000000000001, 0x0000000000000001, 0x0000000000000001,
        0x0000000000000001, 0x0000000000000001, 0x0000000000000001, 0x0000000000000001
    };

    private static ulong tmp0, tmp, tmp1, tmp2, dst_mac, src_mac, OQ, broadcast_ports, metadata;
    private static bool LUT_hit, IP;

    // Internal buffer to keep the whole packet
    // TODO maybe we dont need all these buffers as we make the decision based on the first frame
    private static readonly CircularFrameBuffer cfb = new CircularFrameBuffer(BUF_SIZE);

    private static readonly EthernetParserGenerator ep = new EthernetParserGenerator();

    private static readonly MetadataParser mp = new MetadataParser();

    // This method describes the operations required to route the frames
    public static void switch_logic()
    {
        uint i = 0U, ptr = 0U, free = 0U, cnt = 0U;
        var pkt_size = 0U;
        bool exist = false, doneReading;
        IP = false;
        metadata = 0UL;
        src_mac = 0UL;
        dst_mac = 0UL;
        LUT_hit = false;

        while (true) // Process packets indefinately
        {
            // Procedure call for receiving the first frame of the packet

            CircularNetworkFunctions.RecvOne(cfb, true, true);

            ep.Parse(cfb);
            mp.Parse(cfb);

            metadata = ep.Metadata;
            dst_mac = ep.DestMac;
            src_mac = ep.SrcMac;

            // #############################
            // # Switch Logic -- START
            // #############################
            tmp = 0UL;
            tmp1 = 0UL;
            tmp2 = 0UL;
            tmp0 = 0UL;
            ptr = 0U;

            // Search the LUT for the dst_mac and for the src_mac
            for (i = 0U; i < LUT_SIZE; i = i + 1U)
            {
                tmp1 = LUT[i];
                Kiwi.Pause();
                // Get the mac address from LUT
                tmp = tmp1 & 0xffffffffffff0000;
                // Get the output port from LUT
                tmp2 = tmp1 & 0x00000000000000ff;

                // Check if we have a hit in the LUT for the dst_mac
                if (dst_mac == tmp)
                {
                    // Get the engress port numnber from the LUT
                    OQ = tmp2 << 24;
                    LUT_hit = true;
                    //break;
                }

                // Here we check if we need to update an entry based on the src_mac
                // Get rid off the oq, keep only the mac
                if (src_mac == tmp >> 16)
                {
                    // Update if needed
                    // tmp0    = tmp | (metadata & (ulong)0x00ff0000)>>(byte)16;
                    exist = true;
                    ptr = i;
                    //break;
                }

                // Save some cycles (maybe)
                if (LUT_hit && exist)
                    break;
            }

            // If we have a LUT hit prepare the appropriate output port in the metadata, otherwise flood
            InterfaceFunctions.SetDestInterface(LUT_hit ? (byte) OQ : (byte) mp.BroadcastInterfaces, cfb);

            // Update entry
            if (exist) LUT[ptr] = (src_mac << 16) | ((metadata >> 16) & 0x00ff);
            Kiwi.Pause();
            // Create entry
            if (!LUT_hit)
            {
                LUT[ptr] = (src_mac << 16) | ((metadata >> 16) & 0x00ff);
                free = free > LUT_SIZE - 1U ? 0U : free = free + 1U;
            }
            // #############################
            // # Switch Logic -- END 
            // #############################

            // Send out this frame and the rest
            CircularNetworkFunctions.SendAndCut(cfb);
        }
    }

    // #############################
    // # Main Hardware Entry point
    // #############################
    [Kiwi.HardwareEntryPoint]
    private static int EntryPoint()
    {
        while (true) switch_logic();
    }

    //private static int Main()
    //{
    //    return 0;
    //}
}