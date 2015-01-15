using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				if (board == null) return false;
				if (board is FDS) return true;
				if (board.SaveRam == null) return false;
				return true;
			}
		}

		public byte[] CloneSaveRam()
		{
			if (board is FDS)
				return (board as FDS).ReadSaveRam();

			if (board == null || board.SaveRam == null)
				return null;
			return (byte[])board.SaveRam.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (board is FDS)
			{
				(board as FDS).StoreSaveRam(data);
				return;
			}

			if (board == null || board.SaveRam == null)
				return;
			Array.Copy(data, board.SaveRam, data.Length);
		}
	}
}
