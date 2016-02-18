using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		public void SaveStateText(System.IO.TextWriter writer)
		{
			CheckDisposed();
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			CheckDisposed();
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new System.IO.BinaryReader(new System.IO.MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			CheckDisposed();
			LibQuickNES.ThrowStringError(QN.qn_state_save(Context, SaveStateBuff, SaveStateBuff.Length));
			writer.Write(SaveStateBuff.Length);
			writer.Write(SaveStateBuff);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			CheckDisposed();
			int len = reader.ReadInt32();
			if (len != SaveStateBuff.Length)
				throw new InvalidOperationException("Unexpected savestate buffer length!");
			reader.Read(SaveStateBuff, 0, SaveStateBuff.Length);
			LibQuickNES.ThrowStringError(QN.qn_state_load(Context, SaveStateBuff, SaveStateBuff.Length));
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			CheckDisposed();
			var ms = new System.IO.MemoryStream(SaveStateBuff2, true);
			var bw = new System.IO.BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != SaveStateBuff2.Length)
				throw new InvalidOperationException("Unexpected savestate length!");
			bw.Close();
			return SaveStateBuff2;
		}

		private byte[] SaveStateBuff;
		private byte[] SaveStateBuff2;

		private void InitSaveStateBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(QN.qn_state_size(Context, ref size));
			SaveStateBuff = new byte[size];
			SaveStateBuff2 = new byte[size + 13];
		}
	}
}
