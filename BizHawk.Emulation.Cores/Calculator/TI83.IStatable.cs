using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			_cpu.SyncState(ser);

			ser.BeginSection("TI83");
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
			ser.Sync("m_LinkOutput", ref _linkOutput);
			ser.Sync("VRAM", ref _vram, false);
			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);
			ser.Sync("ON_key_int", ref ON_key_int);
			ser.Sync("ON_key_int_EN", ref ON_key_int_EN);
			ser.Sync("TIM_1_int", ref TIM_1_int);
			ser.Sync("TIM_1_int_EN", ref TIM_1_int_EN);
			ser.Sync("TIM_frq", ref TIM_frq);
			ser.Sync("TIM_mult", ref TIM_mult);
			ser.Sync("TIM_count", ref TIM_count);
			ser.Sync("TIM_hit", ref TIM_hit);

			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
