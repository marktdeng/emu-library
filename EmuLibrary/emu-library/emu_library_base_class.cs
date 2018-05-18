// Emu library base class
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//
// Some port instatiations copyright 2016 Salvator Galea <salvator.galea@cl.cam.ac.uk>


using KiwiSystem;

namespace EmuLibrary
{
    public class Emu
    {
        // ----------------
        // - I/O PORTS
        // ----------------
        // These are the ports of the circuit (and will appear as ports in the generated Verilog)
        // Slave Stream Ports
        [Kiwi.InputWordPort("s_axis_tdata_0")] // s_axis_tdata
        protected internal static ulong s_axis_tdata_0; // Data to be received

        [Kiwi.InputWordPort("s_axis_tdata_1")] // s_axis_tdata
        protected internal static ulong s_axis_tdata_1; // Data to be received

        [Kiwi.InputWordPort("s_axis_tdata_2")] // s_axis_tdata
        protected internal static ulong s_axis_tdata_2; // Data to be received

        [Kiwi.InputWordPort("s_axis_tdata_3")] // s_axis_tdata
        protected internal static ulong s_axis_tdata_3; // Data to be received

        [Kiwi.InputBitPort("s_axis_tkeep")] // s_axis_tkeep
        protected internal static uint s_axis_tkeep; // Offset of valid bytes in the data bus

        [Kiwi.InputBitPort("s_axis_tlast")] // s_axis_tlast
        protected internal static bool s_axis_tlast; // End of frame indicator

        [Kiwi.InputBitPort("s_axis_tvalid")] // s_axis_tvalid
        protected internal static bool s_axis_tvalid; // Valid data on the bus - indicator

        [Kiwi.OutputBitPort("s_axis_tready")] // s_axis_tready
        protected internal static bool s_axis_tready; // Ready to receive data - indicator

        [Kiwi.InputWordPort("s_axis_tuser_hi")] // s_axis_tuser_hi
        protected internal static ulong s_axis_tuser_hi; // metadata

        [Kiwi.InputWordPort("s_axis_tuser_low")] // s_axis_tuser_low
        protected internal static ulong s_axis_tuser_low; // metadata

        // Master Stream Ports
        [Kiwi.OutputWordPort("m_axis_tdata_0")] // m_axis_tdata
        protected internal static ulong m_axis_tdata_0; // Data to be sent 

        [Kiwi.OutputWordPort("m_axis_tdata_1")] // m_axis_tdata
        protected internal static ulong m_axis_tdata_1; // Data to be sent 

        [Kiwi.OutputWordPort("m_axis_tdata_2")] // m_axis_tdata
        protected internal static ulong m_axis_tdata_2; // Data to be sent 

        [Kiwi.OutputWordPort("m_axis_tdata_3")] // m_axis_tdata
        protected internal static ulong m_axis_tdata_3; // Data to be sent 

        [Kiwi.OutputBitPort("m_axis_tkeep")] // m_axis_tkeep
        protected internal static uint m_axis_tkeep; // Offset of valid bytes in the data bus

        [Kiwi.OutputBitPort("m_axis_tlast")] // m_axis_tlast
        protected internal static bool m_axis_tlast; // End of frame indicator

        [Kiwi.OutputBitPort("m_axis_tvalid")] // m_axis_tvalid
        protected internal static bool m_axis_tvalid; // Valid data on the bus - indicator

        [Kiwi.InputBitPort("m_axis_tready")] // m_axis_tready
        protected internal static bool m_axis_tready; // Ready to transmit data - indicator

        [Kiwi.OutputBitPort("m_axis_tuser_hi")] // m_axis_tuser_hi
        protected internal static ulong m_axis_tuser_hi; // metadata

        [Kiwi.OutputWordPort("m_axis_tuser_low")] // m_axis_tuser_low
        protected internal static ulong m_axis_tuser_low; // metadata

        // CAM Memory Ports
        // Input Ports
        [Kiwi.InputWordPort("cam_busy")] // cam_busy
        protected internal static bool cam_busy; // Busy signal from the CAM

        [Kiwi.InputBitPort("cam_match")] // cam_match
        protected internal static bool cam_match; // Match singal if data has been found

        [Kiwi.InputBitPort("cam_match_addr")] // cam_match_addr
        protected internal static byte cam_match_addr; // Return address of the matched data

        // Output Ports
        [Kiwi.OutputWordPort("cam_cmp_din")] // cam_cmp_din
        protected internal static ulong cam_cmp_din = ~(ulong) 0x00; // Data to compare against the content of the CAM

        [Kiwi.OutputWordPort("cam_din")] // cam_din
        protected internal static ulong cam_din = 0x00; // Data to be writen in the CAM

        [Kiwi.OutputBitPort("cam_we")] // cam_we
        protected internal static bool cam_we = false; // Write enable signal

        [Kiwi.OutputBitPort("cam_wr_addr")] // cam_wr_addr
        protected internal static byte cam_wr_addr = 0x00; // Address to write data in CAM

        // CAM Memory Ports
        // Input Ports
        [Kiwi.InputBitPort("cam_busy_learn")] // cam_busy
        protected internal static bool cam_busy_learn; // Busy signal from the CAM

        [Kiwi.InputBitPort("cam_match_learn")] // cam_match
        protected internal static bool cam_match_learn; // Match singal if data has been found

        [Kiwi.InputBitPort("cam_match_addr_learn")] // cam_match_addr
        protected internal static byte cam_match_addr_learn; // Return address of the matched data

        // Output Ports
        [Kiwi.OutputWordPort("cam_cmp_din_learn")] // cam_cmp_din
        protected internal static ulong
            cam_cmp_din_learn = ~(ulong) 0x00; // Data to compare against the content of the CAM

        [Kiwi.OutputWordPort("cam_din_learn")] // cam_din
        protected internal static ulong cam_din_learn = 0x00; // Data to be writen in the CAM

        [Kiwi.OutputBitPort("cam_we_learn")] // cam_we
        protected internal static bool cam_we_learn = false; // Write enable signal

        [Kiwi.OutputBitPort("cam_wr_addr_learn")] // cam_wr_addr
        protected internal static byte cam_wr_addr_learn = 0x00; // Address to write data in CAM

        // FIFO Ports
        [Kiwi.OutputBitPort("fifo_wr_en")] // fifo_wr_we
        protected internal static bool fifo_wr_en = false; // Write enable signal

        [Kiwi.OutputBitPort("fifo_din")] // fifo_wr_we
        protected internal static ulong fifo_din = 0x00;

        [Kiwi.OutputBitPort("fifo_rd_en")] // fifo_rd_we
        protected internal static bool fifo_rd_en = false; // Read enable signal

        [Kiwi.OutputBitPort("fifo_dout")] // fifo_rd_we
        protected internal static ulong fifo_dout = 0x00;

        [Kiwi.InputBitPort("fifo_full")] // fifo_full
        protected internal static bool fifo_full;

        [Kiwi.InputBitPort("fifo_empty")] // fifo_empty
        protected internal static bool fifo_empty;

        // AXI Master Ports
        [Kiwi.OutputBitPort("axi_m_axis_tvalid")] // m_axis_tready
        protected internal static bool axi_m_axis_tvalid; // Ready to receive data - indicator

        [Kiwi.InputBitPort("axi_m_axis_tready")] // m_axis_tready
        protected internal static bool axi_m_axis_tready; // Ready to receive data - indicator

        [Kiwi.OutputBitPort("axi_m_axis_tdata_0")] // m_axis_tdata
        protected internal static ulong axi_m_axis_tdata_0; // Data to be received

        [Kiwi.OutputBitPort("axi_m_axis_tdata_1")] // m_axis_tdata
        protected internal static ulong axi_m_axis_tdata_1; // Data to be received

        [Kiwi.OutputBitPort("axi_m_axis_tdata_2")] // m_axis_tdata
        protected internal static ulong axi_m_axis_tdata_2; // Data to be received

        [Kiwi.OutputBitPort("axi_m_axis_tdata_3")] // m_axis_tdata
        protected internal static ulong axi_m_axis_tdata_3; // Data to be received

        [Kiwi.OutputBitPort("axi_m_axis_tstrb")] // m_axis_tkeep
        protected internal static uint axi_m_axis_tstrb; // Offset of valid bytes in the data bus

        [Kiwi.OutputBitPort("axi_m_axis_tkeep")] // m_axis_tkeep
        protected internal static uint axi_m_axis_tkeep; // Offset of valid bytes in the data bus

        [Kiwi.OutputBitPort("axi_m_axis_tlast")] // m_axis_tlast
        protected internal static bool axi_m_axis_tlast; // End of frame indicator

        [Kiwi.OutputBitPort("axi_m_axis_tid")] // m_axis_tvalid
        protected internal static bool axi_m_axis_tid; // Valid data on the bus - indicator

        [Kiwi.OutputWordPort("axi_m_axis_tdest")] // m_axis_tuser_hi
        protected internal static ulong axi_m_axis_tdest; // metadata

        [Kiwi.OutputWordPort("axi_m_axis_tuser_0")] // m_axis_tuser_low
        protected internal static ulong axi_m_axis_tuser_0; // metadata

        [Kiwi.OutputWordPort("axi_m_axis_tuser_1")] // m_axis_tuser_low
        protected internal static ulong axi_m_axis_tuser_1; // metadata

        // AXI Slave Ports
        [Kiwi.InputBitPort("axi_s_axis_tvalid")] // s_axis_tready
        protected internal static bool axi_s_axis_tvalid; // Ready to receive data - indicator

        [Kiwi.OutputBitPort("axi_s_axis_tready")] // s_axis_tready
        protected internal static bool axi_s_axis_tready; // Ready to receive data - indicator

        [Kiwi.InputWordPort("axi_s_axis_tdata_0")] // s_axis_tdata
        protected internal static ulong axi_s_axis_tdata_0; // Data to be received

        [Kiwi.InputWordPort("axi_s_axis_tdata_1")] // s_axis_tdata
        protected internal static ulong axi_s_axis_tdata_1; // Data to be received

        [Kiwi.InputWordPort("axi_s_axis_tdata_2")] // s_axis_tdata
        protected internal static ulong axi_s_axis_tdata_2; // Data to be received

        [Kiwi.InputWordPort("axi_s_axis_tdata_3")] // s_axis_tdata
        protected internal static ulong axi_s_axis_tdata_3; // Data to be received

        [Kiwi.InputBitPort("axi_s_axis_tstrb")] // s_axis_tkeep
        protected internal static uint axi_s_axis_tstrb; // Offset of valid bytes in the data bus

        [Kiwi.InputBitPort("axi_s_axis_tkeep")] // s_axis_tkeep
        protected internal static uint axi_s_axis_tkeep; // Offset of valid bytes in the data bus

        [Kiwi.InputBitPort("axi_s_axis_tlast")] // s_axis_tlast
        protected internal static bool axi_s_axis_tlast; // End of frame indicator

        [Kiwi.InputBitPort("axi_s_axis_tid")] // s_axis_tvalid
        protected internal static bool axi_s_axis_tid; // Valid data on the bus - indicator

        [Kiwi.InputWordPort("axi_s_axis_tdest")] // s_axis_tuser_hi
        protected internal static ulong axi_s_axis_tdest; // metadata

        [Kiwi.InputWordPort("axi_s_axis_tuser_0")] // s_axis_tuser_low
        protected internal static ulong axi_s_axis_tuser_0; // metadata

        [Kiwi.InputWordPort("axi_s_axis_tuser_1")] // s_axis_tuser_low
        protected internal static ulong axi_s_axis_tuser_1; // metadata

        // Debug register - Output Port
        [Kiwi.OutputWordPort("debug_reg")] // debug_reg
        protected internal static uint debug_reg = 0x00; // Register for debuging purpose

        // Registers
        [Kiwi.OutputWordPort("PktIn")]
        protected internal static uint PktIn = 0x00; // Incoming Packet Counter

        [Kiwi.OutputWordPort("PktOut")]
        protected internal static uint PktOut = 0x00; // Outgoing Packet Counter

        [Kiwi.OutputWordPort("Interrupts")]
        protected internal static ulong Interrupts = 0x00; // Interrupt indicator

        [Kiwi.OutputWordPort("Status")]
        protected internal static ulong Status = 0x00; // Status indicator

        protected internal static ulong DEFAULT_oqs = 0x0000000000550000;
        
        public static uint SwapEndian(uint x)
        {
            return ((x & 0x000000ff) << 24) | ((x & 0x0000ff00) << 8) |
                   ((x & 0x00ff0000) >> 8) | ((x & 0xff000000) >> 24);
        }

        public static ulong SwapEndian(ulong x)
        {
            return ((x & 0x00000000000000ff) << 56) |
                   ((x & 0x000000000000ff00) << 40) |
                   ((x & 0x0000000000ff0000) << 24) |
                   ((x & 0x00000000ff000000) << 8)  |
                   ((x & 0x000000ff00000000) >> 8)  |
                   ((x & 0x0000ff0000000000) >> 24) |
                   ((x & 0x00ff000000000000) >> 40) |
                   ((x & 0xff00000000000000) >> 56);
        }
    }
}