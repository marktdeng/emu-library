//	Emu packet parsing library
//
//	Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
//	All rights reserved
//
//	Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

using KiwiSystem;

namespace EmuLibrary
{
    public static class CircularNetworkFunctions
    {
        public static uint RecvFrame(CircularFrameBuffer cfb)
        {
            // The start condition 
            var doneReading = false;

            // Local variables - counters
            var cnt = 0U;
            uint psize = 0;

            while (!doneReading)
            {
                if (Emu.s_axis_tvalid && cfb.CanPush() && Emu.s_axis_tready) // Receive data
                {
                    cfb.Push(Emu.s_axis_tkeep, Emu.s_axis_tlast, Emu.s_axis_tdata_0, Emu.s_axis_tdata_1,
                        Emu.s_axis_tdata_2, Emu.s_axis_tdata_3, Emu.s_axis_tuser_hi, Emu.s_axis_tuser_low);

                    psize = cnt++;
                    // Condition to stop receiving data
                    doneReading = Emu.s_axis_tlast || !Emu.s_axis_tvalid;
                    // Create backpresure to whatever sends data to us
                    Emu.s_axis_tready = !Emu.s_axis_tlast;
                    if (!cfb.CanPush()) // Buffer is full, stop receiving data
                        Emu.s_axis_tready = false;
                }
                else if (!cfb.CanPush()) // Buffer is still full
                {
                    Emu.s_axis_tready = false;
                }
                else if (!Emu.s_axis_tready) // Restart receiving data 
                {
                    Emu.s_axis_tready = true;
                }

                Kiwi.Pause();
            }

            Emu.PktIn++;

            Emu.s_axis_tready = false;
            return psize;
        }

        public static bool RecvOne(CircularFrameBuffer cfb, bool stop)
        {
            if (cfb.CanPush())
                if (Emu.s_axis_tvalid && cfb.CanPush())
                {
                    Emu.s_axis_tready = true;

                    cfb.Push(Emu.s_axis_tkeep, Emu.s_axis_tlast, Emu.s_axis_tdata_0, Emu.s_axis_tdata_1,
                        Emu.s_axis_tdata_2, Emu.s_axis_tdata_3, Emu.s_axis_tuser_hi, Emu.s_axis_tuser_low);
                    Kiwi.Pause();
                    if (stop) Reset();
                    return true;
                }
                else
                {
                    return false;
                }

            Emu.s_axis_tready = false;
            return false;
        }

        public static void SendFrame(CircularFrameBuffer cfb)
        {
            Reset();

            var status = 0U;

            while (status <= 1) status = SendOne(cfb, false, true);

            Reset();
        }

        private static uint SendOne(CircularFrameBuffer cfb, bool stop = true, bool movepeek = false,
            bool checkready = true)
        {
            if (cfb.CanPop() && (!checkready || Emu.m_axis_tready))
            {
                if (Emu.m_axis_tready) cfb.Pop(movepeek);

                Emu.m_axis_tvalid = true;
                Emu.m_axis_tdata_0 = cfb.PopData.Tdata0;
                Emu.m_axis_tdata_1 = cfb.PopData.Tdata1;
                Emu.m_axis_tdata_2 = cfb.PopData.Tdata2;
                Emu.m_axis_tdata_3 = cfb.PopData.Tdata3;

                Emu.m_axis_tkeep = cfb.PopData.Tkeep;
                Emu.m_axis_tlast = cfb.PopData.Tlast;
                Emu.m_axis_tuser_hi = cfb.PopData.TuserHi;
                Emu.m_axis_tuser_low = cfb.PopData.TuserLow;

                bool done = cfb.PopData.Tlast;

                Kiwi.Pause();

                if (stop) Reset();

                if (done)
                {
                    Emu.PktOut++;
                    return 2U;
                }

                return 0U;
            }

            if (!cfb.CanPop()) return 3U;

            Reset();
            return 1U;
        }

        private static void Reset()
        {
            Emu.m_axis_tvalid = false;
            Emu.m_axis_tlast = false;
            Emu.m_axis_tdata_0 = 0x0;
            Emu.m_axis_tdata_1 = 0x0;
            Emu.m_axis_tdata_2 = 0x0;
            Emu.m_axis_tdata_3 = 0x0;
            Emu.m_axis_tkeep = 0x0;
            Emu.m_axis_tuser_hi = 0x0;
            Emu.m_axis_tuser_low = 0x0;
        }

        public static void SendAndCut(CircularFrameBuffer cfb)
        {
            uint status = 0U;
            while (status <= 1) status = SendOne(cfb, false, true);

            if (status == 2)
                Reset();
            else if (status == 3) CutThrough();
        }

        private static void CutThrough()
        {
            bool done = false;
            do
            {
                if (Emu.s_axis_tvalid)
                {
                    Emu.m_axis_tvalid = true;

                    Emu.s_axis_tready = Emu.m_axis_tready;

                    Emu.m_axis_tdata_0 = Emu.s_axis_tdata_0;
                    Emu.m_axis_tdata_1 = Emu.s_axis_tdata_1;
                    Emu.m_axis_tdata_2 = Emu.s_axis_tdata_2;
                    Emu.m_axis_tdata_3 = Emu.s_axis_tdata_3;

                    Emu.m_axis_tkeep = Emu.s_axis_tkeep;
                    Emu.m_axis_tlast = Emu.s_axis_tlast;
                    Emu.m_axis_tuser_hi = 0U;
                    Emu.m_axis_tuser_low = 0U;

                    done = Emu.s_axis_tlast && Emu.s_axis_tvalid && Emu.m_axis_tready;
                }
                else
                {
                    Emu.m_axis_tvalid = false;
                    Emu.s_axis_tready = false;
                }

                Kiwi.Pause();
            } while (!done);

            Emu.PktOut++;

            Reset();
        }

        public static void SendWithFCS(CircularFrameBuffer cfb)
        {
        }
    }

    public class crc32
    {
        private uint value = 0xffffffff;
    }
}