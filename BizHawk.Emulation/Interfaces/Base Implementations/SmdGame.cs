using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
    /// <summary>
    /// Loader for .SMD Genesis ROM format (Super Magic Drive)
    /// </summary>
    public sealed class SmdGame : IGame
    {
        public byte[] RomData;
        private string name;
        private List<string> options;

        private const int BankSize = 16384;

        // TODO bleh, just make this a minimal SMD implementation. Its good to have SMD support in base emu lib, but it doesn't need to be extravagent.

        // TODO we need a consistent interface for these ROM loader implementations, that easily supports loading direct files, or from Content.
        // TODO also should support Name-set, and some other crap.
        // TODO we should inject a way to support IPS patches, because the patch needs to be applied before de-interlacing (I assume).
        public SmdGame(string path, params string[] options)
        {
            name = Path.GetFileNameWithoutExtension(path).Replace('_', ' ');
            this.options = new List<string>(options);

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int length = (int)stream.Length;

                stream.Position = 0;
                if (length % BankSize == 512) // 512-byte ROM header is present
                {
                    stream.Position += 512;
                    length -= 512;
                }
                               
                if (length % BankSize != 0)
                    throw new Exception("Not a valid ROM.");
                var rawRomData = new byte[length];
                stream.Read(rawRomData, 0, length);
                RomData = DeInterleave(rawRomData);
            }
        }

        public byte[] DeInterleave(byte[] source)
        {
            // SMD files are interleaved in pages of 16k, with the first 8k containing all 
            // odd bytes and the second 8k containing all even bytes.

            int size = source.Length;
            if (size > 0x400000) size = 0x400000;
            int pages = size / 0x4000;
            byte[] output = new byte[size];

            for (int page = 0; page < pages; page++)
            {
                for (int i = 0; i < 0x2000; i++)
                {
                    output[(page * 0x4000) + (i * 2) + 0] = source[(page * 0x4000) + 0x2000 + i];
                    output[(page * 0x4000) + (i * 2) + 1] = source[(page * 0x4000) + 0x0000 + i];
                }
            }
            return output;
        }

        public byte[] GetRomData()
        {
            return RomData;
        }

        public IList<string> GetOptions()
        {
            return options;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}