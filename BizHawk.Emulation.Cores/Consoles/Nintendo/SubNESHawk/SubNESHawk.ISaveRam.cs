using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				if (subnes.Board == null) return false;
				if (subnes.Board is NES.FDS) return true;
				if (subnes.Board.SaveRam == null) return false;
				return true;
			}
		}

		public byte[] CloneSaveRam()
		{
			if (subnes.Board is NES.FDS)
				return (subnes.Board as NES.FDS).ReadSaveRam();

			if (subnes.Board == null || subnes.Board.SaveRam == null)
				return null;
			return (byte[])subnes.Board.SaveRam.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (subnes.Board is NES.FDS)
			{
				(subnes.Board as NES.FDS).StoreSaveRam(data);
				return;
			}

			if (subnes.Board == null || subnes.Board.SaveRam == null)
				return;
			Array.Copy(data, subnes.Board.SaveRam, data.Length);
		}
	}
}
