using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
    public sealed class Game : IGame
    {
        public byte[] RomData;
        private string name;
        private List<string> options;
        private const int BankSize = 4096;

        public Game(string path, params string[] options)
        {
            name = Path.GetFileNameWithoutExtension(path).Replace('_',' ');
            this.options = new List<string>(options);

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int header = (int)(stream.Length % BankSize);
                stream.Position = header;
                int length = (int)stream.Length - header;

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
