//	Emu network library
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
        /*
         * Function: RecvFrame
         * Description: Receive and buffer an entire Ethernet frame.
         */
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

        /*
         * Function: RecvOne
         * Description: Receive and buffer a single segment of the ethernet frame.
         */
        public static bool RecvOne(CircularFrameBuffer cfb, bool stop)
        {
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
                Emu.s_axis_tready = false;
                return false;
            }

        }

        /*
         * Function: SendFrame
         * Description: Send the entirety of the buffered ethernet frame.
         */
        public static void SendFrame(CircularFrameBuffer cfb)
        {
            Reset();

            var status = 0U;

            while (status <= 1) status = SendOne(cfb, false, true);

            Reset();
        }

        /*
        * Function: SendOne
        * Description: Send a single segment of the buffered ethernet frame.
        */       
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

                var done = cfb.PopData.Tlast;

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

        /*
         * Function: Reset
         * Description: Reset the interfaces to a clean state.
         */
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

        /*
         * Function: SendAndCut
         * Description: Send the entirety of buffer and cut if the whole frame hasn't been sent.
         */
        public static void SendAndCut(CircularFrameBuffer cfb)
        {
            var status = 0U;
            while (status <= 1) status = SendOne(cfb, false, true);

            if (status == 2)
                Reset();
            else if (status == 3) CutThrough();
        }

        /*
         * Function: CutThrough
         * Description: Continuously sends received data until end of frame.
         */
        private static void CutThrough()
        {
            var done = false;
            do
            {
                Emu.m_axis_tvalid = Emu.s_axis_tvalid;

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

                Kiwi.Pause();
            } while (!done);

            Emu.PktOut++;

            Reset();
        }

        /*
         * Function: SendWithFCS
         * Description: Sends the whole buffered frame, appending the FCS at the end.
         */
        public static void SendWithFCS(CircularFrameBuffer cfb)
        {
        }
    }

    public class crc32
    {
        private uint value = 0xffffffff;
    }
}