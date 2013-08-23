using System;
using System.IO;

namespace BizHawk
{
    public static class IPS
    {
        public static void Patch(byte[] rom, Stream patch)
        {
            var ipsHeader = new byte[5];
            patch.Read(ipsHeader, 0, 5);

            string header = "PATCH";
            for (int i = 0; i < 5; i++)
            {
                if (ipsHeader[i] != header[i])
                {
                    Console.WriteLine("Patch file specified is invalid.");
                    return;
                }
            }

            // header verified, loop over patch entries
            uint EOF = ('E' * 0x10000 + 'O' * 0x100 + 'F');

            while (true)
            {
                uint offset = Read24(patch);
                if (offset == EOF) return;
                ushort size = Read16(patch);

                if (size != 0) // non-RLE patch
                {
                    var patchData = new byte[size];
                    patch.Read(patchData, 0, size);
                    for (int i = 0; i < size; i++)
                        rom[offset++] = patchData[i];
                }
                else // RLE patch
                {
                    size = Read16(patch);
                    byte value = (byte)patch.ReadByte();
                    for (int i = 0; i < size; i++)
                        rom[offset++] = value;
                }
            }
        }

        private static ushort Read16(Stream patch)
        {
            int Upper = patch.ReadByte();
            int Lower = patch.ReadByte();
            return (ushort)(Upper * 0x100 + Lower);
        }

        private static uint Read24(Stream patch)
        {
            int Upper = patch.ReadByte();
            int Middle = patch.ReadByte();
            int Lower = patch.ReadByte();
            return (uint)(Upper * 0x10000 + Middle * 0x100 + Lower);
        }
    }
}