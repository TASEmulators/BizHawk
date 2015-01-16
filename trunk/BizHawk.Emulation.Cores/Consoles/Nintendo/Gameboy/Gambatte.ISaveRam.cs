using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				if (LibGambatte.gambatte_savesavedatalength(GambatteState) == 0)
					return false;
				else
					return true; // need to wire more stuff into the core to actually know this
			}
		}

		public byte[] CloneSaveRam()
		{
			int length = LibGambatte.gambatte_savesavedatalength(GambatteState);

			if (length > 0)
			{
				byte[] ret = new byte[length];
				LibGambatte.gambatte_savesavedata(GambatteState, ret);
				return ret;
			}
			else
				return new byte[0];
		}

		public void StoreSaveRam(byte[] data)
		{
			int expected = LibGambatte.gambatte_savesavedatalength(GambatteState);
			switch (data.Length - expected)
			{
				case 0:
					break;
				default:
					throw new ArgumentException("Size of saveram data does not match expected!");
				case 44:
					data = FixRTC(data, 44);
					break;
				case 40:
					data = FixRTC(data, 40);
					break;
			}
			LibGambatte.gambatte_loadsavedata(GambatteState, data);
		}

		private byte[] FixRTC(byte[] data, int offset)
		{
			// length - offset is the start of the VBA-only data; so
			// length - offset - 4 is the start of the RTC block
			int idx = data.Length - offset - 4;

			byte[] ret = new byte[idx + 4];
			Buffer.BlockCopy(data, 0, ret, 0, idx);
			data[idx] = (byte)zerotime;
			data[idx + 1] = (byte)(zerotime >> 8);
			data[idx + 2] = (byte)(zerotime >> 16);
			data[idx + 3] = (byte)(zerotime >> 24);

			return ret;
		}
	}
}
