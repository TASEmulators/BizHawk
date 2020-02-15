using System;
using System.IO;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			CheckDisposed();
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
		}

		public void LoadStateText(TextReader reader)
		{
			CheckDisposed();
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

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

		public byte[] SaveStateBinary()
		{
			CheckDisposed();
			var ms = new MemoryStream(_saveStateBuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != _saveStateBuff2.Length)
			{
				throw new InvalidOperationException("Unexpected savestate length!");
			}

			bw.Close();
			return _saveStateBuff2;
		}

		private byte[] _saveStateBuff;
		private byte[] _saveStateBuff2;

		private void InitSaveStateBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(QN.qn_state_size(Context, ref size));
			_saveStateBuff = new byte[size];
			_saveStateBuff2 = new byte[size + 13];
		}
	}
}
