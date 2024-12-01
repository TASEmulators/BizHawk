using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				if (Board == null) return false;
				if (Board is FDS) return true;
				if (Board.SaveRam == null) return false;
				return true;
			}
		}

		public byte[] CloneSaveRam()
		{
			if (Board is FDS fds)
			{
				return fds.ReadSaveRam();
			}

			return (byte[]) Board?.SaveRam?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (Board is FDS fds)
			{
				fds.StoreSaveRam(data);
				return;
			}

			if (Board?.SaveRam == null)
			{
				return;
			}

			Array.Copy(data, Board.SaveRam, data.Length);
		}
	}
}
