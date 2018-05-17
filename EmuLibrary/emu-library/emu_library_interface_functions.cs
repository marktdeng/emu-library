// Emu interface management library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

namespace EmuLibrary
{
    public static class InterfaceFunctions
    {
        public static readonly byte PORT_BROADCAST = 0x55;
        public static readonly byte PORT_1 = 0x01;
        public static readonly byte PORT_2 = 0x04;
        public static readonly byte PORT_3 = 0x10;
        public static readonly byte PORT_4 = 0x40;

        public static void SetDestInterface(byte portNumber, CircularFrameBuffer cfb)
        {
            Emu.Status = 0xff;
            cfb.Peek();
            cfb.PeekData.TuserLow = (cfb.PeekData.TuserLow & 0xFFFFFFFF00FFFFFF) |  ((ulong) portNumber << 24);
            cfb.UpdatePeek(cfb.PeekData);
        }

        public static void SetDestInterface(byte portNumber, CircularFrameBuffer.BufferEntry be)
        {
            be.TuserLow = (be.TuserLow & 0xFFFFFFFF00FFFFFF) | ((ulong) portNumber << 24);
        }

        public static void SetDestInterface(byte portNumber, EthernetParserGenerator ep)
        {
            ep.Metadata = (ep.Metadata & 0xFFFFFFFF00FFFFFF) | ((ulong) portNumber << 24);
        }
    }

    public class BusWidthConverter
    {
        private readonly byte[] _buffer = new byte[16];
        private byte _readpt;
        private byte _size;
        private byte _writept;

        public void Push(ulong data, byte length = 8)
        {
            if (length > 8) length = 8;
            while (length >= 1)
            {
                _buffer[_writept] = (byte) data;
                _writept = (byte) ((_writept + 1) % 16);
                data >>= 8;
                length--;
                _size++;
            }
        }

        public void Push(uint data, byte length = 4)
        {
            if (length > 4) length = 4;
            while (length >= 1)
            {
                _buffer[_writept] = (byte) data;
                _writept = (byte) ((_writept + 1) % 16);
                data >>= 8;
                length--;
                _size++;
            }
        }

        public void Push(ushort data, byte length = 2)
        {
            if (length > 2) length = 2;
            while (length >= 1)
            {
                _buffer[_writept] = (byte) data;
                _writept = (byte) ((_writept + 1) % 16);
                data >>= 8;
                length--;
                _size++;
            }
        }

        public void Push(byte data, byte length = 1)
        {
            _buffer[_writept] = data;
            _writept = (byte) ((_writept + 1) % 16);
            data >>= 8;
            _size++;
        }

        public byte PopByte()
        {
            if (_size >= 1)
            {
                _size--;
                byte data = _buffer[_readpt];
                _readpt = (byte) ((_readpt + 1) % 16);
                return data;
            }

            return 0;
        }

        public ulong PopULong(byte length = 8)
        {
            if (length > 8) length = 8;
            if (_size >= length)
            {
                ulong temp = 0;
                byte offset = 0;
                for (int i = 0; i < length; i++)
                {
                    temp = (ulong) PopByte() << offset;
                    offset += 8;
                }
                return temp;
            }

            return 0;
        }

        public uint PopUInt(byte length = 4)
        {
            if (length > 4) length = 4;
            if (_size >= length)
            {
                uint temp = 0;
                byte offset = 0;
                for (int i = 0; i < length; i++)
                {
                    temp = (uint) PopByte() << offset;
                    offset += 8;
                }
                return temp;
            }

            return 0;
        }

        public ushort PopUShort(byte length = 2)
        {
            if (length > 2) length = 2;
            if (_size >= length)
            {
                ushort temp = 0;
                byte offset = 0;
                for (int i = 0; i < length; i++)
                {
                    temp = (ushort) (PopByte() << offset);
                    offset += 8;
                }
                return temp;
            }
            
            return 0;
        }
    }
}
