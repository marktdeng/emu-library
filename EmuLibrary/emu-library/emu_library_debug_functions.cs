// Emu debugging library
//
// Copyright 2018 Mark Deng <mtd36@cam.ac.uk>
// All rights reserved
//
// Use of this source code is governed by the Apache 2.0 license; see LICENSE file
//

using System;

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
        public enum Errors : byte
        {
            PACKET_DROP,
            PACKET_BUFFER_FULL,
            PACKET_BUFFER_INVALID,
            SEND_NOT_READY,
            PARSE_FAIL,
            ILLEGAL_PACKET_FORMAT,
            EMPTY_PACKET,
            FIFO_FULL,
            FIFO_EMPTY,
            AXI_NOT_READY,
            AXI_NOT_VALID,
        }

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

        public static void push_interrupt(Errors errortype)
        {
            if (interrupts_enabled) Emu.Interrupts = Emu.Interrupts | (1ul << (byte) errortype);
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