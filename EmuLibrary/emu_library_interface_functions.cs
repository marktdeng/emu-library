// Emu Interface Management Functions
//
// Copyright 2017 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//

using System.Reflection;
using KiwiSystem;

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
            cfb.PeekData.TuserLow = (cfb.PeekData.TuserLow & 0xFFFFFFFF00FFFFFF) | (ulong) (portNumber << 24);
            cfb.WritePeek();
        }
    }

    public class BusWidthConverter
    {
        private byte[] buffer = new byte[16];
        private byte _writept;
        private byte _readpt;
        private byte _size;

        public void Push(ulong data, byte length = 8)
        {
            if (length > 8)
            {
                length = 8;
            }
            while (length >= 1)
            {
                buffer[_writept++] = (byte) data;
                data >>= 8;
                length--;
                _size++;
            }
        }
        
        public void Push(uint data, byte length = 4)
        {
            if (length > 4)
            {
                length = 4;
            }
            while (length >= 1)
            {
                buffer[_writept++] = (byte) data;
                data >>= 8;
                length--;
                _size++;
            }
        }
        
        public void Push(ushort data, byte length = 2)
        {
            if (length > 2)
            {
                length = 2;
            }
            while (length >= 1)
            {
                buffer[_writept++] = (byte) data;
                data >>= 8;
                length--;
                _size++;
            }
        }
        
        public void Push(byte data, byte length = 1)
        {
           buffer[_writept++] = (byte) data;
           data >>= 8;
           _size++;
        }

        public byte PopByte()
        {
            if (_size >= 1)
            {
                return buffer[_readpt++];
            }
            else
            {
                return 0;
            }
        }
        
        public ulong PopULong()
        {
            if (_size >= 8)
            {
                ulong temp;
                temp = PopByte();
                temp |= (ulong) PopByte() << 8;
                temp |= (ulong) PopByte() << 16;
                temp |= (ulong) PopByte() << 24;
                temp |= (ulong) PopByte() << 32;
                temp |= (ulong) PopByte() << 40;
                temp |= (ulong) PopByte() << 48;
                temp |= (ulong) PopByte() << 56;
                return temp;
            }
            else
            {
                return 0;
            }
        }

        public uint PopUInt()
        {
            if (_size >= 8)
            {
                uint temp;
                temp = PopByte();
                temp |= (uint) PopByte() << 8;
                temp |= (uint) PopByte() << 16;
                temp |= (uint) PopByte() << 24;
                return temp;
            }
            else
            {
                return 0;
            }
        }
        
        public uint PopUShort()
        {
            if (_size >= 8)
            {
                ushort temp;
                temp = PopByte();
                temp |= (ushort) ((ushort) PopByte() << 8);
                return temp;
            }
            else
            {
                return 0;
            }
        }
    }
}