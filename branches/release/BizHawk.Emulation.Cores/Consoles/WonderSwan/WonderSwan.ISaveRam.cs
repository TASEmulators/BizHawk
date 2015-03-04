using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan : ISaveRam
	{
		byte[] saverambuff;

		void InitISaveRam()
		{
			saverambuff = new byte[BizSwan.bizswan_saveramsize(Core)];
		}

		public byte[] CloneSaveRam()
		{
			if (!BizSwan.bizswan_saveramsave(Core, saverambuff, saverambuff.Length))
				throw new InvalidOperationException("bizswan_saveramsave() returned false!");
			return (byte[])saverambuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!BizSwan.bizswan_saveramload(Core, data, data.Length))
				throw new InvalidOperationException("bizswan_saveramload() returned false!");
		}

		public bool SaveRamModified
		{
			get { return BizSwan.bizswan_saveramsize(Core) > 0; }
		}
	}
}
