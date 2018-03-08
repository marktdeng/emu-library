using System;
using KiwiSystem;


namespace EmuLibrary
{
    /*
    [Serializable]
    public class InvalidBufferAdvanceException : Exception
    {
        public InvalidBufferAdvanceException()
        {
        }

        public InvalidBufferAdvanceException(string message)
            : base(message)
        {
        }

        public InvalidBufferAdvanceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    */

    public class FrameBuffer
    {
        public uint[] tkeep;
        public bool[] tlast;
        public ulong[] tdata_0;
        public ulong[] tdata_1;
        public ulong[] tdata_2;
        public ulong[] tdata_3;
        public ulong[] tuser_hi; // unused
        public ulong[] tuser_low;
        public uint psize;

        public FrameBuffer(uint bufsize)
        {
            tkeep = new uint[bufsize];
            tlast = new bool[bufsize];

            tdata_0 = new ulong[bufsize];
            tdata_1 = new ulong[bufsize];
            tdata_2 = new ulong[bufsize];
            tdata_3 = new ulong[bufsize];

            tuser_hi = new ulong[bufsize];
            tuser_low = new ulong[bufsize];
        }
    }


    public class CircularFrameBuffer
    {
        public class BufferEntry
        {
            public uint Tkeep;

            public bool Tlast;

            public ulong Tdata0;

            public ulong Tdata1;

            public ulong Tdata2;

            public ulong Tdata3;

            public ulong TuserHi;

            public ulong TuserLow;

            public void update(uint tkeep, bool tlast, ulong tdata0, ulong tdata1, ulong tdata2, ulong tdata3,
                ulong tuserHi, ulong tuserLow)
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

        private object _lck = new object();
        private uint writeloc = 0;
        private uint peekloc = 0;
        private uint poploc = 0;
        private uint size;
        private uint count = 0;
        private uint[] _tkeep;
        private bool[] _tlast;
        private ulong[] _tdata_0;
        private ulong[] _tdata_1;
        private ulong[] _tdata_2;
        private ulong[] _tdata_3;
        private ulong[] _tuser_hi; // unused
        private ulong[] _tuser_low;
        private uint[] _pstart;
        private uint _curstart = 0;

        public BufferEntry PopData = new BufferEntry();

        public BufferEntry PeekData = new BufferEntry();

        public uint bufsize;

        public CircularFrameBuffer(uint bufsize)
        {
            _tkeep = new uint[bufsize];
            _tlast = new bool[bufsize];

            _tdata_0 = new ulong[bufsize];
            _tdata_1 = new ulong[bufsize];
            _tdata_2 = new ulong[bufsize];
            _tdata_3 = new ulong[bufsize];

            _tuser_hi = new ulong[bufsize];
            _tuser_low = new ulong[bufsize];
            _pstart = new uint[bufsize];
        }

        public bool CanPush()
        {
            return (count < size);
        }

        public bool CanPop(bool movePeek = false)
        {
            if (movePeek)
            {
                return count > 0;
            }
            else
            {
                return count > 0 && poploc != peekloc;
            }
        }

        public bool CanAdvance()
        {
            return (peekloc != writeloc);
        }

        public bool Push(uint tkeep, bool tlast, ulong tdata_0, ulong tdata_1, ulong tdata_2, ulong tdata_3,
            ulong tuser_hi, ulong tuser_low, bool pstart = false)
        {
            if (!CanPush()) return false;
            lock (_lck)
            {
                if (pstart)
                {
                    _curstart = writeloc;
                }
                _tkeep[writeloc] = tkeep;
                _tlast[writeloc] = tlast;
                _tdata_0[writeloc] = tdata_0;
                _tdata_1[writeloc] = tdata_1;
                _tdata_2[writeloc] = tdata_2;
                _tdata_3[writeloc] = tdata_3;
                _tuser_hi[writeloc] = tuser_hi;
                _tuser_low[writeloc] = tuser_low;
                _pstart[writeloc] = _curstart;
                writeloc++;
                count++;
                if (writeloc >= size)
                {
                    writeloc = 0;
                }
            }

            return true;
        }

        public bool Pop(bool movePeek = false)
        {
            if (!CanPop(movePeek)) return false;
            lock (PopData)
            {
                lock (_lck)
                {
                    PopData.update(_tkeep[poploc], _tlast[poploc], _tdata_0[poploc], _tdata_1[poploc], _tdata_2[poploc],
                        _tdata_3[poploc], _tuser_hi[poploc], _tuser_low[poploc]);

                    poploc++;
                    count--;

                    if (poploc >= size) poploc = 0;
                    if (movePeek) peekloc = poploc;
                }
            }

            return true;
        }

        public bool AdvancePeek()
        {
            if (!CanAdvance()) return false;
            lock (PeekData)
            {
                lock (_lck)
                {
                    peekloc++;

                    if (peekloc >= size)
                    {
                        peekloc = 0;
                    }

                    PeekData.update(_tkeep[peekloc], _tlast[peekloc], _tdata_0[peekloc], _tdata_1[peekloc],
                        _tdata_2[peekloc], _tdata_3[peekloc], _tuser_hi[peekloc], _tuser_low[peekloc]);
                }
            }

            return true;
        }

        public void WritePeek()
        {
            lock (_lck)
            {
                _tkeep[peekloc] = PeekData.Tkeep;
                _tlast[peekloc] = PeekData.Tlast;
                _tdata_0[peekloc] = PeekData.Tdata0;
                _tdata_1[peekloc] = PeekData.Tdata1;
                _tdata_2[peekloc] = PeekData.Tdata2;
                _tdata_3[peekloc] = PeekData.Tdata3;
                _tuser_hi[peekloc] = PeekData.TuserHi;
                _tuser_low[peekloc] = PeekData.TuserLow;
            }
        }
    }
}