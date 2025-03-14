using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles._3DO
{
	public partial class Opera : ISaveRam
	{
		public new bool SaveRamModified
		{
			get
			{
				bool sramChanged = _libOpera.sram_changed();
				return sramChanged;
			}
		}

		public new byte[] CloneSaveRam()
		{
			var sramSize = _libOpera.get_sram_size();
			Console.WriteLine("SRAM Size {0}", sramSize);

			byte[] sramArray = new byte[sramSize];
			unsafe
			{
				fixed (byte* p = sramArray)
				{
					IntPtr ptr = (IntPtr) p;
					_libOpera.get_sram(ptr);
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
					_libOpera.set_sram(ptr);
				}
			}

		}
	}
}