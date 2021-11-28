using System;

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

			return new byte[0];
		}

		public void StoreSaveRam(byte[] data)
		{
			int expected = LibSameboy.sameboy_sramlen(SameboyState);
			if (data.Length - expected != 0)
			{
				throw new ArgumentException("Size of saveram data does not match expected!");
			}

			LibSameboy.sameboy_loadsram(SameboyState, data, data.Length);
		}
	}
}
