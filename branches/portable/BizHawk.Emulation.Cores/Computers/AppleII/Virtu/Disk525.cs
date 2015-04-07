using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Jellyfish.Library;

namespace Jellyfish.Virtu
{
    public abstract class Disk525
    {
        protected Disk525(string name, byte[] data, bool isWriteProtected)
        {
            Name = name;
            Data = data;
            IsWriteProtected = isWriteProtected;
        }

        public static Disk525 CreateDisk(string name, Stream stream, bool isWriteProtected)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.EndsWith(".do", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".dsk", StringComparison.OrdinalIgnoreCase)) // assumes dos sector skew
            {
                return new DiskDsk(name, stream, isWriteProtected, SectorSkew.Dos);
            }
            else if (name.EndsWith(".nib", StringComparison.OrdinalIgnoreCase))
            {
                return new DiskNib(name, stream, isWriteProtected);
            }
            else if (name.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
            {
                return new DiskDsk(name, stream, isWriteProtected, SectorSkew.ProDos);
            }

            return null;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "version")]
        public static Disk525 LoadState(BinaryReader reader, Version version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            string name = reader.ReadString();
			var dataSize = reader.ReadInt32();
			var data = reader.ReadBytes(dataSize);
            bool isWriteProtected = reader.ReadBoolean();

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

        public void SaveState(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write(Name);
            writer.Write(Data.Length);
            writer.Write(Data);
            writer.Write(IsWriteProtected);
        }

        public abstract void ReadTrack(int number, int fraction, byte[] buffer);
        public abstract void WriteTrack(int number, int fraction, byte[] buffer);

        public string Name { get; private set; }
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Data { get; protected set; }
        public bool IsWriteProtected { get; private set; }

        public const int SectorCount = 16;
        public const int SectorSize = 0x100;
        public const int TrackCount = 35;
        public const int TrackSize = 0x1A00;
    }
}
