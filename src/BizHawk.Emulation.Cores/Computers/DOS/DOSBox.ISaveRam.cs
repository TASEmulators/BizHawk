using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ISaveRam
	{
		public override bool SaveRamModified
		{
			get
			{
				bool sramChanged = _libDOSBox.sram_changed();
				return sramChanged;
			}
		}

		public override byte[] CloneSaveRam()
		{
			var sramSize = _libDOSBox.get_sram_size();
			Console.WriteLine("SRAM Size {0}", sramSize);

			byte[] sramArray = new byte[sramSize];
			unsafe
			{
				fixed (byte* p = sramArray)
				{
					IntPtr ptr = (IntPtr) p;
					_libDOSBox.get_sram(ptr);
				}
			}

			return sramArray;
		}

		public override void StoreSaveRam(byte[] data)
		{
			if (data.Length != (int) _syncSettings.WriteableHardDisk)
			{
				Console.WriteLine("SRAM size {0} does not match that of the chosen writable hard disk {1}. Aborting SRAM loading.", data.Length, (int) _syncSettings.WriteableHardDisk);
				return;
			}

			unsafe
			{
				fixed (byte* p = data)
				{
					IntPtr ptr = (IntPtr) p;
					_libDOSBox.set_sram(ptr);
				}
			}

		}
	}
}
