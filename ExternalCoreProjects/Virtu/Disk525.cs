using System;

namespace Jellyfish.Virtu
{
	internal abstract class Disk525
	{
		// ReSharper disable once UnusedMember.Global
		// ReSharper disable once PublicConstructorInAbstractClass
		public Disk525() { }

		protected Disk525(byte[] data, bool isWriteProtected)
		{
			Data = data;
			IsWriteProtected = isWriteProtected;
		}

		public static Disk525 CreateDisk(string name, byte[] data, bool isWriteProtected)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (name.EndsWith(".do", StringComparison.OrdinalIgnoreCase) ||
				name.EndsWith(".dsk", StringComparison.OrdinalIgnoreCase)) // assumes dos sector skew
			{
				return new DiskDsk(data, isWriteProtected, SectorSkew.Dos);
			}

			if (name.EndsWith(".nib", StringComparison.OrdinalIgnoreCase))
			{
				return new DiskNib(data, isWriteProtected);
			}

			if (name.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
			{
				return new DiskDsk(data, isWriteProtected, SectorSkew.ProDos);
			}

			return null;
		}

		public abstract void ReadTrack(int number, int fraction, byte[] buffer);
		public abstract void WriteTrack(int number, int fraction, byte[] buffer);

		public byte[] Data;

		public bool IsWriteProtected;

		public const int SectorCount = 16;
		public const int SectorSize = 0x100;
		public const int TrackSize = 0x1A00;
	}
}
