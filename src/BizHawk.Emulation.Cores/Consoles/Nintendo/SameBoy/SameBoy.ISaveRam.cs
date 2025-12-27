using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : ISaveRam
	{
		public bool SaveRamModified => LibSameboy.sameboy_sramlen(SameboyState) != 0;

		public byte[] CloneSaveRam(bool clearDirty)
		{
			int length = LibSameboy.sameboy_sramlen(SameboyState);

			if (length == 0)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			else
			{
				byte[] ret = new byte[length];
				LibSameboy.sameboy_savesram(SameboyState, ret);
				return ret;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			int expected = LibSameboy.sameboy_sramlen(SameboyState);
			if (expected == 0) throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			if (data.Length != expected) throw new ArgumentException(message: "Size of saveram data does not match expected!", paramName: nameof(data));

			LibSameboy.sameboy_loadsram(SameboyState, data, data.Length);
		}
	}
}
