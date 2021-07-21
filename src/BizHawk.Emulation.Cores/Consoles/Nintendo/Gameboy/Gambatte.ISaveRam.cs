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
				if (LibGambatte.gambatte_savesavedatalength(GambatteState, DeterministicEmulation) == 0)
				{
					return false;
				}

				return true; // need to wire more stuff into the core to actually know this
			}
		}

		public byte[] CloneSaveRam()
		{
			int length = LibGambatte.gambatte_savesavedatalength(GambatteState, DeterministicEmulation);

			if (length > 0)
			{
				byte[] ret = new byte[length];
				LibGambatte.gambatte_savesavedata(GambatteState, ret, DeterministicEmulation);
				return ret;
			}

			return new byte[0];
		}

		public void StoreSaveRam(byte[] data)
		{
			int expected = LibGambatte.gambatte_savesavedatalength(GambatteState, DeterministicEmulation);
			switch (data.Length - expected)
			{
				case 0:
					break;
				default:
					throw new ArgumentException("Size of saveram data does not match expected!");
			}

			LibGambatte.gambatte_loadsavedata(GambatteState, data, DeterministicEmulation);
		}
	}
}
