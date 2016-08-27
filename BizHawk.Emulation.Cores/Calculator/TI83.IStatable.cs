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
			Cpu.SyncState(ser);
			ser.Sync("RAM", ref _ram, false);
			ser.Sync("romPageLow3Bits", ref _romPageLow3Bits);
			ser.Sync("romPageHighBit", ref _romPageHighBit);
			ser.Sync("disp_mode", ref _displayMode);
			ser.Sync("disp_move", ref _displayMove);
			ser.Sync("disp_x", ref _displayX);
			ser.Sync("disp_y", ref _displayY);
			ser.Sync("m_CursorMoved", ref _cursorMoved);
			ser.Sync("maskOn", ref _maskOn);
			ser.Sync("onPressed", ref _onPressed);
			ser.Sync("keyboardMask", ref _keyboardMask);
			ser.Sync("m_LinkOutput", ref LinkOutput);
			ser.Sync("VRAM", ref _vram, false);
			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);
			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
