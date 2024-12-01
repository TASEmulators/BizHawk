using System.IO;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ISaveRam
	{
		private readonly byte[] _saveScratch = new byte[262144];

		public byte[] CloneSaveRam()
		{
			int len = LibmGBA.BizGetSaveRam(Core, _saveScratch, _saveScratch.Length);
			if (len == _saveScratch.Length)
			{
				throw new InvalidOperationException("Save buffer not long enough");
			}

			len = TruncateRTCIfUsingDeterministicTime(len);

			if (len == 0)
			{
				return null;
			}

			var ret = new byte[len];
			Array.Copy(_saveScratch, ret, len);
			return ret;
		}

		private static readonly byte[] _legacyHeader = Encoding.ASCII.GetBytes("GBABATT\0");

		public void StoreSaveRam(byte[] data)
		{
			if (data.AsSpan().Slice(0, 8).SequenceEqual(_legacyHeader))
			{
				data = LegacyFix(data);
			}

			LibmGBA.BizPutSaveRam(Core, data, TruncateRTCIfUsingDeterministicTime(data.Length));
		}

		public bool SaveRamModified => LibmGBA.BizGetSaveRam(Core, _saveScratch, _saveScratch.Length) > 0;

		private static byte[] LegacyFix(byte[] saveram)
		{
			// at one point vbanext-hawk had a special saveram format which we want to load.
			var br = new BinaryReader(new MemoryStream(saveram, false));
			br.ReadBytes(8); // header;
			int flashSize = br.ReadInt32();
			int eepromsize = br.ReadInt32();
			byte[] flash = br.ReadBytes(flashSize);
			byte[] eeprom = br.ReadBytes(eepromsize);

			if (flash.Length == 0)
			{
				return eeprom;
			}

			if (eeprom.Length == 0)
			{
				return flash;
			}

			// well, isn't this a sticky situation!
			return flash; // woops
		}

		// probably don't want RTC data in the save if a user is not using real time
		private int TruncateRTCIfUsingDeterministicTime(int len)
			=> (!DeterministicEmulation && _syncSettings.RTCUseRealTime) ? len : len & ~0xff;
	}
}
