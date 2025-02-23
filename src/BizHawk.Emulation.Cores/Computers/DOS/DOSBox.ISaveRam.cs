using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ISaveRam
	{
		public new bool SaveRamModified
		{
			get
			{
				bool sramChanged = _libDOSBox.sram_changed();
				Console.WriteLine("SRAM Changed {0}", sramChanged);
				return sramChanged;
			}
		}

		public new byte[] CloneSaveRam()
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

		public new void StoreSaveRam(byte[] data)
		{
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
