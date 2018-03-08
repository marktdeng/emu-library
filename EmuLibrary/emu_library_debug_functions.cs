// IP packet class definition
//
// Copyright 2017 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//

using KiwiSystem;

// ReSharper disable InconsistentNaming

namespace EmuLibrary
{
    public static class debug_functions
    {
        private static bool interrupts_enabled = false;
        
        public const byte PACKET_DROP = 0;
        public const byte PACKET_BUFFER_FULL = 1;
        public const byte SEND_NOT_READY = 2;
        public const byte PARSE_FAIL = 3;
        public const byte ILLEGAL_PACKET_FORMAT = 4;
        public const byte EMPTY_PACKET = 5;
        public const byte FIFO_FULL = 6;
        public const byte FIFO_EMPTY = 7;
        public const byte AXI_NOT_READY = 8;
        public const byte AXI_NOT_VALID = 9;

        public static void interrupts_enable()
        {
            interrupts_enabled = true;
        }

        public static void interrupts_disable()
        {
            interrupts_enabled = false;
        }
        
        public static void push_interrupt(byte errortype)
        {
            if (interrupts_enabled)
            {
                Emu.Interrupts = Emu.Interrupts | 1ul << errortype;
            }
        }

        public static void reset_interrupt()
        {
            Emu.Interrupts = 0;
        }
    }

    public static class status_functions
    {
        public static void set_status(ulong status)
        {
            Emu.Status = status;
        }
    }
}