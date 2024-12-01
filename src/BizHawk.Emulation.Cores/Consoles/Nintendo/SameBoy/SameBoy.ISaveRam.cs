using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : ISaveRam
	{
		public bool SaveRamModified => LibSameboy.sameboy_sramlen(SameboyState) != 0;

		public byte[] CloneSaveRam()
		{
			int length = LibSameboy.sameboy_sramlen(SameboyState);

			if (length > 0)
			{
				byte[] ret = new byte[length];
				LibSameboy.sameboy_savesram(SameboyState, ret);
				return ret;
			}

			return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			int expected = LibSameboy.sameboy_sramlen(SameboyState);
			if (data.Length != expected) throw new ArgumentException(message: "Size of saveram data does not match expected!", paramName: nameof(data));

			if (expected > 0)
			{
				LibSameboy.sameboy_loadsram(SameboyState, data, data.Length);
			}
		}
	}
}
