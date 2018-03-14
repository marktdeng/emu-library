using KiwiSystem;

namespace EmuLibrary
{
    public class emu_packet_generator : Emu
    {
        const uint BUF_SIZE = 200U; // Max frame size = 1526 Bytes ~ 191x8Bxmd entries
        
        static CircularFrameBuffer cfb = new CircularFrameBuffer(BUF_SIZE);
        
        static void generate_packet()
        {
            
        }
        
        
        
        [Kiwi.HardwareEntryPoint()]
        static int EntryPoint()
        {
            while (true)
            {
                generate_packet();
            };
        }

        static int Main()
        {
            return 0;
        }
    }
}