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
            private uint _tkeep;

            private bool _tlast;

            private ulong _tdata0;

            private ulong _tdata1;

            private ulong _tdata2;

            private ulong _tdata3;

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
        private bool[] _valid;
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

        public bool ForwardPeek()
        {
            lock (PeekData)
            {
                if (writeloc == 0)
                {
                    peekloc = bufsize - 1;
                }
                else
                {
                    peekloc = writeloc - 1;
                }
                
                return Peek();
            }
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
                _valid[writeloc] = true;
                writeloc++;
                count++;
                if (writeloc >= size)
                {
                    writeloc = 0;
                }
            }

            return true;
        }

        public bool Push(BufferEntry be, bool pstart = false)
        {
            return Push(be.Tkeep, be.Tlast, be.Tdata0, be.Tdata1, be.Tdata2, be.Tdata3, be.TuserHi, be.TuserHi, pstart);
        }

        public bool UpdatePeek(BufferEntry be)
        {
            lock (_lck)
            {
                lock (PeekData)
                {
                    if (!_valid[peekloc]) return false;
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

                    poploc = (poploc + 1) % size;
                    count--;

                    if (movePeek) peekloc = poploc;
                    
                    return true;
                }
            }
        }

        public bool AdvancePeek()
        {
            return Peek(true);
        }

        public bool Peek(bool advance = false)
        {
            lock (_lck)
            {
                lock (PeekData)
                {
                    if (advance && CanAdvance()) peekloc = (peekloc + 1) % size;
                    else if (!CanAdvance()) return false;

                    PeekData.Update(_tkeep[peekloc], _tlast[peekloc], _tdata_0[peekloc], _tdata_1[peekloc],
                        _tdata_2[peekloc], _tdata_3[peekloc], _tuser_hi[peekloc], _tuser_low[peekloc]);

                    return _valid[peekloc];
                }
            }
        }
    }
}