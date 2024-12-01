using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IStatable
	{
		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			CheckDisposed();
			LibQuickNES.ThrowStringError(QN.qn_state_save(Context, _saveStateBuff, _saveStateBuff.Length));
			writer.Write(_saveStateBuff.Length);
			writer.Write(_saveStateBuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			CheckDisposed();
			int len = reader.ReadInt32();
			if (len != _saveStateBuff.Length)
			{
				throw new InvalidOperationException("Unexpected savestate buffer length!");
			}

			reader.Read(_saveStateBuff, 0, _saveStateBuff.Length);
			LibQuickNES.ThrowStringError(QN.qn_state_load(Context, _saveStateBuff, _saveStateBuff.Length));
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		private byte[] _saveStateBuff;

		private void InitSaveStateBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(QN.qn_state_size(Context, ref size));
			_saveStateBuff = new byte[size];
		}
	}
}
