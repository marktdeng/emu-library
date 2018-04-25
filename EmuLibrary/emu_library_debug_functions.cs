// Emu debugging library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//

namespace EmuLibrary
{
/*
    public class EmuInterruptException : Exception
    {
        public EmuInterruptException()
        {
        }

        public EmuInterruptException(string message) : base(message)
        {
        }

        public EmuInterruptException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EmuInterruptException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
*/

    public static class DebugFunctions
    {
        public const byte PACKET_DROP = 0;
        public const byte PACKET_BUFFER_FULL = 1;
        public const byte PACKET_BUFFER_INVALID = 2;
        public const byte SEND_NOT_READY = 3;
        public const byte PARSE_FAIL = 4;
        public const byte ILLEGAL_PACKET_FORMAT = 5;
        public const byte EMPTY_PACKET = 6;
        public const byte FIFO_FULL = 7;
        public const byte FIFO_EMPTY = 8;
        public const byte AXI_NOT_READY = 9;
        public const byte AXI_NOT_VALID = 10;
        private static bool interrupts_enabled;
        private static readonly bool enable_software_exceptions = false;

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
            if (interrupts_enabled) Emu.Interrupts = Emu.Interrupts | (1ul << errortype);
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