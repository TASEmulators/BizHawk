using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISaveRam
	{
		// need to wire more stuff into the core to actually know this
		public bool SaveRamModified => LibGambatte.gambatte_getsavedatalength(GambatteState) != 0;

		public byte[] CloneSaveRam()
		{
			var length = LibGambatte.gambatte_getsavedatalength(GambatteState);

			if (length > 0)
			{
				var ret = new byte[length];
				LibGambatte.gambatte_savesavedata(GambatteState, ret);
				return ret;
			}

			return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			var expected = LibGambatte.gambatte_getsavedatalength(GambatteState);
			if (data.Length != expected) throw new ArgumentException(message: "Size of saveram data does not match expected!", paramName: nameof(data));

			LibGambatte.gambatte_loadsavedata(GambatteState, data);

			if (DeterministicEmulation)
			{
				var dividers = _syncSettings.InitialTime * (0x400000UL + (ulong)_syncSettings.RTCDivisorOffset) / 2UL;
				LibGambatte.gambatte_settime(GambatteState, dividers);
			}
		}
	}
}
