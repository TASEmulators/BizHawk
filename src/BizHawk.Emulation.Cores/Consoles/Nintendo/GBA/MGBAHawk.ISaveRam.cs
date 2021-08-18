using System;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			int len = LibmGBA.BizGetSaveRam(Core, _saveScratch, _saveScratch.Length);
			if (len == _saveScratch.Length)
			{
				throw new InvalidOperationException("Save buffer not long enough");
			}

			if (len == 0)
			{
				return null;
			}

			var ret = new byte[len];
			Array.Copy(_saveScratch, ret, len);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Take(8).SequenceEqual(Encoding.ASCII.GetBytes("GBABATT\0")))
			{
				data = LegacyFix(data);
			}

			LibmGBA.BizPutSaveRam(Core, data, data.Length);
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
	}
}
