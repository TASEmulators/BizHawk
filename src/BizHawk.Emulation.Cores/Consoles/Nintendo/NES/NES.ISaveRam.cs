using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed partial class NES : ISaveRam
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

		public bool SupportsSaveRam
		{
			get
			{
				if (Board == null) return false;
				if (Board is FDS) return true;
				if (Board.SaveRam == null) return false;
				return true;
			}
		}

		public byte[] CloneSaveRam(bool clearDirty)
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

			if (data.Length != Board.SaveRam.Length) throw new InvalidOperationException("Incorrect sram size.");
			Array.Copy(data, Board.SaveRam, data.Length);
		}
	}
}
