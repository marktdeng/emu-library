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
using KiwiSystem;
using EmuLibrary;

class EmuReferenceSwitchThreads : Emu
{
    // This class describes the OPL of the reference_switch of the NetFPGA

    // ----------------
    // - I/O PORTS
    // ----------------
    // These are the ports of the circuit (and will appear as ports in the generated Verilog)
    // Slave Stream Ports


    // Constants variables
    const uint LUT_SIZE = 16U;

    [Kiwi.Volatile] static ulong dst_mac, src_mac;
    [Kiwi.Volatile] static ulong broadcast_ports;
    [Kiwi.Volatile] static ulong metadata = 0UL;
    [Kiwi.Volatile] static bool eth_header_rdy = false;
    
    static fifo_interface fifo = new fifo_interface();

    //	This class descirbes the parser for the ethernet header
    public class Ethernet
    {
        static CircularFrameBuffer cfb = new CircularFrameBuffer(1);
        public void eth_parser()
        {
            var p = new EthernetParser();
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
        static ulong[] LUT = new ulong[LUT_SIZE];

        static uint mem_controller_cnt = 0U;
        static ulong dst_mac = 0UL;
        static ulong src_mac = 0UL;
        static ulong metadata = 0UL;
        static ulong broadcast_ports = 0UL;
        static ulong tmp_dst_ports = 0UL;

        public void CAM_lookup_n_learn()
        {
            byte tmp_addr = 0x00;

            CAM_controller.mem_controller_cnt = 0U;

            while (true)
            {
                // Trigger this logic whenever the ethernet class is done
                if (eth_header_rdy)
                {
                    CAM_controller.dst_mac = dst_mac;
                    CAM_controller.src_mac = src_mac << 16;
                    CAM_controller.metadata = metadata;
                    CAM_controller.broadcast_ports = broadcast_ports;

                    
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
                        fifo.push((LUT[(byte) cam_match_addr] | CAM_controller.metadata));
                    else
                        fifo.push((ulong) (CAM_controller.broadcast_ports | CAM_controller.metadata));

                    if (!cam_match_learn)
                    {
                        cam_we = true;
                        cam_din = CAM_controller.src_mac;
                        cam_wr_addr = (byte) mem_controller_cnt;

                        cam_we_learn = true;
                        cam_din_learn = CAM_controller.src_mac;
                        cam_wr_addr_learn = (byte) mem_controller_cnt;

                        // Update the LUT with the src port
                        tmp_addr = (byte) CAM_controller.mem_controller_cnt;
                        LUT[(byte) CAM_controller.mem_controller_cnt] =
                            (CAM_controller.metadata & (ulong) 0x00FF0000) << 8;

                        if (CAM_controller.mem_controller_cnt == (uint) (LUT_SIZE - 1U))
                            CAM_controller.mem_controller_cnt = 0U;
                        else
                            CAM_controller.mem_controller_cnt += 1U;
                    }
                    else
                    {
                        cam_din = cam_din_learn = 0xFFFFFFFFFFFFFFFF;
                        cam_we = cam_we_learn = false;
                        tmp_addr = (byte) cam_match_addr_learn;
                    }
                }
                else
                {
                    cam_cmp_din = s_axis_tdata_0 << (byte) 16; //Emu.dst_mac;
                    cam_cmp_din_learn = (((s_axis_tdata_0 >> (byte) 48) & (ulong) 0x00ffff) |
                                         (s_axis_tdata_1 & (ulong) 0x00ffffffff) << (byte) 16) << 16;
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

    // #############################
    // # Main Hardware Enrty point
    // #############################
    [Kiwi.HardwareEntryPoint()]
    static int EntryPoint()
    {
        // Create and start the thread for the CAM controller
        CAM_controller cam_controller = new CAM_controller();
        Thread CAM_lookup_n_learn = new Thread(new ThreadStart(cam_controller.CAM_lookup_n_learn));
        CAM_lookup_n_learn.Start();

        // Create and start the thread for the ethernet parser
        Ethernet eth_controller = new Ethernet();
        Thread eth_ctrl = new Thread(new ThreadStart(eth_controller.eth_parser));
        eth_ctrl.Start();

        // Create and start the thread for the FIFO controller
        FIFO_controller fifo_controller = new FIFO_controller();
//		Thread FIFO_receive = new Thread(new ThreadStart(fifo_controller.FIFO_receive));
//		FIFO_receive.Start();
        Thread FIFO_send = new Thread(new ThreadStart(fifo_controller.FIFO_send));
        FIFO_send.Start();
        while (true)
        {
            Kiwi.Pause();
        }
        ;
        return 0;
    }

    //static int Main()
    //{
    //    return 0;
    //}
}