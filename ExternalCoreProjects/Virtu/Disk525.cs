using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Jellyfish.Library;

namespace Jellyfish.Virtu
{
    public abstract class Disk525
    {
		public Disk525() { }
        protected Disk525(string name, byte[] data, bool isWriteProtected)
        {
            Name = name;
            Data = data;
            IsWriteProtected = isWriteProtected;
        }

        public static Disk525 CreateDisk(string name, byte[] data, bool isWriteProtected)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.EndsWith(".do", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".dsk", StringComparison.OrdinalIgnoreCase)) // assumes dos sector skew
            {
                return new DiskDsk(name, data, isWriteProtected, SectorSkew.Dos);
            }
            else if (name.EndsWith(".nib", StringComparison.OrdinalIgnoreCase))
            {
                return new DiskNib(name, data, isWriteProtected);
            }
            else if (name.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
            {
                return new DiskDsk(name, data, isWriteProtected, SectorSkew.ProDos);
            }

            return null;
        }

        public abstract void ReadTrack(int number, int fraction, byte[] buffer);
        public abstract void WriteTrack(int number, int fraction, byte[] buffer);

        public string Name { get; private set; }

        public byte[] Data { get; protected set; }
        public bool IsWriteProtected { get; private set; }

        public const int SectorCount = 16;
        public const int SectorSize = 0x100;
        public const int TrackCount = 35;
        public const int TrackSize = 0x1A00;
    }
}
