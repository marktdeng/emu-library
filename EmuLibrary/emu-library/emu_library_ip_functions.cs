// Emu IP interfacing library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

using KiwiSystem;

namespace EmuLibrary
{

    public class AxiInterface
    {
        // AXI Ports
        public ulong axi_m_axis_tdata_0; // Data to be received
        public ulong axi_m_axis_tdata_1; // Data to be received
        public ulong axi_m_axis_tdata_2; // Data to be received
        public ulong axi_m_axis_tdata_3; // Data to be received
        public ulong axi_m_axis_tdest; // Routing information
        public bool axi_m_axis_tid; // Data stream identifier
        public uint axi_m_axis_tkeep; // Offset of valid bytes in the data bus
        public bool axi_m_axis_tlast; // Packet boundary
        public uint axi_m_axis_tstrb; // Valid Bytes marker
        public ulong axi_m_axis_tuser_0; // user metadata
        public ulong axi_m_axis_tuser_1; // user metadata

        public ulong axi_s_axis_tdata_0; // Data to be received
        public ulong axi_s_axis_tdata_1; // Data to be received
        public ulong axi_s_axis_tdata_2; // Data to be received
        public ulong axi_s_axis_tdata_3; // Data to be received
        public ulong axi_s_axis_tdest; // Routing information
        public bool axi_s_axis_tid; // Data stream identifier
        public uint axi_s_axis_tkeep; // Offset of valid bytes in the data bus
        public bool axi_s_axis_tlast; // Packet boundary
        public uint axi_s_axis_tstrb; // Valid Bytes marker
        public ulong axi_s_axis_tuser_0; // user metadata
        public ulong axi_s_axis_tuser_1; // user metadata
        public bool m_updated;
        public bool s_updated;


        public static bool isReady()
        {
            return Emu.axi_m_axis_tready;
        }

        public static bool isValid()
        {
            return Emu.axi_s_axis_tvalid;
        }

        public void setValid(bool status)
        {
            Emu.axi_m_axis_tvalid = status;
        }

        public bool recvOne(bool check_updated = true)
        {
            if ((!check_updated || !s_updated) && Emu.axi_s_axis_tvalid)
            {
                s_updated = true;
                Emu.axi_s_axis_tready = true;
                axi_s_axis_tdata_0 = Emu.axi_s_axis_tdata_0;
                axi_s_axis_tdata_1 = Emu.axi_s_axis_tdata_1;
                axi_s_axis_tdata_2 = Emu.axi_s_axis_tdata_2;
                axi_s_axis_tdata_3 = Emu.axi_s_axis_tdata_3;
                axi_s_axis_tstrb = Emu.axi_s_axis_tstrb;
                axi_s_axis_tkeep = Emu.axi_s_axis_tkeep;
                axi_s_axis_tlast = Emu.axi_s_axis_tlast;
                axi_s_axis_tid = Emu.axi_s_axis_tid;
                axi_s_axis_tdest = Emu.axi_s_axis_tdest;
                axi_s_axis_tuser_0 = Emu.axi_s_axis_tuser_0;
                axi_s_axis_tuser_1 = Emu.axi_s_axis_tuser_1;
                Kiwi.Pause();
                Emu.axi_s_axis_tready = false;
                return true;
            }

            return false;
        }

        public bool sendOne(bool check_ready = true, bool check_updated = true)
        {
            if ((!check_updated || s_updated) && (!check_ready || Emu.axi_m_axis_tready))
            {
                s_updated = false;
                Emu.axi_m_axis_tvalid = true;
                Emu.axi_m_axis_tdata_0 = axi_m_axis_tdata_0;
                Emu.axi_m_axis_tdata_1 = axi_m_axis_tdata_1;
                Emu.axi_m_axis_tdata_2 = axi_m_axis_tdata_2;
                Emu.axi_m_axis_tdata_3 = axi_m_axis_tdata_3;
                Emu.axi_m_axis_tstrb = axi_m_axis_tstrb;
                Emu.axi_m_axis_tkeep = axi_m_axis_tkeep;
                Emu.axi_m_axis_tlast = axi_m_axis_tlast;
                Emu.axi_m_axis_tid = axi_m_axis_tid;
                Emu.axi_m_axis_tdest = axi_m_axis_tdest;
                Emu.axi_m_axis_tuser_0 = axi_m_axis_tuser_0;
                Emu.axi_m_axis_tuser_1 = axi_m_axis_tuser_1;
                Kiwi.Pause();
                Emu.axi_m_axis_tvalid = false;
                return true;
            }

            if (check_ready && !Emu.axi_m_axis_tready)
            {
                DebugFunctions.push_interrupt(DebugFunctions.Errors.SEND_NOT_READY);
                return false;
            }

            return false;
        }
    }

    public class FifoInterface
    {
        public bool push(ulong data)
        {
            if (!Emu.fifo_full)
            {
                Emu.fifo_din = data;
                Emu.fifo_wr_en = true;
                Kiwi.Pause();
                Emu.fifo_wr_en = false;
                return true;
            }

            DebugFunctions.push_interrupt(DebugFunctions.Errors.FIFO_FULL);
            return false;
        }

        public bool canPush()
        {
            return !Emu.fifo_full;
        }

        public bool canPop()
        {
            return !Emu.fifo_empty;
        }

        public ulong pop()
        {
            if (!Emu.fifo_empty)
            {
                var data = Emu.fifo_dout;
                Emu.fifo_rd_en = true;
                Kiwi.Pause();

                Emu.fifo_rd_en = false;
                return data;
            }

            DebugFunctions.push_interrupt(DebugFunctions.Errors.FIFO_EMPTY);
            return 0;
        }

        public static bool AXIReady(AxiInterface axi)
        {
            return AxiInterface.isReady();
        }

        public static bool AXIValid(AxiInterface axi)
        {
            return AxiInterface.isValid();
        }

        public static bool pushAXI(ulong data, AxiInterface axi)
        {
            if (AxiInterface.isReady())
            {
                axi.axi_m_axis_tdata_0 = data;
                axi.m_updated = true;
                axi.sendOne(true);
                return true;
            }

            return false;
        }
    }
}