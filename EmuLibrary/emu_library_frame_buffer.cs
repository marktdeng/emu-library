// Emu Library Frame Buffer
//
// Copyright 2017-10`8 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
//	Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

namespace EmuLibrary
{
    public class CircularFrameBuffer
    {
        public readonly BufferEntry PeekData = new BufferEntry();

        public readonly BufferEntry PopData = new BufferEntry();

        public readonly BufferEntry PushData = new BufferEntry();
        private uint _curstart;

        private readonly object _lck = new object();
        private readonly uint[] _pstart;
        private readonly ulong[] _tdata_0;
        private readonly ulong[] _tdata_1;
        private readonly ulong[] _tdata_2;
        private readonly ulong[] _tdata_3;
        private readonly uint[] _tkeep;
        private readonly bool[] _tlast;
        private readonly ulong[] _tuser_hi; // unused
        private readonly ulong[] _tuser_low;
        private readonly bool[] _valid;

        public uint Bufsize;
        private uint count;
        private uint peekloc;
        private uint poploc;
        private uint writeloc;


        /*
         * Function: CircularFrameBuffer
         * Description: Initialise a new circular frame buffer of a specified size
         */
        public CircularFrameBuffer(uint bufsize)
        {
            Bufsize = bufsize;

            _tkeep = new uint[bufsize];
            _tlast = new bool[bufsize];

            _tdata_0 = new ulong[bufsize];
            _tdata_1 = new ulong[bufsize];
            _tdata_2 = new ulong[bufsize];
            _tdata_3 = new ulong[bufsize];

            _tuser_hi = new ulong[bufsize];
            _tuser_low = new ulong[bufsize];
            _pstart = new uint[bufsize];

            _valid = new bool[bufsize];
        }

        /*
         * Function: CanPush
         * Description: Check if there is space within buffer to push a buffer.
         */
        public bool CanPush()
        {
            return count < Bufsize;
        }

        /*
         * Function: CanPop
         * Description: Check if there is a packet present to be popped
         *              if movePeek is not specified then we check that the current peek location will not be popped
         */
        public bool CanPop(bool movePeek = false)
        {
            if (movePeek)
                return count > 0;
            return count > 0 && poploc != peekloc;
        }

        /*
         * Function: CanAdvance
         * Description: Check if the peek can be advanced
         */
        public bool CanAdvance()
        {
            return peekloc != writeloc;
        }

        /*
         * Function: ForwardPeek
         * Description: Advance the peek to the most newly pushed location
         */
        public bool ForwardPeek()
        {
            lock (PeekData)
            {
                if (writeloc == 0)
                    peekloc = Bufsize - 1;
                else
                    peekloc = writeloc - 1;

                return Peek();
            }
        }


        /*
         * Function: ResetPeek
         * Description: Reset the peek to the poploc
         */
        public bool ResetPeek()
        {
            lock (PeekData)
            {
                peekloc = poploc;
            }

            return Peek();
        }

        /*
         * Function: RewindPeek
         * Description: Return the peek to the first location within the packet
         */
        public bool RewindPeek()
        {
            lock (PeekData)
            {
                peekloc = _pstart[peekloc];
                return Peek();
            }
        }

        /*
         * Function: Push
         * Description: Push a new segment into the buffer
         */
        public bool Push(uint tkeep, bool tlast, ulong tdata_0, ulong tdata_1, ulong tdata_2, ulong tdata_3,
            ulong tuser_hi, ulong tuser_low, bool pstart = false)
        {
            if (!CanPush())
            {
                debug_functions.push_interrupt(debug_functions.FIFO_FULL);
                return false;
            }

            lock (_lck)
            {
                if (pstart) _curstart = writeloc;

                _tkeep[writeloc] = tkeep;
                _tlast[writeloc] = tlast;
                _tdata_0[writeloc] = tdata_0;
                _tdata_1[writeloc] = tdata_1;
                _tdata_2[writeloc] = tdata_2;
                _tdata_3[writeloc] = tdata_3;
                _tuser_hi[writeloc] = tuser_hi;
                _tuser_low[writeloc] = tuser_low;
                _pstart[writeloc] = _curstart;
                _valid[writeloc] = true;
                writeloc++;
                count++;
                if (writeloc >= Bufsize) writeloc = 0;
            }

            return true;
        }

        /*
         * Function: Push
         * Description: Push the value of a Bufferentry into the buffer
         */
        public bool Push(BufferEntry be, bool pstart = false)
        {
            return Push(be.Tkeep, be.Tlast, be.Tdata0, be.Tdata1, be.Tdata2, be.Tdata3, be.TuserHi, be.TuserHi, pstart);
        }

        /*
         * Function: UpdatePeek
         * Description: Update the value of the current peek location with the contents of a BufferEntry
         */
        public bool UpdatePeek(BufferEntry be)
        {
            lock (_lck)
            {
                lock (PeekData)
                {
                    if (!_valid[peekloc])
                    {
                        debug_functions.push_interrupt(debug_functions.PACKET_BUFFER_INVALID);
                        return false;
                    }

                    _tkeep[peekloc] = be.Tkeep;
                    _tlast[peekloc] = be.Tlast;
                    _tdata_0[peekloc] = be.Tdata0;
                    _tdata_1[peekloc] = be.Tdata1;
                    _tdata_2[peekloc] = be.Tdata2;
                    _tdata_3[peekloc] = be.Tdata3;
                    _tuser_hi[peekloc] = be.TuserHi;
                    _tuser_low[peekloc] = be.TuserLow;
                    return true;
                }
            }
        }

        /*
         * Function: WritePeek
         * Description: Same as UpdatePeek
         */
        public void WritePeek(BufferEntry be)
        {
            UpdatePeek(be);
        }

        /*
         * Function: Pop
         * Description: Pops the last element from the buffer
         *              If movePeek is not set, it will not pop if the the peek location would be popped
         */
        public bool Pop(bool movePeek = false)
        {
            if (!CanPop(movePeek)) return false;
            lock (_lck)
            {
                lock (PopData)
                {
                    PopData.Update(_tkeep[poploc], _tlast[poploc], _tdata_0[poploc], _tdata_1[poploc], _tdata_2[poploc],
                        _tdata_3[poploc], _tuser_hi[poploc], _tuser_low[poploc]);

                    _valid[poploc] = false;

                    poploc = (poploc + 1) % Bufsize;
                    count--;

                    if (movePeek) peekloc = poploc;

                    return true;
                }
            }
        }

        /*
         * Function: AdvancePeek
         * Description: Advance the peek location and update the peek BufferEntry
         */
        public bool AdvancePeek()
        {
            return Peek(true);
        }

        /*
         * Function: Peek
         * Description: Update the value of the peek BufferEntry
         *              If advance is set, the location will be incremented before updating the value
         */
        public bool Peek(bool advance = false)
        {
            lock (_lck)
            {
                lock (PeekData)
                {
                    if (advance && CanAdvance()) peekloc = (peekloc + 1) % Bufsize;
                    else if (!CanAdvance()) return false;

                    PeekData.Update(_tkeep[peekloc], _tlast[peekloc], _tdata_0[peekloc], _tdata_1[peekloc],
                        _tdata_2[peekloc], _tdata_3[peekloc], _tuser_hi[peekloc], _tuser_low[peekloc]);

                    return _valid[peekloc];
                }
            }
        }

        /*
         * Function: PrintContents
         * Description: Software only debugging function to print contents of circular frame buffer
         */
        /*
        public void PrintContents()
        {
            ResetPeek();
            while (CanAdvance())
            {
                System.Console.WriteLine("Data0: " + PeekData.Tdata0.ToString("X16"));
                System.Console.WriteLine("Data1: " + PeekData.Tdata1.ToString("X16"));
                System.Console.WriteLine("Data2: " + PeekData.Tdata2.ToString("X16"));
                System.Console.WriteLine("Data3: " + PeekData.Tdata3.ToString("X16"));
                System.Console.WriteLine("TuserLow: " + PeekData.TuserLow.ToString("X16"));
                System.Console.WriteLine("TuserHi: " + PeekData.TuserHi.ToString("X16"));
                AdvancePeek();
            }
        }
        */

        public class BufferEntry
        {
            private ulong _tdata0;

            private ulong _tdata1;

            private ulong _tdata2;

            private ulong _tdata3;
            private uint _tkeep;

            private bool _tlast;

            private ulong _tuserHi;

            private ulong _tuserLow;

            public uint Tkeep
            {
                get { return _tkeep; }
                set
                {
                    lock (this)
                    {
                        _tkeep = value;
                    }
                }
            }

            public bool Tlast
            {
                get { return _tlast; }
                set
                {
                    lock (this)
                    {
                        _tlast = value;
                    }
                }
            }

            public ulong Tdata0
            {
                get { return _tdata0; }
                set
                {
                    lock (this)
                    {
                        _tdata0 = value;
                    }
                }
            }

            public ulong Tdata1
            {
                get { return _tdata1; }
                set
                {
                    lock (this)
                    {
                        _tdata1 = value;
                    }
                }
            }

            public ulong Tdata2
            {
                get { return _tdata2; }
                set
                {
                    lock (this)
                    {
                        _tdata2 = value;
                    }
                }
            }

            public ulong Tdata3
            {
                get { return _tdata3; }
                set
                {
                    lock (this)
                    {
                        _tdata3 = value;
                    }
                }
            }

            public ulong TuserHi
            {
                get { return _tuserHi; }
                set
                {
                    lock (this)
                    {
                        _tuserHi = value;
                    }
                }
            }

            public ulong TuserLow
            {
                get { return _tuserLow; }
                set
                {
                    lock (this)
                    {
                        _tuserLow = value;
                    }
                }
            }

            /*
             * Function: Update
             * Description: Update the value of the buffer entry
             *              Does not update the value held within the buffer.
             */
            public void Update(uint tkeep, bool tlast, ulong tdata0, ulong tdata1, ulong tdata2, ulong tdata3,
                ulong tuserHi, ulong tuserLow)
            {
                lock (this)
                {
                    Tkeep = tkeep;
                    Tlast = tlast;
                    Tdata0 = tdata0;
                    Tdata1 = tdata1;
                    Tdata2 = tdata2;
                    Tdata3 = tdata3;
                    TuserHi = tuserHi;
                    TuserLow = tuserLow;
                }
            }

            public void Reset()
            {
                lock (this)
                {
                    _tkeep = 0;
                    _tlast = false;
                    _tdata0 = 0;
                    _tdata1 = 0;
                    _tdata2 = 0;
                    _tdata3 = 0;
                    _tuserHi = 0;
                    _tuserLow = 0;
                }
            }
        }
    }
}