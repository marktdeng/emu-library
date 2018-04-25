//	Reference learning Switch of the NetFPGA infrastructure
//	This program (C#) replicates the functionality and the logic of
//	the OPL module (verilog) of the reference datapath
//	It is supposed to be used with the kiwi compiler and for 
//	within the NetFPGA project
//
//	 - Use of the 256 AXIS bus width
//	 - Use of a x2 CAM 16 depth, 64b width >>>>BRAM implementation<<<<
//	 - Use of small_through_fifo to store the incoming packet
//	 - Use of small_through_fifo to store the destination port number
//	 - freq : 200MHz,	s_tlast->m_tlast : 40ns,	throughput : 60Mpps
//
//
//	Copyright (C) 2017 Salvator Galea <salvator.galea@cl.cam.ac.uk>
//	All rights reserved
//
//	This software was developed by the University of Cambridge,
//	Computer Laboratory under EPSRC NaaS Project EP/K034723/1 
//
//	Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//
//
//	TODO:
//
//	Latest working set-up:
//		-Vivado 2014.4
//		-KiwiC Version Alpha 0.3.1x
//

using System.Threading;
using EmuLibrary;
using KiwiSystem;

internal class EmuReferenceSwitchThreads : Emu
{
    // This class describes the OPL of the reference_switch of the NetFPGA

    // ----------------
    // - I/O PORTS
    // ----------------
    // These are the ports of the circuit (and will appear as ports in the generated Verilog)
    // Slave Stream Ports


    // Constants variables
    private const uint LUT_SIZE = 16U;

    [Kiwi.Volatile] private static ulong dst_mac, src_mac;
    [Kiwi.Volatile] private static ulong broadcast_ports;
    [Kiwi.Volatile] private static ulong metadata;
    [Kiwi.Volatile] private static bool eth_header_rdy;

    private static readonly FifoInterface fifo = new FifoInterface();

    // #############################
    // # Main Hardware Enrty point
    // #############################
    [Kiwi.HardwareEntryPoint]
    private static int EntryPoint()
    {
        // Create and start the thread for the CAM controller
        var cam_controller = new CAM_controller();
        var CAM_lookup_n_learn = new Thread(cam_controller.CAM_lookup_n_learn);
        CAM_lookup_n_learn.Start();

        // Create and start the thread for the ethernet parser
        var eth_controller = new Ethernet();
        var eth_ctrl = new Thread(eth_controller.eth_parser);
        eth_ctrl.Start();

        // Create and start the thread for the FIFO controller
        var fifo_controller = new FIFO_controller();
//		Thread FIFO_receive = new Thread(new ThreadStart(fifo_controller.FIFO_receive));
//		FIFO_receive.Start();
        var FIFO_send = new Thread(fifo_controller.FIFO_send);
        FIFO_send.Start();
        while (true) Kiwi.Pause();
        ;
        return 0;
    }

    //	This class descirbes the parser for the ethernet header
    public class Ethernet
    {
        private static readonly CircularFrameBuffer cfb = new CircularFrameBuffer(1);

        public void eth_parser()
        {
            var p = new EthernetParserGenerator();
            while (true)
            {
                CircularNetworkFunctions.RecvOne(cfb, true);
                p.Parse(cfb);
                metadata = p.Metadata;
                dst_mac = p.DestMac;
                src_mac = p.SrcMac;
                eth_header_rdy = p.EthHeaderRdy;
                broadcast_ports = p.BroadcastPorts;
            }
        }
    }

    // This class describes the operation needed for the CAMs
    // It tries to match and learn the dst_mac and src_mac according to the port number
    public class CAM_controller
    {
        // Memory to store the Output queue of a particular src_mac
        // Each entry is associated with an entry into the CAM
        private static readonly ulong[] LUT = new ulong[LUT_SIZE];

        private static uint mem_controller_cnt;
        private static ulong dst_mac;
        private static ulong src_mac;
        private static ulong metadata;
        private static ulong broadcast_ports;
        private static ulong tmp_dst_ports = 0UL;

        public void CAM_lookup_n_learn()
        {
            byte tmp_addr = 0x00;

            mem_controller_cnt = 0U;

            while (true)
            {
                // Trigger this logic whenever the ethernet class is done
                if (eth_header_rdy)
                {
                    dst_mac = dst_mac;
                    src_mac = src_mac << 16;
                    metadata = metadata;
                    broadcast_ports = broadcast_ports;


                    cam_din = cam_din_learn = 0xFFFFFFFFFFFFFFFF;
                    cam_we = cam_we_learn = false;

                    cam_cmp_din = cam_cmp_din_learn = 0xFFFFFFFFFFFFFFFF;

                    do
                    {
                        Kiwi.Pause();
                    } while (cam_busy | cam_busy_learn);

                    //	Check if we have the dst_mac in the CAM and retrieve the destination port number
                    //	otherwise broadcast
                    if (cam_match)
                        fifo.push(LUT[cam_match_addr] | metadata);
                    else
                        fifo.push(broadcast_ports | metadata);

                    if (!cam_match_learn)
                    {
                        cam_we = true;
                        cam_din = src_mac;
                        cam_wr_addr = (byte) mem_controller_cnt;

                        cam_we_learn = true;
                        cam_din_learn = src_mac;
                        cam_wr_addr_learn = (byte) mem_controller_cnt;

                        // Update the LUT with the src port
                        tmp_addr = (byte) mem_controller_cnt;
                        LUT[(byte) mem_controller_cnt] =
                            (metadata & 0x00FF0000) << 8;

                        if (mem_controller_cnt == LUT_SIZE - 1U)
                            mem_controller_cnt = 0U;
                        else
                            mem_controller_cnt += 1U;
                    }
                    else
                    {
                        cam_din = cam_din_learn = 0xFFFFFFFFFFFFFFFF;
                        cam_we = cam_we_learn = false;
                        tmp_addr = cam_match_addr_learn;
                    }
                }
                else
                {
                    cam_cmp_din = s_axis_tdata_0 << 16; //Emu.dst_mac;
                    cam_cmp_din_learn = (((s_axis_tdata_0 >> 48) & 0x00ffff) |
                                         ((s_axis_tdata_1 & 0x00ffffffff) << 16)) << 16;
                    cam_din = cam_din_learn = 0xFFFFFFFFFFFFFFFF;
                    cam_we = cam_we_learn = false;
                }

                Kiwi.Pause();
            }
        }
    }

    // TODO -- future work
    public class FIFO_controller
    {
//		public void FIFO_receive()
//		{
//			while(true)
//			{
//			
//			s_axis_tready	= (!s_axis_tlast) ? true : false;

//			
//			fifo_wr_en	= (s_axis_tvalid  && !s_axis_tlast && !fifo_nearly_full) ? true : false;

//			if(s_axis_tvalid)
//			{
//				s_axis_tready	=  true;
//				fifo_wr_en	=  !fifo_nearly_full && !s_axis_tlast;
//			}
//			else
//			{
//				s_axis_tready	= !fifo_nearly_full;
//				fifo_wr_en	= false;
//			}

//				Kiwi.Pause();
//			}
//		}

        public void FIFO_send()
        {
            byte state = 0x00;
            m_axis_tvalid = false;
            while (true)
            {
                switch (state)
                {
                    // WAIT_STATE
                    case 0x00:
                        if (!fifo.canPop())
                        {
                            state = 0x01;
                            m_axis_tvalid = true;
                            fifo.pop();
                        }
                        else
                        {
                            m_axis_tvalid = false;
                        }

                        break;
                    // SEND_STATE
                    case 0x01:

                        if (m_axis_tlast & m_axis_tvalid & m_axis_tready)
                        {
                            state = 0x00;
                            m_axis_tvalid = !fifo_empty & !fifo.canPop();
                        }

                        break;

                    default:
                        break;
                }

                Kiwi.Pause();
            }
        }
    }

    //static int Main()
    //{
    //    return 0;
    //}
}