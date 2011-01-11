using System;
using System.IO;

namespace BizHawk
{
    public sealed class Game : IGame
    {
        public byte[] RomData;
        private string name;
        private string[] options;
        private const int BankSize = 4096;

        public Game(string path, params string[] options)
        {
            name = Path.GetFileNameWithoutExtension(path).Replace('_',' ');
            this.options = options;
            if (this.options == null)
                this.options = new string[0];

            this.options = options;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int length = (int)stream.Length;

                stream.Position = 0;
                if (length % BankSize == 512) // 512-byte ROM header is present
                {
                    stream.Position += 512;
                    length -= 512;
                }
                if (length % BankSize == 64) // 64-byte ROM header is present
                {
                    stream.Position += 64;
                    length -= 64;
                }

                if (length % BankSize != 0)
                    throw new Exception("Not a valid ROM.");
                RomData = new byte[length];
                stream.Read(RomData, 0, length);
            }
        }

        public void Patch(string patch)
        {
            using (var stream = new FileStream(patch, FileMode.Open, FileAccess.Read))
            {
                IPS.Patch(RomData, stream);
            }
        }

        public byte[] GetRomData()
        {
            return RomData;
        }

        public string[] GetOptions()
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
