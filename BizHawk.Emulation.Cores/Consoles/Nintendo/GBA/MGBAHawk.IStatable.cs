using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IStatable
	{
		private byte[] _savebuff = new byte[0];
		private byte[] _savebuff2 = new byte[13];

		public void SaveStateText(TextWriter writer)
		{
			var tmp = SaveStateBinary();
			BizHawk.Common.BufferExtensions.BufferExtensions.SaveAsHexFast(tmp, writer);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			BizHawk.Common.BufferExtensions.BufferExtensions.ReadFromHexFast(state, hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		private void StartSaveStateBinaryInternal()
		{
			IntPtr p = IntPtr.Zero;
			int size = 0;
			if (!LibmGBA.BizStartGetState(_core, ref p, ref size))
			{
				throw new InvalidOperationException("Core failed to save!");
			}

			if (size != _savebuff.Length)
			{
				_savebuff = new byte[size];
				_savebuff2 = new byte[size + 13];
			}

			LibmGBA.BizFinishGetState(p, _savebuff, size);
		}

		private void FinishSaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_savebuff.Length);
			writer.Write(_savebuff, 0, _savebuff.Length);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			StartSaveStateBinaryInternal();
			FinishSaveStateBinaryInternal(writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != _savebuff.Length)
			{
				_savebuff = new byte[length];
				_savebuff2 = new byte[length + 13];
			}

			reader.Read(_savebuff, 0, length);
			if (!LibmGBA.BizPutState(_core, _savebuff, length))
			{
				throw new InvalidOperationException("Core rejected the savestate!");
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			StartSaveStateBinaryInternal();
			using var ms = new MemoryStream(_savebuff2, true);
			using var bw = new BinaryWriter(ms);
			FinishSaveStateBinaryInternal(bw);
			bw.Flush();
			ms.Close();
			return _savebuff2;
		}
	}
}
