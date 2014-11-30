using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IStatable
	{
		private byte[] _stateBuffer;

		public bool BinarySaveStatesPreferred
		{
			get { return false; }
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(Serializer.CreateBinaryReader(br));
		}

		public void SaveStateText(TextWriter tw)
		{
			SyncState(Serializer.CreateTextWriter(tw));
		}

		public void LoadStateText(TextReader tr)
		{
			SyncState(Serializer.CreateTextReader(tr));
		}

		public byte[] SaveStateBinary()
		{
			if (_stateBuffer == null)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				_stateBuffer = stream.ToArray();
				writer.Close();
				return _stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(_stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Close();
				return _stateBuffer;
			}
		}

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("TI83");
			cpu.SyncState(ser);
			ser.Sync("RAM", ref ram, false);
			ser.Sync("romPageLow3Bits", ref romPageLow3Bits);
			ser.Sync("romPageHighBit", ref romPageHighBit);
			ser.Sync("disp_mode", ref disp_mode);
			ser.Sync("disp_move", ref disp_move);
			ser.Sync("disp_x", ref disp_x);
			ser.Sync("disp_y", ref disp_y);
			ser.Sync("m_CursorMoved", ref m_CursorMoved);
			ser.Sync("maskOn", ref maskOn);
			ser.Sync("onPressed", ref onPressed);
			ser.Sync("keyboardMask", ref keyboardMask);
			ser.Sync("m_LinkOutput", ref m_LinkOutput);
			ser.Sync("VRAM", ref vram, false);
			ser.Sync("Frame", ref frame);
			ser.Sync("LagCount", ref lagCount);
			ser.Sync("IsLag", ref isLag);
			ser.EndSection();
		}
	}
}
