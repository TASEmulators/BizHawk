using System;

namespace Jellyfish.Virtu
{
	internal abstract class Disk525
	{
		// ReSharper disable once UnusedMember.Global
		// ReSharper disable once PublicConstructorInAbstractClass
		public Disk525() { }

		protected Disk525(string name, byte[] data, bool isWriteProtected)
		{
			_name = name;
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
				return new DiskDsk(name, data, isWriteProtected, SectorSkew.Dos);
			}

			if (name.EndsWith(".nib", StringComparison.OrdinalIgnoreCase))
			{
				return new DiskNib(name, data, isWriteProtected);
			}

			if (name.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
			{
				return new DiskDsk(name, data, isWriteProtected, SectorSkew.ProDos);
			}

			return null;
		}

		public abstract void ReadTrack(int number, int fraction, byte[] buffer);
		public abstract void WriteTrack(int number, int fraction, byte[] buffer);

		private string _name;

		public byte[] Data { get; protected set; }

		// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
		public bool IsWriteProtected { get; private set; }

		public const int SectorCount = 16;
		public const int SectorSize = 0x100;
		public const int TrackSize = 0x1A00;
	}
}
