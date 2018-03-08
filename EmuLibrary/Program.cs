using System;
using System.IO;

namespace EmuLibrary
{
    internal class Program
    {
        /*
        public static void Main(string[] args)
        {
            var file = File.Open("message1", FileMode.Open);
            var binReader = new BinaryReader(file);
            var b = (byte) file.ReadByte();
            file.Position = 0;
            var header = binReader.ReadBytes(IPv4Packet.get_ihl(b) * 4);
            var i = new IPv4Packet(header);
            Console.Out.WriteLine(i.get_total_length());
            Console.Out.WriteLine(i.get_src_address());
            Console.Out.WriteLine(i.get_dest_address());
        }
        */
    }
}