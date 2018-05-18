// Emu network library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
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
                    cfb.PushData.Tkeep = Emu.s_axis_tkeep;
                    cfb.PushData.Tlast = Emu.s_axis_tlast;
                    cfb.PushData.Tdata0 = Emu.s_axis_tdata_0;
                    cfb.PushData.Tdata1 = Emu.s_axis_tdata_1;
                    cfb.PushData.Tdata2 = Emu.s_axis_tdata_2;
                    cfb.PushData.Tdata3 = Emu.s_axis_tdata_3;
                    cfb.PushData.TuserHi = Emu.s_axis_tuser_hi;
                    cfb.PushData.TuserLow = Emu.s_axis_tuser_low;
                    
                    Emu.s_axis_tready = !Emu.s_axis_tlast;

                    cfb.Push(cfb.PushData);

                    psize = cnt++;
                    // Condition to stop receiving data
                    doneReading = Emu.s_axis_tlast || !Emu.s_axis_tvalid;
                    
                    
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

                //Kiwi.Pause();
            }

            Emu.PktIn++;

            Emu.s_axis_tready = false;
            return psize;
        }

        /*
         * Function: RecvOne
         * Description: Receive and buffer a single segment of the ethernet frame.
         */
        public static bool RecvOne(CircularFrameBuffer cfb, bool stop, bool wait = false)
        {
            Emu.s_axis_tready = false;
            Emu.Status = 1;
            while (wait)
            {
                wait = !Emu.s_axis_tvalid;
            }
            bool cont = !stop;
            
            if (Emu.s_axis_tvalid && cfb.CanPush())
            {
		        Emu.s_axis_tready = true;
                cfb.PushData.Tkeep = Emu.s_axis_tkeep;
                cfb.PushData.Tlast = Emu.s_axis_tlast;
                cfb.PushData.Tdata0 = Emu.s_axis_tdata_0;
                cfb.PushData.Tdata1 = Emu.s_axis_tdata_1;
                cfb.PushData.Tdata2 = Emu.s_axis_tdata_2;
                cfb.PushData.Tdata3 = Emu.s_axis_tdata_3;
                cfb.PushData.TuserHi = Emu.s_axis_tuser_hi;
                cfb.PushData.TuserLow = Emu.s_axis_tuser_low;
                
                Emu.s_axis_tready = cont;

                cfb.Push(cfb.PushData);
                
                Emu.Status = 2;
                
                //Kiwi.Pause();
                return true;
            }

            Emu.s_axis_tready = false;
            return false;
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
        public static uint SendOne(CircularFrameBuffer cfb, bool stop = true, bool movepeek = false,
            bool checkready = true)
        {
            if (cfb.CanPop(movepeek) && (!checkready || Emu.m_axis_tready))
            {
                Emu.Status = 11;
                SetData(cfb, movepeek, true);

                var done = cfb.PopData.Tlast;
                Emu.Status = 3;

                Kiwi.Pause();

                if (stop) Reset();

                if (done)
                {
                    Emu.PktOut++;
                    Emu.Status = 12;
                    return 2U;
                }
                Emu.Status = 13;
                return 0U;
            }

            Emu.Status = 14;

            if (stop) Reset();

            return 3U;
        }

        /*
        * Function: SetData
        * Description: Pop and set the output data to the popped contents.
        */    
        private static bool SetData(CircularFrameBuffer cfb, bool movepeek = false, bool wait = false, bool valid = true)
        {
            bool ready = Emu.m_axis_tready;

            if (ready || wait) 
            {
                cfb.Pop(movepeek);
            }

            Emu.m_axis_tvalid = valid;
            Emu.m_axis_tdata_0 = cfb.PopData.Tdata0;
            Emu.m_axis_tdata_1 = cfb.PopData.Tdata1;
            Emu.m_axis_tdata_2 = cfb.PopData.Tdata2;
            Emu.m_axis_tdata_3 = cfb.PopData.Tdata3;

            Emu.m_axis_tkeep = cfb.PopData.Tkeep;
            Emu.m_axis_tlast = cfb.PopData.Tlast;
            Emu.m_axis_tuser_low = cfb.PopData.TuserLow;
            Emu.m_axis_tuser_hi = cfb.PopData.TuserHi;
            
            if (wait)
            {
                WaitReady();
            }

            return ready;
        }

        /*
        * Function: WaitReady
        * Description: Wait until the receiver is ready to accept data.
        */    
        private static void WaitReady()
        {
            while (!Emu.m_axis_tready)
            {
                Kiwi.Pause();
            }
        }

        /*
         * Function: Reset
         * Description: Reset the interfaces to a clean state.
         */
        private static void Reset()
        {
            Emu.s_axis_tready = false;
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
            if (cfb.CanPop(true)) {Emu.Interrupts = 1;}
            else {Emu.Interrupts = 2;}

            Emu.debug_reg = cfb.Count; 

            while (status <= 1)
            {
                status = SendOne(cfb, false, true, false);
            }

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
                Emu.Status = 4;

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

                done = Emu.s_axis_tlast && Emu.s_axis_tvalid;
                
                Kiwi.Pause();
               
                Emu.s_axis_tready = false;
                Emu.m_axis_tvalid = false;
            } while (!done);

            Emu.PktOut++;

            Reset();
            Kiwi.Pause();
        }

        /*
         * Function: SendWithFCS
         * Description: Sends the whole buffered frame, appending the FCS at the end.
         */
        public static bool SendWithFCS(CircularFrameBuffer cfb, crc32 crc)
        {
            bool cont = true;
            crc.Reset();
            while (cont)
            {
                if (cfb.CanPop(true))
                {
                    if (SetData(cfb, true, false, false))
                    {
                        crc.CRC_Compute(cfb.PopData);
                        cont = !cfb.PopData.Tlast;
                    };

                    if (cfb.PopData.Tlast)
                    {
                        var size = (int) NumSize(cfb.PopData.Tkeep);
                        if (size >= 24)
                        {
                            Emu.m_axis_tdata_3 = cfb.PopData.Tdata3 | (crc.CRC_LittleEndian() << ((size % 8) * 8));
                            
                        }
                        else if (size > 20)
                        {
                            Emu.m_axis_tdata_3 = cfb.PopData.Tdata3 | (crc.CRC_LittleEndian() >> ((24 - size) * 8));
                            Emu.m_axis_tdata_2 = cfb.PopData.Tdata2 | (crc.CRC_LittleEndian() << ((size % 8) * 8));
                        }
                        else if (size >= 16)
                        {
                            Emu.m_axis_tdata_2 = cfb.PopData.Tdata2 | (crc.CRC_LittleEndian() >> ((size % 8) * 8));
                        }
                        else if (size > 12)
                        {
                            Emu.m_axis_tdata_2 = cfb.PopData.Tdata2 | (crc.CRC_LittleEndian() >> ((16 - size) * 8));
                            Emu.m_axis_tdata_1 = cfb.PopData.Tdata1 | (crc.CRC_LittleEndian() << ((size % 8) * 8));
                        }
                        else if (size >= 8)
                        {
                            Emu.m_axis_tdata_1 = cfb.PopData.Tdata1 | (crc.CRC_LittleEndian() << ((size % 8) * 8));
                        }
                        else if (size > 4)
                        {
                            Emu.m_axis_tdata_1 = cfb.PopData.Tdata1 | (crc.CRC_LittleEndian() >> ((8 - size) * 8));
                            Emu.m_axis_tdata_0 = cfb.PopData.Tdata0 | (crc.CRC_LittleEndian() << (size * 8));
                        }
                        else
                        {
                            Emu.m_axis_tdata_0 = cfb.PopData.Tdata0 | (crc.CRC_LittleEndian() << (size * 8));
                        }

                        Emu.PktIn = (uint) size;

                        Emu.m_axis_tkeep = Emu.m_axis_tkeep << 4 | 0xF;
                        
                        //Console.WriteLine($"{size}:{crc.CRC_LittleEndian():X16}");

                    }
                    Emu.m_axis_tvalid = true;
                    WaitReady();
                    //System.Console.WriteLine($"{Emu.m_axis_tdata_0:X16},\n{Emu.m_axis_tdata_1:X16},\n{Emu.m_axis_tdata_2:X16},\n{Emu.m_axis_tdata_3:X16}");
                    //Kiwi.Pause();
                }
                else
                {
                    Reset();
                    return false;
                }
            }
            Reset();

            Emu.PktOut++;
            return true;
        }
        
        /*
        * Function: NumSize
        * Description: Calculate the size of tkeep
        */    
        private static uint NumSize(ulong input)
        {
            if (input == 0) return 64;

            uint n = 1;
            
            if (input >> 32 == 0) { n = n + 32; input = input << 32; }
            if (input >> 48 == 0) { n = n + 16; input = input << 16; }
            if (input >> 56 == 0) { n = n + 8; input = input << 8; }
            if (input >> 60 == 0) { n = n + 4; input = input << 4; }
            if (input >> 62 == 0) { n = n + 2; input = input << 2; }
            n = n - (uint) (input >> 63);

            return 64 - n;
        }
    }
    
    
    /*
    * Class: crc32
    * Description: A class to calculate crc32.
    */    
    public class crc32
    {
        private static readonly uint[] crc_table = {
            0x00000000U, 0x77073096U, 0xee0e612cU, 0x990951baU, 0x076dc419U, 0x706af48fU, 0xe963a535U, 0x9e6495a3U,
            0x0edb8832U, 0x79dcb8a4U, 0xe0d5e91eU, 0x97d2d988U, 0x09b64c2bU, 0x7eb17cbdU, 0xe7b82d07U, 0x90bf1d91U,
            0x1db71064U, 0x6ab020f2U, 0xf3b97148U, 0x84be41deU, 0x1adad47dU, 0x6ddde4ebU, 0xf4d4b551U, 0x83d385c7U,
            0x136c9856U, 0x646ba8c0U, 0xfd62f97aU, 0x8a65c9ecU, 0x14015c4fU, 0x63066cd9U, 0xfa0f3d63U, 0x8d080df5U,
            0x3b6e20c8U, 0x4c69105eU, 0xd56041e4U, 0xa2677172U, 0x3c03e4d1U, 0x4b04d447U, 0xd20d85fdU, 0xa50ab56bU,
            0x35b5a8faU, 0x42b2986cU, 0xdbbbc9d6U, 0xacbcf940U, 0x32d86ce3U, 0x45df5c75U, 0xdcd60dcfU, 0xabd13d59U,
            0x26d930acU, 0x51de003aU, 0xc8d75180U, 0xbfd06116U, 0x21b4f4b5U, 0x56b3c423U, 0xcfba9599U, 0xb8bda50fU,
            0x2802b89eU, 0x5f058808U, 0xc60cd9b2U, 0xb10be924U, 0x2f6f7c87U, 0x58684c11U, 0xc1611dabU, 0xb6662d3dU,
            0x76dc4190U, 0x01db7106U, 0x98d220bcU, 0xefd5102aU, 0x71b18589U, 0x06b6b51fU, 0x9fbfe4a5U, 0xe8b8d433U,
            0x7807c9a2U, 0x0f00f934U, 0x9609a88eU, 0xe10e9818U, 0x7f6a0dbbU, 0x086d3d2dU, 0x91646c97U, 0xe6635c01U,
            0x6b6b51f4U, 0x1c6c6162U, 0x856530d8U, 0xf262004eU, 0x6c0695edU, 0x1b01a57bU, 0x8208f4c1U, 0xf50fc457U,
            0x65b0d9c6U, 0x12b7e950U, 0x8bbeb8eaU, 0xfcb9887cU, 0x62dd1ddfU, 0x15da2d49U, 0x8cd37cf3U, 0xfbd44c65U,
            0x4db26158U, 0x3ab551ceU, 0xa3bc0074U, 0xd4bb30e2U, 0x4adfa541U, 0x3dd895d7U, 0xa4d1c46dU, 0xd3d6f4fbU,
            0x4369e96aU, 0x346ed9fcU, 0xad678846U, 0xda60b8d0U, 0x44042d73U, 0x33031de5U, 0xaa0a4c5fU, 0xdd0d7cc9U,
            0x5005713cU, 0x270241aaU, 0xbe0b1010U, 0xc90c2086U, 0x5768b525U, 0x206f85b3U, 0xb966d409U, 0xce61e49fU,
            0x5edef90eU, 0x29d9c998U, 0xb0d09822U, 0xc7d7a8b4U, 0x59b33d17U, 0x2eb40d81U, 0xb7bd5c3bU, 0xc0ba6cadU,
            0xedb88320U, 0x9abfb3b6U, 0x03b6e20cU, 0x74b1d29aU, 0xead54739U, 0x9dd277afU, 0x04db2615U, 0x73dc1683U,
            0xe3630b12U, 0x94643b84U, 0x0d6d6a3eU, 0x7a6a5aa8U, 0xe40ecf0bU, 0x9309ff9dU, 0x0a00ae27U, 0x7d079eb1U,
            0xf00f9344U, 0x8708a3d2U, 0x1e01f268U, 0x6906c2feU, 0xf762575dU, 0x806567cbU, 0x196c3671U, 0x6e6b06e7U,
            0xfed41b76U, 0x89d32be0U, 0x10da7a5aU, 0x67dd4accU, 0xf9b9df6fU, 0x8ebeeff9U, 0x17b7be43U, 0x60b08ed5U,
            0xd6d6a3e8U, 0xa1d1937eU, 0x38d8c2c4U, 0x4fdff252U, 0xd1bb67f1U, 0xa6bc5767U, 0x3fb506ddU, 0x48b2364bU,
            0xd80d2bdaU, 0xaf0a1b4cU, 0x36034af6U, 0x41047a60U, 0xdf60efc3U, 0xa867df55U, 0x316e8eefU, 0x4669be79U,
            0xcb61b38cU, 0xbc66831aU, 0x256fd2a0U, 0x5268e236U, 0xcc0c7795U, 0xbb0b4703U, 0x220216b9U, 0x5505262fU,
            0xc5ba3bbeU, 0xb2bd0b28U, 0x2bb45a92U, 0x5cb36a04U, 0xc2d7ffa7U, 0xb5d0cf31U, 0x2cd99e8bU, 0x5bdeae1dU,
            0x9b64c2b0U, 0xec63f226U, 0x756aa39cU, 0x026d930aU, 0x9c0906a9U, 0xeb0e363fU, 0x72076785U, 0x05005713U,
            0x95bf4a82U, 0xe2b87a14U, 0x7bb12baeU, 0x0cb61b38U, 0x92d28e9bU, 0xe5d5be0dU, 0x7cdcefb7U, 0x0bdbdf21U,
            0x86d3d2d4U, 0xf1d4e242U, 0x68ddb3f8U, 0x1fda836eU, 0x81be16cdU, 0xf6b9265bU, 0x6fb077e1U, 0x18b74777U,
            0x88085ae6U, 0xff0f6a70U, 0x66063bcaU, 0x11010b5cU, 0x8f659effU, 0xf862ae69U, 0x616bffd3U, 0x166ccf45U,
            0xa00ae278U, 0xd70dd2eeU, 0x4e048354U, 0x3903b3c2U, 0xa7672661U, 0xd06016f7U, 0x4969474dU, 0x3e6e77dbU,
            0xaed16a4aU, 0xd9d65adcU, 0x40df0b66U, 0x37d83bf0U, 0xa9bcae53U, 0xdebb9ec5U, 0x47b2cf7fU, 0x30b5ffe9U,
            0xbdbdf21cU, 0xcabac28aU, 0x53b39330U, 0x24b4a3a6U, 0xbad03605U, 0xcdd70693U, 0x54de5729U, 0x23d967bfU,
            0xb3667a2eU, 0xc4614ab8U, 0x5d681b02U, 0x2a6f2b94U, 0xb40bbe37U, 0xc30c8ea1U, 0x5a05df1bU, 0x2d02ef8dU
        };
    
        private static ulong crc_value = 0xffffffff;

        /*
        * Function: CRC_Compute
        * Description: Add the contents of the buffer entry to the crc.
        */    
        public void CRC_Compute(CircularFrameBuffer.BufferEntry be)
        {
            CRC_Compute(be.Tdata0);
            CRC_Compute(be.Tdata1);
            CRC_Compute(be.Tdata2);
            CRC_Compute(be.Tdata3);
        }

        /*
        * Function: CRC_Compute
        * Description: Add the contents of data to the crc.
        */    
        public void CRC_Compute(ulong data)
        {
            while (data > 0)
            {
                CRC_Compute((byte) data);
                data = data >> 8;
            }
        }
        
        /*
        * Function: CRC_Compute
        * Description: Add the contents of data to the crc.
        */   
        public void CRC_Compute(uint data)
        {
            while (data > 0)
            {
                CRC_Compute((byte) data);
                data = data >> 8;
            }
        }

        /*
        * Function: CRC_Compute
        * Description: Add the contents of data to the crc.
        */   
        public void CRC_Compute(byte data)
        {
            byte j = (byte) (((byte) (crc_value) ^ data) & 0xFF);
            crc_value = (crc_value >> 8) ^ crc_table[j];
        }

        /*
        * Function: CRC_Finalise
        * Description: Finalise the crc so that it can be used.
        */   
        public ulong CRC_Finalise()
        {
            return crc_value ^ 0xffffffff;
        }

        /*
        * Function: CRC_LittleEndian
        * Description: Finalize and convert to little endian.
        */ 
        public ulong CRC_LittleEndian()
        {
            return Emu.SwapEndian(CRC_Finalise());
        }

        /*
        * Function: Reset
        * Description: Reset so that a new crc can be computed.
        */ 
        public void Reset()
        {
            crc_value = 0xffffffff;
        }
        
    }
        
}
