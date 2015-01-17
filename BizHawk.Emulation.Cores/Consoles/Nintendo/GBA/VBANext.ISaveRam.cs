using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				return LibVBANext.SaveRamSize(Core) != 0;
			}
		}

		public byte[] CloneSaveRam()
		{
			var data = new byte[LibVBANext.SaveRamSize(Core)];
			if (!LibVBANext.SaveRamSave(Core, data, data.Length))
			{
				throw new InvalidOperationException("SaveRamSave() failed!");
			}

			return data;
		}

		public void StoreSaveRam(byte[] data)
		{
			// internally, we try to salvage bad-sized saverams
			if (!LibVBANext.SaveRamLoad(Core, data, data.Length))
			{
				throw new InvalidOperationException("SaveRamLoad() failed!");
			}
		}
	}
}
