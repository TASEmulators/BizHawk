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

		private bool HasSaveRam()
		{
			if (Board == null) return false;
			if (Board is FDS) return true;
			if (Board.SaveRam == null) return false;
			return true;
		}

		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (Board is FDS fds)
			{
				return fds.ReadSaveRam();
			}

			byte[]/*?*/ sram = (byte[])Board?.SaveRam?.Clone();
			return sram ?? throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
		}

		public void StoreSaveRam(byte[] data)
		{
			if (Board is FDS fds)
			{
				fds.StoreSaveRam(data);
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			if (Board?.SaveRam == null)
			{
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			if (data.Length != Board.SaveRam.Length) throw new InvalidOperationException("Incorrect sram size.");
			Array.Copy(data, Board.SaveRam, data.Length);
		}
	}
}
