using System;
using System.IO;
using Jellyfish.Library;

namespace Jellyfish.Virtu
{
    public sealed class DiskNib : Disk525
    {
		public DiskNib() { }
        public DiskNib(string name, byte[] data, bool isWriteProtected) : 
            base(name, data, isWriteProtected)
        {
        }

        public DiskNib(string name, Stream stream, bool isWriteProtected) :
            base(name, new byte[TrackCount * TrackSize], isWriteProtected)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.ReadBlock(Data);
        }

        public override void ReadTrack(int number, int fraction, byte[] buffer)
        {
            Buffer.BlockCopy(Data, (number / 2) * TrackSize, buffer, 0, TrackSize);
        }

        public override void WriteTrack(int number, int fraction, byte[] buffer)
        {
            Buffer.BlockCopy(buffer, 0, Data, (number / 2) * TrackSize, TrackSize);
        }
    }
}
